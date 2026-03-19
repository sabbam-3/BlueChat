using BlueChat.Core.Abstractions;
using BlueChat.Core.Constants;
using BlueChat.Core.Services;
using InTheHand.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace BlueChat.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<BluetoothClient>();

        services.AddSingleton(s =>
        {
            return new BluetoothListener(ServiceConstants.ServiceId);
        });

        services.AddSingleton<IBluetoothService, BluetoothService>();

        return services;
    }
}