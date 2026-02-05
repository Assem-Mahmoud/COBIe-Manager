using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using COBIeManager.Shared.DependencyInjection;
using COBIeManager.Shared.Logging;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Services;
using System;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace COBIeManager
{
    /// <summary>
    /// Revit External Application - Template for new plugin features
    /// </summary>
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            try
            {
                // Initialize Dependency Injection Container
                InitializeDependencyInjection();
            }
            catch (InvalidOperationException ex)
            {
                // DI initialization failed - cannot proceed
                System.Windows.MessageBox.Show(
                    $"Plugin startup failed because Dependency Injection could not be initialized.\n\n" +
                    $"Error: {ex.InnerException?.Message ?? ex.Message}\n\n" +
                    $"Please check the logs at: %APPDATA%\\COBIeManager\\Logs\\",
                    "Plugin Startup Failed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return Result.Failed;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Unexpected error during plugin startup: {ex.Message}",
                    "Plugin Startup Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return Result.Failed;
            }

            try
            {
                // Create ribbon tab
                string tabName = "COBIe Manager";
                try { app.CreateRibbonTab(tabName); }
                catch { }

                RibbonPanel panel = app.CreateRibbonPanel(tabName, "Tools");

                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                // TODO: Add your feature buttons here
                // Example:
                // PushButtonData buttonData = new PushButtonData(
                //     "MyFeatureBtn",
                //     "My Feature",
                //     assemblyPath,
                //     "RevitCadConverter.Features.MyFeature.Commands.MyFeatureCommand");
                //
                // PushButton button = panel.AddItem(buttonData) as PushButton;
                // button.ToolTip = "Description of my feature";

                // COBie Parameters button
                PushButtonData cobieParamsButtonData = new PushButtonData(
                    "CobieParametersBtn",
                    "COBie Parameters",
                    assemblyPath,
                    "COBIeManager.Features.CobieParameters.Commands.CobieParametersCommand");

                PushButton cobieParamsButton = panel.AddItem(cobieParamsButtonData) as PushButton;
                cobieParamsButton.ToolTip = "Manage COBie parameters from Autodesk Platform Services";

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to create ribbon UI: {ex.Message}",
                    "UI Initialization Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return Result.Failed;
            }
        }

        private void InitializeDependencyInjection()
        {
            ILogger logger = null;
            try
            {
                // Create and register logger
                logger = new FileLogger();
                logger.Info("=== Application Startup: Initializing Dependency Injection ===");

                // Create service collection
                var services = new ServiceCollection();

                // Register core services
                logger.Info("Registering ILogger singleton...");
                services.RegisterSingleton<ILogger>(logger);

                logger.Info("Registering IUnitConversionService singleton...");
                services.RegisterSingleton<IUnitConversionService>(new UnitConversionService(logger));

          

                logger.Info("Registering IWarningSuppressionService singleton...");
                services.RegisterSingleton<IWarningSuppressionService>(new WarningSuppressionService(logger));

                // COBie Parameters services
                logger.Info("Registering IApsBridgeClient singleton...");
                services.AddSingleton<Shared.Interfaces.IApsBridgeClient>(sp => new Shared.APS.ApsBridgeClient());

                logger.Info("Registering ApsBridgeProcessService singleton...");
                services.AddSingleton<Shared.Services.ApsBridgeProcessService>(sp =>
                {
                    var bridgeClient = sp.GetService<Shared.Interfaces.IApsBridgeClient>();
                    return new Shared.Services.ApsBridgeProcessService(bridgeClient);
                });

                logger.Info("Registering ParameterCacheService singleton...");
                services.RegisterSingleton(new Shared.Services.ParameterCacheService());


                logger.Info("Building service provider...");
                var serviceProvider = services.BuildServiceProvider();

                logger.Info("Initializing ServiceLocator...");
                ServiceLocator.Initialize(serviceProvider);

                logger.Info("✓ Dependency Injection initialization successful");
                logger.Info("✓ All services registered and available");
                logger.Info("✓ ServiceLocator ready for use");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to initialize services: {ex.Message}\n\n" +
                    $"Stack Trace: {ex.StackTrace}\n\n" +
                    $"Check logs at: %APPDATA%\\COBIeManager\\Logs\\";

                if (logger != null)
                {
                    logger.Error($"CRITICAL: Failed to initialize Dependency Injection", ex);
                    logger.Error($"Exception Type: {ex.GetType().Name}");
                    logger.Error($"Exception Message: {ex.Message}");
                    logger.Error($"Stack Trace: {ex.StackTrace}");
                }

                System.Windows.MessageBox.Show(
                    errorMessage,
                    "Critical Initialization Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);

                throw new InvalidOperationException("DI initialization failed - ServiceLocator not initialized", ex);
            }
        }

        public Result OnShutdown(UIControlledApplication app)
        {
            return Result.Succeeded;
        }
    }
}
