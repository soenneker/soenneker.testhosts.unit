using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;
using Soenneker.Atomics.ValueBools;
using Soenneker.Serilog.Sinks.TUnit;
using Soenneker.TestHosts.Unit.Abstract;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Soenneker.TestHosts.Unit;

public class UnitTestHost : IUnitTestHost
{
    private ServiceProvider? _serviceProvider;
    private ILoggerFactory? _loggerFactory;
    private SerilogLoggerProvider? _serilogProvider;
    private Logger? _serilogLogger;
    private TUnitTestContextSink? _tUnitSink;

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

            EnsureLoggingConfigured();

            _serviceProvider = Services.BuildServiceProvider(validateScopes: true);
            _built = true;
        }
    }

    private void EnsureLoggingConfigured()
    {
        if (_serilogLogger is null)
        {
            _tUnitSink = new TUnitTestContextSink();

            _serilogLogger = new LoggerConfiguration()
                             .MinimumLevel.Verbose()
                             .Enrich.FromLogContext()
                             .WriteTo.Sink(_tUnitSink)
                             .CreateLogger();

            _serilogProvider = new SerilogLoggerProvider(_serilogLogger, dispose: false);
            _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_serilogProvider));
        }

        Log.Logger = _serilogLogger;

        if (!Services.Any(descriptor => descriptor.ServiceType == typeof(ILoggerFactory)))
            Services.AddSingleton(_loggerFactory!);

        if (!Services.Any(descriptor => descriptor.ServiceType == typeof(ILogger<>)))
            Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
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
        _tUnitSink?.Dispose();

        Log.Logger = Logger.None;
    }
}
