namespace BlueChat.Core.Messaging;

public class BluetoothErrorEventArgs : EventArgs
{
    public string Message { get; set; }
    public Exception? Exception { get; set; }
    public ErrorType Type { get; set; }
}
