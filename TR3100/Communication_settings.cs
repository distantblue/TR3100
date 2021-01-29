using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Ports;

namespace TR3100
{
    [Serializable]
    public class Communication_settings
    {
        // ИМЯ ФАЙЛА НАСТРОЕК
        [NonSerialized]
        public readonly string CommunicationSettingsFileName = @"Communication_settings.dat";

        // ПУТЬ к ФАЙЛУ НАСТРОЕК        
        [NonSerialized]
        public readonly string CommunicationSettingsFilePath;

        // ИНТЕРВАЛ ОПРОСА
        public int PollingInterval { get; set; }

        // АДРЕС УСТРОЙСТВА     
        public byte SlaveAddress { get; set; }

        // НАСТРОЙКИ COM-порта
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        [NonSerialized]
        public readonly Parity Parity = Parity.None;
        [NonSerialized]
        public readonly StopBits StopBits = StopBits.One;
        [NonSerialized]
        public readonly int DataBits = 8;
        [NonSerialized]
        public readonly Handshake Handshake = Handshake.None;

        // ИНТЕРВАЛ ТИШИНЫ после отправки сообщения ModbusRTU 
        [NonSerialized]
        public readonly int SilentInterval; // Гарантированный интервал тишины после отправки данных устройству после которого устройство начинает обработку запроса   

        // ВРЕМЯ ОЖИДАНИЯ ЧТЕНИЯ из COM порта [мс]
        [NonSerialized]
        public readonly int ReadTimeout = -1; // [-1] - бесконечное время ожидания

        // ВРЕМЯ ОЖИДАНИЯ ЗАПИСИ в порт [мс]
        [NonSerialized]
        public readonly int WriteTimeout = 100;

        // ВРЕМЯ ОЖИДАНИЯ ОТВЕТА от устройства [мс]
        [NonSerialized]
        public readonly int ResponseTimeout = 120;

        // Объявляю делегат
        public delegate void CommunicationSettingsErrorHandler(string message);

        // Обявляю событие "не найден файл настроек"
        public event CommunicationSettingsErrorHandler SettingsFileNotFoundError;

        // Обявляю событие "ошибка при чтении файла настроек"
        public event CommunicationSettingsErrorHandler SettingsFileReadingError;

        public Communication_settings()
        {
            // Инициализируем переменные значениями по умолчанию, чтоб не ссылались в null
            this.SilentInterval = GetSilentInterval();
            this.PortName = "COM1";
            this.PollingInterval = 1;
            this.SlaveAddress = 0x0A;
            this.BaudRate = 9600;

            // Формирование пути к файлу настроек
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Settings");
            stringBuilder.Append(@"\");
            stringBuilder.Append($"{CommunicationSettingsFileName}");
            CommunicationSettingsFilePath = stringBuilder.ToString();
        }

        public Communication_settings(string portName, int pollingInterval, byte slaveAddress, byte baudRate)
        {
            this.PortName = portName;
            this.PollingInterval = pollingInterval;
            this.SlaveAddress = slaveAddress;
            this.BaudRate = baudRate;
            this.SilentInterval = GetSilentInterval();

            // Формирование пути к файлу настроек
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Settings");
            stringBuilder.Append(@"\");
            stringBuilder.Append($"{CommunicationSettingsFileName}");
            CommunicationSettingsFilePath = stringBuilder.ToString();
        }

        public void GetCurrentSettings()
        {
            Communication_settings currentCommunicationSettings = GetCurrentSettings(this.CommunicationSettingsFilePath);
            this.PortName = currentCommunicationSettings.PortName;
            this.PollingInterval = currentCommunicationSettings.PollingInterval;
            this.SlaveAddress = currentCommunicationSettings.SlaveAddress;
            this.BaudRate = currentCommunicationSettings.BaudRate;
        }

        public Communication_settings GetCurrentSettings(string settingsFilePath)
        {
            Communication_settings currentSettings = null;
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            try
            {
                FileStream fileStream = new FileStream(settingsFilePath, FileMode.Open);
                currentSettings = (Communication_settings)binaryFormatter.Deserialize(fileStream); // получаем текущие настройки подключения
                fileStream.Dispose();
            }
            catch (FileNotFoundException exception)
            {
                SettingsFileNotFoundError?.Invoke($"В директории \"Settings\" отсутствует файл настроек {CommunicationSettingsFileName} \n\n Подробнее о возникшей исключительной ситуации: \n\n {exception.Message}");
            }
            catch (System.Runtime.Serialization.SerializationException exception)
            {
                SettingsFileReadingError?.Invoke($"Возникла ошибка при десериализации объекта настроек программы из файла настроек {CommunicationSettingsFileName} \n\n Подробнее о возникшей исключительной ситуации: \n\n {exception.Message}");
            }
            catch (Exception exception)
            {
                SettingsFileReadingError?.Invoke($"Возникла ошибка при считывании настроек программы из файла настроек {CommunicationSettingsFileName} \n\n Подробнее о возникшей исключительной ситуации: \n\n {exception.Message}");
            }

            return currentSettings;
        }

        public void SaveSettings(Communication_settings settings, string settingsFilePath)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            try
            {
                FileStream fileStream = new FileStream(settingsFilePath, FileMode.OpenOrCreate);
                binaryFormatter.Serialize(fileStream, settings); // сериализация объекта
                fileStream.Dispose();
            }
            catch (System.Runtime.Serialization.SerializationException exception)
            {
                SettingsFileReadingError?.Invoke($"Возникла ошибка при сериализации объекта настроек программы в файл настроек {CommunicationSettingsFileName} \n\n Подробнее о возникшей исключительной ситуации: \n\n {exception.Message}");
            }
            catch (Exception exception)
            {
                SettingsFileReadingError?.Invoke($"Возникла ошибка при сериализации объекта настроек программы в файл настроек {CommunicationSettingsFileName} \n\n Подробнее о возникшей исключительной ситуации: \n\n {exception.Message}");
            }
        }

        private int GetSilentInterval()
        {
            int delay = 1; // задержка в [мс]
            if (this.BaudRate == 19200)
            {
                return delay;
            }
            if (this.BaudRate == 9600 | BaudRate > 19200)
            {
                return delay = 2;
            }
            return delay;
        }
    }
}
