using System;
using TUnit.Core.Interfaces;

namespace Soenneker.TestHosts.Unit.Abstract;

/// <summary>
/// A minimal host for building and running tests.
/// </summary>
public interface IUnitTestHost : IAsyncInitializer, IAsyncDisposable
{
}