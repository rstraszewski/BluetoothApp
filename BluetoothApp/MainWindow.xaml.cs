using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using Microsoft.Win32;
using static System.IO.Path;

namespace BluetoothApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _info = "Listener not connected";
        private bool _isPaired;
        private const string Paired = "Paired";
        private const string NotPaired = "Not paired";
        private const string Pairing = "Pairing";

        public ObservableCollection<BluetoothRadio> BluetoothRadios => new ObservableCollection<BluetoothRadio>(BluetoothRadio.AllRadios);

        public ObservableCollection<BluetoothDeviceInfo> BluetoothDevices { get; set; } = new ObservableCollection<BluetoothDeviceInfo>();

        public BluetoothRadio SelectedBluetoothRadio { get; set; }

        public BluetoothDeviceInfo SelectedBluetoothDevice { get; set; }

        public BluetoothClient BluetoothClient { get; set; }

        public bool IsPaired
        {
            get { return _isPaired; }
            set
            {
                _isPaired = value;
                OnPropertyChanged();
            }
        }

        public string Info
        {
            get { return _info; }
            set
            {
                _info = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private async void BluetoothRadioSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            BluetoothClient = new BluetoothClient(new BluetoothEndPoint(SelectedBluetoothRadio.LocalAddress, Guid.NewGuid()));
            BluetoothDevices.Clear();

            Info = "Finding devices...";

            var devices = await Task.Run(() => BluetoothClient.DiscoverDevices());
            foreach (var device in devices)
            {
                BluetoothDevices.Add(device);
            }

            Info = $"Found {devices.Length} devices";
        }

        private async void PairDevice(object sender, RoutedEventArgs e)
        {
            Info = Pairing;
            var isPaired = await Task.Run(() => BluetoothSecurity.PairRequest(SelectedBluetoothDevice.DeviceAddress, "123456"));
            Info = isPaired ? Paired : NotPaired;
            IsPaired = isPaired;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void SendFile(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Multiselect = false,
                Title = "Send file to choosen device"
            };

            var result = ofd.ShowDialog();
            if (result.HasValue && result == true)
            {
                await SendFile(ofd.FileName);
            }
        }

        private async Task SendFile(string filePath)
        {
            var filename = GetFullPath(filePath);
            var requestUri = new Uri($"obex-push://{SelectedBluetoothDevice.DeviceAddress}/{filename}");

            Info = $"Sending file: {filename}";

            var owr = new ObexWebRequest(requestUri);
            owr.ReadFile(filePath);

            var response = await Task.Run(() => (ObexWebResponse)owr.GetResponse());
            Info = $"Response: {response.StatusCode}";
            response.Close();
        }
    }
}
