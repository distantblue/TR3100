using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TR3100
{
    /// <summary>
    /// This class describes logic of MMK encrypting protocol. It works over serial line (RS-232 standard) 
    /// </summary>
    class MMK
    {
        // Переменная которая хранит сообщние MMK протокола в виде List
        //private List<byte> MMK_Message;
        // Переменная которая хранит сообщние MMK протокола в виде byte[]
        private byte[] MMK_message;
        // Экземпляр класса SerialPort
        private SerialPort SerialPort;
        // Гарантированный интервал тишины после отправки команды устройству после которого устройство начинает обработку команды
        private readonly int SilentInterval;
        // Интервал после которого начинается считывание поступивших данных на COM порт или вызывается исключение TimeoutException
        private readonly int ResponseTimeout;

        // Объявляем делегат
        public delegate void MMKEventHandler(byte[] buffer);
        // Объявляем событие "получен ответ от устройства"
        public event MMKEventHandler ResponseReceived;
        // Объявляем событие "отправлена команда"
        public event MMKEventHandler RequestSent;

        // Объявляем делегат
        public delegate void MMKErrorHandler(string message);
        // Объявляем событие "не корректная контрольная сумма сообщения ответа Slave-устройства"
        public event MMKErrorHandler CRC_Error;
        // Объявляем событие "устройство не ответило на запрос"
        public event MMKErrorHandler DeviceNotRespondingError;
        // Объявляем событие "не удалось открыть порт"
        public event MMKErrorHandler SerialPortOpeningError;
        // Объявляем событие "не удалось записать данные в порт"
        public event MMKErrorHandler SerialPortWritingError;
        // Объявляем событие "устройство сообщило об ошибке"
        public event MMKErrorHandler SlaveError;
        // Перемення хранит ожидаемое количество байт данных в ответе устройства
        // Объявляем событие "не получен ответ от SLAVE-устройства"
        //public event MMKEventHandler ResponseError;
        private int ExpectedQuantityOfDataBytesInResponse;
        // Перемення хранит ожидаемое количество байт в ответе устройства
        private int ExpectedQuantityOfBytesInResponse;


        /// <summary>
        /// Конструктор класса MMK
        /// </summary>
        /// <param name="communicationSettings">Экземпляр класса настроек сообщения с устройством Communication_settings</param>
        public MMK(Communication_settings communicationSettings)
        {
            // КОНФИГУРИРОВАНИЕ COM-ПОРТА
            SerialPort = new SerialPort(communicationSettings.PortName, communicationSettings.BaudRate, communicationSettings.Parity, communicationSettings.DataBits, communicationSettings.StopBits);
            SerialPort.Handshake = communicationSettings.Handshake; // Аппаратное рукопожатие
            SerialPort.ReadTimeout = communicationSettings.ReadTimeout; // Время ожидания ответа устройства на COM-порт
            SerialPort.WriteTimeout = communicationSettings.WriteTimeout; // Время ожидания записи данных в COM-порт
            SilentInterval = communicationSettings.SilentInterval; // Гарантированный интервал тишины после отправки данных устройству после которого устройство начинает обработку запроса
            ResponseTimeout = communicationSettings.ResponseTimeout; // Интервал после которого начинается считывание поступивших данных на COM порт или вызывается исключение TimeoutException
        }

        private byte[] Build_MMK_message(byte SlaveAddress, byte MMK_function_code, ushort StartingAddressOfRegister, byte QuantityOfRegisters)
        {
            byte[] temp_array = new byte[5];

            temp_array[0] = SlaveAddress;
            temp_array[1] = MMK_function_code;
            temp_array[2] = (byte)(StartingAddressOfRegister >> 8); // сдвиг регистров на 8 позиций вправо, чтобы получить старший байт [HI Byte] 16 битного числа
            temp_array[3] = (byte)(StartingAddressOfRegister & 0xFF); // накладываем битовую маску 00000000 11111111 (0xFF) чтобы получить младший байт [LO Byte] 16 битного числа
            temp_array[4] = QuantityOfRegisters;

            ushort CRC_value = CRC.CRC16_BUYPASS(temp_array); // генерация контрольной суммы

            byte CRC_HI_byte = (byte)(CRC_value >> 8);// разделение 2 байт на старший и младший байты
            byte CRC_LO_byte = (byte)(CRC_value & 0xFF);

            byte[] inform_part_of_message = new byte[7];

            for (int i = 0; i < 5; i++)
            {
                inform_part_of_message[i] = temp_array[i];
            }
            inform_part_of_message[5] = CRC_HI_byte; // добавление байтов контрольной суммы к сообщению MMK
            inform_part_of_message[6] = CRC_LO_byte;
            temp_array = null;

            List<byte> MMK_Message = new List<byte>();
            MMK_Message.Add(0x10); // Начало информационного кадра 0x10 0x02 
            MMK_Message.Add(0x02);

            // Выяыление в информационной последовательности значения 0x10 и экранирование его еще одним значением 0x10
            foreach (var item in inform_part_of_message)
            {
                if (item == 0x10)
                {
                    MMK_Message.Add(0x10);
                    MMK_Message.Add(item);
                }
                else
                {
                    MMK_Message.Add(item);
                }
            }

            MMK_Message.Add(0x10); // Конец информационного кадра 0x10 0x04 
            MMK_Message.Add(0x04);

            MMK_message = MMK_Message.ToArray(); // получаем массив байт (сообщение MMK)
            MMK_Message = null;

            return MMK_message;
        }

        private byte[] Build_MMK_message(byte MMK_function_code, ushort StartingAddressOfRegister, ushort ValueOfRegisterToWrite, byte SlaveAddress)
        {
            MMK_Message = new List<byte>();
            MMK_Message.Add(SlaveAddress);
            MMK_Message.Add(MMK_function_code);
            MMK_Message.Add((byte)(StartingAddressOfRegister >> 8)); // сдвиг регистров на 8 позиций вправо, чтобы получить старший байт [HI Byte] 16 битного числа
            MMK_Message.Add((byte)(StartingAddressOfRegister & 0xFF)); // накладываем битовую маску 00000000 11111111 (0xFF) чтобы получить младший байт [LO Byte] 16 битного числа
            MMK_Message.Add((byte)(ValueOfRegisterToWrite >> 8)); // сдвиг регистров на 8 позиций вправо, чтобы получить старший байт [HI Byte] 16 битного числа
            MMK_Message.Add((byte)(ValueOfRegisterToWrite & 0xFF)); // накладываем битовую маску 00001111 (0xF) чтобы получить младший байт [LO Byte] 16 битного числа
            byte[] data = MMK_Message.ToArray(); // формируем массив данных по которым будет выполнен подсчет контрольной суммы
            ushort CRC = GenerateCRC(data); // генерация контрольной суммы
            byte CRC_LO_byte = (byte)(CRC & 0xFF); // разделение 2 байт на старший и младший байты
            byte CRC_HI_byte = (byte)(CRC >> 8);
            MMK_Message.Add(CRC_LO_byte); // добавление байтов контрольной суммы к сообщению MODBUS
            MMK_Message.Add(CRC_HI_byte);
            MMK_message = MMK_Message.ToArray(); // получаем массив байт (сообщение Modbus)

            return MMK_message;
        }

        /// <summary>
        /// CRC-16/MODBUS algorithm
        /// </summary>
        /// <param name="data">Data bytes</param>
        /// <returns>16-bit CRC16_MODBUS value</returns>
        private ushort GenerateCRC(byte[] data)
        {
            ushort poly = 0xA001;
            ushort crc = 0xFFFF;
            foreach (var item in data) // For each byte in buffer
            {
                crc ^= item; // Carry out XOR with CRC 

                for (int i = 0; i < 8; i++) // For each bit in byte
                {
                    if ((crc & 0x0001) != 0) // If LSB of CRC == 1
                    {
                        crc >>= 1;
                        crc ^= poly;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }

        public void SendCommandToReadRegisters(byte SlaveAddress, byte ModbusFunctionCode, ushort StartingAddressOfRegisterToRead, ushort QuantityOfRegistersToRead, int QuantityOfBytesInReg)
        {
            this.ExpectedQuantityOfDataBytesInResponse = QuantityOfRegistersToRead * QuantityOfBytesInReg; // Ожидаемое количество байтов данных в сообщении ответа устройства
            this.ExpectedQuantityOfBytesInResponse = 5 + ExpectedQuantityOfDataBytesInResponse; // Ожидаемое количество байтов в сообщении ответа устройства
            byte[] messageToSend = Build_MMK_message(SlaveAddress, ModbusFunctionCode, StartingAddressOfRegisterToRead, QuantityOfRegistersToRead); // Формируем массив байт для отправки
            SendModbusMessage(messageToSend); // Отправляем данные
            ReadResponse(); // Читаем данные 
        }


        public void SendCommandToWriteRegisters(byte SlaveAddress, byte ModbusFunctionCode, ushort StartingAddressOfRegisterToWrite, ushort ValueOfRegisterToWrite)
        {
            byte[] messageToSend = Build_MMK_message(ModbusFunctionCode, StartingAddressOfRegisterToWrite, ValueOfRegisterToWrite, SlaveAddress);
            SendModbusMessage(messageToSend); // Отправляем данные
        }

        private void SendModbusMessage(byte[] modbusMessage)
        {
            // ЕСЛИ ПОРТ ЗАКРЫТ
            if (!SerialPort.IsOpen)
            {
                try
                {
                    SerialPort.Open();
                }
                catch (InvalidOperationException ex)
                {
                    SerialPortOpeningError?.Invoke($"Возникла ошибка при попытке открыть порт {SerialPort.PortName}. Подробнее о возникшей исключительной ситуации: \n\n {ex.Message}");
                }
            }

            // Очищаем буффер исходящих данных (порт может быть уже открытым и там могут быть данные с прошлой отправки)
            SerialPort.DiscardOutBuffer();
            Thread.Sleep(50);

            try
            {
                // Отправляем данные
                SerialPort.Write(modbusMessage, 0, modbusMessage.Length);
            }
            catch (TimeoutException ex) // По истечении WriteTimeout [мс]
            {
                SerialPortWritingError?.Invoke($"Произошла ошибка при записи данных порт. \n\nПодробнее о возникшей исключительной ситуации: \n\n {ex.Message}");
            }
            RequestSent?.Invoke(modbusMessage); // Вызов события "отправлена команда"

            Thread.Sleep(ResponseTimeout); // Задержка выжидания поступления данных на COM порт
        }

        private void ReadResponse()
        {
            // ЕСЛИ ПОРТ ЗАКРЫТ 
            if (!SerialPort.IsOpen)
            {
                return; // Выходим из метода, невозможно читать из порта когда он закрыт
            }

            // ЕСЛИ ВХОДЯЩИЙ БУФФЕР ПУСТОЙ
            if (SerialPort.BytesToRead == 0)
            {
                DeviceNotRespondingError?.Invoke("Ошибка! Устройство не ответило на запрос.");
            }

            // ЕСЛИ ВО ВХОДЯЩЕМ БУФФЕРЕ ПОРТА БОЛЕЕ 5 байтов
            if (SerialPort.BytesToRead >= 5)
            {
                int bufferSize = SerialPort.BytesToRead; // Получаем количество пришедших байтов данных в буффере приема
                byte[] buffer = new byte[bufferSize]; // Создаем массив байтов
                SerialPort.DiscardNull = false; // Не игнорировать пустые байты - 0000 0000

                // Считываем побайтно и заполняем массив байтов:
                for (int i = 0; i < bufferSize; i++)
                {
                    buffer[i] = (byte)SerialPort.ReadByte();
                }

                // ПРОВЕРКА КОНТРОЛЬНОЙ СУММЫ 
                // ЕСЛИ КОНТРОЛЬНАЯ СУММА СОШЛАСЬ
                if (CheckCRC_Correct(buffer))
                {
                    // Если адрес устройства правильный
                    if (buffer[0] == MMK_message[0])
                    {
                        // ЕСЛИ ПРИШЛО ОЖИДАЕМОЕ КОЛИЧЕСТВО БАЙТОВ В ОТВЕТЕ
                        if (bufferSize == ExpectedQuantityOfBytesInResponse)
                        {
                            // Если совпадает функция команды и количество запрошенных байтов данных 
                            if (buffer[1] == MMK_message[1] && buffer[2] == ExpectedQuantityOfDataBytesInResponse)
                            {
                                // Событие "пришли данные"
                                ResponseReceived?.Invoke(buffer);
                            }
                            else
                            {
                                SlaveError?.Invoke("Ошибка! В сообщении ответа устройства не совпадает функция команды или количество запрошенных байтов данных.");
                            }
                        }
                        else
                        {
                            if (buffer[1] == 81 || buffer[1] == 82)
                            {
                                SlaveError?.Invoke("Ошибка! Запрашиваемая функция или адресное поле не поддерживается устройством.");
                            }
                            else
                            {
                                SlaveError?.Invoke("Неустановленная ошибка.");
                            }
                        }
                    }
                    else
                    {
                        SlaveError?.Invoke("Ошибка! В сообщении ответа неверный адрес устройства.");
                    }
                }
                // ЕСЛИ КОНТРОЛЬНАЯ СУММА НЕ СОШЛАСЬ
                else
                {
                    CRC_Error?.Invoke("Ошибка! В сообщении ответа устройства неверная контрольная сумма.");
                }
            }
            // ЕСЛИ ВО ВХОДЯЩЕМ БУФФЕРЕ ПОРТА МЕНЕЕ 5 байтов
            else
            {
                SlaveError?.Invoke("Ошибка! В сообщении ответа устройства менее 5 байтов.");
            }
        }

        private bool CheckCRC_Correct(byte[] modbusMessage)
        {
            bool res = false;
            byte[] data = new byte[modbusMessage.Length - 2];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = modbusMessage[i];
            }

            ushort CRC = GenerateCRC(data); // генерация контрольной суммы

            //Cтарший и младший байт контрольной суммы
            byte CRC_LO_byte = (byte)(CRC & 0xFF);
            byte CRC_HI_byte = (byte)(CRC >> 8);

            //Полученные байты контрольной суммы
            byte received_CRC_LO_byte = modbusMessage[modbusMessage.Length - 2];
            byte received_CRC_HI_byte = modbusMessage[modbusMessage.Length - 1];

            //Сравнение
            if (CRC_LO_byte == received_CRC_LO_byte && CRC_HI_byte == received_CRC_HI_byte)
            {
                res = true;
            }
            return res;
        }

        public void Close()
        {
            SerialPort.Close();
        }

        private void SendMessageAgain()
        {
            // Очищаем буффер входящих данных 
            SerialPort.DiscardInBuffer();

            // Пауза для очистки буффера входящих данных
            Thread.Sleep(50);

            // Повторно отправляем сообщение
            SendModbusMessage(this.MMK_message);
        }
    }
}
