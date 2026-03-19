namespace BlueChat.Core.Messaging;

public class HostingStateEventArgs : EventArgs
{
    public HostingState State { get; set; }
    public string? Message { get; set; }
}