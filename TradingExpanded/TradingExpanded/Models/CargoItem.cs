using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Kargo tipleri
    /// </summary>
    public enum CargoType
    {
        /// <summary>
        /// Para taşıma
        /// </summary>
        Money,
        
        /// <summary>
        /// Mal taşıma
        /// </summary>
        Goods
    }
    
    /// <summary>
    /// Kuryenin taşıdığı kargo
    /// </summary>
    public class CargoItem
    {
        /// <summary>
        /// Kargonun tipi
        /// </summary>
        public CargoType Type { get; private set; }
        
        /// <summary>
        /// Kargonun miktarı
        /// </summary>
        public int Amount { get; private set; }
        
        /// <summary>
        /// Taşınan malın türü (para için null)
        /// </summary>
        public ItemObject Item { get; private set; }
        
        /// <summary>
        /// Para kargosu oluşturur
        /// </summary>
        public static CargoItem CreateMoneyCargo(int amount)
        {
            return new CargoItem
            {
                Type = CargoType.Money,
                Amount = amount,
                Item = null
            };
        }
        
        /// <summary>
        /// Mal kargosu oluşturur
        /// </summary>
        public static CargoItem CreateGoodsCargo(ItemObject item, int amount)
        {
            return new CargoItem
            {
                Type = CargoType.Goods,
                Amount = amount,
                Item = item
            };
        }
        
        /// <summary>
        /// Kargonun açıklamasını döndürür
        /// </summary>
        public override string ToString()
        {
            if (Type == CargoType.Money)
            {
                return $"{Amount} dinar";
            }
            else
            {
                string itemName = Item?.Name?.ToString() ?? "Belirsiz mal";
                return $"{Amount}x {itemName}";
            }
        }
    }
} 