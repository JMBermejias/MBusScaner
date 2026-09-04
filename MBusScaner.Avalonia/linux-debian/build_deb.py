#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Construye un paquete Debian (.deb) para MBusScaner usando Python estándar.
Genera manualmente el archivo 'ar' y los tarballs control/data."""

import io
import tarfile
import hashlib
import sys
import time
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parent.parent
PUBLISH = REPO_ROOT / "publish-linux"
OUT = SCRIPT_DIR
OUT.mkdir(parents=True, exist_ok=True)

if not PUBLISH.exists():
    print(f"ERROR: No se encuentra el directorio {PUBLISH}")
    print("Ejecuta primero: dotnet publish MBusScaner.Avalonia/MBusScaner.Avalonia.csproj -c Release -r linux-x64 --self-contained -o publish-linux")
    sys.exit(1)

VERSION = "1.0.8"
ARCH = "amd64"
PACKAGE = "mbusscaner"
MAINTAINER = "Jose Manuel Bernabeu Mejias <jmbernab@users.noreply.github.com>"

INSTALL_PREFIX = "/opt/mbusscaner"

# ---------------------------------------------------------------------------
# 1. CONTENIDO DEL PAQUETE (data.tar.gz)
# ---------------------------------------------------------------------------
def build_data(data_items):
    buf = io.BytesIO()
    with tarfile.open(fileobj=buf, mode="w:gz", format=tarfile.GNU_FORMAT) as tar:
        dirs = set()
        for deb_path, _, _ in data_items:
            if "/" in deb_path.lstrip("/"):
                parts = deb_path.lstrip("/").split("/")[:-1]
                for i in range(1, len(parts) + 1):
                    dirs.add("/" + "/".join(parts[:i]) + "/")
        for d in sorted(dirs):
            ti = tarfile.TarInfo(d)
            ti.type = tarfile.DIRTYPE
            ti.mode = 0o755
            ti.mtime = int(time.time())
            tar.addfile(ti)
        for deb_path, on_disk, mode in data_items:
            if on_disk is None:
                content, decomp = mode
                ti = tarfile.TarInfo(deb_path.lstrip("/"))
                ti.size = len(content)
                ti.mode = decomp
                ti.mtime = int(time.time())
                tar.addfile(ti, io.BytesIO(content))
            else:
                ti = tarfile.TarInfo(deb_path.lstrip("/"))
                data = Path(on_disk).read_bytes()
                ti.size = len(data)
                ti.mode = mode
                ti.mtime = int(time.time())
                tar.addfile(ti, io.BytesIO(data))
    return buf.getvalue()


# ---------------------------------------------------------------------------
# 2. CONTROL (control.tar.gz)
# ---------------------------------------------------------------------------
def build_control(installed_size_kb):
    control = (
        f"Package: {PACKAGE}\n"
        f"Version: {VERSION}\n"
        f"Section: utils\n"
        f"Priority: optional\n"
        f"Architecture: {ARCH}\n"
        f"Maintainer: {MAINTAINER}\n"
        f"Depends: libc6 (>= 2.31), libfontconfig1, libfreetype6 (>= 2.0), libx11-6, libxrandr2, libxinerama1, libxcursor1, libxi6, libgl1, libice6, libsm6\n"
        f"Installed-Size: {installed_size_kb}\n"
        f"Description: Escaner y control de redes de climatizacion (HVAC)\n"
        f" Software de escaneo y control de redes de climatizacion (HVAC) mediante conexion RJ45.\n"
        f" Protocolos: Modbus TCP/IP y BACnet/IP.\n"
        f" Interfaz grafica con tema azul claro.\n"
        f" Licencia: GNU General Public License v3.0.\n"
        f" Desarrollado por Jose Manuel Bernabeu Mejias.\n"
    )
    return control.encode("utf-8")


def build_postinst():
    script = (
        "#!/bin/sh\n"
        "set -e\n"
        "ldconfig\n"
        "if command -v update-desktop-database >/dev/null 2>&1; then\n"
        "    update-desktop-database /usr/share/applications || true\n"
        "fi\n"
        "if command -v gtk-update-icon-cache >/dev/null 2>&1; then\n"
        "    gtk-update-icon-cache -f -t /usr/share/icons/hicolor || true\n"
        "fi\n"
    )
    return script.encode("utf-8")


def build_md5sums(data_items):
    lines = []
    for deb_path, on_disk, _ in data_items:
        if on_disk is None:
            continue
        data = Path(on_disk).read_bytes()
        md5 = hashlib.md5(data).hexdigest()
        lines.append(f"{md5}  {deb_path.lstrip('/')}\n")
    return "".join(lines).encode("utf-8")


def build_control_tar(control_bytes, postinst_bytes, md5sums_bytes):
    buf = io.BytesIO()
    with tarfile.open(fileobj=buf, mode="w:gz", format=tarfile.GNU_FORMAT) as tar:
        ti = tarfile.TarInfo("control")
        ti.size = len(control_bytes)
        ti.mode = 0o644
        ti.mtime = int(time.time())
        tar.addfile(ti, io.BytesIO(control_bytes))

        ti = tarfile.TarInfo("md5sums")
        ti.size = len(md5sums_bytes)
        ti.mode = 0o644
        ti.mtime = int(time.time())
        tar.addfile(ti, io.BytesIO(md5sums_bytes))

        ti = tarfile.TarInfo("postinst")
        ti.size = len(postinst_bytes)
        ti.mode = 0o755
        ti.mtime = int(time.time())
        tar.addfile(ti, io.BytesIO(postinst_bytes))
    return buf.getvalue()


# ---------------------------------------------------------------------------
# 3. ARCHIVO AR (.deb)
# ---------------------------------------------------------------------------
def build_deb(data_tar_bytes, control_tar_gz_bytes):
    out = io.BytesIO()
    out.write(b"!<arch>\n")

    def add_member(name, data):
        ts = int(time.time())
        out.write(name.encode("ascii").ljust(16, b" "))
        out.write(str(ts).encode("ascii").ljust(12, b" "))
        out.write(b"0     ")
        out.write(b"0     ")
        out.write(b"100644  ")
        out.write(str(len(data)).encode("ascii").ljust(10, b" "))
        out.write(b"`\n")
        out.write(data)
        if len(data) % 2 != 0:
            out.write(b"\n")

    add_member("debian-binary", b"2.0\n")
    add_member("control.tar.gz", control_tar_gz_bytes)
    add_member("data.tar.gz", data_tar_bytes)
    return out.getvalue()


# ---------------------------------------------------------------------------
# MAIN
# ---------------------------------------------------------------------------
def main():
    data_items = []

    for f in sorted(PUBLISH.iterdir()):
        if f.is_file():
            deb_path = INSTALL_PREFIX + "/" + f.name
            if f.suffix in (".so", ".dylib") or f.name in ("MBusScaner",):
                mode = 0o755
            else:
                mode = 0o644
            data_items.append((deb_path, str(f), mode))

    print(f"Archivos del publish: {len([i for i in data_items if i[0].startswith(INSTALL_PREFIX)])}")

    launcher = (
        "#!/bin/sh\n"
        "INSTALL_DIR=/opt/mbusscaner\n"
        "export LD_LIBRARY_PATH=\"$INSTALL_DIR:${LD_LIBRARY_PATH:-}\"\n"
        "export DOTNET_ROOT=\"$INSTALL_DIR\"\n"
        "cd \"$INSTALL_DIR\"\n"
        "exec \"$INSTALL_DIR/MBusScaner\" \"$@\"\n"
    )
    data_items.append(("/usr/bin/mbusscaner", None, (launcher.encode("utf-8"), 0o755)))

    desktop = (
        "[Desktop Entry]\n"
        "Type=Application\n"
        "Name=MBusScaner HVAC\n"
        "GenericName=HVAC Bus Scanner\n"
        "Comment=Escáner y control de redes de climatización\n"
        "Exec=/usr/bin/mbusscaner\n"
        "Icon=mbusscaner\n"
        "Terminal=false\n"
        "Categories=Utility;Network;\n"
    )
    data_items.append(("/usr/share/applications/mbusscaner.desktop", None, (desktop.encode("utf-8"), 0o644)))

    svg_icon = (
        '<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128">\n'
        '  <rect width="128" height="128" rx="20" fill="#2196F3"/>\n'
        '  <circle cx="64" cy="52" r="22" fill="white"/>\n'
        '  <rect x="30" y="78" width="26" height="8" rx="4" fill="#CFE8FF"/>\n'
        '  <rect x="60" y="84" width="26" height="8" rx="4" fill="#CFE8FF"/>\n'
        '  <rect x="90" y="78" width="20" height="8" rx="4" fill="white"/>\n'
        '</svg>\n'
    )
    data_items.append(("/usr/share/icons/hicolor/scalable/apps/mbusscaner.svg", None, (svg_icon.encode("utf-8"), 0o644)))

    ldconfig_conf = "/opt/mbusscaner\n"
    data_items.append(("/etc/ld.so.conf.d/mbusscaner.conf", None, (ldconfig_conf.encode("utf-8"), 0o644)))

    data_tar = build_data(data_items)

    total_size = sum(f.stat().st_size for f in PUBLISH.iterdir() if f.is_file())
    installed_size_kb = int(total_size / 1024) + 1
    control_bytes = build_control(installed_size_kb)
    postinst_bytes = build_postinst()
    md5sums_bytes = build_md5sums(data_items)
    control_tar_gz = build_control_tar(control_bytes, postinst_bytes, md5sums_bytes)

    deb_bytes = build_deb(data_tar, control_tar_gz)

    deb_path = OUT / f"mbusscaner_{VERSION}_{ARCH}.deb"
    deb_path.write_bytes(deb_bytes)
    print(f"OK -> {deb_path} ({len(deb_bytes)/1024/1024:.1f} MB)")


if __name__ == "__main__":
    main()
