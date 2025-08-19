using System.Threading;
using System.Threading.Tasks;
using Content.Server._NullLink.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.GrainReferences;
using Orleans.Hosting;
using Orleans.Runtime;
using Robust.Shared.Physics;
using StackExchange.Redis;

namespace Content.Server._NullLink.Core;

internal static class OrleansClientHolder
{
    private static readonly TaskCompletionSource _connectionTcs = new();
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static IHost? s_host;
    public static event Action OnConnected = () => { };

    public static Task Connection => _connectionTcs.Task;
    public static IClusterClient? Client { get; private set; }
    public static async ValueTask Configure(string conn, ISawmill log, bool rebuild = false)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (Client != null && !rebuild)
                return;

            await ShutdownSafe();

            s_host = new HostBuilder()
                .UseOrleansClient((ctx, client)
                    => client.UseRedisClustering(opt => opt.ConfigurationOptions = ConfigurationOptions.Parse(conn))
                          .AddRobustSawmill(log)
                          .AddGatewayCountChangedHandler(GatewayHandler)
                          .Services.AddTransient<IClientConnectionRetryFilter, ClientConnectRetryFilter>())
                .Build();

            await s_host.StartAsync();

            Client = s_host.Services.GetRequiredService<IClusterClient>();

            _connectionTcs.TrySetResult();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    static async void GatewayHandler(object sender, GatewayCountChangedEventArgs e)
    {
        if (e.PreviousNumberOfConnectedGateways != 0 || e.NumberOfConnectedGateways <= 0)
            return;
        _connectionTcs.Task
            .Then(OnConnected.Invoke)
            .FireAndForget(); // Ensure the connection is established before invoking the event.
    }

    public static async ValueTask Shutdown()
    {
        if (s_host == null) return;
        await _semaphore.WaitAsync();
        try
        {
            await ShutdownSafe();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async ValueTask ShutdownSafe()
    {
        if (s_host == null) return;

        try { await s_host.StopAsync(); }
        catch { }

        s_host.Dispose();
        s_host = null;
        Client = null;
    }
}
