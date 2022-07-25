﻿
global using System.Text.Json;
global using System.Collections.ObjectModel;
global using Microsoft.ApplicationInsights;
global using Chat.Core.Model;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using System;
global using System.Collections.Generic;
global using System.Threading.Tasks;
global using Microsoft.ApplicationInsights.Extensibility;
global using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
global using System.Reflection;
global using Microsoft.Extensions.Configuration;
global using System.Globalization;
global using Microsoft.ApplicationInsights.DataContracts;

namespace ChatCore;

public static class Globals
{
    public static TelemetryClient TelemetryInstance;

    public static TelemetryClient GetTelemetryClient()
    {
        TelemetrySettings settings = Settings();
        TelemetryConfiguration cfg = TelemetryConfiguration.CreateDefault();
                               cfg.ConnectionString = settings.AppInsights;
        QuickPulseTelemetryProcessor qp = null;
        cfg.DefaultTelemetrySink.TelemetryProcessorChainBuilder
            .Use((next) =>
            {
                qp = new QuickPulseTelemetryProcessor(next);
                return qp;
            })
            .Build();

        var qpm = new QuickPulseTelemetryModule
        {
            AuthenticationApiKey = settings.QuickPulse
        };
        qpm.Initialize(cfg);
        qpm.RegisterTelemetryProcessor(qp);
        TelemetryClient client = new(cfg);

        string uniqueID = Guid.NewGuid().ToString();

      //  RegisterUnhandledExceptions();

        client.Context.User.AccountId ??= uniqueID;
        client.Context.User.Id ??= uniqueID;

        TelemetryInstance = client;
        return TelemetryInstance;
    }

    public static void SetMAUIProperties(this TelemetryContext context)
    {
        context.Device.Model ??= DeviceInfo.Model;
        context.Device.OperatingSystem ??= DeviceInfo.Platform.ToString();
        context.Device.Language ??= CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
       // client.Context.Device.ScreenResolution ??= DeviceDisplay.MainDisplayInfo.ToString();
        context.Device.OemName ??= DeviceInfo.Current.Manufacturer.ToString();
        context.Device.Type ??= $"{DeviceInfo.Current.DeviceType} {DeviceInfo.Current.Idiom}";
        context.Device.NetworkType ??= Connectivity.Current.NetworkAccess.ToString();
    }

    private static void RegisterUnhandledExceptions()
    {
        // This is a hack, purpose is only to show it is possible
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {

            string page = Shell.Current?.CurrentPage?.GetType().Name;
            string viewmodel = Shell.Current?.CurrentPage?.BindingContext?.GetType().Name;

            Preferences.Set("Exception", $"{page} {viewmodel} =>  {(e.ExceptionObject as Exception).Message} {(e.ExceptionObject as Exception)?.InnerException} {(e.ExceptionObject as Exception)}");
        };

        AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
        {
            if (args.Exception.Message.Contains("canceled") || 
                args.Exception.Message.Contains("supported")||
                args.Exception.Message.Contains("Failed to perform")||
                !string.IsNullOrEmpty(Preferences.Get("Exception","")))
                return;

            string page = Shell.Current.CurrentPage?.GetType().Name;
            string viewmodel = Shell.Current.CurrentPage?.BindingContext?.GetType().Name;
            Preferences.Set("Exception", $"{page} {viewmodel} => {args.Exception.Message} {args.Exception?.InnerException}  {args.Exception}");
        };
    }

    private static TelemetrySettings Settings()
    {
        var a = typeof(Globals).GetTypeInfo().Assembly;
        var s = a.GetManifestResourceStream($"{a.GetName().Name}.appsettings.json");

        var config = new ConfigurationBuilder().AddJsonStream(s).Build();
        TelemetrySettings settings = config.GetRequiredSection(nameof(Settings)).Get<TelemetrySettings>();
        return settings;
    }
}