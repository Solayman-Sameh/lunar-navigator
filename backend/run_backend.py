"""Production headless entry point for LunarNavigator Scientific Service.

Single-process, single-worker, no-reload, loopback-only service with
atomic startup descriptor and Windows Job Object / cooperative shutdown support.
"""
import argparse
import ctypes
import json
import logging
import os
from pathlib import Path
import socket
import sys
import threading
import time


def setup_file_logging(log_file_path: str | None):
    root_logger = logging.getLogger()
    root_logger.setLevel(logging.INFO)
    formatter = logging.Formatter("[%(asctime)s] [%(levelname)s] [%(name)s] %(message)s")

    if log_file_path:
        log_path = Path(log_file_path).resolve()
        log_path.parent.mkdir(parents=True, exist_ok=True)
        file_handler = logging.FileHandler(str(log_path), encoding="utf-8")
        file_handler.setFormatter(formatter)
        root_logger.addHandler(file_handler)
    else:
        # Fallback to stderr if available
        try:
            stream_handler = logging.StreamHandler(sys.stderr)
            stream_handler.setFormatter(formatter)
            root_logger.addHandler(stream_handler)
        except Exception:
            pass


def monitor_shutdown_event(event_name: str, server):
    """Monitors a Windows named event for cooperative shutdown."""
    if not event_name or sys.platform != "win32":
        return

    SYNCHRONIZE = 0x00100000
    from ctypes import wintypes
    kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel32.OpenEventW.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.LPCWSTR]
    kernel32.OpenEventW.restype = wintypes.HANDLE
    kernel32.WaitForSingleObject.argtypes = [wintypes.HANDLE, wintypes.DWORD]
    kernel32.WaitForSingleObject.restype = wintypes.DWORD
    kernel32.CloseHandle.argtypes = [wintypes.HANDLE]
    kernel32.CloseHandle.restype = wintypes.BOOL

    # Open existing named event created by the launcher
    h_event = kernel32.OpenEventW(SYNCHRONIZE, False, event_name)
    if not h_event:
        logging.warning("Could not open shutdown event '%s'; falling back to process lifecycle.", event_name)
        return

    logging.info("Monitoring cooperative shutdown event: %s", event_name)

    def _wait():
        WAIT_OBJECT_0 = 0
        while not server.should_exit:
            result = kernel32.WaitForSingleObject(h_event, 500)
            if result == 0xFFFFFFFF:
                logging.error("Shutdown event wait failed: %s", ctypes.get_last_error())
                break
            if result == WAIT_OBJECT_0:
                logging.info("Shutdown event signaled by launcher. Requesting graceful Uvicorn stop.")
                server.should_exit = True
                break
        kernel32.CloseHandle(h_event)

    t = threading.Thread(target=_wait, name="ShutdownEventWatcher", daemon=True)
    t.start()


def write_startup_descriptor(descriptor_path: str, data: dict):
    target = Path(descriptor_path).resolve()
    target.parent.mkdir(parents=True, exist_ok=True)
    tmp_file = target.with_suffix(f".tmp.{os.getpid()}.{time.time_ns()}")
    tmp_file.write_text(json.dumps(data, indent=2), encoding="utf-8")
    os.replace(tmp_file, target)
    logging.info("Wrote startup descriptor to: %s", target)


def main():
    parser = argparse.ArgumentParser(description="LunarNavigator Scientific Service")
    parser.add_argument("--host", default="127.0.0.1", help="Binding host (default 127.0.0.1)")
    parser.add_argument("--port", type=int, default=0, help="Port to bind (default 0 for dynamic)")
    parser.add_argument("--storage-dir", default=None, help="Explicit writable directory for runs and cache")
    parser.add_argument("--session-id", default=None, help="Managed session identifier")
    parser.add_argument("--startup-file", default=None, help="Atomic JSON startup descriptor path")
    parser.add_argument("--log-file", default=None, help="Direct log file path")
    parser.add_argument("--shutdown-event", default=None, help="Named Windows event for cooperative shutdown")

    args = parser.parse_args()

    # 1. Initialize file logging before importing heavier modules
    setup_file_logging(args.log_file)
    logging.info("LunarNavigator backend starting. PID: %d, Python: %s", os.getpid(), sys.version)

    # 1b. Ensure SSL certificate verification path
    try:
        import certifi
        cert_path = certifi.where()
        if os.path.isfile(cert_path):
            os.environ.setdefault("SSL_CERT_FILE", cert_path)
            logging.info("Configured SSL_CERT_FILE: %s", cert_path)
    except Exception as cert_err:
        logging.warning("Could not set SSL_CERT_FILE from certifi: %s", cert_err)

    # 2. Strict loopback enforcement
    if args.host not in ("127.0.0.1", "localhost", "::1"):
        logging.error("Security violation: Refusing to bind to non-loopback host '%s'", args.host)
        sys.exit(2)

    # 3. Import and create application with explicit storage and session
    try:
        from science.api import create_app
        app = create_app(storage_root=args.storage_dir, session_id=args.session_id)
    except Exception as exc:
        logging.exception("Failed to initialize scientific application: %s", exc)
        sys.exit(1)

    # 4. Retained socket binding for port allocation
    import uvicorn
    bound_sock = None
    actual_port = args.port

    try:
        bound_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        # SO_EXCLUSIVEADDRUSE or standard reuse options
        bound_sock.bind((args.host, args.port))
        bound_sock.listen(128)
        actual_port = bound_sock.getsockname()[1]
        logging.info("Socket bound to %s:%d (retained for Uvicorn)", args.host, actual_port)
    except Exception as exc:
        logging.exception("Failed to bind socket on %s:%d: %s", args.host, args.port, exc)
        if bound_sock:
            bound_sock.close()
        sys.exit(3)

    # 5. Write atomic startup descriptor
    descriptor = {
        "status": "ready_to_listen",
        "service_identity": "LunarNavigator-ScienceAPI",
        "api_profile": "lunar-analyze-v1",
        "host": args.host,
        "port": actual_port,
        "pid": os.getpid(),
        "session_id": args.session_id,
        "timestamp_utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
    }

    if args.startup_file:
        try:
            write_startup_descriptor(args.startup_file, descriptor)
        except Exception as exc:
            logging.exception("Failed to write startup descriptor to '%s': %s", args.startup_file, exc)
            bound_sock.close()
            sys.exit(4)

    # 6. Configure Uvicorn server
    config = uvicorn.Config(
        app=app,
        host=args.host,
        port=actual_port,
        workers=1,
        reload=False,
        access_log=False,
        log_config=None,
    )
    server = uvicorn.Server(config)

    # 7. Start cooperative shutdown watcher if requested
    if args.shutdown_event:
        monitor_shutdown_event(args.shutdown_event, server)

    # 8. Run Uvicorn server on the retained socket
    logging.info("Uvicorn running on retained socket (port %d)", actual_port)
    try:
        server.run(sockets=[bound_sock])
    except Exception as exc:
        logging.exception("Uvicorn server encountered fatal error: %s", exc)
        sys.exit(5)
    finally:
        try:
            bound_sock.close()
        except Exception:
            pass
        logging.info("LunarNavigator backend exited cleanly.")


if __name__ == "__main__":
    main()
