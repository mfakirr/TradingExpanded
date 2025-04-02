using System;
using System.Collections.Generic;
using System.Linq;

namespace TradingExpanded.Repositories
{
    /// <summary>
    /// IRepository arabiriminin temel uygulaması
    /// </summary>
    /// <typeparam name="T">Yönetilecek entity türü</typeparam>
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly Dictionary<string, T> _entities;
        
        /// <summary>
        /// Constructor
        /// </summary>
        protected BaseRepository()
        {
            _entities = new Dictionary<string, T>();
        }
        
        /// <summary>
        /// Constructor, mevcut varlıklarla
        /// </summary>
        protected BaseRepository(Dictionary<string, T> entities)
        {
            _entities = entities ?? new Dictionary<string, T>();
        }
        
        /// <summary>
        /// Varlığın ID'sini alır
        /// </summary>
        protected abstract string GetEntityId(T entity);
        
        /// <summary>
        /// ID'ye göre varlığı getirir
        /// </summary>
        public virtual T GetById(string id)
        {
            if (string.IsNullOrEmpty(id) || !_entities.ContainsKey(id))
                return null;
                
            return _entities[id];
        }
        
        /// <summary>
        /// Tüm varlıkları getirir
        /// </summary>
        public virtual IEnumerable<T> GetAll()
        {
            return _entities.Values;
        }
        
        /// <summary>
        /// Varlık ekler veya günceller
        /// </summary>
        public virtual void AddOrUpdate(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
                
            string id = GetEntityId(entity);
            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException("Entity ID cannot be null or empty");
                
            _entities[id] = entity;
        }
        
        /// <summary>
        /// Varlığı kaldırır
        /// </summary>
        public virtual bool Remove(string id)
        {
            if (string.IsNullOrEmpty(id) || !_entities.ContainsKey(id))
                return false;
                
            return _entities.Remove(id);
        }
        
        /// <summary>
        /// Belirli bir koşulu karşılayan varlıkların sayısı
        /// </summary>
        public virtual int Count(Predicate<T> predicate = null)
        {
            if (predicate == null)
                return _entities.Count;
                
            return _entities.Values.Count(entity => predicate(entity));
        }
    }
} 