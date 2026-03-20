using System.Text;
using BlueChat.Core.Abstractions;
using BlueChat.Core.Constants;
using BlueChat.Core.Models;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace BlueChat.Core.Services;

internal class BluetoothService(
    BluetoothClient client,
    BluetoothListener listener) : IBluetoothService
{
    private Stream? _stream;
    private BluetoothRadio _radio => BluetoothRadio.Default;

    public ConnectionState ConnectionState { get; private set; }
    
    public event Action<ConnectionState>? OnConnectionChanged;
    public event Action<BluetoothMessage>? OnMessageReceived;
    public event Action<HostingState>? OnHostingStateChanged;

    public async Task<List<BluetoothDevice>> ScanForDevicesAsync()
    {
        var devices = await client.DiscoverDevicesAsync().ToArrayAsync();

        var bluetoothDevices = devices.Select(x => new BluetoothDevice
        {
            Name = x.DeviceName,
            Address = x.DeviceAddress.ToString(),
            IsConnected = x.Connected
        })
        .ToList();

        return bluetoothDevices;
    }

    public async Task StartHostAsync()
    {
        listener.Start();

        var task = listener.AcceptBluetoothClientAsync();

        SetState(HostingState.HostingStarted);

        if(await Task.WhenAny(task, Task.Delay(TimeSpan.FromMinutes(2))) == task)
        {
            var client = task.Result;
            _stream = client.GetStream();
            ConnectionState = ConnectionState.Connected;

            SetState(HostingState.HostingCompleted);
            SetState(ConnectionState.Connected);
        }
        else
        {
            SetState(HostingState.HostingStopped);
            if(ConnectionState != ConnectionState.Connecting ||
                ConnectionState != ConnectionState.Connected)
            {
                listener.Stop();
            }
        }
    }

    public async Task ConnectAsync(string address)
    {
        if (ConnectionState == ConnectionState.Connecting || ConnectionState == ConnectionState.Connected)
        {
            return;
        }

        SetState(ConnectionState.Connecting);



        await client.ConnectAsync(BluetoothAddress.Parse(address), ServiceConstants.ServiceId);

        _stream = client.GetStream();

        SetState(ConnectionState.Connected);
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        if (_stream == null)
        {
            return;
        }

        byte[] buffer = new byte[1024];
        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead > 0)
            {
                byte[] data = buffer.Take(bytesRead).ToArray();

                string dataAsString = Encoding.UTF8.GetString(data);

                var bluetoothMessage = BluetoothMessage.Deserialize(dataAsString);

                OnMessageReceived?.Invoke(bluetoothMessage);
            }
        }
    }

    public async Task SendAsync(string message)
    {
        if (_stream == null || !_stream.CanWrite)
        {
            throw new Exception("Not connected to any device.");
        }

        BluetoothMessage bluetoothMessage = BluetoothMessage.Create(_radio.Name, _radio.LocalAddress.ToString(), message);

        string bluetoothMessageAsString = bluetoothMessage.Serialize();

        await _stream.WriteAsync(Encoding.UTF8.GetBytes(bluetoothMessageAsString));
        await _stream.FlushAsync();
    }

    public async Task DisconnectAsync()
    {
        if (ConnectionState == ConnectionState.Disconnected)
        {
            return;
        }

        SetState(ConnectionState.Disconnecting);

        listener.Stop();

        SetState(HostingState.HostingStopped);

        client.Close();

        SetState(ConnectionState.Connected);
    }

    public string GetRemoteDeviceName()
    {
        return client.PairedDevices.First().DeviceName;
    }

    private void SetState(ConnectionState state)
    {
        ConnectionState = state;

        OnConnectionChanged?.Invoke(state);
    }

    private void SetState(HostingState state)
    {
        OnHostingStateChanged?.Invoke(state);
    }
}