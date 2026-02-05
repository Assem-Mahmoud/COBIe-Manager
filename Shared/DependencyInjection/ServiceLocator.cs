using System;

namespace COBIeManager.Shared.DependencyInjection
{
    /// <summary>
    /// Service locator for accessing services globally.
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes the service locator with a service provider.
        /// </summary>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException(
                    "ServiceLocator has not been initialized. Call ServiceLocator.Initialize() first.");
            }

            var service = _serviceProvider.GetService<T>();
            if (service == null)
            {
                throw new InvalidOperationException(
                    $"Service of type {typeof(T).Name} is not registered.");
            }

            return service;
        }

        /// <summary>
        /// Tries to get a service of the specified type. Returns null if not found.
        /// </summary>
        public static T? TryGetService<T>() where T : class
        {
            if (_serviceProvider == null)
            {
                return null;
            }

            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// Gets the current service provider.
        /// </summary>
        public static IServiceProvider Current => _serviceProvider;
    }
}
