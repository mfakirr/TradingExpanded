using System;

namespace TradingExpanded.Helpers
{
    /// <summary>
    /// SaveSystem için kullanılan sabitleri içeren yardımcı sınıf
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// SaveSystem için temel ID değeri.
        /// </summary>
        public const int SaveBaseId = 2873510; // Rasgele benzersiz bir sayı
        
        // İç sınıflar için sabit değerler
        public const int FiyatKaydiId = 80001;
        public const int IslemKaydiId = 80002;
        
        // Temel sabitler
        public const int BaseShopMaintenanceCost = 500;
    }
    
    /// <summary>
    /// ID oluşturma işlemleri için yardımcı sınıf
    /// </summary>
    public static class IdGenerator
    {
        /// <summary>
        /// Benzersiz ID oluşturur
        /// </summary>
        /// <returns>Benzersiz GUID değeri</returns>
        public static string GenerateUniqueId()
        {
            return Guid.NewGuid().ToString();
        }
    }
} 