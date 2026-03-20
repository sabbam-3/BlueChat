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

        // Use the provided key or generate a new one
        byte[] key = EncryptedStreamHelper.GenerateKey();

        services.AddSingleton(key);

        services.AddSingleton<IBluetoothService, BluetoothService>();

        return services;
    }
}