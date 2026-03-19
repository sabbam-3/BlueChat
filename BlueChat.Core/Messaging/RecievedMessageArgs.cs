using BlueChat.Core.Models;

namespace BlueChat.Core.Messaging;

public class RecievedMessageArgs : EventArgs
{
    public BluetoothMessage Message { get; }
    public RecievedMessageArgs(BluetoothMessage message)
    {
        Message = message;
    }
}