using System;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Skia;
using Xunit;

namespace FastTreeDataGrid.Control.Tests;

public sealed class AvaloniaFixture : IDisposable
{
    public AvaloniaFixture()
    {
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false
            })
            .SetupWithoutStarting();
    }

    public void Dispose()
    {
    }

    private sealed class TestApp : Application
    {
    }
}

[CollectionDefinition("Avalonia")]
public sealed class AvaloniaCollection : ICollectionFixture<AvaloniaFixture>
{
}
