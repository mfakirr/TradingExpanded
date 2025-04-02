using System.Collections.Generic;

namespace TradingExpanded.Repositories
{
    /// <summary>
    /// Repository deseni için genel arabirim
    /// </summary>
    /// <typeparam name="T">Entity türü</typeparam>
    public interface IRepository<T>
    {
        /// <summary>
        /// ID'ye göre varlığı getirir
        /// </summary>
        /// <param name="id">Varlık ID'si</param>
        /// <returns>Bulunan varlık veya null</returns>
        T GetById(string id);
        
        /// <summary>
        /// Tüm varlıkları getirir
        /// </summary>
        /// <returns>Varlıkların listesi</returns>
        IEnumerable<T> GetAll();
        
        /// <summary>
        /// Varlık ekler veya günceller
        /// </summary>
        /// <param name="entity">Eklenecek veya güncellenecek varlık</param>
        void AddOrUpdate(T entity);
        
        /// <summary>
        /// Varlığı kaldırır
        /// </summary>
        /// <param name="id">Kaldırılacak varlığın ID'si</param>
        /// <returns>İşlemin başarılı olup olmadığı</returns>
        bool Remove(string id);
        
        /// <summary>
        /// Belirli bir koşulu karşılayan varlıkların sayısı
        /// </summary>
        /// <param name="predicate">Koşul</param>
        /// <returns>Varlık sayısı</returns>
        int Count(System.Predicate<T> predicate = null);
    }
} 