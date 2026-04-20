using System;
using System.Threading;
using System.Threading.Tasks;
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

namespace Soenneker.TestHosts.Unit;

///<inheritdoc cref="IUnitTestHost"/>
public class UnitTestHost : IUnitTestHost
{
    private ServiceProvider? _serviceProvider;
    private ILoggerFactory? _loggerFactory;
    private SerilogLoggerProvider? _serilogProvider;
    private Logger? _serilogLogger;
    private TUnitTestContextSink? _tUnitSink;

    private readonly Lazy<AutoFaker> _autoFaker;
    private readonly Lazy<Faker> _faker;

    private ValueAtomicBool _disposed;

    public IServiceCollection Services { get; } = new ServiceCollection();

    public IServiceProvider ServicesProvider =>
        _serviceProvider ?? throw new InvalidOperationException("TestHost has not been initialized. Call Initialize() first.");

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Provides access to AutoFaker (lazy).
    /// </summary>
    public AutoFaker AutoFaker => _autoFaker.Value;

    /// <summary>
    /// Provides access to Faker (lazy).
    /// </summary>
    public Faker Faker => _faker.Value;

    public UnitTestHost(AutoFakerConfig? autoFakerConfig = null)
    {
        _autoFaker = new Lazy<AutoFaker>(() => new AutoFaker(autoFakerConfig), LazyThreadSafetyMode.ExecutionAndPublication);
        _faker = new Lazy<Faker>(() => _autoFaker.Value.Faker, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Configure Serilog for this host. Optional.
    /// </summary>
    public UnitTestHost UseSerilog(Action<LoggerConfiguration>? configure = null)
    {
        _tUnitSink = new TUnitTestContextSink();

        LoggerConfiguration config = new LoggerConfiguration().MinimumLevel.Verbose().Enrich.FromLogContext().WriteTo.Sink(_tUnitSink);

        configure?.Invoke(config);

        _serilogLogger = config.CreateLogger();
        _serilogProvider = new SerilogLoggerProvider(_serilogLogger, dispose: false);

        return this;
    }

    /// <summary>
    /// Build the service provider and finalize the host.
    /// </summary>
    public ValueTask Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException("TestHost is already initialized.");

        if (_serilogProvider != null)
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(_serilogProvider);
            });

            Services.AddSingleton(_loggerFactory);
            Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        }

        _serviceProvider = Services.BuildServiceProvider();
        IsInitialized = true;

        return ValueTask.CompletedTask;
    }

    public T GetRequiredService<T>() where T : notnull
    {
        return ServicesProvider.GetRequiredService<T>();
    }

    public object GetRequiredService(Type type)
    {
        return ServicesProvider.GetRequiredService(type);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return;

        if (_serviceProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            _serviceProvider?.Dispose();

        _loggerFactory?.Dispose();

        if (_serilogProvider is not null)
            await _serilogProvider.DisposeAsync().ConfigureAwait(false);

        if (_serilogLogger is not null)
            await _serilogLogger.DisposeAsync().ConfigureAwait(false);

        if (_tUnitSink is not null)
            await _tUnitSink.DisposeAsync().ConfigureAwait(false);

        Log.Logger = Logger.None;
    }
}