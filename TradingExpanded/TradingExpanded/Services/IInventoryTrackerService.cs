using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TradingExpanded.Models;

namespace TradingExpanded.Services
{
    /// <summary>
    /// Envanter takibi ve fiyat analizi için servis arayüzü
    /// </summary>
    public interface IInventoryTrackerService
    {
        /// <summary>
        /// Tüm ürün istatistiklerini getirir
        /// </summary>
        /// <returns>Ürün istatistikleri</returns>
        Dictionary<ItemObject, ItemStats> GetAllItemStats();
        
        /// <summary>
        /// Bir ürünün istatistiklerini getirir
        /// </summary>
        /// <param name="item">Ürün</param>
        /// <returns>Ürün istatistikleri</returns>
        ItemStats GetItemStats(ItemObject item);
        
        /// <summary>
        /// Şehirdeki tüm ürünlerin fiyat geçmişini getirir
        /// </summary>
        /// <param name="town">Şehir</param>
        /// <returns>Ürün fiyat geçmişi</returns>
        Dictionary<ItemObject, PriceHistory> GetPriceHistoryInTown(Town town);
        
        /// <summary>
        /// Bir ürünün belirli bir şehirdeki fiyat geçmişini getirir
        /// </summary>
        /// <param name="item">Ürün</param>
        /// <param name="town">Şehir</param>
        /// <returns>Fiyat geçmişi</returns>
        PriceHistory GetPriceHistory(ItemObject item, Town town);
        
        /// <summary>
        /// Bir ürünün en karlı alım yeri
        /// </summary>
        /// <param name="item">Ürün</param>
        /// <returns>En ucuz şehir</returns>
        Town GetBestBuyLocation(ItemObject item);
        
        /// <summary>
        /// Bir ürünün en karlı satış yeri
        /// </summary>
        /// <param name="item">Ürün</param>
        /// <returns>En pahalı şehir</returns>
        Town GetBestSellLocation(ItemObject item);
        
        /// <summary>
        /// En karlı ürünü getirir
        /// </summary>
        /// <returns>En karlı ürün</returns>
        ItemObject GetMostProfitableItem();
        
        /// <summary>
        /// Alım-satım işlemi kaydeder
        /// </summary>
        /// <param name="item">Ürün</param>
        /// <param name="quantity">Miktar</param>
        /// <param name="price">Fiyat</param>
        /// <param name="town">Şehir</param>
        /// <param name="isPurchase">Alım işlemi mi?</param>
        void RecordTransaction(ItemObject item, int quantity, int price, Town town, bool isPurchase);
        
        /// <summary>
        /// Şehirdeki mevcut fiyatları kaydeder
        /// </summary>
        /// <param name="town">Şehir</param>
        void RecordCurrentPrices(Town town);
        
        /// <summary>
        /// Genel güncelleme işlemi
        /// </summary>
        void Update();
    }
} 