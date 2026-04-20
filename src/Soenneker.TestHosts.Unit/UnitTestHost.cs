using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;
using Soenneker.Atomics.ValueBools;
using Soenneker.TestHosts.Unit.Abstract;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;
using System;
using System.Threading.Tasks;

namespace Soenneker.TestHosts.Unit;

public class UnitTestHost : IUnitTestHost
{
    private ServiceProvider? _serviceProvider;
    private ILoggerFactory? _loggerFactory;
    private SerilogLoggerProvider? _serilogProvider;
    private Logger? _serilogLogger;

    private readonly object _buildLock = new();

    private ValueAtomicBool _disposed;
    private bool _built;

    private readonly Lazy<AutoFaker> _autoFaker;
    private readonly Lazy<Faker> _faker;

    public IServiceCollection Services { get; } = new ServiceCollection();

    public IServiceProvider ServicesProvider
    {
        get
        {
            EnsureBuilt();
            return _serviceProvider!;
        }
    }

    public Faker Faker => _faker.Value;

    public AutoFaker AutoFaker => _autoFaker.Value;

    public UnitTestHost()
    {
        _faker = new Lazy<Faker>(() => new Faker(), true);
        _autoFaker = new Lazy<AutoFaker>(() =>
        {
            var config = new AutoFakerConfig();
            return new AutoFaker(config);
        }, true);
    }

    public virtual ValueTask Initialize()
    {
        EnsureBuilt();
        return ValueTask.CompletedTask;
    }

    public void Build()
    {
        EnsureBuilt();
    }

    private void EnsureBuilt()
    {
        if (_built)
            return;

        lock (_buildLock)
        {
            if (_built)
                return;

            _serviceProvider = Services.BuildServiceProvider(validateScopes: true);
            _built = true;
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_disposed.TrySetTrue())
            return;

        if (_serviceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else
            _serviceProvider?.Dispose();

        _serilogProvider?.Dispose();
        _loggerFactory?.Dispose();
        _serilogLogger?.Dispose();
    }
}