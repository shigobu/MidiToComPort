using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MIDIIOCSWrapper;
using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;

namespace MidiToComPort
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		const int MIDI_BaudRate = 31250;

		SerialPort serialPort = null;

		MIDIIN mIDIIN = null;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			//midiデバイスの列挙
			int midiDeviceNum = MIDIIN.GetDeviceNum();
			for (int i = 0; i < midiDeviceNum; i++)
			{
				midiDeviceCombo.Items.Add(MIDIIN.GetDeviceName(i));
				midiDeviceCombo.SelectedIndex = 0;
			}

			//comポートの列挙
			foreach (var deviceName in GetComDeviceNames())
			{
				comPortCombo.Items.Add(deviceName);
				comPortCombo.SelectedIndex = 0;
			}
		}

		/// <summary>
		/// COMポートの名前を列挙する
		/// </summary>
		/// <returns>COMポートの名前配列</returns>
		public static string[] GetComDeviceNames()
		{
			Regex check = new Regex("(COM[1-9][0-9]?[0-9]?)");

			ManagementClass mcPnPEntity = new ManagementClass("Win32_PnPEntity");
			ManagementObjectCollection manageObjCol = mcPnPEntity.GetInstances();

			//全てのPnPデバイスを探索しシリアル通信が行われるデバイスを随時追加する
			var deviceNameList = new List<string>();
			foreach (ManagementObject manageObj in manageObjCol)
			{
				//Nameプロパティを取得
				var namePropertyValue = manageObj.GetPropertyValue("Name");
				if (namePropertyValue == null)
				{
					continue;
				}

				//Nameプロパティ文字列の一部が"(COM1)～(COM999)"と一致するときリストに追加"
				string name = namePropertyValue.ToString();
				if (check.IsMatch(name))
				{
					deviceNameList.Add(name);
				}
			}
			return deviceNameList.ToArray();
		}

		private void ConnectButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				mIDIIN = new MIDIIN(midiDeviceCombo.SelectedItem.ToString());
				foreach (var portName in SerialPort.GetPortNames())
				{
					if (comPortCombo.SelectedItem.ToString().Contains(portName))
					{
						serialPort = new SerialPort(portName, MIDI_BaudRate, Parity.None, 8, StopBits.One);
						break;
					}
				}

				if (serialPort == null)
				{
					return;
				}

				//シリアルポートを開く
				serialPort.Open();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);

				mIDIIN?.Dispose();
				serialPort?.Dispose();
				return;
			}

			//midiのリスニングを開始
			mIDIIN.MidiMessageReceived += MIDIIN_MidiMessageReceived;
			mIDIIN.StartListening();

			connectButton.IsEnabled = false;
			disConnectButton.IsEnabled = true;

		}

		/// <summary>
		/// 
		/// </summary>
		private void DisConnectButton_Click(object sender, RoutedEventArgs e)
		{
			DisposeObj();

			connectButton.IsEnabled = true;
			disConnectButton.IsEnabled = false;
		}

		//MIDIとシリアルポートを閉じます。
		private void DisposeObj()
		{
			mIDIIN?.StopListening();
			mIDIIN?.Dispose();
			mIDIIN = null;
			serialPort?.Dispose();
			serialPort = null;
		}

		/// <summary>
		/// MIDIメッセージ受信時のイベント
		/// </summary>
		private void MIDIIN_MidiMessageReceived(object sender, MidiMessageReceivedEventArgs e)
		{
			if (serialPort == null)
			{
				return;
			}

			if (!serialPort.IsOpen)
			{
				return;
			}

			serialPort.Write(e.MIDIMessage, 0, e.MIDIMessage.Length);
			byte[] data = new byte[serialPort.BytesToRead];
			serialPort.Read(data, 0, data.Length);
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			DisposeObj();
		}
	}
}
