namespace BlueChat.Core.Messaging;

public class ConnectionStateEventArgs : EventArgs
{
    public ConnectionState State { get; set; }
    public string? Message { get; set; }
}