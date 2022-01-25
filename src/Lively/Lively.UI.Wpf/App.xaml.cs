﻿using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.UI.Wpf.Factories;
using Lively.UI.Wpf.Helpers;
using Lively.UI.Wpf.ViewModels;
using Lively.UI.Wpf.Views;
using Lively.UI.Wpf.Views.SetupWizard;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Lively.UI.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance for the current application instance.
        /// </summary>
        public static IServiceProvider Services
        {
            get
            {
                IServiceProvider serviceProvider = ((App)Current)._serviceProvider;
                return serviceProvider ?? throw new InvalidOperationException("The service provider is not initialized");
            }
        }

        public App()
        {
            _serviceProvider = ConfigureServices();
            //Logger writes to console(stdout) only for now.
            SetupUnhandledExceptionLogging();
            var userSettings = Services.GetRequiredService<IUserSettingsClient>();

            try
            {
               var lang = userSettings.Settings.Language;
               Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(lang);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            Logger.Info("Initialization complete.");

            if (userSettings.Settings.IsFirstRun)
            {
                InitializeMainWindowHidden();
                var setupWizard = Services.GetRequiredService<SetupView>();
                setupWizard.Show();
            }
            else
            {
                Services.GetRequiredService<MainWindow>().Show();
            }
        }

        private void InitializeMainWindowHidden()
        {
            var appWindow = Services.GetRequiredService<MainWindow>();
            var minWidth = appWindow.MinWidth;
            var minHeight = appWindow.MinHeight;
            var width = appWindow.Width;
            var height = appWindow.Height;
            appWindow.MinHeight = appWindow.MinWidth = appWindow.Width = appWindow.Height = 1;
            appWindow.Show();
            appWindow.Hide();
            appWindow.MinWidth = minWidth;
            appWindow.MinHeight = minHeight;
            appWindow.Width = width;
            appWindow.Height = height;
        }

        private IServiceProvider ConfigureServices()
        {
            var provider = new ServiceCollection()
                //singleton
                .AddSingleton<IDesktopCoreClient, WinDesktopCoreClient>()
                .AddSingleton<IUserSettingsClient, UserSettingsClient>()
                .AddSingleton<IDisplayManagerClient, DisplayManagerClient>()
                .AddSingleton<ICommandsClient, CommandsClient>()
                .AddSingleton<IAppUpdaterService, GithubUpdaterService>()
                .AddSingleton<MainWindow>()
                .AddSingleton<LibraryViewModel>() //Library items are stored..
                .AddSingleton<SettingsViewModel>() //Some events..
                //transient
                .AddTransient<SetupView>()
                .AddTransient<AddWallpaperViewModel>()
                .AddTransient<HelpViewModel>()
                .AddTransient<AboutViewModel>()
                .AddTransient<LibraryUtil>()
                .AddTransient<ScreenLayoutViewModel>()
                .AddTransient<ApplicationRulesViewModel>()
                .AddTransient<IApplicationsRulesFactory, ApplicationsRulesFactory>()
                .BuildServiceProvider();

            return provider;
        }

        public static void ShutDown()
        {
            try
            {
                ((ServiceProvider)App.Services)?.Dispose();
            }
            catch (InvalidOperationException) { /* not initialised */ }
            //Shutdown needs to be called from dispatcher..
            Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
        }

        private void SetupUnhandledExceptionLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            Dispatcher.UnhandledException += (s, e) =>
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) =>
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            Logger.Error(exception);
        }
    }
}
