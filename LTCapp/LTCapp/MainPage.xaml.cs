using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Plugin.BLE.Abstractions.EventArgs;

namespace LTCapp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        MainPageViewModel viewModel;

        public MainPage()
        {
            InitializeComponent();

            BindingContext = viewModel = new MainPageViewModel();
        }
    }

    enum Mode{On, Off, Control};

    public class MainPageViewModel : BaseViewModel
    {
        private string text;
        public string Text
        {
            get => text;
            set => SetProperty(ref text, value);
        }

        private float refTemp = 57;
        public float RefTemp
        {
            get => refTemp;
            set => SetProperty(ref refTemp, value);
        }

        public Command On { get; private set; }
        public Command Off { get; private set; }
        public Command Control { get; private set; }

        BLEManager ble;

        Mode mode = Mode.Off;

        public MainPageViewModel()
        {
            ble = BLEManager.Instance;
            ble.BLEConnectionEvent += BLEConnectionEvent;

            On = new Command(async () =>
            {
                mode = Mode.On;
                if(!ble.IsConnected) await ble.Connect();
            });
            Off = new Command(async () =>
            {
                mode = Mode.Off;
                if (!ble.IsConnected) await ble.Connect();
            });
            Control = new Command(async () =>
            {
                mode = Mode.Control;
                if (!ble.IsConnected) await ble.Connect();
            });

            Text = "デバイスを検索中";
            ble.Connect();
        }

        private void BLEConnectionEvent(BLEConnectionState state)
        {
            switch (state)
            {
                case BLEConnectionState.DeviceDiscovered:
                    Text = "デバイスに接続中";
                    break;
                case BLEConnectionState.DeviceConnected:
                    Text = "サービスに接続中";
                    break;
                case BLEConnectionState.ServiceFound:
                    Text = "キャラクタリスティックに接続中";
                    break;
                case BLEConnectionState.CharacteristicFound:
                    Text = "";
                    ble.RxCharacteristic.ValueUpdated += ValueUpdated;
                    Device.StartTimer(TimeSpan.FromMilliseconds(500), TimerFunc);
                    break;
                case BLEConnectionState.BLEStateIsOFF:
                    Text = "BluetoothをONにして下さい";
                    break;
                case BLEConnectionState.CannotConnect:
                    Text = "接続できませんでした";
                    break;
                case BLEConnectionState.ScanTimeoutElapsed:
                    Text = "デバイスが見つかりませんでした";
                    break;
            }
        }

        private bool TimerFunc()
        {
            if (ble.TxCharacteristic.CanWrite)
            {
                int data = mode == Mode.Control ? (int)(RefTemp * 2.0f)
                         : mode == Mode.On ? 255
                         : 254;
                ble.TxCharacteristic.WriteAsync(new byte[] { (byte)data });
            }
            return true;
        }

        private void ValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
        {
            var data = e.Characteristic.Value;
            if (data.Length != 2) return;
            float temperature = data[1] / 2.0f;
            string mode = data[0] == 255 ? "On" 
                        : data[0] == 254 ? "Off" 
                        : "Control";
            Text = mode + Environment.NewLine + temperature.ToString() + "℃";
        }
    }
}