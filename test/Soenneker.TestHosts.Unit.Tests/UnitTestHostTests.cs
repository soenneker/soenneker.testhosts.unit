using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Threading.Tasks;

namespace Soenneker.TestHosts.Unit.Tests;

public sealed class UnitTestHostTests
{
    [Test]
    public void Default()
    {
    }

    [Test]
    public async Task Initialize_registers_fallback_logging_services()
    {
        await using var host = new UnitTestHost();

        await host.Initialize();

        ILogger<UnitTestHostTests> logger = host.ServicesProvider.GetRequiredService<ILogger<UnitTestHostTests>>();

        await Assert.That(logger).IsNotNull();
    }

    [Test]
    public async Task Initialize_supports_consumer_added_logging_pipeline()
    {
        await using var host = new UnitTestHost();

        host.Services.AddLogging(builder => builder.AddSerilog(dispose: false));

        await host.Initialize();

        ILogger<UnitTestHostTests> logger = host.ServicesProvider.GetRequiredService<ILogger<UnitTestHostTests>>();

        logger.LogInformation("Host logging is configured");

        await Assert.That(logger).IsNotNull();
    }
}
