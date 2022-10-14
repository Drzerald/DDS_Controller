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
using System.Diagnostics;

namespace DDS_Controler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		private UARTConnector uartConector;
		private DDSConfig[] ddsConfigs = new DDSConfig[4];

		public MainWindow()
        {
            InitializeComponent();

			uartConector = new UARTConnector();
			comboBox.ItemsSource=uartConector.listOfPorts;
			if(uartConector.listOfPorts.Count > 0){
				comboBox.SelectedIndex = 0;
			}

			//Conetign GUI to parameters
			ddsConfigs[0] = new DDSConfig();
			ddsConfigs[1] = new DDSConfig();
			ddsConfigs[2] = new DDSConfig();
			ddsConfigs[3] = new DDSConfig();

			amplitude0_Slider.DataContext	= ddsConfigs[0];
			amplitude0_Label.DataContext	= ddsConfigs[0];
			amplitude1_Slider.DataContext	= ddsConfigs[1];
			amplitude1_Label.DataContext	= ddsConfigs[1];
			amplitude2_Slider.DataContext	= ddsConfigs[2];
			amplitude2_Label.DataContext	= ddsConfigs[2];
			amplitude3_Slider.DataContext	= ddsConfigs[3];
			amplitude3_Label.DataContext	= ddsConfigs[3];

			frequency0_Label.DataContext = ddsConfigs[0];
			frequency0_Slider.DataContext = ddsConfigs[0];

			phase0_Slider.DataContext	= ddsConfigs[0];
			phase0_Label.DataContext	= ddsConfigs[0];
			phase1_Slider.DataContext	= ddsConfigs[1];
			phase1_Label.DataContext	= ddsConfigs[1];
			phase2_Slider.DataContext	= ddsConfigs[2];
			phase2_Label.DataContext	= ddsConfigs[2];
			phase3_Slider.DataContext	= ddsConfigs[3];
			phase3_Label.DataContext	= ddsConfigs[3];

			TextBlock_Status.DataContext = uartConector;
		}
        
		/// <summary>
		/// Refresh list of existing serial ports
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Refresh_Click(object sender, RoutedEventArgs e)
		{
			uartConector.refreshPortsList();
			if (uartConector.listOfPorts.Count > 0){
				comboBox.SelectedIndex = 0;
            }
            else{
                uartConector.ConectionStatus = false;
            } 
		}

		/// <summary>
		/// Send Only data of new signal aplitudes
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Apply_Click(object sender, RoutedEventArgs e)
		{  
            if ((string)comboBox.SelectedItem != null) {
                uartConector.sendDDSValues((string)comboBox.SelectedItem, ddsConfigs[0], ddsConfigs[1]);
            }
            else{
                System.Windows.MessageBox.Show("Please select Serial Port","No Port Selected!");
                uartConector.ConectionStatus = false;
            }
		}

		/// <summary>
		/// On Change of Port
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (comboBox.SelectedItem != null && String.Empty != (string)comboBox.SelectedItem) { 
				uartConector.checkConection((string)comboBox.SelectedItem); //Check if DDS is detected
			}
		}

		/// <summary>
		/// Send FULL DDS Config
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e) ///
        {
			if( (string) comboBox.SelectedItem != null){
				uartConector.sendFullDDSConfig((string)comboBox.SelectedItem, ddsConfigs);
			}
			else{
				System.Windows.MessageBox.Show("Please select Serial Port", "No Port Selected!");
				uartConector.ConectionStatus = false;
			}
		}

		private void Button_Click_1(object sender, RoutedEventArgs e) {
			//byte[] fB = BitConverter.GetBytes(ddsConfigs[0].GetFrequencyCommand());
			//ddsConfigs[0].setFreqFromSPI(fB);
		}

		private void ClearTexBox(object sender, RoutedEventArgs e) {
			((TextBox)sender).Text = "";
		}
	}
}
