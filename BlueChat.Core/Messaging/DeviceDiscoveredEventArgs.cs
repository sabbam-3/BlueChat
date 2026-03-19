namespace BlueChat.Core.Messaging;

public class DeviceDiscoveredEventArgs : EventArgs
{
    public string Name { get; set; }
    public string Address { get; set; }
    public bool IsPaired { get; set; }
}