using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using ControlCatalog.Pages;

namespace ControlCatalog.NetCore;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--wait-for-attach"))
        {
            WaitForDebugger();
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);

        return 0;
    }

    private static void WaitForDebugger()
    {
        Console.WriteLine("Attach debugger and use 'Set next statement'.");
        while (!Debugger.IsAttached)
        {
            Thread.Sleep(100);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UseReactiveUI()
            .WithInterFont()
            .LogToTrace();

        builder.UsePlatformDetect();

        builder
            .UsePlatformDetect()
            .AfterSetup(_ => EmbedSample.Implementation = CreateNativeDemoControl());

        return builder;
    }

    private static INativeDemoControl? CreateNativeDemoControl()
    {
        if (OperatingSystem.IsWindows())
        {
            return new EmbedSampleWin();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new EmbedSampleMac();
        }

        if (OperatingSystem.IsLinux())
        {
            return new EmbedSampleGtk();
        }

        return null;
    }
}
