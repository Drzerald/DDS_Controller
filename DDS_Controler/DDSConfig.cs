using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace DDS_Controler
{
           public class DDSConfig  : INotifyPropertyChanged
    {
		//DDS Configuration
        public const int maxPhaseRegValue = 4096; //Max PHASE Registery value
		/// <summary>
		///	Used MCLK signal frequency in Hz
		/// </summary>
		public const float MclkFreqInHz = 8000000;
		public int MaxFrequencyInHz = 400000;
		private int amplitude = 0;
		private float maxAmplitude = 2; 
        private int phase = 0;			
        private int frequency = 3691;


		/// <summary>
		/// Value of DigiPot register
		/// </summary>
		public int Amplitude {
			get { return amplitude; }
			set {
				if (value < 0) {
					amplitude = 0;
				}
				else if (value > 255) {
					amplitude = 255;
				}
				else{
					amplitude = value;
				}
				NotifyPropertyChanged();
				NotifyPropertyChanged("AmplitudeInVolts");
			}
		}
 		public float MaxAmplitude {
			get { return maxAmplitude; }
			set {
				maxAmplitude = value;
				NotifyPropertyChanged();
				NotifyPropertyChanged("AmplitudeInVolts");
			}
		}
		/// <summary>
		/// Signal Amplitude in Volts
		/// </summary>
		public float AmplitudeInVolts { 
			get {
				return (float)Amplitude / 255 * MaxAmplitude;
			}
			set {
				Amplitude = (int)Math.Floor(value / MaxAmplitude * 255);
				NotifyPropertyChanged();
			}
			
		}
		/// <summary>
		/// Returns Max PHASE registery value
		/// </summary>
        public int MaxPhaseRegValue { get { return maxPhaseRegValue; } }
             		/// <summary>
		/// PHASE Register Value
		/// </summary>
		public int Phase{
            get { return phase; }
            set {
                phase = Math.Abs(value % 4096);
                NotifyPropertyChanged("PhaseInDeg");
            }
        }
		/// <summary>
		/// Signal phase shift in degrees   
		/// </summary>
		public float PhaseInDeg{
            get { return phase * 360 / (float)maxPhaseRegValue; }
			set {
				float x = (value < 0) ? x = 360 + value : value; 
				Phase = (int)Math.Ceiling((x / 360) * (float)maxPhaseRegValue);
				NotifyPropertyChanged("PhaseInDeg");
				NotifyPropertyChanged("Phase");
			}
        }
		/// <summary>
		///	FREG register value
		/// </summary>   
        public int Frequency
        {
            get { return frequency; }
            set
            {
                frequency = value % 268435456; // In case overflow
                NotifyPropertyChanged("FrequencyInHz");				
            }
        }
		/// <summary>
		/// Signal Frequency in Hz
		/// </summary>
        public float FrequencyInHz
        {
            get { return frequency * MclkFreqInHz / (1<<28); }//268435456
			set {
				float f = Math.Abs(value);
				f = (f >= MaxFrequencyInHz) ? FrequencyInHz : f;
				Frequency = (int)Math.Ceiling(268435456 * f / MclkFreqInHz);
				NotifyPropertyChanged("FrequencyInHz");
				NotifyPropertyChanged("Frequency");
			}
		}


		/// <summary>
		/// Constructs DSS Config,
		/// Object contains confiuration of DDS unit and conteted Digital Potentiometer position.
		/// </summary>
		/// <param name="phaseshift">PHASE registery (DDS AD9833 documetation for more informations)</param>
		/// <param name="freq">FREG registery (DDS AD9833 documetation for more informations)</param>
		/// <param name="maxAmplitude">Maximal signal amplitude in Volts (for 255 DigiPot position)</param>
		/// <param name="sigAmplitude">DigiPot position</param>
		public DDSConfig( int phaseshift = 0, int freq = 33555, float maxAmplitude = 2f,int sigAmplitude = 128) {
			this.Amplitude = sigAmplitude;
			this.MaxAmplitude = maxAmplitude;
            this.Phase = phaseshift;
            this.Frequency = freq;
		}

		/// <summary>
		///  Returns UInt32 of bits of two commands required to set FREQ register of DDS (Includes REGISTER ID)
		/// </summary>
		public byte[] GetFrequencyCommand(){  
			byte[] temp = { 0, 0, 0, 0 };
			byte[] freq =  BitConverter.GetBytes(Frequency);

			temp[0] |= freq[0];
			temp[1] |= (byte)(freq[1] & 0b00111111);
			temp[1] |= 0b01000000;
			temp[2] |= (byte)((freq[1]>>6) & 0b00000011);
			temp[2] |= (byte)((freq[2] << 2) & 0b11111100);
			temp[3] |= (byte)((freq[2] >> 6) & 0b00000011);
			temp[3] |= 0b01000000; //Adding Register header

			byte[] x = {0,0,0,0};
			x[0] |= temp[3]; // Reg + MSB
			x[1] |= temp[2]; // MSB
			x[2] |= temp[1]; // Reg + LSB
			x[3] |= temp[0]; // LSB

			//debug
			System.Diagnostics.Debug.Print("GetFrequencyCommand:");
			foreach (byte b in x) {
				System.Diagnostics.Debug.Print(String.Format("[{0:X}]", b));
			}

			//System.Diagnostics.Debug.Print(String.Format("Binary: {0:X}", x));
			return x;
		}
		/// <summary>
		/// Returns Bits set in PHASE Registery
		/// </summary>
		/// <returns></returns>
		public byte[] GetPhaseCommand() {
			byte[] p = BitConverter.GetBytes(Phase);
			byte[] temp = { 0, 0 };
			temp[1] |= p[0];
			temp[0] |= (byte)(p[1] & 0b00001111);
			temp[0] |= 0b11000000;


			System.Diagnostics.Debug.Print(String.Format("Binary: {0:X}", temp[0]));
			System.Diagnostics.Debug.Print(String.Format("Binary: {0:X}", temp[1]));
			return temp;
		}


		/// <summary>
		/// COnvert and Write freq as it was writen in SPI ([MSB][][][LLSB])
		/// </summary>
		/// <param name="freqMessage"></param>
		public void setFreqFromSPI (byte[] freqMessage) {
			if(freqMessage.Length == 4) {
				int f = 0;
				f += ((freqMessage[3] | 0b11111111));
				f += ((freqMessage[2] | 0b00111111)<<8);
				f += ((freqMessage[1] | 0b11111111) << 6);
				f += ((freqMessage[0] | 0b00111111) << 8);
				System.Diagnostics.Debug.Write("FREQ = " + f.ToString());
			}
		}

		//DEBUG
		public void DebugPrint()
		{
			System.Diagnostics.Debug.Print(String.Format("Aplitude: {0}  AplitudeVolts: {1}   Vref: {2}", Amplitude, AmplitudeInVolts, MaxAmplitude));
		}

		// INotifyPropertyChanged interface
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "") {
			if (PropertyChanged != null) {
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
