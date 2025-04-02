using System;
using System.Collections.Generic;

namespace TradingExpanded.Services
{
    /// <summary>
    /// IServiceProvider oluşturmak için akıcı API builder
    /// </summary>
    public class ServiceProviderBuilder
    {
        private readonly Dictionary<Type, object> _services;
        private readonly Dictionary<Type, Func<object>> _factories;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ServiceProviderBuilder()
        {
            _services = new Dictionary<Type, object>();
            _factories = new Dictionary<Type, Func<object>>();
        }
        
        /// <summary>
        /// Singleton servis ekler
        /// </summary>
        /// <typeparam name="TInterface">Servis arabirimi</typeparam>
        /// <typeparam name="TImplementation">Servis uygulaması</typeparam>
        /// <returns>Builder</returns>
        public ServiceProviderBuilder AddSingleton<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            _services[typeof(TInterface)] = new TImplementation();
            return this;
        }
        
        /// <summary>
        /// Var olan bir singleton örneği ekler
        /// </summary>
        /// <typeparam name="T">Servis türü</typeparam>
        /// <param name="instance">Servis örneği</param>
        /// <returns>Builder</returns>
        public ServiceProviderBuilder AddSingleton<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance ?? throw new ArgumentNullException(nameof(instance));
            return this;
        }
        
        /// <summary>
        /// Her istek için yeni bir örnek oluşturan geçici servis ekler
        /// </summary>
        /// <typeparam name="TInterface">Servis arabirimi</typeparam>
        /// <typeparam name="TImplementation">Servis uygulaması</typeparam>
        /// <returns>Builder</returns>
        public ServiceProviderBuilder AddTransient<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            _factories[typeof(TInterface)] = () => new TImplementation();
            return this;
        }
        
        /// <summary>
        /// Özel fabrika metodu ile servis ekler
        /// </summary>
        /// <typeparam name="T">Servis türü</typeparam>
        /// <param name="factory">Fabrika metodu</param>
        /// <returns>Builder</returns>
        public ServiceProviderBuilder AddFactory<T>(Func<T> factory) where T : class
        {
            _factories[typeof(T)] = () => factory();
            return this;
        }
        
        /// <summary>
        /// ServiceProvider oluşturur
        /// </summary>
        /// <returns>Oluşturulan ITradingExpandedServiceProvider</returns>
        public ITradingExpandedServiceProvider Build()
        {
            // Final service dictionary'sini oluştur
            var services = new Dictionary<Type, object>(_services);
            
            // Fabrika metodlarından anlık örnekleri ekle
            foreach (var factory in _factories)
            {
                services[factory.Key] = factory.Value();
            }
            
            return new ServiceProvider(services);
        }
    }
} 