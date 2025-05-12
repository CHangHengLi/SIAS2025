using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace _2025毕业设计.Common
{
    /// <summary>
    /// 安全密钥管理类，负责AES加密密钥和向量的生成与存储
    /// </summary>
    public static class SecurityKeys
    {
        // 密钥文件
        private const string KeyFile = "encryption.key";
        private const string IvFile = "encryption.iv";
        
        // 缓存密钥和向量
        private static byte[] _cachedKey;
        private static byte[] _cachedIV;
        
        /// <summary>
        /// 获取AES加密密钥
        /// </summary>
        public static byte[] GetEncryptionKey()
        {
            // 有缓存时直接返回
            if (_cachedKey != null && _cachedKey.Length > 0)
            {
                return _cachedKey;
            }
            
            _cachedKey = GetOrCreateKey(KeyFile);
            return _cachedKey;
        }
        
        /// <summary>
        /// 获取AES加密向量
        /// </summary>
        public static byte[] GetEncryptionIV()
        {
            // 有缓存时直接返回
            if (_cachedIV != null && _cachedIV.Length > 0)
            {
                return _cachedIV;
            }
            
            _cachedIV = GetOrCreateKey(IvFile);
            return _cachedIV;
        }
        
        /// <summary>
        /// 获取或创建密钥
        /// </summary>
        private static byte[] GetOrCreateKey(string fileName)
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(exePath, fileName);
            
            if (File.Exists(filePath))
            {
                try
                {
                    // 从文件读取密钥
                    string base64Key = File.ReadAllText(filePath);
                    return Convert.FromBase64String(base64Key);
                }
                catch (Exception ex)
                {
                    // 减少消息框弹出，只在调试模式下显示
                    #if DEBUG
                    MessageBox.Show($"读取加密密钥时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    #endif
                    
                    // 如果读取失败，生成新密钥
                    return GenerateAndSaveKey(filePath);
                }
            }
            else
            {
                // 如果文件不存在，生成新密钥
                return GenerateAndSaveKey(filePath);
            }
        }
        
        /// <summary>
        /// 生成并保存新密钥
        /// </summary>
        private static byte[] GenerateAndSaveKey(string filePath)
        {
            try
            {
                // 生成16字节的随机密钥（AES-128）
                byte[] key = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(key);
                }
                
                // 将密钥保存到文件（Base64编码）
                string base64Key = Convert.ToBase64String(key);
                File.WriteAllText(filePath, base64Key);
                
                return key;
            }
            catch (Exception ex)
            {
                // 减少消息框弹出，只在调试模式下显示
                #if DEBUG
                MessageBox.Show($"生成加密密钥时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                #endif
                
                // 如果生成失败，返回默认密钥
                return Encoding.UTF8.GetBytes("A8C7D9E5F3B1G2H6");
            }
        }
        
        /// <summary>
        /// 重新生成所有加密密钥（谨慎使用，会使现有加密数据无法解密）
        /// </summary>
        public static void RegenerateAllKeys()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            
            // 删除现有密钥文件
            string keyPath = Path.Combine(exePath, KeyFile);
            string ivPath = Path.Combine(exePath, IvFile);
            
            try
            {
                if (File.Exists(keyPath)) File.Delete(keyPath);
                if (File.Exists(ivPath)) File.Delete(ivPath);
                
                // 清除缓存
                _cachedKey = null;
                _cachedIV = null;
                
                // 重新生成密钥
                GetEncryptionKey();
                GetEncryptionIV();
                
                MessageBox.Show("加密密钥已重新生成。注意：现有加密数据将无法解密。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重新生成密钥时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 