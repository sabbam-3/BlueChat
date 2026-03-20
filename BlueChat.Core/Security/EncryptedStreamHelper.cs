using System.Security.Cryptography;

namespace BlueChat.Core.Security;

/// <summary>
/// Provides AES-GCM encryption and decryption for Bluetooth messages.
/// Both devices must share the same pre-shared key.
/// </summary>
public static class EncryptedStreamHelper
{
    private const int NonceSize = 12;  // AES-GCM recommended nonce size
    private const int TagSize = 16;    // AES-GCM authentication tag size

    /// <summary>
    /// Encrypts plaintext bytes using AES-GCM with the provided key.
    /// Output format: [nonce (12 bytes)][tag (16 bytes)][ciphertext]
    /// </summary>
    public static byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentNullException.ThrowIfNull(key);

        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Pack: nonce + tag + ciphertext
        byte[] result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

        return result;
    }

    /// <summary>
    /// Decrypts data previously encrypted by <see cref="Encrypt"/>.
    /// Throws <see cref="CryptographicException"/> if the data was tampered with.
    /// </summary>
    public static byte[] Decrypt(byte[] encryptedData, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        ArgumentNullException.ThrowIfNull(key);

        if (encryptedData.Length < NonceSize + TagSize)
            throw new CryptographicException("Encrypted data is too short to be valid.");

        byte[] nonce = new byte[NonceSize];
        byte[] tag = new byte[TagSize];
        int ciphertextLength = encryptedData.Length - NonceSize - TagSize;
        byte[] ciphertext = new byte[ciphertextLength];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(encryptedData, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(encryptedData, NonceSize + TagSize, ciphertext, 0, ciphertextLength);

        byte[] plaintext = new byte[ciphertextLength];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    /// <summary>
    /// Generates a new 256-bit AES key.
    /// </summary>
    public static byte[] GenerateKey()
    {
        byte[] key = new byte[32]; // 256-bit
        RandomNumberGenerator.Fill(key);
        return key;
    }
}