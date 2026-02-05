using System;

namespace COBIeManager.Shared.DependencyInjection
{
    /// <summary>
    /// Service provider interface for dependency injection.
    /// </summary>
    public interface IServiceProvider
    {
        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        object GetService(Type serviceType);
    }

    /// <summary>
    /// Extension methods for IServiceProvider.
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        public static T GetService<T>(this IServiceProvider provider) where T : class
        {
            return (T)provider.GetService(typeof(T));
        }
    }
}
