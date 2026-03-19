using System.Text;
using BlueChat.Core.Abstractions;
using BlueChat.Core.Messaging;
using InTheHand.Net;
using InTheHand.Net.Sockets;

namespace BlueChat.Core.Services;

internal class BluetoothConnection(
    BluetoothClient client,
    BluetoothListener listener) : IBluetoothConnection
{
    private Stream? _stream;
    private CancellationTokenSource? _receiveCts;

    public event EventHandler<ConnectionStateEventArgs>? ConnectionStateChanged;
    public event EventHandler<DataReceivedEventArgs>? DataReceived;
    public event EventHandler<BluetoothErrorEventArgs>? ErrorOccurred;

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public bool IsConnected => client.Connected;


    public async Task<BluetoothDeviceInfo[]> DiscoverDevicesAsync()
    {
        var devices = await client.DiscoverDevicesAsync().ToArrayAsync();

        return devices;
    }

    public async Task StartHostAsync()
    {
        listener.Start();

        while (IsConnected is false)
        {
            var internalClient = await listener.AcceptBluetoothClientAsync();

            client = internalClient;
        }
    }

    public async Task ConnectAsync(string address, Guid serviceId)
    {
        if (State == ConnectionState.Connecting || State == ConnectionState.Connected)
        {
            return;
        }

        SetState(ConnectionState.Connecting, "Connecting...");

        await client.ConnectAsync(BluetoothAddress.Parse(address), serviceId);

        _stream = client.GetStream();

        SetState(ConnectionState.Connected, "Connected");
    }

    public async Task<string> RecieveDataAsync()
    {
        if (_stream == null)
        {
            RaiseError("Not connected");
            return string.Empty;
        }

        StreamReader reader = new StreamReader(_stream, Encoding.UTF8);

        return await reader.ReadToEndAsync();
    }

    public async Task StartListeningAsync()
    {
        if (_stream == null)
        {
            RaiseError("Not connected");
            return;
        }
        _receiveCts = new CancellationTokenSource();

        byte[] buffer = new byte[1024];
        while (!_receiveCts.Token.IsCancellationRequested)
        {
            try
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _receiveCts.Token);
                if (bytesRead > 0)
                {
                    byte[] data = buffer.Take(bytesRead).ToArray();
                    DataReceived?.Invoke(this, new DataReceivedEventArgs
                    {
                        RawData = data,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Listening was cancelled, exit the loop
                break;
            }
            catch (Exception ex)
            {
                RaiseError("Error receiving data", ex);
                break;
            }
        }
    }

    public async Task<bool> SendAsync(string message)
    {
        if (_stream == null || !_stream.CanWrite)
        {
            RaiseError("Not connected");
            return false;
        }

        await _stream.WriteAsync(Encoding.UTF8.GetBytes(message));
        await _stream.FlushAsync();
        //I should raise an event here to notify that the message was sent
        //I should have listener which always listens for incoming messages and raises DataReceived event when a message is received

        return true;
    }

    public void Disconnect()
    {
        if (State == ConnectionState.Disconnected)
        {
            return;
        }

        SetState(ConnectionState.Disconnecting, "Disconnecting...");

        _receiveCts?.Cancel();

        _stream?.Close();
        client.Close();

        SetState(ConnectionState.Disconnected, "Disconnected");
    }

    private void SetState(ConnectionState state, string? message = null)
    {
        State = state;
        ConnectionStateChanged?.Invoke(this, new ConnectionStateEventArgs
        {
            State = state,
            Message = message
        });
    }

    private void RaiseError(string message, Exception? ex = null)
    {
        ErrorOccurred?.Invoke(this, new BluetoothErrorEventArgs
        {
            Message = message,
            Exception = ex
        });
    }

    public void Dispose()
    {
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        _stream?.Dispose();
        client?.Dispose();
    }
}