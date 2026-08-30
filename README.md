[![](https://img.shields.io/nuget/v/soenneker.testhosts.unit.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.testhosts.unit/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.testhosts.unit/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.testhosts.unit/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.testhosts.unit.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.testhosts.unit/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.testhosts.unit/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.testhosts.unit/actions/workflows/codeql.yml)

# Soenneker.TestHosts.Unit

A TUnit-compatible shared test host with dependency injection, Serilog-to-test-output logging, Bogus, and AutoFaker.

## Installation

```bash
dotnet add package Soenneker.TestHosts.Unit
```

## Define a host

```csharp
using Microsoft.Extensions.DependencyInjection;
using Soenneker.TestHosts.Unit;

public sealed class Host : UnitTestHost
{
    public override Task InitializeAsync()
    {
        Services.AddSingleton<IClock, TestClock>();
        Services.AddScoped<OrderService>();

        return base.InitializeAsync();
    }
}
```

Add registrations before calling `base.InitializeAsync()`. Initialization builds one service provider with scope validation enabled. Changes made to `Services` after that point do not alter the built provider.

## Share it with TUnit

```csharp
using Microsoft.Extensions.DependencyInjection;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class OrderServiceTests
{
    private readonly Host _host;

    public OrderServiceTests(Host host)
    {
        _host = host;
    }

    [Test]
    public async Task Creates_an_order()
    {
        await using AsyncServiceScope scope = _host.ServicesProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<OrderService>();

        var command = _host.AutoFaker.Generate<CreateOrder>();
        Order result = await service.Create(command);

        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
    }
}
```

Create a scope when resolving scoped services; resolving one directly from the root provider fails because scope validation is enabled. `ServicesProvider` is available only after initialization.

## Lifecycle

TUnit calls the host's asynchronous initialization and disposal through its data-source lifecycle. Disposing the host disposes the service provider and therefore any disposable services owned by it, then releases the logging pipeline. Test output written through `ILogger<T>` or Serilog is routed to the active TUnit context.

`Faker` and `AutoFaker` are created lazily and shared with the host. A per-test-session host therefore shares their randomizer state across its tests; do not rely on call order when tests execute concurrently.
