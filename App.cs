using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Aps.Core.Interfaces;
using Aps.Core.Logging;
using Aps.Core.Services;
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

                // Add your feature buttons using the pattern below
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

                // Auto-Fill Parameters button
                PushButtonData fillParamsButtonData = new PushButtonData(
                    "ParameterFillBtn",
                    "Auto-Fill Parameters",
                    assemblyPath,
                    "COBIeManager.Features.ParameterFiller.Commands.ParameterFillCommand");

                PushButton fillParamsButton = panel.AddItem(fillParamsButtonData) as PushButton;
                fillParamsButton.ToolTip = "Auto-fill level and room parameters on model elements";

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

                // COBie Parameters services - Direct APS integration
                logger.Info("Registering FileApsLogger singleton...");
                var apsLogger = new FileApsLogger();
                services.RegisterSingleton<IApsLogger>(apsLogger);
                logger.Info($"APS Auth log file: {apsLogger.GetLogPath()}");

                logger.Info("Registering ApsAuthService singleton...");
                var authService = new ApsAuthService(apsLogger);
                services.RegisterSingleton<Aps.Core.Services.ApsAuthService>(authService);

                logger.Info("Registering ITokenStorage singleton...");
                services.RegisterSingleton<ITokenStorage>(new ApsTokenStorage());

                logger.Info("Registering ApsSessionManager singleton...");
                services.AddSingleton<ApsSessionManager>(sp =>
                {
                    var resolvedAuthService = sp.GetService<Aps.Core.Services.ApsAuthService>();
                    var tokenStorage = sp.GetService<ITokenStorage>();
                    var apsLoggerFromSp = sp.GetService<IApsLogger>();

                    if (resolvedAuthService == null)
                        throw new InvalidOperationException("Failed to resolve ApsAuthService during ApsSessionManager registration");
                    if (tokenStorage == null)
                        throw new InvalidOperationException("Failed to resolve ITokenStorage during ApsSessionManager registration");

                    return new ApsSessionManager(resolvedAuthService, tokenStorage, apsLoggerFromSp);
                });

                logger.Info("Registering ApsCategoryStorageService singleton...");
                services.AddSingleton<ApsCategoryStorageService>(sp =>
                {
                    var sessionManager = sp.GetService<ApsSessionManager>();
                    var apsLoggerFromSp = sp.GetService<IApsLogger>();

                    if (sessionManager == null)
                        throw new InvalidOperationException("Failed to resolve ApsSessionManager during ApsCategoryStorageService registration");

                    return new ApsCategoryStorageService(sessionManager, apsLoggerFromSp);
                });

                logger.Info("Registering ApsParametersService singleton...");
                services.AddSingleton<ApsParametersService>(sp =>
                {
                    var sessionManager = sp.GetService<ApsSessionManager>();
                    var apsLoggerFromSp = sp.GetService<IApsLogger>();
                    var categoryStorage = sp.GetService<ApsCategoryStorageService>();

                    if (sessionManager == null)
                        throw new InvalidOperationException("Failed to resolve ApsSessionManager during ApsParametersService registration");
                    if (apsLoggerFromSp == null)
                        throw new InvalidOperationException("Failed to resolve IApsLogger during ApsParametersService registration");
                    if (categoryStorage == null)
                        throw new InvalidOperationException("Failed to resolve ApsCategoryStorageService during ApsParametersService registration");

                    return new ApsParametersService(sessionManager, apsLoggerFromSp, categoryStorage);
                });

                logger.Info("Registering ParameterCacheService singleton...");
                services.RegisterSingleton(new Shared.Services.ParameterCacheService());

                logger.Info("Registering IParameterCreationService singleton...");
                services.RegisterSingleton<IParameterCreationService>(new Shared.Services.ParameterCreationService());

                logger.Info("Registering IParameterBindingService singleton...");
                services.RegisterSingleton<IParameterBindingService>(new Shared.Services.ParameterBindingService(logger));

                logger.Info("Registering IParameterConflictService singleton...");
                services.RegisterSingleton<IParameterConflictService>(new Shared.Services.ParameterConflictService());

                // ParameterFiller services
                logger.Info("Registering ILevelAssignmentService singleton...");
                services.RegisterSingleton<ILevelAssignmentService>(new LevelAssignmentService(logger));

                logger.Info("Registering IRoomAssignmentService singleton...");
                services.AddSingleton<IRoomAssignmentService>(sp =>
                {
                    var docLogger = sp.GetService<ILogger>();
                    return new RoomAssignmentService(docLogger);
                });

                logger.Info("Registering IBoxIdFillService singleton...");
                services.RegisterSingleton<IBoxIdFillService>(new BoxIdFillService(logger));

                logger.Info("Registering IRoomFillService singleton...");
                services.AddSingleton<IRoomFillService>(sp =>
                {
                    var roomService = sp.GetService<IRoomAssignmentService>();
                    if (roomService == null)
                        throw new InvalidOperationException("Failed to resolve IRoomAssignmentService during IRoomFillService registration");
                    return new RoomFillService(logger, roomService);
                });

                logger.Info("Registering IParameterFillService singleton...");
                services.AddSingleton<IParameterFillService>(sp =>
                {
                    var levelService = sp.GetService<ILevelAssignmentService>();
                    var roomService = sp.GetService<IRoomAssignmentService>();
                    var boxIdService = sp.GetService<IBoxIdFillService>();
                    var roomFillService = sp.GetService<IRoomFillService>();
                    if (levelService == null)
                        throw new InvalidOperationException("Failed to resolve ILevelAssignmentService during IParameterFillService registration");
                    if (roomService == null)
                        throw new InvalidOperationException("Failed to resolve IRoomAssignmentService during IParameterFillService registration");
                    if (boxIdService == null)
                        throw new InvalidOperationException("Failed to resolve IBoxIdFillService during IParameterFillService registration");
                    if (roomFillService == null)
                        throw new InvalidOperationException("Failed to resolve IRoomFillService during IParameterFillService registration");
                    return new ParameterFillService(logger, levelService, roomService, boxIdService, roomFillService);
                });

                logger.Info("Registering IProcessingLogger singleton...");
                services.AddSingleton<IProcessingLogger>(sp =>
                {
                    var spLogger = sp.GetService<ILogger>();
                    return new ProcessingLogger(spLogger);
                });

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
