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

    public static BluetoothMessage Deserialize(string line)
    {
        var parts = line.Split('|');
        if (parts.Length != 4)
            throw new FormatException("Invalid message format");

        return new BluetoothMessage
        {
            SenderDeviceName = parts[0],
            SenderAddress = parts[1],
            Payload = parts[2],
            Timestamp = DateTime.Parse(parts[3])
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