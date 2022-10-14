using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DDS_Controler
{
	public class UARTConnector : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		//protected byte[] correctResponse = { 0b01000011, 0b00100011, 0b00010011, 0b00001011 };
		/// <summary>
		/// Contains list of existing Serial ports
		/// </summary>
		public ObservableCollection<string> listOfPorts { get; set; } = new ObservableCollection<string>();
		private bool conectionStatus = false;
		/// <summary>
		/// Status of conection with DDSGenerator
		/// </summary>
		public bool ConectionStatus {
			get { return conectionStatus; }
			set {
				conectionStatus = value;
				NotifyPropertyChanged("ConectionStatus");
			}
		}

		/// <summary>
		/// Construct UARTConnector
		///	Class is used to controll and observe contetion with DDSGenerator Circuit (Designed PCB)
		///	Class is also used to convert object DDSConfig to data understandable by ATMEGA microcontroler
		/// </summary>
		public UARTConnector()
		{
			refreshPortsList();
		}
		/// <summary>
		/// Refreshes the list of existing serial ports
		/// </summary>
		public void refreshPortsList()
		{
			string[] list = SerialPort.GetPortNames();
			listOfPorts.Clear();
			foreach (string portname in list)
			{
				listOfPorts.Add(portname);
			}
			
		}
		/// <summary>
		/// Send Data to DDSGenerator that changes ONLY DigiPot values (Ampliitude)
		/// </summary>
		/// <param name="PortName">Used Seriial Port</param>
		/// <param name="ConfigLeft">Left chanel DDSConfig</param>
		/// <param name="ConfigRight">Right chanel DDSConfig</param>
		public void sendDDSValues(string PortName, DDSConfig ConfigLeft, DDSConfig ConfigRight) {
			using (SerialPort serial = new SerialPort()) {

				serial.BaudRate = 9600;
				serial.ReadBufferSize = 4;
				serial.ReadTimeout = 100;
				serial.PortName = PortName;
                
				Byte[] messeage = { 0b00000011, (Byte)ConfigLeft.Amplitude, (Byte)ConfigRight.Amplitude, 0b00000011 };
                Byte[] correctResponse = { 0b00000111, 0b00000111, 0b00000111, 0b00000111 };
                Byte[] response = new byte[4];

                try {
					serial.Open();
                    serial.DiscardInBuffer();
					serial.Write(messeage, 0, 4);
                    for (int i = 0; i < 4; i++){
                        serial.Read(response, i, 1);
                    }
                    ConectionStatus = compareMesseages(response,correctResponse);
                }
				catch (Exception e) {
					System.Windows.MessageBox.Show("ERROR");
                    ConectionStatus = false;
				}
				finally {
					serial.Close();
				}
			}
		}

		/// <summary>
		/// Send Full DDS Config in 18 bytes, Changes: Signal Frequency, Phase Shift, Amplitude 
		/// </summary>
		/// <param name="PortName">Used Seriial Port</param>
		/// <param name="ConfigLeft">Left chanel DDSConfig</param>
		/// <param name="ConfigRight">Right chanel DDSConfig</param>
		/// <returns></returns>
		public bool sendFullDDSConfig(string PortName, DDSConfig[] ddsConfigs) {
			using (SerialPort serial = new SerialPort()) {
				if (ddsConfigs.Length != 4) { return false;}// useless i guess
				serial.BaudRate = 9600;
				serial.ReadBufferSize = 4;
				serial.ReadTimeout = 400;
				if (PortName == "") { return false; }
				serial.PortName = PortName;

				byte[] freqBytes = ddsConfigs[0].GetFrequencyCommand();
				byte[] phaseBytes = new byte[8];
				for(int i = 0;i<4; i++) {
					byte[] tempBytes = ddsConfigs[i].GetPhaseCommand();
					phaseBytes[i * 2] = tempBytes[0];
					phaseBytes[i * 2 + 1] = tempBytes[1];
				}
				foreach(byte b in phaseBytes) {
					System.Diagnostics.Debug.Print("Phasebyte " + b.ToString());
				}

				Byte[] messeage = {
									0b00001011,		//0
									freqBytes[0],	//1
									freqBytes[1],	//2
									freqBytes[2],	//3
									freqBytes[3],	//4
									phaseBytes[0],	//5
									phaseBytes[1],	//6
									phaseBytes[2],	//7
									phaseBytes[3],	//8
									phaseBytes[4],	//9
									phaseBytes[5],	//10
									phaseBytes[6],	//11
									phaseBytes[7],	//12
									(Byte)ddsConfigs[0].Amplitude,	//13
									(Byte)ddsConfigs[1].Amplitude,	//14
									(Byte)ddsConfigs[2].Amplitude,	//15
									(Byte)ddsConfigs[3].Amplitude,	//16
									0b00001011		//17
				};
				Byte[] correctResponse = { 0b00000111, 0b00000111, 0b00000111, 0b00000111 };
				Byte[] response = new byte[4];
				foreach (byte bit in messeage) {							//debug (Show all bites)
					System.Diagnostics.Debug.Write(bit.ToString() + ", ");  //debug
				}															//debug
				try {
					serial.Open();
					serial.DiscardInBuffer();
					serial.Write(messeage, 0, 18);
					for (int i = 0; i < 4; i++) {
						serial.Read(response, i, 1);
					}
					ConectionStatus = compareMesseages(response, correctResponse);
				}
				catch (Exception e) {
					System.Windows.MessageBox.Show("ERROR");
					ConectionStatus = false;
				}
				finally {
					serial.Close();
				}
				return ConectionStatus;
			}
		}

		/// <summary>
		///	Check if DDS generator can be detected on given serial port
		/// </summary>
		/// <param name="PortName">Used Serial Port</param>
		/// <returns></returns>
		public bool checkConection (string PortName){
			using (SerialPort serial = new SerialPort())
			{
				serial.BaudRate = 9600;
				serial.ReadBufferSize = 4;
				serial.ReadTimeout = 50;
				serial.PortName = PortName;
				
				Byte[] messeage		=		{ 0b00000111, 0b00000111, 0b00000111, 0b00000111 };
				Byte[] correctResponse  =	{ 0b00000111, 0b00000111, 0b00000111, 0b00000111 };
				Byte[] response = new byte[4];
				try
				{
					serial.Open();
					serial.Write(messeage, 0, 4);
					for (int i = 0; i<4; i++){
						serial.Read(response, i, 1);
					}
                    ConectionStatus = compareMesseages(response, correctResponse);
                    serial.Close();
					return ConectionStatus;	
				}
				catch (Exception e)
				{
					serial.Close();
					ConectionStatus = false;
					return false;
				}
				finally
				{
					serial.Close();
				}
			}
		}

		/// <summary>
		///	Sends messeage to controlled Circuit to aquire is't configuration
		/// </summary>
		/// <returns></returns>
		public bool getGeneratosConfig() {

			return false;
		}

        private bool compareMesseages(Byte[] msn0, Byte[] msn1) { //Debug
            if (msn0.Length == msn1.Length) {
                for (int i = 0; i < msn1.Length; i++) {
                   // System.Diagnostics.Debug.Print(String.Format("{0}  ;{1}", msn0[i], msn1[i]));
                    if(msn1[i] != msn0[i]){
                        return false;
                    }
                }
                return true;
            }
            else{
                return false;
            }
		}

		public void sendDEBUG(string PortName) {
			using (SerialPort serial = new SerialPort()) {
				serial.BaudRate = 9600;
				serial.ReadBufferSize = 4;
				serial.ReadTimeout = 50;
				serial.PortName = PortName;

				Byte[] messeage = { (byte)'X', 0b00000111, 0b00000111, 0b00000111 };
				Byte[] correctResponse = { 0b00000111, 0b00000111, 0b00000111, 0b00000111 };
				Byte[] response = new byte[4];
				try {
					serial.Open();
					serial.Write(messeage, 0, 4);
					System.Diagnostics.Debug.WriteLine(response);
					serial.Close();
				}
				catch (Exception e) {
					serial.Close();
				}
			}
		}

		public bool downloadConfiguration(DDSConfig[] ddsConfigs, string PortName) {
			using (SerialPort serial = new SerialPort()) {
				if (ddsConfigs.Length != 4) { return false; }// useless i guess
				serial.BaudRate = 9600;
				serial.ReadBufferSize = 4;
				serial.ReadTimeout = 400;
				if (PortName == "") { return false; }
				serial.PortName = PortName;

				byte[] freqBytes = ddsConfigs[0].GetFrequencyCommand();
				byte[] phaseBytes = new byte[8];
				for (int i = 0; i < 4; i++) {
					byte[] tempBytes = ddsConfigs[i].GetPhaseCommand();
					phaseBytes[i * 2] = tempBytes[0];
					phaseBytes[i * 2 + 1] = tempBytes[1];
				}
				foreach (byte b in phaseBytes) {
					System.Diagnostics.Debug.Print("Phasebyte " + b.ToString());
				}

				Byte[] messeage = {
					45,
					45,
					45,
					45
				};

				Byte[] OLD = {
									0b00001011,		//0
									freqBytes[0],	//1
									freqBytes[1],	//2
									freqBytes[2],	//3
									freqBytes[3],	//4
									phaseBytes[0],	//5
									phaseBytes[1],	//6
									phaseBytes[2],	//7
									phaseBytes[3],	//8
									phaseBytes[4],	//9
									phaseBytes[5],	//10
									phaseBytes[6],	//11
									phaseBytes[7],	//12
									(Byte)ddsConfigs[0].Amplitude,	//13
									(Byte)ddsConfigs[1].Amplitude,	//14
									(Byte)ddsConfigs[2].Amplitude,	//15
									(Byte)ddsConfigs[3].Amplitude,	//16
									0b00001011		//17
				};
				
				Byte[] response = new byte[18];

				try {
					serial.Open();
					serial.DiscardInBuffer();
					serial.Write(messeage, 0, 18);
					for (int i = 0; i < 18; i++) {
						serial.Read(response, i, 1);
					}

					if (response[0] == 45 && response[17] == 45) {
						
						//ddsConfigs[0].Frequency = 
					}
				}
				catch (Exception e) {
					System.Windows.MessageBox.Show("ERROR");
					ConectionStatus = false;
				}
				finally {
					serial.Close();
				}
				return ConectionStatus;
			}
		}
	

	//INotifyPropertyChanged interface
	private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

			}
		}

	}
}
