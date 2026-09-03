#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Construye un paquete Debian (.deb) para MBusScaner usando Python estándar.
Genera manualmente el archivo 'ar' y los tarballs control/data."""

import os
import io
import tarfile
import hashlib
import gzip
import sys
from pathlib import Path

PUBLISH = Path(r"C:\Users\jmber\Software\MBusScaner\publish-linux")
OUT = Path(r"C:\Users\jmber\Software\MBusScaner\MBusScaner.Avalonia\linux-debian")
OUT.mkdir(parents=True, exist_ok=True)

VERSION = "1.0.2"
ARCH = "amd64"
PACKAGE = "mbusscaner"
MAINTAINER = "Jose Manuel Bernabeu Mejias <jmbernab@users.noreply.github.com>"

INSTALL_PREFIX = "/opt/mbusscaner"

# ---------------------------------------------------------------------------
# 1. CONTENIDO DEL PAQUETE (data)
# ---------------------------------------------------------------------------
def build_data(data_items):
    """data_items: list of (deb_path, on_disk_path, mode)"""
    buf = io.BytesIO()
    with tarfile.open(fileobj=buf, mode="w:gz", format=tarfile.GNU_FORMAT) as tar:
        # Directorios
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
            tar.addfile(ti)
        # Archivos
        for deb_path, on_disk, mode in data_items:
            if on_disk is None:
                # Archivo generado (script) -> contenido en bytes
                content, decomp = mode  # mode es (bytes, modo_unix)
                ti = tarfile.TarInfo(deb_path.lstrip("/"))
                ti.size = len(content)
                ti.mode = decomp
                tar.addfile(ti, io.BytesIO(content))
            else:
                ti = tarfile.TarInfo(deb_path.lstrip("/"))
                data = Path(on_disk).read_bytes()
                ti.size = len(data)
                ti.mode = mode
                tar.addfile(ti, io.BytesIO(data))
    return buf.getvalue()


# ---------------------------------------------------------------------------
# 2. CONTROL
# ---------------------------------------------------------------------------
def build_control():
    control = f"""Package: {PACKAGE}
Version: {VERSION}
Section: utils
Priority: optional
Architecture: {ARCH}
Maintainer: {MAINTAINER}
Depends: libc6 (>= 2.31), libfontconfig1, freetype2 (>= 2.0), libx11-6, libxrandr2, libxinerama1, libxcursor1, libxi6, libgl1, libice6, libsm6
Installed-Size: {int((Path(PUBLISH) / 'MBusScaner').stat().st_size / 1024)}
Description: Escáner y control de redes de climatización (HVAC)
 Software de escaneo y control de redes de climatización (HVAC) mediante conexión RJ45.
 Protocolos: Modbus TCP/IP y BACnet/IP.
 Interfaz gráfica con tema azul claro.
 Licencia: GNU General Public License v3.0.
 Desarrollado por Jose Manuel Bernabeu Mejias.
"""
    return control.encode("utf-8")


# ---------------------------------------------------------------------------
# 3. SCRIPTS DE INSTALACIÓN (control)
# ---------------------------------------------------------------------------
def build_control_tar(control_bytes):
    buf = io.BytesIO()
    with tarfile.open(fileobj=buf, mode="w:gz", format=tarfile.GNU_FORMAT) as tar:
        # control
        ti = tarfile.TarInfo("control")
        ti.size = len(control_bytes)
        ti.mode = 0o644
        tar.addfile(ti, io.BytesIO(control_bytes))
        # md5sums (de los datos) - lo calculamos después, lo pondremos en data
    return buf.getvalue()


# ---------------------------------------------------------------------------
# 4. ARMAR EL ARCHIVO AR (.deb)
# ---------------------------------------------------------------------------
def build_deb(data_tar_bytes, control_tar_gz_bytes):
    """Ensambla el archivo .deb (formato ar) con Python puro."""
    out = io.BytesIO()

    # Encabezado ar global
    out.write(b"!<arch>\n")

    def add_ar_member(name, data):
        # Nombre (16 bytes)
        name_bytes = name.encode("ascii")
        out.write(name_bytes.ljust(16, b" "))
        # Timestamp, uid, gid, mode (12+6+6+8)
        out.write((b"0".ljust(12, b" ")))
        out.write((b"0".ljust(6, b" ")))
        out.write((b"0".ljust(6, b" ")))
        out.write((b"100644".ljust(8, b" ")))
        # Tamaño (10 bytes)
        size = len(data)
        out.write(str(size).encode("ascii").ljust(10, b" "))
        out.write(b"`\n")
        out.write(data)
        # Alineación a 2 bytes
        if size % 2 != 0:
            out.write(b"\n")

    add_ar_member("debian-binary", b"2.0\n")
    add_ar_member("control.tar.gz", control_tar_gz_bytes)
    add_ar_member("data.tar.gz", data_tar_bytes)
    return out.getvalue()


# ---------------------------------------------------------------------------
# MAIN
# ---------------------------------------------------------------------------
def main():
    # --- Datos: binario + libs + script de lanzamiento + desktop + icono ---
    bin_path = PUBLISH / "MBusScaner"
    data_items = []
    data_items.append((INSTALL_PREFIX + "/MBusScaner", str(bin_path), 0o755))
    data_items.append((INSTALL_PREFIX + "/libSkiaSharp.so", str(PUBLISH / "libSkiaSharp.so"), 0o755))
    data_items.append((INSTALL_PREFIX + "/libHarfBuzzSharp.so", str(PUBLISH / "libHarfBuzzSharp.so"), 0o755))

    # Script de lanzamiento
    launcher = f"""#!/bin/sh
exec /opt/mbusscaner/MBusScaner "$@"
"""
    data_items.append(("/usr/bin/mbusscaner", None, (launcher.encode("utf-8"), 0o755)))

    # .desktop (menú)
    desktop = f"""[Desktop Entry]
Type=Application
Name=MBusScaner HVAC
GenericName=HVAC Bus Scanner
Comment=Escáner y control de redes de climatización
Exec=/usr/bin/mbusscaner
Icon=mbusscaner
Terminal=false
Categories=Utility;Network;
"""
    data_items.append(("/usr/share/applications/mbusscaner.desktop", None, (desktop.encode("utf-8"), 0o644)))

    # Icono agnóstico (PNG placeholder simple) - pondremos un SVG simple
    svg_icon = """<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128">
  <rect width="128" height="128" rx="20" fill="#2196F3"/>
  <circle cx="64" cy="52" r="22" fill="white"/>
  <rect x="30" y="78" width="26" height="8" rx="4" fill="#CFE8FF"/>
  <rect x="60" y="84" width="26" height="8" rx="4" fill="#CFE8FF"/>
  <rect x="90" y="78" width="20" height="8" rx="4" fill="white"/>
</svg>
"""
    data_items.append(("/usr/share/icons/hicolor/scalable/apps/mbusscaner.svg", None, (svg_icon.encode("utf-8"), 0o644)))

    data_tar = build_data(data_items)

    control_bytes = build_control()
    control_tar_gz = build_control_tar(control_bytes)

    deb_bytes = build_deb(data_tar, control_tar_gz)

    deb_path = OUT / f"mbusscaner_{VERSION}_{ARCH}.deb"
    deb_path.write_bytes(deb_bytes)
    print(f"OK -> {deb_path} ({len(deb_bytes)/1024/1024:.1f} MB)")

    # Verificación de integridad del tamaño
    if not deb_path.name.lower().endswith(".deb"):
        print("ADVERTENCIA: la extensión no es .deb")

if __name__ == "__main__":
    main()
