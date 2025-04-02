using System.Collections.Generic;
using System.Linq;
using TradingExpanded.Models;

namespace TradingExpanded.Repositories
{
    /// <summary>
    /// TradeCaravan varlıkları için repository uygulaması
    /// </summary>
    public class TradeCaravanRepository : BaseRepository<TradeCaravan>
    {
        /// <summary>
        /// Boş constructor
        /// </summary>
        public TradeCaravanRepository() : base() { }
        
        /// <summary>
        /// Mevcut varlıklarla constructor
        /// </summary>
        public TradeCaravanRepository(Dictionary<string, TradeCaravan> caravans) : base(caravans) { }
        
        /// <summary>
        /// TradeCaravan'ın ID'sini döndürür
        /// </summary>
        protected override string GetEntityId(TradeCaravan entity)
        {
            return entity?.Id;
        }
        
        /// <summary>
        /// Aktif oyuncu kervanlarını getirir
        /// </summary>
        public IEnumerable<TradeCaravan> GetPlayerCaravans()
        {
            return _entities.Values
                .Where(caravan => caravan.AktifMi)
                .ToList();
        }
        
        /// <summary>
        /// Kervanı inaktif olarak işaretler (dağıtır)
        /// </summary>
        public bool DisbandCaravan(string id)
        {
            var caravan = GetById(id);
            if (caravan == null)
                return false;
                
            caravan.AktifMi = false;
            return true;
        }
    }
} 