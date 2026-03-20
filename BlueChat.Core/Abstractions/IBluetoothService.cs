using BlueChat.Core.Constants;
using BlueChat.Core.Models;

namespace BlueChat.Core.Abstractions;

public interface IBluetoothService
{
    ConnectionState ConnectionState { get; }

    event Action<ConnectionState>? OnConnectionChanged;
    
    event Action<BluetoothMessage>? OnMessageReceived;
    
    event Action<HostingState>? OnHostingStateChanged;

    Task<List<BluetoothDevice>> ScanForDevicesAsync();

    Task StartHostAsync();
    
    Task ConnectAsync(string address);
    
    Task StartListeningAsync(CancellationToken cancellationToken);
    
    Task SendAsync(string message);
    
    Task DisconnectAsync();
}