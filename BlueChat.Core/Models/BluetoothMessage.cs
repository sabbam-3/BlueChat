namespace BlueChat.Core.Models;

public class BluetoothMessage
{
    public string SenderDeviceName { get; set; }

    public string SenderAddress { get; set; }

    public string Payload { get; set; }

    public DateTime Timestamp { get; set; }

    public string Serialize()
    {
        return $"{SenderDeviceName}|{SenderAddress}|{Payload}|{Timestamp:o}";
    }

    // Maximum allowed length of a serialized message string (64 KB).
    private const int MaxSerializedLength = 65_536;

    public static BluetoothMessage Deserialize(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (line.Length > MaxSerializedLength)
        {
            throw new FormatException($"Message exceeds maximum allowed length of {MaxSerializedLength} characters.");
        }

        // Split with a limit to prevent excessive allocations from malformed input
        var parts = line.Split('|', 5);
        if (parts.Length != 4)
        {
            throw new FormatException("Invalid message format: expected exactly 4 pipe-delimited fields.");
        }

        if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            throw new FormatException("SenderDeviceName and SenderAddress must not be empty.");
        }

        if (!DateTime.TryParse(parts[3], System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var timestamp))
        {
            throw new FormatException("Invalid timestamp format.");
        }

        return new BluetoothMessage
        {
            SenderDeviceName = parts[0].Trim(),
            SenderAddress = parts[1].Trim(),
            Payload = parts[2],
            Timestamp = timestamp
        };
    }

    public static BluetoothMessage Create(string senderDeviceName, string senderAddress, string payload)
    {
        return new BluetoothMessage
        {
            SenderDeviceName = senderDeviceName,
            SenderAddress = senderAddress,
            Payload = payload,
            Timestamp = DateTime.UtcNow
        };
    }
}