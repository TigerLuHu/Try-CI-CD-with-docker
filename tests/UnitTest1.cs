using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using System.Net;
using Testcontainers.PostgreSql;

namespace myWebApp.Tests;

public class UnitTest1 : IAsyncLifetime, IDisposable
{
    private const ushort HttpPort = 80;

    private readonly CancellationTokenSource _cts = new(TimeSpan.FromMinutes(1));

    private readonly INetwork _network;

    private readonly IContainer _dbContainer;

    private readonly IContainer _appContainer;

    public UnitTest1()
    {
        _network = new NetworkBuilder()
            .WithName("myWebApp-Tests-net")
            .Build();

        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres")
            .WithNetwork(_network)
            .WithNetworkAliases("postgres-db")
            .WithVolumeMount("postgres-data", "/var/lib/postgresql/data")
            .WithEnvironment(new Dictionary<string, string>{ { "POSTGRES_USER", "postgres" }, { "POSTGRES_PASSWORD", "example" } })
            .Build();

        _appContainer = new ContainerBuilder()
            .WithImage("dotnet-docker")
            .WithNetwork(_network)
            .WithPortBinding(HttpPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPath("/")))
            .Build();
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _network.CreateAsync(_cts.Token)
            .ConfigureAwait(false);
        }
        catch(Exception ex)
        {

        }

        try 
        {
            await _dbContainer.StartAsync(_cts.Token)
            .ConfigureAwait(false);
        }
        catch(Exception ex)
        {

        }

        try
        {
            await _appContainer.StartAsync(_cts.Token)
            .ConfigureAwait(false);
        }
        catch(Exception ex)
        {

        }
    }

    public Task DisposeAsync()
    {
        // Volume was not removed, the following code does not work
        // await _dbContainer.StopAsync();
        // await _dbContainer.DisposeAsync();

        // await _appContainer.StopAsync();
        // await _appContainer.DisposeAsync();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts.Dispose();
    }

    [Fact]
    public async Task Test1()
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new UriBuilder("http", _appContainer.Hostname, _appContainer.GetMappedPublicPort(HttpPort)).Uri;

        var httpResponseMessage = await httpClient.GetAsync(string.Empty)
            .ConfigureAwait(false);

        var body = await httpResponseMessage.Content.ReadAsStringAsync()
            .ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, httpResponseMessage.StatusCode);
        Assert.Contains("Welcome", body);
    }
}