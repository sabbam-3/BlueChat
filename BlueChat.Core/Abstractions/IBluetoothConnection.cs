using BlueChat.Core.Messaging;
using InTheHand.Net.Sockets;

namespace BlueChat.Core.Abstractions;

public interface IBluetoothConnection
{
    event EventHandler<ConnectionStateEventArgs> ConnectionStateChanged;
    event EventHandler<DataReceivedEventArgs> DataReceived;
    event EventHandler<BluetoothErrorEventArgs> ErrorOccurred;
    event EventHandler<HostingStateEventArgs> HostingStateChanged;

    ConnectionState State { get; }
    bool IsConnected { get; }

    Task<BluetoothDeviceInfo[]> DiscoverDevicesAsync();

    Task StartHostAsync();

    Task ConnectAsync(string address, Guid serviceId);

    Task<string> RecieveDataAsync();

    Task<bool> SendAsync(string message);

    Task StartListeningAsync();

    void Disconnect();
}