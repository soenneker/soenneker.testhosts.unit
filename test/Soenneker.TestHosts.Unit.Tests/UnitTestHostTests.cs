using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Threading.Tasks;
using System.Threading;

namespace Soenneker.TestHosts.Unit.Tests;

public sealed class UnitTestHostTests
{
    [Test]
    public void Default()
    {
    }

    [Test]
    public async ValueTask Initialize_registers_fallback_logging_services(CancellationToken cancellationToken)
    {
        await using var host = new UnitTestHost();

        await host.InitializeAsync();

        ILogger<UnitTestHostTests> logger = host.ServicesProvider.GetRequiredService<ILogger<UnitTestHostTests>>();

        await Assert.That(logger).IsNotNull();
    }

    [Test]
    public async ValueTask Initialize_supports_consumer_added_logging_pipeline(CancellationToken cancellationToken)
    {
        await using var host = new UnitTestHost();

        host.Services.AddLogging(builder => builder.AddSerilog(dispose: false));

        await host.InitializeAsync();

        ILogger<UnitTestHostTests> logger = host.ServicesProvider.GetRequiredService<ILogger<UnitTestHostTests>>();

        logger.LogInformation("Host logging is configured");

        await Assert.That(logger).IsNotNull();
    }

    [Test]
    public async ValueTask Initialize_is_safe_to_call_multiple_times(CancellationToken cancellationToken)
    {
        await using var host = new UnitTestHost();

        await host.InitializeAsync();

        await host.InitializeAsync();

        ILogger<UnitTestHostTests> logger = host.ServicesProvider.GetRequiredService<ILogger<UnitTestHostTests>>();

        await Assert.That(logger).IsNotNull();
    }
}