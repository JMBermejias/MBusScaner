# HVAC Bus Scanner (MBusScaner)

![Versión](https://img.shields.io/badge/version-1.0.0-blue)
![Licencia](https://img.shields.io/badge/licencia-GPL--3.0-green)
![Plataforma](https://img.shields.io/badge/plataforma-Windows-blue)

Software profesional para el **escaneo, monitorización y control de redes de climatización (HVAC)** a través de conexión RJ45 (Ethernet) con el ordenador.

Desarrollado por **Jose Manuel Bernabeu Mejias** y distribuido bajo licencia **GNU General Public License v3.0**.

---

## 🎯 Funcionalidades

### 🔍 Escaneo de Red
- **Auto-detección de la subred local** del ordenador conectado por RJ45
- Ping sweep para localizar hosts activos en la red
- Escaneo de puertos de climatización (Modbus 502, BACnet 47808)
- Identificación automática del protocolo de cada dispositivo

### 📟 Descubrimiento de Dispositivos
- Detección de controladores HVAC, sensores, actuadores y unidades
- Lectura de identificación de fabricante, modelo y versión de firmware (Modbus FC43)
- Descubrimiento de dispositivos BACnet mediante Who-Is / I-Am

### 📊 Monitorización
- **Temperaturas**: ambiente, impulsión, retorno, agua (ida/retorno)
- **Velocidades**: fan-coils, ventiladores, compresores
- **Estados**: válvulas, bombas, contactores
- **Humedad, presión, caudal** y otros sensores

### 🎛️ Control de Parámetros
Modificación en tiempo real desde el ordenador:
- Setpoint de temperatura
- Velocidad del ventilador
- Modo de operación (Auto/Refrigeración/Calefacción/Ventilación/Off)
- Encendido/Apagado del equipo
- Apertura de válvulas y estado de componentes

### 🖥️ Interfaz
- Interfaz gráfica **WPF en azul claro** moderna y profesional
- Botones de navegación lateral intuitivos
- Vista de detalle con pestañas: Parámetros de Control, Sensores y Puertos
- Panel de log de actividad en tiempo real
- Barra de estado con información de conexión

---

## 📖 Instalación

### Opción 1: Instalador de Windows
Descarga el instalador desde la sección [Releases](https://github.com/jmbernabeu/MBusScaner/releases) y ejecuta `MBusScaner-Setup.exe`.

### Opción 2: Compilar desde el código fuente

Requisitos: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
git clone https://github.com/jmbernabeu/MBusScaner.git
cd MBusScaner
dotnet build MBusScaner.sln -c Release
```

El ejecutable se generará en `MBusScaner/bin/Release/net8.0-windows/`.

---

## 🚀 Guía rápida de uso

1. **Conecte el cable RJ45** entre el ordenador portátil y la red de climatización.
2. Abra la aplicación y haga clic en **"Escanear Red RJ45"**.
3. La aplicación detectará automáticamente la subred y buscará dispositivos.
4. Los dispositivos encontrados aparecerán en la tabla. **Haga clic** en uno para ver su detalle.
5. En la ficha **"Parámetros de Control"** seleccione un parámetro, modifique el valor y pulse **"Guardar cambios"**.
6. Use las pestañas **"Sensores"** y **"Puertos"** para monitorizar todos los componentes conectados.

---

## 🛠️ Protocolos soportados

| Protocolo | Puerto | Descripción |
|-----------|--------|-------------|
| **Modbus TCP/IP** | 502 | Lectura/escritura de registros holding, input, coils. Identificación FC43 |
| **BACnet/IP** | 47808 (0xBAC0) | Who-Is / I-Am, ReadProperty |

### Mapa de registros Modbus por defecto

| Registro | Función                    | Tipo         | Rango  |
|----------|----------------------------|--------------|--------|
| 40001    | Setpoint de temperatura    | R/W (x10)    | 10-40°C|
| 40002    | Modo de operación          | R/W          | 0-4    |
| 40003    | Velocidad del ventilador   | R/W          | 0-4    |
| 40004    | Temperatura actual         | R (x10)      | -10-60°C|
| Coil 1   | Encendido/Apagado          | R/W          | on/off |

> El mapa de registros es **configurable** en `%APPDATA%\MBusScaner\settings.json`.

---

## 📁 Estructura del proyecto

```
MBusScaner/
├── MBusScaner.sln           # Solución
├── LICENSE                  # GPL v3
├── README.md                # Este archivo
├── docs/
│   └── MODBUS_REGISTERS.md  # Documentación de registros
├── MBusScaner/              # Aplicación principal (WPF MVVM)
│   ├── Models/              # Modelos de datos
│   ├── Services/            # Servicios (Modbus, BACnet, escáner)
│   ├── ViewModels/          # Lógica de presentación
│   ├── Views/               # Vistas XAML
│   ├── Themes/              # Tema azul claro
│   ├── Converters/          # Conversores de datos
│   └── Helpers/             # Utilidades
└── MBusScaner.Setup/        # Proyecto de instalador
```

---

## ⚖️ Licencia

Copyright © 2026 Jose Manuel Bernabeu Mejias

Este proyecto está licenciado bajo la **GNU General Public License v3.0** (GPL-3.0).
Usted es libre de usar, modificar y distribuir este software siempre que las versiones
modificadas también se distribuyan bajo GPL. Ver el archivo [LICENSE](LICENSE) para más detalles.

**AVISO LEGAL**: Este software está destinado al mantenimiento y control de instalaciones
de climatización. El usuario es responsable del uso adecuado del mismo y de verificar la
compatibilidad con sus equipos.

---

## 📧 Contacto

- Desarrollador: Jose Manuel Bernabeu Mejias
- Repositorio: [https://github.com/jmbernabeu/MBusScaner](https://github.com/jmbernabeu/MBusScaner)