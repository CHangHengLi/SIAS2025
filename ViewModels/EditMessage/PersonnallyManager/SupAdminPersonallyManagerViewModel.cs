using _2025毕业设计.Common;
using _2025毕业设计.Context;
using _2025毕业设计.Models;
using _2025毕业设计.Services;
using _2025毕业设计.Converter;
using HandyControl.Controls;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ConverterImage = _2025毕业设计.Converter.ConVerterImage;

namespace _2025毕业设计.ViewModels.EditMessage.PersonnallyManager
{
    public class SupAdminPersonallyManagerViewModel : BindableBase, INavigationAware
    {
        #region 数据库上下文context
        private DataBaseContext context;
        #endregion

        #region 二进制图片存储ByteImage
        private byte[] byteImage;
        public byte[] ByteImage
        {
            get { return byteImage; }
            set { SetProperty(ref byteImage, value); }
        }
        #endregion

        #region  View模型展示图片存储Image
        private BitmapImage image;
        public BitmapImage Image
        {
            get { return image; }
            set { SetProperty(ref image, value); }
        }
        #endregion

        #region 当前超级管理员
        private SupAdmin currentSupAdmin;

        public SupAdmin CurrentSupAdmin { get => currentSupAdmin; set => SetProperty(ref currentSupAdmin, value); }
        #endregion

        #region Account生成状态
        private string accountGenerationStatus;
        public string AccountGenerationStatus 
        { 
            get => accountGenerationStatus; 
            set => SetProperty(ref accountGenerationStatus, value); 
        }

        private bool isGeneratingAccounts;
        public bool IsGeneratingAccounts 
        { 
            get => isGeneratingAccounts; 
            set => SetProperty(ref isGeneratingAccounts, value); 
        }
        #endregion

        #region 服务
        private readonly IEmployeeService employeeService;
        private readonly ISupAdminService supAdminService;
        private readonly IAdminService adminService;
        #endregion

        #region 构造函数
        public SupAdminPersonallyManagerViewModel(IEmployeeService employeeService, ISupAdminService supAdminService, IAdminService adminService)
        {
            this.employeeService = employeeService;
            this.supAdminService = supAdminService;
            this.adminService = adminService;
            this.accountGenerationStatus = "点击按钮生成账号";
            this.isGeneratingAccounts = false;
            
            using (context = new DataBaseContext())
            {
                // 优先根据Account查找，如果找不到再根据UserName查找
                CurrentSupAdmin = null;
                
                // 如果Account有值，优先尝试用Account查询
                if (!string.IsNullOrEmpty(CurrentUser.Account))
                {
                    CurrentSupAdmin = context.SupAdmins.FirstOrDefault(a => a.Account == CurrentUser.Account);
                }
                
                // 如果通过Account没找到，再尝试用UserName查询
                if (CurrentSupAdmin == null && !string.IsNullOrEmpty(CurrentUser.UserName))
                {
                    CurrentSupAdmin = context.SupAdmins.FirstOrDefault(a => a.SupAdminName == CurrentUser.UserName);
                }
                
                // 如果还是没找到，创建一个新对象
                if (CurrentSupAdmin == null)
                {
                    CurrentSupAdmin = new SupAdmin
                    {
                        Account = CurrentUser.Account ?? "",  // 确保Account有值
                        SupAdminName = CurrentUser.UserName,
                        SupAdminPassword = CurrentUser.Password,
                        SupAdminImage = CurrentUser.Image
                    };
                }
                
                // 确保Account属性已设置（防止数据库中的记录没有Account值）
                if (string.IsNullOrEmpty(CurrentSupAdmin.Account))
                {
                    CurrentSupAdmin.Account = CurrentUser.Account ?? "";
                }
            }
            
            // 检查数据库中还有多少用户没有Account
            CheckAccountStatusAsync();
        }
        #endregion

        #region 保存信息

        private DelegateCommand saveCommand;
        public ICommand SaveCommand => saveCommand ??= new DelegateCommand(Save);

        private async void Save()
        {
            using (context = new DataBaseContext())
            {
                //账号密码不能为空
                if (CurrentSupAdmin.SupAdminName.IsNullOrEmpty() || CurrentSupAdmin.SupAdminPassword.IsNullOrEmpty())
                {
                    Growl.Warning("姓名或密码不能为空");
                    return;
                }
                
                // 如果用户名没有变化，直接更新
                if (CurrentSupAdmin.SupAdminName == CurrentUser.UserName)
                {
                    if (ByteImage != null)
                    {
                        CurrentSupAdmin.SupAdminImage = ByteImage;
                    }
                    context.SupAdmins.Update(CurrentSupAdmin);
                    context.SaveChanges();
                    // 更新CurrentUser的信息
                    CurrentUser.UserName = CurrentSupAdmin.SupAdminName;
                    CurrentUser.Password = CurrentSupAdmin.SupAdminPassword;
                    CurrentUser.Image = CurrentSupAdmin.SupAdminImage;
                    CurrentUser.Account = CurrentSupAdmin.Account;
                    Growl.Success("保存成功");
                    return;
                }
                
                // 检查用户名是否已存在，排除当前用户自己
                bool nameExists = await Task.Run(() =>
                {
                    // 检查超级管理员中是否有同名用户（排除自己）
                    bool existsInSupAdmin = false;
                    using (var tempContext = new DataBaseContext())
                    {
                        existsInSupAdmin = tempContext.SupAdmins
                            .Any(s => s.SupAdminName == CurrentSupAdmin.SupAdminName 
                                  && s.SupAdminId != CurrentSupAdmin.SupAdminId);
                    }
                    
                    // 检查管理员中是否有同名用户
                    bool existsInAdmin = adminService.IsAdminNameExist(CurrentSupAdmin.SupAdminName);
                    
                    // 检查员工中是否有同名用户
                    bool existsInEmployee = employeeService.IsEmployeeNameExist(CurrentSupAdmin.SupAdminName);
                    
                    return existsInSupAdmin || existsInAdmin || existsInEmployee;
                });
                
                if (nameExists)
                {
                    Growl.Warning("用户名已存在");
                    return;
                }
                
                if (ByteImage != null)
                {
                    CurrentSupAdmin.SupAdminImage = ByteImage;
                }
                context.SupAdmins.Update(CurrentSupAdmin);
                context.SaveChanges();
                // 更新CurrentUser的信息
                CurrentUser.UserName = CurrentSupAdmin.SupAdminName;
                CurrentUser.Password = CurrentSupAdmin.SupAdminPassword;
                CurrentUser.Image = CurrentSupAdmin.SupAdminImage;
                CurrentUser.Account = CurrentSupAdmin.Account;
                Growl.Success("保存成功");
            }
        }
        #endregion

        #region 图片选择
        private DelegateCommand changeImageCommand;
        public ICommand ChangeImageCommand => changeImageCommand ??= new DelegateCommand(ChangeImage);

        private void ChangeImage()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                ByteImage = File.ReadAllBytes(openFileDialog.FileName);
                Image = ConverterImage.ConvertByteArrayToBitmapImage(ByteImage);
            }
        }
        #endregion

        #region 批量生成账号
        private DelegateCommand generateAccountsCommand;
        public ICommand GenerateAccountsCommand => generateAccountsCommand ??= new DelegateCommand(GenerateAccountsForUsers);

        /// <summary>
        /// 为所有没有Account的管理员和员工生成账号
        /// </summary>
        private async void GenerateAccountsForUsers()
        {
            try
            {
                IsGeneratingAccounts = true;
                AccountGenerationStatus = "正在生成账号...";
                
                await Task.Run(async () => 
                {
                    using (var context = new DataBaseContext())
                    {
                        int updatedSupAdmins = 0;
                        int updatedAdmins = 0;
                        int updatedEmployees = 0;

                        // 更新超级管理员Account
                        var supAdminsWithoutAccount = context.SupAdmins
                            .Where(a => string.IsNullOrEmpty(a.Account) || a.Account == "")
                            .ToList();
                        
                        foreach (var supAdmin in supAdminsWithoutAccount)
                        {
                            // 生成6位数账号：使用特定前缀 + ID + 随机数
                            string newAccount = GenerateUniqueAccount(supAdmin.SupAdminId, "S");
                            supAdmin.Account = newAccount;
                            updatedSupAdmins++;
                        }

                        // 更新管理员Account
                        var adminsWithoutAccount = context.Admins
                            .Where(a => string.IsNullOrEmpty(a.Account) || a.Account == "")
                            .ToList();
                        
                        foreach (var admin in adminsWithoutAccount)
                        {
                            // 生成6位数账号
                            string newAccount = GenerateUniqueAccount(admin.AdminId, "A");
                            admin.Account = newAccount;
                            updatedAdmins++;
                        }
                        
                        // 更新员工Account
                        var employeesWithoutAccount = context.Employees
                            .Where(e => string.IsNullOrEmpty(e.Account) || e.Account == "")
                            .ToList();
                        
                        foreach (var employee in employeesWithoutAccount)
                        {
                            // 生成6位数账号
                            string newAccount = GenerateUniqueAccount(employee.EmployeeId, "E");
                            employee.Account = newAccount;
                            updatedEmployees++;
                        }
                        
                        // 保存更改
                        await context.SaveChangesAsync();
                        
                        // 更新状态
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            AccountGenerationStatus = $"生成成功: {updatedSupAdmins}个超管, {updatedAdmins}个管理员, {updatedEmployees}个员工";
                            IsGeneratingAccounts = false;
                            
                            if (updatedSupAdmins + updatedAdmins + updatedEmployees > 0)
                            {
                                Growl.Success("批量生成账号成功！");
                            }
                            else
                            {
                                Growl.Info("所有用户都已有账号，无需更新");
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                AccountGenerationStatus = $"生成失败: {ex.Message}";
                IsGeneratingAccounts = false;
                Growl.Error($"生成账号失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查数据库中还有多少用户没有Account
        /// </summary>
        private async void CheckAccountStatusAsync()
        {
            try
            {
                await Task.Run(async () => 
                {
                    using (var context = new DataBaseContext())
                    {
                        int supAdmins = context.SupAdmins.Count(a => string.IsNullOrEmpty(a.Account) || a.Account == "");
                        int admins = context.Admins.Count(a => string.IsNullOrEmpty(a.Account) || a.Account == "");
                        int employees = context.Employees.Count(e => string.IsNullOrEmpty(e.Account) || e.Account == "");
                        
                        // 更新界面上的统计信息
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            int total = supAdmins + admins + employees;
                            if (total > 0)
                            {
                                AccountGenerationStatus = $"发现 {total} 个用户需要生成账号 ({supAdmins}个超管, {admins}个管理员, {employees}个员工)";
                            }
                            else
                            {
                                AccountGenerationStatus = "所有用户都已有账号";
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                AccountGenerationStatus = $"检查账号状态失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 生成唯一的6位数账号
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="prefix">账号前缀 (S:超管, A:管理员, E:员工)</param>
        /// <returns>生成的6位数账号</returns>
        private string GenerateUniqueAccount(int id, string prefix)
        {
            // 格式: 前缀 + ID部分 + 随机数，总共6位
            string idPart = id.ToString().PadLeft(3, '0');
            
            // 计算需要的随机数位数
            int randomDigits = 6 - prefix.Length - idPart.Length;
            if (randomDigits < 0) randomDigits = 0;
            
            // 生成随机数
            Random random = new Random();
            string randomPart = random.Next((int)Math.Pow(10, randomDigits)).ToString().PadLeft(randomDigits, '0');
            
            // 组合账号
            string account = prefix + idPart + randomPart;
            
            // 如果长度超过6位，截断为6位
            if (account.Length > 6)
            {
                account = account.Substring(0, 6);
            }
            
            // 确保长度为6位
            while (account.Length < 6)
            {
                account += random.Next(10).ToString();
            }
            
            return account;
        }
        #endregion

        #region INavigationAware接口实现
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (CurrentSupAdmin.SupAdminImage != null)
            {
                Image = ConverterImage.ConvertByteArrayToBitmapImage(CurrentSupAdmin.SupAdminImage);
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
           
        }
        #endregion
    }
}
