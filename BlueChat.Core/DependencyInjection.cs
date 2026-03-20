using System.Security.Cryptography;
using System.Text;
using BlueChat.Core.Abstractions;
using BlueChat.Core.Constants;
using BlueChat.Core.Security;
using BlueChat.Core.Services;
using InTheHand.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace BlueChat.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(
        this IServiceCollection services)
    {
        services.AddSingleton<BluetoothClient>();

        services.AddSingleton(s =>
        {
            return new BluetoothListener(ServiceConstants.ServiceId);
        });

        services.AddSingleton(sp =>
        {
            //ToDo: use a more secure way to generate and store the encryption key
            string text = "my-secret-key";

            using var sha256 = SHA256.Create();
            var key = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));

            return new EncryptionKey(key);
        });

        services.AddSingleton<IBluetoothService, BluetoothService>();

        return services;
    }
}