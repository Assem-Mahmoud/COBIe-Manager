using COBIeManager.Shared.Adapters;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using COBIeManager.Shared.Services;
using System;
using System.Collections.Generic;

namespace COBIeManager.Shared.DependencyInjection
{
    /// <summary>
    /// Simple service collection for dependency injection.
    /// </summary>
    public class ServiceCollection
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services = new();

        /// <summary>
        /// Registers a singleton service instance.
        /// </summary>
        public void RegisterSingleton<TInterface>(TInterface implementation)
            where TInterface : class
        {
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                Implementation = implementation,
                Lifetime = ServiceLifetime.Singleton
            };
        }

        /// <summary>
        /// Registers a singleton service.
        /// </summary>
        public void AddSingleton<TInterface, TImplementation>(TImplementation implementation)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                Implementation = implementation,
                Lifetime = ServiceLifetime.Singleton
            };
        }

        /// <summary>
        /// Registers a singleton service with factory.
        /// </summary>
        public void AddSingleton<TInterface>(Func<IServiceProvider, TInterface> factory)
            where TInterface : class
        {
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                Factory = factory,
                Lifetime = ServiceLifetime.Singleton
            };
        }

        /// <summary>
        /// Registers a transient service.
        /// </summary>
        public void AddTransient<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                ImplementationType = typeof(TImplementation),
                Lifetime = ServiceLifetime.Transient
            };
        }

        /// <summary>
        /// Resolves a service of the specified type.
        /// </summary>
        public T Resolve<T>() where T : class
        {
            var provider = new ServiceProvider(_services);
            return provider.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Builds the service provider.
        /// </summary>
        public IServiceProvider BuildServiceProvider()
        {
            return new ServiceProvider(_services);
        }
    }

    /// <summary>
    /// Service descriptor.
    /// </summary>
    internal class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public object Implementation { get; set; }
        public Delegate Factory { get; set; }
        public ServiceLifetime Lifetime { get; set; }
    }

    /// <summary>
    /// Service lifetime enum.
    /// </summary>
    public enum ServiceLifetime
    {
        Transient,
        Singleton
    }

    /// <summary>
    /// Service provider implementation.
    /// </summary>
    internal class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services;
        private readonly Dictionary<Type, object> _singletons = new();

        public ServiceProvider(Dictionary<Type, ServiceDescriptor> services)
        {
            _services = services;
        }

        public object GetService(Type serviceType)
        {
            if (!_services.TryGetValue(serviceType, out var descriptor))
            {
                throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
            }

            // Return singleton if already created
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                if (_singletons.TryGetValue(serviceType, out var singleton))
                {
                    return singleton;
                }

                // Create and cache singleton
                object instance;
                if (descriptor.Implementation != null)
                {
                    instance = descriptor.Implementation;
                }
                else if (descriptor.Factory != null)
                {
                    instance = descriptor.Factory.DynamicInvoke(this);
                }
                else
                {
                    instance = Activator.CreateInstance(descriptor.ImplementationType);
                }

                _singletons[serviceType] = instance;
                return instance;
            }

            // Create new instance for transient
            if (descriptor.Implementation != null)
            {
                return descriptor.Implementation;
            }

            if (descriptor.Factory != null)
            {
                return descriptor.Factory.DynamicInvoke(this);
            }

            return Activator.CreateInstance(descriptor.ImplementationType);
        }
    }
}
