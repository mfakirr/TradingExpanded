using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TradingExpanded.Models;

namespace TradingExpanded.Repositories
{
    /// <summary>
    /// WholesaleShop varlıkları için repository uygulaması
    /// </summary>
    public class WholesaleShopRepository : BaseRepository<WholesaleShop>
    {
        /// <summary>
        /// Boş constructor
        /// </summary>
        public WholesaleShopRepository() : base() { }
        
        /// <summary>
        /// Mevcut varlıklarla constructor
        /// </summary>
        public WholesaleShopRepository(Dictionary<string, WholesaleShop> shops) : base(shops) { }
        
        /// <summary>
        /// WholesaleShop'ın ID'sini döndürür
        /// </summary>
        protected override string GetEntityId(WholesaleShop entity)
        {
            return entity?.Id;
        }
        
        /// <summary>
        /// Belirli bir şehirdeki dükkanı getirir
        /// </summary>
        public WholesaleShop GetShopInTown(Town town)
        {
            if (town == null)
                return null;
                
            return _entities.Values
                .FirstOrDefault(shop => shop.Town == town && shop.IsActive);
        }
        
        /// <summary>
        /// Aktif oyuncu dükkanlarını getirir
        /// </summary>
        public IEnumerable<WholesaleShop> GetPlayerShops()
        {
            return _entities.Values
                .Where(shop => shop.IsActive)
                .ToList();
        }
        
        /// <summary>
        /// Dükkanı inaktif olarak işaretler (kapatır)
        /// </summary>
        public bool CloseShop(string id)
        {
            var shop = GetById(id);
            if (shop == null)
                return false;
                
            shop.IsActive = false;
            return true;
        }
    }
} 