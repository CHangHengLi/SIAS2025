using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Windows;

namespace _2025毕业设计.Common
{
    /// <summary>
    /// 数据库连接字符串管理工具类
    /// </summary>
    public static class DbConnectionManager
    {
        // 存储原始密码的文件路径（相对于程序执行目录）
        private const string PasswordFile = "dbpass.key";
        
        // 添加缓存，避免重复解密
        private static string _cachedOriginalPassword;
        private static string _cachedConnectionString;
        private static string _cachedEncryptedPassword;
        
        /// <summary>
        /// 获取原始密码
        /// </summary>
        private static string GetOriginalPassword()
        {
            // 如果有缓存，直接返回
            if (!string.IsNullOrEmpty(_cachedOriginalPassword))
            {
                return _cachedOriginalPassword;
            }
            
            // 如果密码文件存在，则从文件读取
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(exePath, PasswordFile);
            
            if (File.Exists(filePath))
            {
                try
                {
                    _cachedOriginalPassword = File.ReadAllText(filePath);
                    return _cachedOriginalPassword;
                }
                catch
                {
                    // 如果读取失败，返回默认密码
                    _cachedOriginalPassword = "123456";
                    return _cachedOriginalPassword;
                }
            }
            
            // 如果文件不存在，返回默认密码
            _cachedOriginalPassword = "123456";
            return _cachedOriginalPassword;
        }
        
        /// <summary>
        /// 保存原始密码到文件
        /// </summary>
        private static void SaveOriginalPassword(string password)
        {
            try
            {
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(exePath, PasswordFile);
                File.WriteAllText(filePath, password);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存密码文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 将连接字符串中的密码替换为AES加密值
        /// </summary>
        /// <param name="showMessage">是否显示操作成功的消息框，默认为true</param>
        public static void ApplyAESToConnectionString(bool showMessage = true)
        {
            try
            {
                // 打开配置文件
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                ConnectionStringSettings setting = config.ConnectionStrings.ConnectionStrings["CoonStr"];
                
                if (setting != null)
                {
                    string connString = setting.ConnectionString;
                    
                    // 从连接字符串中提取原始密码
                    string originalPassword = ExtractPassword(connString);
                    
                    // 保存原始密码到文件
                    SaveOriginalPassword(originalPassword);
                    
                    // 使用AES加密密码
                    string encryptedPassword = EncryptAES(originalPassword);
                    
                    // 替换连接字符串中的密码
                    string newConnString = connString.Replace(
                        $"Password={originalPassword}", 
                        $"Password={encryptedPassword}");
                    
                    // 更新配置文件
                    setting.ConnectionString = newConnString;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("connectionStrings");
                    
                    // 只有在showMessage为true时才显示消息框
                    if (showMessage)
                    {
                        MessageBox.Show("数据库连接字符串密码已使用AES加密", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                // 异常情况下总是显示错误消息
                MessageBox.Show($"处理连接字符串时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 从连接字符串中提取密码
        /// </summary>
        private static string ExtractPassword(string connectionString)
        {
            // 查找Password=开始的部分
            int startIndex = connectionString.IndexOf("Password=");
            if (startIndex >= 0)
            {
                // 移动到Password=后面
                startIndex += "Password=".Length;
                
                // 查找下一个分号或字符串结束
                int endIndex = connectionString.IndexOf(';', startIndex);
                if (endIndex < 0)
                {
                    endIndex = connectionString.Length;
                }
                
                // 提取密码部分
                return connectionString.Substring(startIndex, endIndex - startIndex);
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// 获取用于数据库连接的实际连接字符串
        /// </summary>
        public static string GetActualConnectionString()
        {
            // 如果已有缓存的连接字符串，直接返回
            if (!string.IsNullOrEmpty(_cachedConnectionString))
            {
                return _cachedConnectionString;
            }
            
            string connString = ConfigurationManager.ConnectionStrings["CoonStr"].ConnectionString;
            string originalPassword = GetOriginalPassword();
            
            if (string.IsNullOrEmpty(originalPassword))
            {
                _cachedConnectionString = connString;
                return connString; // 如果无法获取原始密码，则直接返回
            }
            
            if (string.IsNullOrEmpty(_cachedEncryptedPassword))
            {
                _cachedEncryptedPassword = EncryptAES(originalPassword);
            }
            
            // 替换加密后的密码为原始密码
            if (connString.Contains($"Password={_cachedEncryptedPassword}"))
            {
                _cachedConnectionString = connString.Replace($"Password={_cachedEncryptedPassword}", $"Password={originalPassword}");
                return _cachedConnectionString;
            }
            
            _cachedConnectionString = connString;
            return _cachedConnectionString;
        }
        
        /// <summary>
        /// 使用AES加密字符串
        /// </summary>
        public static string EncryptAES(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;
                
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = SecurityKeys.GetEncryptionKey();
                aesAlg.IV = SecurityKeys.GetEncryptionIV();
                
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        byte[] encrypted = msEncrypt.ToArray();
                        
                        // 将加密后的字节数组转换为Base64字符串
                        return Convert.ToBase64String(encrypted);
                    }
                }
            }
        }
        
        /// <summary>
        /// 使用AES解密字符串
        /// </summary>
        public static string DecryptAES(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;
                
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = SecurityKeys.GetEncryptionKey();
                    aesAlg.IV = SecurityKeys.GetEncryptionIV();
                    
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    
                    using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch
            {
                // 如果解密失败，返回原始文本
                return cipherText;
            }
        }
        
        // 保留MD5方法用于向后兼容
        public static string CreateMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                
                return sb.ToString();
            }
        }
    }
} 