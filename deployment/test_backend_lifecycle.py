"""Process-level deployment tests; no live NASA access or global network changes."""
import ctypes
from ctypes import wintypes
import json
import os
import site
from pathlib import Path
import subprocess
import sys
import time
import urllib.request
import uuid

import pytest

ROOT = Path(__file__).resolve().parents[1]
BACKEND = Path(os.environ.get("LUNAR_TEST_BACKEND_DIR", ROOT / "backend")).resolve()
# Windows venv python.exe is a redirector, not the final interpreter PID.
PYTHON = os.environ.get("LUNAR_TEST_PYTHON", getattr(sys, "_base_executable", sys.executable))


def child_environment():
    env = os.environ.copy()
    if "LUNAR_TEST_PYTHON" not in env:
        env["PYTHONPATH"] = os.pathsep.join(site.getsitepackages())
    return env


@pytest.fixture
def tmp_path():
    # Use ordinary application directory permissions, not owner-only tempfile ACLs.
    path = ROOT / "Builds" / ("lifecycle-" + uuid.uuid4().hex)
    path.mkdir(parents=True)
    return path


@pytest.mark.skipif(sys.platform != "win32", reason="Windows event lifecycle")
def test_backend_ephemeral_health_catalog_and_graceful_shutdown(tmp_path):
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.CreateEventW.argtypes = [ctypes.c_void_p, wintypes.BOOL, wintypes.BOOL, wintypes.LPCWSTR]
    kernel.CreateEventW.restype = wintypes.HANDLE
    kernel.SetEvent.argtypes = [wintypes.HANDLE]
    kernel.SetEvent.restype = wintypes.BOOL
    kernel.CloseHandle.argtypes = [wintypes.HANDLE]
    session = uuid.uuid4().hex
    name = "Local\\LunarNavigator_Test_" + session
    handle = kernel.CreateEventW(None, True, False, name)
    assert handle
    descriptor = tmp_path / "startup.json"
    proc = None
    try:
        proc = subprocess.Popen([
            PYTHON, str(BACKEND / "run_backend.py"), "--port", "0",
            "--session-id", session, "--storage-dir", str(tmp_path / "data"),
            "--startup-file", str(descriptor), "--log-file", str(tmp_path / "backend.log"),
            "--shutdown-event", name,
        ], cwd=tmp_path, env=child_environment(), creationflags=subprocess.CREATE_NO_WINDOW)
        opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
        deadline = time.monotonic() + 20
        while time.monotonic() < deadline:
            assert proc.poll() is None, (tmp_path / "backend.log").read_text()
            if descriptor.exists():
                data = json.loads(descriptor.read_text())
                try:
                    with opener.open(f"http://127.0.0.1:{data['port']}/health", timeout=.5) as response:
                        health = json.load(response)
                    break
                except OSError:
                    pass
            time.sleep(.1)
        else:
            pytest.fail("backend did not become healthy")
        assert health["pid"] == proc.pid == data["pid"]
        assert health["session_id"] == session == data["session_id"]
        assert health["api_profile"] == "lunar-analyze-v1"
        assert data["host"] == "127.0.0.1" and 0 < data["port"] < 65536
        with opener.open(f"http://127.0.0.1:{data['port']}/sites", timeout=2) as response:
            assert len(json.load(response)["sites"]) == 3
        assert kernel.SetEvent(handle)
        assert proc.wait(timeout=5) == 0
        assert "Shutdown event signaled" in (tmp_path / "backend.log").read_text()
    finally:
        if proc is not None and proc.poll() is None:
            proc.kill()
            proc.wait(timeout=5)
        kernel.CloseHandle(handle)


def test_backend_rejects_non_loopback(tmp_path):
    result = subprocess.run([
        PYTHON, str(BACKEND / "run_backend.py"), "--host", "0.0.0.0",
        "--log-file", str(tmp_path / "backend.log")], cwd=tmp_path, env=child_environment(), timeout=10)
    assert result.returncode == 2
