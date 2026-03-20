namespace BlueChat.Core.Security;

public class EncryptionKey
{
    public byte[] Value { get; }

    public EncryptionKey(byte[] value)
    {
        Value = value;
    }
}