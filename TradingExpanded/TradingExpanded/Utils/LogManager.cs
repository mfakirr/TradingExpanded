using System;
using System.IO;
using System.Text;

namespace TradingExpanded.Utils
{
    /// <summary>
    /// Loglama fonksiyonları için yardımcı sınıf
    /// </summary>
    public class LogManager
    {
        private static LogManager _instance;
        private readonly StringBuilder _logCache;
        private readonly object _lockObject = new object();
        private readonly string _logPath;
        private bool _isDebugEnabled;
        
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static LogManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LogManager();
                return _instance;
            }
        }
        
        /// <summary>
        /// Debug logları aktif/pasif yapar
        /// </summary>
        public bool IsDebugEnabled
        {
            get => _isDebugEnabled;
            set => _isDebugEnabled = value;
        }
        
        /// <summary>
        /// Constructor
        /// </summary>
        private LogManager()
        {
            _logCache = new StringBuilder();
            _logPath = Path.Combine(GetModPath(), "TradingExpanded.log");
            _isDebugEnabled = false;
            
            // Log başlangıcı
            WriteInfo($"=== TradingExpanded Log Started: {DateTime.Now} ===");
        }
        
        /// <summary>
        /// Mod klasörünü tespit eder
        /// </summary>
        private string GetModPath()
        {
            // Varsayılan olarak Logs klasörüne yaz
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord", 
                "Logs");
                
            // Klasör yoksa oluştur
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);
                
            return basePath;
        }
        
        /// <summary>
        /// Log dosyasına yazar
        /// </summary>
        private void WriteToLog(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
                
            lock (_lockObject)
            {
                try
                {
                    _logCache.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                    
                    // Belirli bir boyuta ulaşınca dosyaya yaz ve temizle
                    if (_logCache.Length > 4096)
                        Flush();
                }
                catch (Exception)
                {
                    // Loglama hatası görmezden gelinir
                }
            }
        }
        
        /// <summary>
        /// Log önbelleğini dosyaya yazar
        /// </summary>
        public void Flush()
        {
            lock (_lockObject)
            {
                if (_logCache.Length > 0)
                {
                    try
                    {
                        File.AppendAllText(_logPath, _logCache.ToString());
                        _logCache.Clear();
                    }
                    catch (Exception)
                    {
                        // Dosya yazma hataları görmezden gelinir
                    }
                }
            }
        }
        
        /// <summary>
        /// Bilgi mesajı yazar
        /// </summary>
        public void WriteInfo(string message)
        {
            WriteToLog($"[INFO] {message}");
        }
        
        /// <summary>
        /// Hata mesajı yazar
        /// </summary>
        public void WriteError(string message, Exception ex = null)
        {
            if (ex != null)
                WriteToLog($"[ERROR] {message} - {ex.Message}\n{ex.StackTrace}");
            else
                WriteToLog($"[ERROR] {message}");
        }
        
        /// <summary>
        /// Uyarı mesajı yazar
        /// </summary>
        public void WriteWarning(string message)
        {
            WriteToLog($"[WARNING] {message}");
        }
        
        /// <summary>
        /// Debug mesajı yazar (sadece debug modu aktifse)
        /// </summary>
        public void WriteDebug(string message)
        {
            if (_isDebugEnabled)
                WriteToLog($"[DEBUG] {message}");
        }
    }
} 