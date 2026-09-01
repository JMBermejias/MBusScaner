using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using MBusScaner.Models;

namespace MBusScaner.Services
{
    /// <summary>
    /// Cliente Modbus TCP/IP para comunicación con dispositivos de climatización.
    /// Implementa el protocolo Modbus sobre TCP/IP (puerto 502) usando Funciones Modbus estándar:
    /// - 0x03: Read Holding Registers
    /// - 0x04: Read Input Registers
    /// - 0x06: Write Single Register
    /// - 0x10: Write Multiple Registers
    /// - 0x01: Read Coils
    /// - 0x02: Read Discrete Inputs
    /// - 0x05: Write Single Coil
    /// - 0x43: Read Device Identification
    /// </summary>
    public class ModbusTcpService : IDisposable
    {
        private const int DefaultPort = 502;
        private const int DefaultTimeout = 3000;
        private const int UnitId = 1;

        public string Host { get; private set; }
        public int Port { get; private set; }
        public bool IsConnected { get; private set; }
        public int TimeoutMs { get; set; } = DefaultTimeout;

        private TcpClient _client;
        private byte _transactionId;

        public ModbusTcpService()
        {
        }

        public async Task<bool> ConnectAsync(string host, int port = DefaultPort)
        {
            Host = host;
            Port = port;
            try
            {
                _client = new TcpClient();
                var connectTask = _client.ConnectAsync(host, port);
                if (await Task.WhenAny(connectTask, Task.Delay(TimeoutMs)) != connectTask)
                {
                    throw new TimeoutException($"Tiempo de espera agotado al conectar con {host}:{port}");
                }
                IsConnected = _client.Connected;
                return IsConnected;
            }
            catch
            {
                IsConnected = false;
                return false;
            }
        }

        public void Disconnect()
        {
            IsConnected = false;
            _client?.Close();
            _client?.Dispose();
            _client = null;
        }

        /// <summary>
        /// Función Modbus 0x03 - Lectura de registros holding (16 bits)
        /// </summary>
        public async Task<ushort[]> ReadHoldingRegistersAsync(ushort startAddress, ushort count)
        {
            return await ReadRegistersAsync(startAddress, count, 0x03);
        }

        /// <summary>
        /// Función Modbus 0x04 - Lectura de registros de entrada (16 bits)
        /// </summary>
        public async Task<ushort[]> ReadInputRegistersAsync(ushort startAddress, ushort count)
        {
            return await ReadRegistersAsync(startAddress, count, 0x04);
        }

        private async Task<ushort[]> ReadRegistersAsync(ushort startAddress, ushort count, byte functionCode)
        {
            if (!IsConnected) throw new InvalidOperationException("No conectado al dispositivo.");

            var request = BuildRequest(functionCode, UnitId, 5);
            WriteBytes(request, 8, (ushort)startAddress);
            WriteBytes(request, 10, (ushort)count);

            var response = await SendReceiveAsync(request);
            if (response.Length < 9)
                throw new InvalidOperationException("Respuesta Modbus demasiado corta.");

            CheckException(response);

            byte byteCount = response[8];
            int registerCount = byteCount / 2;
            var registers = new ushort[registerCount];
            for (int i = 0; i < registerCount; i++)
            {
                registers[i] = (ushort)((response[9 + i * 2] << 8) | response[10 + i * 2]);
            }
            return registers;
        }

        /// <summary>
        /// Función Modbus 0x06 - Escritura de un solo registro (16 bits)
        /// </summary>
        public async Task WriteSingleRegisterAsync(ushort address, ushort value)
        {
            if (!IsConnected) throw new InvalidOperationException("No conectado al dispositivo.");

            var request = BuildRequest(0x06, UnitId, 5);
            WriteBytes(request, 8, (ushort)address);
            WriteBytes(request, 10, (ushort)value);

            var response = await SendReceiveAsync(request);
            CheckException(response);
        }

        /// <summary>
        /// Función Modbus 0x05 - Escritura de una sola bobina (coil)
        /// </summary>
        public async Task WriteSingleCoilAsync(ushort address, bool value)
        {
            if (!IsConnected) throw new InvalidOperationException("No conectado al dispositivo.");

            var request = BuildRequest(0x05, UnitId, 5);
            WriteBytes(request, 8, (ushort)address);
            WriteBytes(request, 10, (ushort)(value ? 0xFF00 : 0x0000));

            var response = await SendReceiveAsync(request);
            CheckException(response);
        }

        /// <summary>
        /// Función Modbus 0x01 - Lectura de bobinas (coils)
        /// </summary>
        public async Task<bool[]> ReadCoilsAsync(ushort startAddress, ushort count)
        {
            if (!IsConnected) throw new InvalidOperationException("No conectado al dispositivo.");

            var request = BuildRequest(0x01, UnitId, 5);
            WriteBytes(request, 8, (ushort)startAddress);
            WriteBytes(request, 10, (ushort)count);

            var response = await SendReceiveAsync(request);
            CheckException(response);

            byte byteCount = response[8];
            var values = new bool[count];
            for (int i = 0; i < count; i++)
            {
                int byteIndex = 9 + i / 8;
                int bitIndex = i % 8;
                if (byteIndex < response.Length)
                {
                    values[i] = ((response[byteIndex] >> bitIndex) & 0x01) == 0x01;
                }
            }
            return values;
        }

        /// <summary>
        /// Función Modbus 0x43 - Lectura de identificación del dispositivo
        /// </summary>
        public async Task<Dictionary<string, string>> ReadDeviceIdentificationAsync()
        {
            var result = new Dictionary<string, string>();
            if (!IsConnected) return result;

            try
            {
                var request = BuildRequest(0x2B, UnitId, 3); // 0x2B = 43 dec
                WriteBytes(request, 8, (ushort)0x0E); // MEI Type: 14 = Read Device ID
                WriteBytes(request, 10, (ushort)0x01); // Read Device ID code: 1 = Basic
                WriteBytes(request, 11, (ushort)0x00); // Object ID

                var response = await SendReceiveAsync(request);
                if (response.Length < 12) return result;

                int pos = 11;
                int objectCount = response[10];
                for (int i = 0; i < objectCount && pos + 2 < response.Length; i++)
                {
                    if (pos >= response.Length) break;
                    byte objectId = response[pos];
                    byte length = response[pos + 1];
                    if (pos + 2 + length > response.Length) break;
                    string value = System.Text.Encoding.ASCII.GetString(response, pos + 2, length);
                    string key = objectId switch
                    {
                        0 => "VendorName",
                        1 => "ProductCode",
                        2 => "MajorMinorRevision",
                        3 => "VendorUrl",
                        4 => "ProductName",
                        5 => "ModelName",
                        6 => "UserApplicationName",
                        _ => $"Object_{objectId}"
                    };
                    result[key] = value;
                    pos += 2 + length;
                }
            }
            catch
            {
                // Si no soporta identificación, simplemente no populamos
            }
            return result;
        }

        /// <summary>
        /// Intenta detectar si el host remoto responde como dispositivo Modbus TCP
        /// </summary>
        public async Task<bool> DetectAsync(string host, int port = DefaultPort)
        {
            try
            {
                if (await ConnectAsync(host, port))
                {
                    try
                    {
                        // Intentamos leer el registro de identificación
                        var regs = await ReadHoldingRegistersAsync(0x0000, 2);
                        return regs != null;
                    }
                    catch
                    {
                        // Si falla la lectura de registros pero hay conexión TCP en el puerto 502,
                        // es muy probable que sea Modbus
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                Disconnect();
            }
        }

        private byte[] BuildRequest(byte functionCode, byte unitId, int dataLength)
        {
            var mbapLength = 6 + 1 + dataLength; // 6 bytes MBAP header + unit ID + function + data
            var request = new byte[mbapLength];

            // MBAP Header
            _transactionId = (byte)((_transactionId + 1) & 0xFF);
            WriteBytes(request, 0, _transactionId); // Transaction ID
            WriteBytes(request, 2, (ushort)0x0000); // Protocol ID
            WriteBytes(request, 4, (ushort)(1 + dataLength)); // Length
            request[6] = unitId; // Unit ID
            request[7] = functionCode; // Function code

            return request;
        }

        private void WriteBytes(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value >> 8);
            buffer[offset + 1] = (byte)(value & 0xFF);
        }

        private async Task<byte[]> SendReceiveAsync(byte[] request)
        {
            if (_client == null || !_client.Connected)
                throw new InvalidOperationException("No hay conexión TCP activa.");

            var stream = _client.GetStream();
            stream.ReadTimeout = TimeoutMs;
            stream.WriteTimeout = TimeoutMs;

            await stream.WriteAsync(request, 0, request.Length);
            await stream.FlushAsync();

            // Leer respuesta
            var buffer = new byte[256];
            int totalRead = 0;
            int bytesRead = await ReadWithTimeoutAsync(stream, buffer, 0, 7); // header + unit + function
            totalRead += bytesRead;

            if (totalRead < 7)
                throw new InvalidOperationException("Respuesta Modbus incompleta.");

            // Determinar longitud total de la respuesta
            int totalLength = 7; // MBAP(6) + unit(1) + function(1) = 7 wait
            // re-estructurar: MBAP(6) + unitId(1) + function(1) + data
            if (buffer[1] >> 7 == 1) // Error response
            {
                totalLength = 9; // fixed for error
                var rest = new byte[2];
                await ReadWithTimeoutAsync(stream, rest, 0, 2);
                var fullResp = new byte[9];
                Array.Copy(buffer, fullResp, 7);
                Array.Copy(rest, 0, fullResp, 7, 2);
                return fullResp;
            }
            else if (buffer[7] == 0x43 || buffer[7] == 0x2B) // Device ID
            {
                totalLength = 256;
            }
            else if (buffer[7] == 0x01 || buffer[7] == 0x02 || buffer[7] == 0x03 || buffer[7] == 0x04)
            {
                // Byte count is at byte index 8
                var temp = new byte[256];
                Array.Copy(buffer, temp, 7);
                int extra = await ReadWithTimeoutAsync(stream, temp, 7, 2);
                totalRead += extra;
                byte byteCount = temp[8];
                totalLength = 9 + byteCount;
            }

            // Leer el resto de datos
            if (totalLength > totalRead)
            {
                var moreData = new byte[totalLength - totalRead];
                int read = await ReadWithTimeoutAsync(stream, moreData, 0, moreData.Length);
                totalRead += read;
                var fullBuffer = new byte[totalRead];
                Array.Copy(buffer, fullBuffer, Math.Min(7, totalRead));
                Array.Copy(moreData, 0, fullBuffer, 7, read);
                return fullBuffer;
            }

            return buffer;
        }

        private async Task<int> ReadWithTimeoutAsync(System.Net.Sockets.NetworkStream stream, byte[] buffer, int offset, int count)
        {
            var readTask = stream.ReadAsync(buffer, offset, count);
            var timeoutTask = Task.Delay(TimeoutMs);
            var completed = await Task.WhenAny(readTask, timeoutTask);
            if (completed == timeoutTask)
            {
                throw new TimeoutException("Tiempo de espera agotado esperando respuesta Modbus.");
            }
            return await readTask;
        }

        private void CheckException(byte[] response)
        {
            if (response[1] >> 7 == 1)
            {
                byte exceptionCode = response[8];
                throw new InvalidOperationException($"Error Modbus: código de excepción {exceptionCode} ({GetExceptionMessage(exceptionCode)})");
            }
        }

        private string GetExceptionMessage(byte code)
        {
            return code switch
            {
                1 => "Función ilegal",
                2 => "Dirección de datos ilegal",
                3 => "Valor de datos ilegal",
                4 => "Fallo del dispositivo esclavo",
                5 => "Confirmación",
                6 => "Dispositivo esclavo ocupado",
                8 => "Error de paridad de memoria",
                10 => "Función de pasarela indisponible",
                11 => "Destino de pasarela sin respuesta",
                _ => "Error desconocido"
            };
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
