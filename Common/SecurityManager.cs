using System.Windows;

namespace _2025毕业设计.Common
{
    /// <summary>
    /// 安全管理类，提供加密相关的管理功能
    /// </summary>
    public static class SecurityManager
    {
        /// <summary>
        /// 重新加密连接字符串
        /// </summary>
        public static void ReencryptConnectionString()
        {
            try
            {
                DbConnectionManager.ApplyAESToConnectionString(true);
                MessageBox.Show("连接字符串已重新加密", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重新加密连接字符串时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 重新生成加密密钥
        /// </summary>
        public static void RegenerateKeys()
        {
            MessageBoxResult result = MessageBox.Show(
                "警告：重新生成密钥将导致所有现有加密数据无法解密。确定要继续吗？",
                "重新生成密钥",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 重新生成密钥
                    SecurityKeys.RegenerateAllKeys();
                    
                    // 使用新密钥重新加密连接字符串，但不显示加密成功的提示
                    DbConnectionManager.ApplyAESToConnectionString(false);
                    
                    MessageBox.Show("密钥已重新生成，连接字符串已使用新密钥加密", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"重新生成密钥时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        /// <summary>
        /// 检查加密状态
        /// </summary>
        public static void CheckEncryptionStatus()
        {
            try
            {
                // 检查密钥文件是否存在
                bool keysExist = System.IO.File.Exists(
                    System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, 
                        "encryption.key")) &&
                    System.IO.File.Exists(
                        System.IO.Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory, 
                            "encryption.iv"));
                
                // 检查密码文件是否存在
                bool passwordFileExists = System.IO.File.Exists(
                    System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, 
                        "dbpass.key"));
                
                string status = $"加密状态:\n\n" +
                                $"加密密钥: {(keysExist ? "已生成" : "未生成")}\n" +
                                $"密码文件: {(passwordFileExists ? "已存在" : "未存在")}\n";
                
                MessageBox.Show(status, "加密状态", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"检查加密状态时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 