using System;
using Microsoft.Testing.Extensions;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

namespace Elementary.Tests.UWP;

public sealed partial class App : Application
{
    public App() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        Window.Current.Activate();
        try
        {
            var arguments = PackagedAppExtensions.GetTestApplicationArguments(args.Arguments);
            Environment.ExitCode = await MicrosoftTestingPlatformApplication.RunAsync(arguments);
        }
        finally
        {
            Exit();
        }
    }
}
