using System.Collections.Generic;
using System.Linq;
using TradingExpanded.Models;

namespace TradingExpanded.Repositories
{
    /// <summary>
    /// Courier varlıkları için repository uygulaması
    /// </summary>
    public class CourierRepository : BaseRepository<Courier>
    {
        /// <summary>
        /// Boş constructor
        /// </summary>
        public CourierRepository() : base() { }
        
        /// <summary>
        /// Mevcut varlıklarla constructor
        /// </summary>
        public CourierRepository(Dictionary<string, Courier> couriers) : base(couriers) { }
        
        /// <summary>
        /// Courier'ın ID'sini döndürür
        /// </summary>
        protected override string GetEntityId(Courier entity)
        {
            return entity?.Id;
        }
        
        /// <summary>
        /// Aktif kuryeleri getirir
        /// </summary>
        public IEnumerable<Courier> GetActiveCouriers()
        {
            return _entities.Values
                .Where(courier => !courier.HasReturnedInfo)
                .ToList();
        }
        
        /// <summary>
        /// Tamamlanmış kuryeleri getirir
        /// </summary>
        public IEnumerable<Courier> GetCompletedCouriers()
        {
            return _entities.Values
                .Where(courier => courier.HasReturnedInfo)
                .ToList();
        }
    }
} 