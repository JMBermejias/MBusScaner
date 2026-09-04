#!/bin/bash
# Script de compilación para paquete .deb de MBusScaner
# Ejecutar desde la raíz del repositorio

set -e

VERSION="1.0.2"
PKG_NAME="mbusscaner"
ARCH="amd64"
BUILD_DIR="build/deb"
PKG_DIR="${BUILD_DIR}/${PKG_NAME}_${VERSION}_${ARCH}"

echo "=== Construyendo paquete .deb ${PKG_NAME} ${VERSION} ==="

# Verificar que existen los archivos publicados
if [ ! -f "publish/MBusScaner.exe" ]; then
    echo "ERROR: No se encuentra publish/MBusScaner.exe"
    echo "Ejecuta primero: dotnet publish -c Release -r win-x64 --self-contained"
    exit 1
fi

# Limpiar directorio de build
rm -rf "${BUILD_DIR}"
mkdir -p "${PKG_DIR}/DEBIAN"
mkdir -p "${PKG_DIR}/usr/share/mbusscaner"
mkdir -p "${PKG_DIR}/usr/share/doc/${PKG_NAME}"
mkdir -p "${PKG_DIR}/usr/share/applications"
mkdir -p "${PKG_DIR}/usr/share/icons/hicolor/256x256/apps"

# Copiar archivos binarios
cp publish/MBusScaner.exe "${PKG_DIR}/usr/share/mbusscaner/"
cp publish/*.dll "${PKG_DIR}/usr/share/mbusscaner/"

# Copiar documentación
cp LICENSE "${PKG_DIR}/usr/share/doc/${PKG_NAME}/"
cp README.md "${PKG_DIR}/usr/share/doc/${PKG_NAME}/"

# Copiar .desktop
cp debian/mbusscaner.desktop "${PKG_DIR}/usr/share/applications/"

# Copiar icono si existe
if [ -f "MBusScaner/MBusScaner.ico" ]; then
    cp MBusScaner/MBusScaner.ico "${PKG_DIR}/usr/share/icons/hicolor/256x256/apps/mbusscaner.ico"
fi

# Generar control (reemplazar variables)
INSTALLED_SIZE=$(du -sk "${PKG_DIR}" | cut -f1)
sed -e "s/\${VERSION}/${VERSION}/" \
    -e "s/\${INSTALLED_SIZE}/${INSTALLED_SIZE}/" \
    debian/control > "${PKG_DIR}/DEBIAN/control"

# Copiar changelog
cp debian/changelog "${PKG_DIR}/usr/share/doc/${PKG_NAME}/changelog.Debian"
gzip -9n "${PKG_DIR}/usr/share/doc/${PKG_NAME}/changelog.Debian"

# Generar md5sums
cd "${PKG_DIR}"
find . -type f ! -path './DEBIAN/*' -exec md5sum {} \; > DEBIAN/md5sums
cd -

# Empaquetar
echo "Empaquetando..."
dpkg-deb --root-owner-group --build "${PKG_DIR}"

# Mover a Descargas
OUTPUT="${PKG_DIR}.deb"
if [ -d "${HOME}/Descargas" ]; then
    cp "${OUTPUT}" "${HOME}/Descargas/"
    echo "Paquete copiado a: ${HOME}/Descargas/$(basename ${OUTPUT})"
fi

echo "=== Paquete creado: ${OUTPUT} ==="
