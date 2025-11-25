using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace UFF.Monopoly.DesktopApp;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string StartUrl = "https://uffmonopoly.niutech.app.br/";

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Create WebView2 environment with flags to suppress security prompts and allow mixed content if needed
        var options = new CoreWebView2EnvironmentOptions(
            additionalBrowserArguments: "--ignore-certificate-errors --allow-insecure-localhost --allow-running-insecure-content");
        var env = await CoreWebView2Environment.CreateAsync(options: options);

        await webView.EnsureCoreWebView2Async(env);

        // Handle any certificate errors by always allowing (use cautiously)
        webView.CoreWebView2.ServerCertificateErrorDetected += (s, args) =>
        {
            args.Action = CoreWebView2ServerCertificateErrorAction.AlwaysAllow;
        };

        // Tweak settings for better compatibility
        var settings = webView.CoreWebView2.Settings;
        settings.IsStatusBarEnabled = false;
        settings.AreDefaultContextMenusEnabled = true;
        settings.IsZoomControlEnabled = true;
        settings.AreDevToolsEnabled = true;

        // Navigate to the app URL
        webView.Source = new Uri(StartUrl);
    }
}