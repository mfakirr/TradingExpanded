using System;
using System.Collections.Generic;

namespace TradingExpanded.Services
{
    /// <summary>
    /// TradingExpanded için özel servis sağlayıcı arayüzü
    /// System.IServiceProvider ile karışıklık olmaması için tam ad kullanın
    /// </summary>
    public interface ITradingExpandedServiceProvider
    {
        /// <summary>
        /// Belirtilen tipte bir hizmet al
        /// </summary>
        /// <typeparam name="T">Hizmet tipi</typeparam>
        /// <returns>İstenen hizmetin örneği veya null</returns>
        T GetService<T>() where T : class;
        
        /// <summary>
        /// Belirtilen tipte bir hizmetin kayıtlı olup olmadığını kontrol et
        /// </summary>
        /// <typeparam name="T">Kontrol edilecek hizmet tipi</typeparam>
        /// <returns>Hizmet mevcutsa true</returns>
        bool HasService<T>() where T : class;
        
        /// <summary>
        /// Yeni bir hizmet ekle
        /// </summary>
        /// <typeparam name="T">Hizmet tipi</typeparam>
        /// <param name="implementation">Hizmet uygulaması</param>
        void AddService<T>(T implementation) where T : class;
        
        /// <summary>
        /// Kayıtlı tüm hizmetleri listele
        /// </summary>
        /// <returns>Kayıtlı hizmetlerin listesi</returns>
        IEnumerable<Type> GetRegisteredServices();
    }
} 