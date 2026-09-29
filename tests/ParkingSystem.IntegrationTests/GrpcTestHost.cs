using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ParkingSystem.IntegrationTests;

/// <summary>Hosts one gRPC service in memory (no port, no Docker) and gives a channel that talks to it.</summary>
internal sealed class GrpcTestHost : IAsyncDisposable
{
    private readonly IHost host;

    private GrpcTestHost(IHost host, GrpcChannel channel)
    {
        this.host = host;
        Channel = channel;
    }

    public GrpcChannel Channel { get; }

    public static async Task<GrpcTestHost> StartAsync<TService>(Action<IServiceCollection>? configureServices = null)
        where TService : class
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddGrpc();
                    configureServices?.Invoke(services);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<TService>());
                }))
            .StartAsync();

        var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = host.GetTestServer().CreateHandler(),
        });
        return new GrpcTestHost(host, channel);
    }

    public async ValueTask DisposeAsync()
    {
        Channel.Dispose();
        await host.StopAsync();
        host.Dispose();
    }
}
