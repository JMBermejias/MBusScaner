# Mapa de Registros Modbus TCP/IP

Este documento describe los registros Modbus estándar utilizados por HVAC Bus Scanner
para leer y controlar dispositivos de climatización.

## Formato

HVAC Bus Scanner usa la siguiente convención de registro Modbus (común en controladores HVAC):

- Registros HOLDING (FC 03/06/16) para parámetros de configuración y control
- Registros INPUT (FC 04) para lecturas de sensores
- Coils (FC 01/05) para salidas discretas (on/off)

## Tabla de registros por defecto

| Dirección (0-based) | Dirección (PLC) | Nombre                    | Funciones        | Tipo de dato   | Escala | Unidad | Rango     |
|---------------------|-----------------|---------------------------|------------------|----------------|--------|--------|-----------|
| 0                   | 40001           | Setpoint de temperatura   | Read/Write       | UInt16 x10     | 10     | °C     | 10 - 40   |
| 1                   | 40002           | Modo de operación         | Read/Write       | UInt16         | 1      | -      | 0-4       |
| 2                   | 40003           | Velocidad del ventilador  | Read/Write       | UInt16         | 1      | -      | 0-4       |
| 3                   | 40004           | Firmware version          | Read             | UInt16         | 1      | -      | -         |
| 4                   | 40005           | Temperatura ambiente      | Read             | UInt16 x10     | 10     | °C     | -10 - 60  |
| 5                   | 40006           | Humedad relativa          | Read             | UInt16         | 1      | %      | 0 - 100   |
| 6                   | 40007           | Temperatura de impulsión  | Read             | UInt16 x10     | 10     | °C     | -10 - 60  |
| 7                   | 40008           | Temperatura de retorno    | Read             | UInt16 x10     | 10     | °C     | -10 - 60  |

## Códigos de modo de operación

| Valor | Modo           |
|-------|----------------|
| 0     | Auto           |
| 1     | Refrigeración  |
| 2     | Calefacción    |
| 3     | Ventilación    |
| 4     | Off            |

## Códigos de velocidad de ventilador

| Valor | Velocidad |
|-------|-----------|
| 0     | Off       |
| 1     | Baja      |
| 2     | Media     |
| 3     | Alta      |
| 4     | Auto      |

## Coils

| Dirección | Nombre             | Funciones | Descripción |
|-----------|--------------------|-----------|-------------|
| 0         | Encendido/Apagado  | Read/Write| Estado de alimentación del equipo |

## Personalización

El mapa de registros se puede personalizar en el archivo de configuración:

```
%APPDATA%\MBusScaner\settings.json
```

```json
{
  "ModbusRegisterMap": [
    {
      "Address": 0,
      "Name": "Setpoint Temperatura",
      "Type": "Temperature",
      "Scale": 10,
      "Unit": "°C",
      "Direction": "ReadWrite"
    }
  ]
}
```

> **Nota**: La escala se usa para convertir el valor entero del registro al valor real
> (registro = valor_real × escala). Por ejemplo, un registro con valor 225 y escala 10
> representa 22.5 °C.