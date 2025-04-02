namespace TradingExpanded.Helpers
{
    /// <summary>
    /// SaveableTypeDefiner için kullanılan tip ID'lerini içeren sabitler sınıfı
    /// </summary>
    public static class TradingExpandedTypeIds
    {
        // Ana iş nesneleri
        public const int WholesaleShop = 1;
        public const int WholesaleBuyOrder = 11;
        public const int WholesaleSellOrder = 12;
        
        public const int WholesaleEmployee = 2;
        public const int TradeCaravan = 3;
        public const int TradeRoute = 4;
        public const int Courier = 5;
        public const int MerchantRelation = 6;
        public const int TradeAgreement = 7;
        
        // Analiz ve istatistik nesneleri
        public const int InventoryTracker = 8;
        public const int ItemStats = 9;
        public const int PriceHistory = 10;
        public const int PriceDataPoint = 13;
        
        // İç sınıflar için ID'ler
        public const int PriceTrackerFiyatKaydi = 41;
        public const int PriceTrackerIslemKaydi = 42;
    }
} 