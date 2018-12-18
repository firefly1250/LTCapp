using System;

using System.Threading.Tasks;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions.EventArgs;

namespace LTCapp
{
    enum BLEConnectionState
    {
        DeviceDiscovered,
        DeviceConnected,
        ServiceFound,
        CharacteristicFound,

        ScanTimeoutElapsed,
        BLEStateIsOFF,
        CannotConnect
    }
    delegate void BLEConnectionEventHandler(BLEConnectionState state);

    class BLEManager
    {
        //Singleton
        public static BLEManager Instance { get; } = new BLEManager();

        IBluetoothLE ble;
        IDevice device;

        readonly Guid serviceID, rxID, txID;

        public ICharacteristic TxCharacteristic { get; private set; }
        public ICharacteristic RxCharacteristic { get; private set; }

        public bool IsConnected { get; private set; } = false;

        public event BLEConnectionEventHandler BLEConnectionEvent;

        private BLEManager()
        {
            serviceID = new Guid("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
            rxID =      new Guid("6E400003-B5A3-F393-E0A9-E50E24DCCA9E");
            txID =      new Guid("6E400002-B5A3-F393-E0A9-E50E24DCCA9E");

            ble = Plugin.BLE.CrossBluetoothLE.Current;
            ble.Adapter.ScanTimeout = 10000;

            ble.Adapter.DeviceDiscovered += DeviceDiscovered;


            ble.Adapter.ScanTimeoutElapsed += (s, e) 
                => BLEConnectionEvent(BLEConnectionState.ScanTimeoutElapsed);
        }

        private async void DeviceDiscovered(object s, DeviceEventArgs e)
        {
            BLEConnectionEvent(BLEConnectionState.DeviceDiscovered);
            await ble.Adapter.StopScanningForDevicesAsync();
            device = e.Device;

            try
            {
                await ble.Adapter.ConnectToDeviceAsync(device);

                if (device.State != DeviceState.Connected)
                {
                    BLEConnectionEvent(BLEConnectionState.CannotConnect);
                    return;
                }
                BLEConnectionEvent(BLEConnectionState.DeviceConnected);

                var service = await device.GetServiceAsync(serviceID);
                BLEConnectionEvent(BLEConnectionState.ServiceFound);

                RxCharacteristic = await service.GetCharacteristicAsync(rxID);
                TxCharacteristic = await service.GetCharacteristicAsync(txID);
                await RxCharacteristic.StartUpdatesAsync();
                IsConnected = true;
                BLEConnectionEvent(BLEConnectionState.CharacteristicFound);

                //RxCharacteristic.ValueUpdated += StateDataStore.Instance.BleRxUpdated;
            }
            catch (DeviceConnectionException)
            {
                BLEConnectionEvent(BLEConnectionState.CannotConnect);
            }

        }

        public async Task Connect()
        {
            if (ble.State == BluetoothState.Off)
            {
                BLEConnectionEvent(BLEConnectionState.BLEStateIsOFF);
                return;
            }

            //なぜかこれではデバイスを発見できなかった
            //await ble.Adapter.StartScanningForDevicesAsync(new Guid[] { serviceID });

            await ble.Adapter.StartScanningForDevicesAsync(deviceFilter: d => d.Name == "LTC");
        }

    }
}
