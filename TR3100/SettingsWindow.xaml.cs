﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TR3100
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        Communication_settings CurrentModbusRTUSettings;

        public delegate void SavingHandler();
        public event SavingHandler SavingSuccess;


        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += SettingsWindow_Loaded;

            CurrentModbusRTUSettings = new Communication_settings();

            CurrentModbusRTUSettings.SettingsFileNotFoundError += this.ShowSettingsError; // Подписываемся на событие "не найден файл настроек"
            CurrentModbusRTUSettings.SettingsFileReadingError += this.ShowSettingsError; // Подписываемся на событие "ошибка при чтении файла настроек"
            this.SavingSuccess += this.ShowSettingsSavingSuccess; // Подписываемся на событие "успешное сохранение настроек"

            CurrentModbusRTUSettings.GetCurrentSettings();
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //ТЕКУЩИЕ НАСТРОЙКИ
            currentSerialPort_label.Content = CurrentModbusRTUSettings.PortName; // отображаем текущий порт в окне настроек
            currentPollingInterval_label.Content = CurrentModbusRTUSettings.PollingInterval; // отображаем текущий интервал опроса
            currentDeviceAddress_label.Content = "0x" + CurrentModbusRTUSettings.SlaveAddress.ToString("X"); // отображаем текущий адрес устройства
            currentBaudRate_label.Content = CurrentModbusRTUSettings.BaudRate;
            currentDataBits_label.Content = CurrentModbusRTUSettings.DataBits;
            currentStopBits_label.Content = "Один";
            currentParity_label.Content = "Нет";
            currentHandShake_label.Content = "Нет";

            //ЗАПОЛНЕНИЕ НАСТРОЕК ДЛЯ ВОЗМОЖНОСТИ ИЗМЕНЕНИЯ
            string[] serialPortNames = SerialPort.GetPortNames(); // получаем массив доступных COM-портов на ПК
            int[] pollingIntervalRange = new int[180]; // получаем максимальный интервал опроса в 180 секунд
            for (int i = 0; i < 180; i++)
            {
                pollingIntervalRange[i] = i + 1;
            }
            int[] addressIntervalRange = new int[253]; // получаем максимальный интервал адресов slave-устройства
            for (int i = 0; i < 253; i++)
            {
                addressIntervalRange[i] = i + 1;
            }
            int[] baudRateRange = new int[8] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };

            portName_ComboBox.ItemsSource = serialPortNames; // заполняем ComboBox доступными COM портами
            pollingInterval_ComboBox.ItemsSource = pollingIntervalRange; // заполняем ComboBox значениями интервала опроса
            baudRate_ComboBox.ItemsSource = baudRateRange; // заполняем ComboBox доступными значениями символьной скорости
            slaveAddress_ComboBox.ItemsSource = addressIntervalRange; // заполняем ComboBox от 1 до 253 (адреса 0xFE и 0xFF запрещены)
        }

        private void SettingsButtonSave_Click(object sender, RoutedEventArgs e)
        {
            //ПРОВЕРКА НА ПУСТЫЕ ПОЛЯ НАСТРОЕК
            if (portName_ComboBox.Text == "" || pollingInterval_ComboBox.Text == "" || slaveAddress_ComboBox.Text == "" || baudRate_ComboBox.Text == "")
            {
                MessageBox.Show("Заполните все поля настроек", "Ошибка!");
            }

            //СОХРАНЕНИЕ НАСТРОЕК
            if (portName_ComboBox.Text != "" && pollingInterval_ComboBox.Text != "" && slaveAddress_ComboBox.Text != "" && baudRate_ComboBox.Text != "")
            {
                Communication_settings newSettings = new Communication_settings(portName_ComboBox.Text, (byte)int.Parse(pollingInterval_ComboBox.Text), (byte)int.Parse(slaveAddress_ComboBox.Text), int.Parse(baudRate_ComboBox.Text));
                newSettings.SaveSettings(newSettings, newSettings.CommunicationSettingsFilePath);

                SavingSuccess?.Invoke();
                this.Close();
            }
        }

        private void SettingsButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void ShowSettingsError(string errorMessage)
        {
            MessageBox.Show(errorMessage, "Ошибка!");
        }

        public void ShowSettingsSavingSuccess()
        {
            MessageBox.Show("Настройки успешно сохранены. \n\nДля вступления с силу новых настроек необходимо заново начать измерение или перезапустить программу.", "Успех!");
        }
    }
}
