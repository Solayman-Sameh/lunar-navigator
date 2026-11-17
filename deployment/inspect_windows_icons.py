"""Read Windows executable icon resources without executing the binary."""
import argparse
import ctypes
from ctypes import wintypes
import io
import json
from pathlib import Path
import struct

from PIL import Image


def inspect(executable, output):
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.LoadLibraryExW.argtypes = [wintypes.LPCWSTR, wintypes.HANDLE, wintypes.DWORD]
    kernel.LoadLibraryExW.restype = wintypes.HMODULE
    kernel.FindResourceW.argtypes = [wintypes.HMODULE, ctypes.c_void_p, ctypes.c_void_p]
    kernel.FindResourceW.restype = wintypes.HRSRC
    kernel.SizeofResource.argtypes = [wintypes.HMODULE, wintypes.HRSRC]
    kernel.SizeofResource.restype = wintypes.DWORD
    kernel.LoadResource.argtypes = [wintypes.HMODULE, wintypes.HRSRC]
    kernel.LoadResource.restype = wintypes.HGLOBAL
    kernel.LockResource.argtypes = [wintypes.HGLOBAL]
    kernel.LockResource.restype = ctypes.c_void_p
    kernel.FreeLibrary.argtypes = [wintypes.HMODULE]
    module = kernel.LoadLibraryExW(str(executable.resolve()), None, 2)
    if not module:
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        callback_type = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HMODULE,
                                          ctypes.c_void_p, ctypes.c_void_p, ctypes.c_ssize_t)
        names = []

        @callback_type
        def collect(module, resource_type, name, param):
            names.append(name if name <= 65535 else ctypes.wstring_at(name))
            return True

        kernel.EnumResourceNamesW.argtypes = [wintypes.HMODULE, ctypes.c_void_p,
                                              callback_type, ctypes.c_ssize_t]
        kernel.EnumResourceNamesW(module, 14, collect, 0)
        if not names:
            raise ValueError(f"No icon group in {executable}")

        def resource(kind, name):
            key = name if isinstance(name, int) else ctypes.cast(ctypes.c_wchar_p(name), ctypes.c_void_p)
            found = kernel.FindResourceW(module, key, kind)
            if not found:
                raise ctypes.WinError(ctypes.get_last_error())
            length = kernel.SizeofResource(module, found)
            return ctypes.string_at(kernel.LockResource(kernel.LoadResource(module, found)), length)

        group = resource(14, names[0])
        count = struct.unpack_from("<H", group, 4)[0]
        header = bytearray(struct.pack("<HHH", 0, 1, count))
        payload = bytearray()
        sizes = []
        for index in range(count):
            width, height, colors, reserved, planes, bits, length, ident = struct.unpack_from(
                "<BBBBHHIH", group, 6 + index * 14)
            data = resource(3, ident)
            header.extend(struct.pack("<BBBBHHII", width, height, colors, reserved,
                                      planes, bits, len(data), 6 + count * 16 + len(payload)))
            payload.extend(data)
            sizes.append([width or 256, height or 256])
        image = Image.open(io.BytesIO(header + payload))
        output.parent.mkdir(parents=True, exist_ok=True)
        image.save(output)
        return {"executable": str(executable), "groups": len(names), "sizes": sizes,
                "preview": str(output)}
    finally:
        kernel.FreeLibrary(module)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("executable", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    print(json.dumps(inspect(args.executable, args.output), indent=2))
