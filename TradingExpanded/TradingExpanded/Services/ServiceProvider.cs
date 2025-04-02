using System;
using System.Collections.Generic;
using System.Linq;
using TradingExpanded.Utils;

namespace TradingExpanded.Services
{
    /// <summary>
    /// TradingExpanded için basit servis sağlayıcı uygulaması
    /// </summary>
    public class ServiceProvider : ITradingExpandedServiceProvider
    {
        private readonly Dictionary<Type, object> _services;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public ServiceProvider()
        {
            _services = new Dictionary<Type, object>();
            LogManager.Instance.WriteDebug("ServiceProvider oluşturuldu");
        }
        
        /// <summary>
        /// Servisleri ekler
        /// </summary>
        /// <param name="services">Servis koleksiyonu</param>
        public ServiceProvider(Dictionary<Type, object> services)
        {
            _services = services ?? new Dictionary<Type, object>();
        }
        
        /// <summary>
        /// Servisi ekler
        /// </summary>
        /// <typeparam name="T">Servis arabirimi</typeparam>
        /// <param name="service">Servis uygulaması</param>
        public void RegisterService<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
                
            _services[typeof(T)] = service;
        }
        
        /// <summary>
        /// Servis tipini ekler
        /// </summary>
        /// <typeparam name="TInterface">Servis arabirimi</typeparam>
        /// <typeparam name="TImplementation">Servis uygulaması</typeparam>
        /// <param name="parameters">Yapıcı parametreleri</param>
        public void RegisterType<TInterface, TImplementation>(params object[] parameters)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            Type implementationType = typeof(TImplementation);
            TImplementation implementation = (TImplementation)Activator.CreateInstance(implementationType, parameters);
            
            _services[typeof(TInterface)] = implementation;
        }
        
        /// <summary>
        /// Belirtilen tipte bir hizmet ekler
        /// </summary>
        public void AddService<T>(T implementation) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                _services[type] = implementation;
                LogManager.Instance.WriteDebug($"{type.Name} servisi güncellendi");
            }
            else
            {
                _services.Add(type, implementation);
                LogManager.Instance.WriteDebug($"{type.Name} servisi eklendi");
            }
        }
        
        /// <summary>
        /// Belirtilen tipte bir hizmet olup olmadığını kontrol eder
        /// </summary>
        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Belirtilen tipte bir hizmet alır
        /// </summary>
        public T GetService<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            
            LogManager.Instance.WriteWarning($"{type.Name} servisi bulunamadı");
            return null;
        }
        
        /// <summary>
        /// Tüm kayıtlı servislerin tiplerini döndürür
        /// </summary>
        public IEnumerable<Type> GetRegisteredServices()
        {
            return _services.Keys.ToList();
        }
    }
} 