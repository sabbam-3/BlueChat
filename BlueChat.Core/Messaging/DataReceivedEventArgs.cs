using System.Text;

namespace BlueChat.Core.Messaging;

public class DataReceivedEventArgs : EventArgs
{
    public byte[] RawData { get; set; }
    public string Text => Encoding.UTF8.GetString(RawData);
    public DateTime Timestamp { get; set; }
}
