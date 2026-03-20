using System.Buffers.Binary;
using System.Text;
using BlueChat.Core.Abstractions;
using BlueChat.Core.Constants;
using BlueChat.Core.Models;
using BlueChat.Core.Security;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace BlueChat.Core.Services;

internal class BluetoothService(
    BluetoothClient client,
    BluetoothListener listener,
    byte[] encryptionKey) : IBluetoothService
{
    private const int MaxMessageSize = 256 * 1024;

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

        if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromMinutes(2))) == task)
        {
            var acceptedClient = task.Result;
            _stream = acceptedClient.GetStream();

            SetState(HostingState.HostingCompleted);
            SetState(ConnectionState.Connected);
        }
        else
        {
            SetState(HostingState.HostingStopped);

            if (ConnectionState != ConnectionState.Connecting &&
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
        if (_stream is null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            // 1. Read the 4-byte length prefix
            byte[] lengthPrefix = new byte[4];
            if (!await ReadExactAsync(_stream, lengthPrefix, cancellationToken))
            {
                break; // Stream closed
            }

            int messageLength = BinaryPrimitives.ReadInt32BigEndian(lengthPrefix);

            // 2. Validate message size
            if (messageLength <= 0 || messageLength > MaxMessageSize)
            {
                throw new InvalidOperationException(
                    $"Received invalid message length: {messageLength}. Max allowed: {MaxMessageSize}.");
            }

            // 3. Read the full encrypted payload
            byte[] encryptedPayload = new byte[messageLength];
            if (!await ReadExactAsync(_stream, encryptedPayload, cancellationToken))
                break;

            // 4. Decrypt
            byte[] decryptedBytes = EncryptedStreamHelper.Decrypt(encryptedPayload, encryptionKey);
            string dataAsString = Encoding.UTF8.GetString(decryptedBytes);

            // 5. Deserialize (now hardened with validation)
            var bluetoothMessage = BluetoothMessage.Deserialize(dataAsString);

            OnMessageReceived?.Invoke(bluetoothMessage);
        }
    }

    public async Task SendAsync(string message)
    {
        if (_stream is null || !_stream.CanWrite)
        {
            throw new InvalidOperationException("Not connected to any device.");
        }

        BluetoothMessage bluetoothMessage = BluetoothMessage.Create(
            _radio.Name, _radio.LocalAddress.ToString(), message);

        string serialized = bluetoothMessage.Serialize();
        byte[] plainBytes = Encoding.UTF8.GetBytes(serialized);

        // 1. Encrypt
        byte[] encrypted = EncryptedStreamHelper.Encrypt(plainBytes, encryptionKey);

        // 2. Validate outgoing size
        if (encrypted.Length > MaxMessageSize)
        {
            throw new InvalidOperationException(
                $"Message too large to send ({encrypted.Length} bytes). Max: {MaxMessageSize}.");
        }

        // 3. Write length prefix + encrypted payload
        byte[] lengthPrefix = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, encrypted.Length);

        await _stream.WriteAsync(lengthPrefix);
        await _stream.WriteAsync(encrypted);
        await _stream.FlushAsync();
    }

    public async Task DisconnectAsync()
    {
        if (ConnectionState == ConnectionState.Disconnected)
        {
            return;
        }

        SetState(ConnectionState.Disconnecting);

        // Dispose the stream to release socket resources
        if (_stream is not null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }

        listener.Stop();

        SetState(HostingState.HostingStopped);

        client.Close();

        SetState(ConnectionState.Disconnected);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;

        listener.Stop();
        client.Close();

        GC.SuppressFinalize(this);
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

    private static async Task<bool> ReadExactAsync(
        Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int bytesRead = await stream.ReadAsync(
                buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken);

            if (bytesRead == 0)
            {
                return false;
            }
            totalRead += bytesRead;
        }
        return true;
    }
}