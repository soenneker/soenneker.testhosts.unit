[![](https://img.shields.io/nuget/v/soenneker.testhosts.unit.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.testhosts.unit/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.testhosts.unit/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.testhosts.unit/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.testhosts.unit.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.testhosts.unit/)

# Soenneker.TestHosts.Unit

A minimal host for building and running tests.

## Install

```bash
dotnet add package Soenneker.TestHosts.Unit
```

## What you get

- `IUnitTestHost` — A minimal host for building and running tests.

## Practical notes

- Dispose instances you own when their scope ends so held resources can be released.
