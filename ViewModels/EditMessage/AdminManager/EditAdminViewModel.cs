using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using SIASGraduate.Services;
using HandyControl.Controls;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace SIASGraduate.ViewModels.EditMessage.AdminManager
{
    public class EditAdminViewModel : BindableBase, INavigationAware
    {
        #region 服务
        private readonly IEmployeeService employeeService;
        private readonly ISupAdminService supAdminService;
        private readonly IAdminService adminService;
        #endregion

        #region 区域导航
        private readonly IRegionManager regionManager;
        private readonly IRegionNavigationService _navigationService;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region  属性

        private ObservableCollection<string> departmentNames;
        public ObservableCollection<string> DepartmentNames
        {
            get { return departmentNames; }
            set { SetProperty(ref departmentNames, value); }
        }

        private string departmentName;
        public string DepartmentName
        {
            get { return departmentName; }
            set { SetProperty(ref departmentName, value); }
        }

        #region 按钮状态
        private bool isSaveEnabled = true;
        public bool IsSaveEnabled
        {
            get { return isSaveEnabled; }
            set { SetProperty(ref isSaveEnabled, value); }
        }

        private bool isCancelEnabled = true;
        public bool IsCancelEnabled
        {
            get { return isCancelEnabled; }
            set { SetProperty(ref isCancelEnabled, value); }
        }
        #endregion

        #region 存储原始账号名称
        private string baseName;
        public string BaseName
        {
            get { return baseName; }
            set { SetProperty(ref baseName, value); }
        }
        #endregion
        
        #region 存储原始账号
        private string baseAccount;
        public string BaseAccount
        {
            get { return baseAccount; }
            set { SetProperty(ref baseAccount, value); }
        }
        #endregion

        #region 原始Id
        private int adminId;
        public int AdminId
        {
            get { return adminId; }
            set { SetProperty(ref adminId, value); }
        }
        #endregion

        #region 修改管理员
        private Admin updateAdmin;
        public Admin UpdateAdmin
        {
            get { return updateAdmin; }
            set { SetProperty(ref updateAdmin, value); }
        }
        #endregion
        
        #region 管理员账号
        private string account;
        public string Account
        {
            get { return account; }
            set { SetProperty(ref account, value); }
        }
        #endregion

        #region 管理员姓名
        private string adminName;
        public string AdminName
        {
            get { return adminName; }
            set { SetProperty(ref adminName, value); }
        }
        #endregion

        #region 管理员密码
        private string adminPassword;
        public string AdminPassword
        {
            get { return adminPassword; }
            set { SetProperty(ref adminPassword, value); }
        }

        #endregion

        #region 管理员邮箱
        private string? email;
        public string? Email
        {
            get { return email; }
            set { SetProperty(ref email, value); }
        }
        #endregion

        #region 管理员部门
        private Department? department;
        public Department? Department
        {
            get { return department; }
            set { SetProperty(ref department, value); }
        }
        #endregion

        #region 入职日期
        private DateTime? hireDate = DateTime.Now;
        public DateTime? HireDate
        {
            get { return hireDate; }
            set { SetProperty(ref hireDate, value); }
        }
        #endregion

        #region 是否在职
        private bool? isActive = true;
        public bool? IsActive
        {
            get { return isActive; }
            set 
            {
                // 如果从在职变为离职状态
                if (isActive == true && value == false)
                {
                    // 自动更新部门为"无部门"
                    DepartmentName = "无部门";
                    System.Diagnostics.Debug.WriteLine($"管理员状态从在职变为离职，自动设置部门为无部门");
                }
                // 更新在职状态
                SetProperty(ref isActive, value);
                System.Diagnostics.Debug.WriteLine($"IsActive已变更为: {value}");
            }
        }
        #endregion

        #region 管理员权限
        private int? roleId = 2;
        public int? RoleId
        {
            get { return roleId; }
            set { SetProperty(ref roleId, value); }
        }
        #endregion

        #endregion

        #region 构造函数
        public EditAdminViewModel(IEmployeeService employeeService, IRegionManager regionManager, ISupAdminService supAdminService, IAdminService adminService, IEventAggregator eventAggregator, IRegionNavigationService navigationService)
        {
            this.employeeService = employeeService;
            this.regionManager = regionManager;
            this.supAdminService = supAdminService;
            this.adminService = adminService;
            this.eventAggregator = eventAggregator;
            this._navigationService = navigationService;
            SaveCommand = new DelegateCommand(OnSave, CanSave).ObservesProperty(() => IsSaveEnabled);
            CancelCommand = new DelegateCommand(OnCancel, CanCancel).ObservesProperty(() => IsCancelEnabled);

            using (var context = new DataBaseContext())
            {
                var departmentNames = context.Departments.Select(d => d.DepartmentName).ToList();
                // 添加"无部门"选项
                departmentNames.Insert(0, "无部门");
                DepartmentNames = new ObservableCollection<string>(departmentNames);
            }
        }
        #endregion

        #region 方法
        public DelegateCommand SaveCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        private bool CanSave()
        {
            return IsSaveEnabled;
        }

        private bool CanCancel()
        {
            return IsCancelEnabled;
        }
        #endregion

        #region INavigationAware接口的实现

        #region  OnNavigatedTo方法: 当导航到该视图模型时调用
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("Admin"))
            {
                UpdateAdmin = navigationContext.Parameters.GetValue<Admin>("Admin");
                AdminId = UpdateAdmin.AdminId;
                Account = UpdateAdmin.Account;
                AdminName = UpdateAdmin.AdminName;
                AdminPassword = UpdateAdmin.AdminPassword;
                Email = UpdateAdmin.Email;
                HireDate = UpdateAdmin.HireDate;
                IsActive = UpdateAdmin.IsActive;
                RoleId = UpdateAdmin.RoleId;
                BaseName = UpdateAdmin.AdminName;
                BaseAccount = UpdateAdmin.Account;
                if (!string.IsNullOrEmpty(UpdateAdmin?.DepartmentId.ToString()))
                {
                    using var context = new DataBaseContext();
                    var department = context.Departments.FirstOrDefault(d => d.DepartmentId == UpdateAdmin.DepartmentId);
                    DepartmentName = department?.DepartmentName;
                }
                else
                {
                    // 如果没有部门关联，则选择"无部门"
                    DepartmentName = "无部门";
                }
            }
        }
        #endregion

        #region IsNavigationTarget方法: 判断是否可以导航到该视图模型
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }
        #endregion

        #region OnNavigatedFrom方法: 当导航离开该视图模型时调用
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            System.Diagnostics.Debug.WriteLine("离开EditAdminViewModel");
        }
        #endregion

        #region OnSave方法: 保存按钮点击事件
        private async void OnSave()
        {
            try
            {
                // 验证密码长度
                if (AdminPassword.Length < 6 || AdminPassword.Length > 20)
                {
                    HandyControl.Controls.Growl.Warning("密码长度必须在6-20位之间");
                    return;
                }
                
                // 禁用保存按钮
                IsSaveEnabled = false;
                
                System.Diagnostics.Debug.WriteLine($"开始保存操作: AdminId={AdminId}, Name={AdminName}, Role={RoleId}");
                
                // 记住当前命令对象，用于稍后重新启用
                var saveCommand = SaveCommand;
                var cancelCommand = CancelCommand;
                
                // 显示加载消息并禁用按钮
                Growl.InfoGlobal("正在处理中...");
                IsSaveEnabled = false;
                IsCancelEnabled = false;
                
                try
                {
                    // 执行实际的保存操作
                    System.Diagnostics.Debug.WriteLine("开始调用OnSaveInternalAsync()");
                    await OnSaveInternalAsync();
                    System.Diagnostics.Debug.WriteLine("OnSaveInternalAsync()执行完成");
                }
                catch (DbUpdateException dbEx)
                {
                    // 特别处理数据库更新异常
                    HandleDbUpdateException(dbEx);
                }
                catch (Exception ex)
                {
                    // 捕获并显示任何错误
                    System.Diagnostics.Debug.WriteLine($"保存失败: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"内部异常: {ex.InnerException.Message}");
                        Growl.ErrorGlobal($"保存失败: {ex.Message} - {ex.InnerException.Message}");
                    }
                    else
                    {
                        Growl.ErrorGlobal($"保存失败: {ex.Message}");
                    }
                }
                finally
                {
                    // 确保按钮重新启用
                    IsSaveEnabled = true;
                    IsCancelEnabled = true;
                    
                    // 通知UI更新按钮状态
                    saveCommand?.RaiseCanExecuteChanged();
                    cancelCommand?.RaiseCanExecuteChanged();
                    
                    System.Diagnostics.Debug.WriteLine("保存操作完成，已重新启用按钮");
                }
            }
            catch (Exception ex)
            {
                LogAndShowError("处理保存操作时出错", ex);
            }
        }
        #endregion

        #region 保存逻辑实现
        private async Task OnSaveInternalAsync()
        {
            try
            {
                if (!ValidateInput())
                    return;

                using var context = new DataBaseContext();
                // 获取执行策略
                var strategy = context.Database.CreateExecutionStrategy();
                
                // 使用执行策略包装事务操作
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await context.Database.BeginTransactionAsync();
                    try
                    {
                        var currentAdmin = await context.Admins.FirstOrDefaultAsync(a => a.AdminId == AdminId);

                        if (currentAdmin == null)
                        {
                            Growl.ErrorGlobal("未找到该管理员信息");
                            return;
                        }

                        // 检查管理员是否有角色变更
                        var hasRoleChanged = currentAdmin.RoleId != RoleId;

                        // 更新管理员信息
                        UpdateAdminInfo(currentAdmin);

                        // 管理员转为员工
                        if (hasRoleChanged && RoleId == 3)
                        {
                            var employee = ConvertAdminToEmployee(currentAdmin);
                            context.Employees.Add(employee);
                            context.Admins.Remove(currentAdmin);
                            await context.SaveChangesAsync();
                            
                            // 提交事务
                            await transaction.CommitAsync();
                            
                            // 清空编辑区域
                            regionManager.Regions["AdminEditRegion"].RemoveAll();
                            
                            // 发布事件通知列表更新，在UI线程中执行
                            App.Current.Dispatcher.Invoke(() => {
                                Growl.SuccessGlobal("管理员已成功转为员工");
                                // 使用优先级较低的事件，避免UI阻塞
                                Task.Delay(100).ContinueWith(_ => {
                                    eventAggregator.GetEvent<AdminUpdateEvent>().Publish();
                                    eventAggregator.GetEvent<EmployeeAddedEvent>().Publish();
                                });
                            });
                            
                            return;
                        }

                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        Growl.SuccessGlobal("修改成功");
                        
                        // 发布事件通知列表更新
                        App.Current.Dispatcher.Invoke(() => {
                            Task.Delay(100).ContinueWith(_ => {
                                eventAggregator.GetEvent<AdminUpdateEvent>().Publish();
                            });
                        });
                        
                        // 清空编辑区域，确保导航页面关闭
                        regionManager.Regions["AdminEditRegion"].RemoveAll();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        LogAndShowError("保存变更时出错", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                LogAndShowError("处理保存操作时出错", ex);
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Account))
            {
                Growl.WarningGlobal("账号不能为空");
                return false;
            }

            if (string.IsNullOrWhiteSpace(AdminName))
            {
                Growl.WarningGlobal("名称不能为空");
                return false;
            }

            if (string.IsNullOrWhiteSpace(AdminPassword))
            {
                Growl.WarningGlobal("密码不能为空");
                return false;
            }

            return true;
        }

        private Employee ConvertAdminToEmployee(Admin admin)
        {
            // 创建新员工实体
            var employee = new Employee
            {
                EmployeeName = admin.AdminName,
                Account = admin.Account,
                EmployeePassword = admin.AdminPassword,
                Email = admin.Email,
                HireDate = admin.HireDate,
                IsActive = admin.IsActive,
                RoleId = 3, // 员工角色ID固定为3
                DepartmentId = admin.DepartmentId
            };

            return employee;
        }

        private void UpdateAdminInfo(Admin admin)
        {
            // 更新管理员基本信息
            admin.Account = Account;
            admin.AdminName = AdminName;
            admin.AdminPassword = AdminPassword;
            admin.Email = Email;
            admin.HireDate = HireDate;
            admin.IsActive = IsActive;
            admin.RoleId = RoleId;

            // 更新部门信息
            if (DepartmentName != "无部门")
            {
                using var context = new DataBaseContext();
                var department = context.Departments.FirstOrDefault(d => d.DepartmentName == DepartmentName);
                admin.DepartmentId = department?.DepartmentId;
            }
            else
            {
                admin.DepartmentId = null;
            }
        }
        #endregion

        #region 管理员转员工逻辑
        private async Task TransferRelatedRecords(int adminId, int employeeId, DataBaseContext context)
        {
            try
            {
                // 记录开始转移相关记录
                System.Diagnostics.Debug.WriteLine($"开始转移管理员(ID:{adminId})相关记录到员工(ID:{employeeId})");
                
                // 检查并处理真实存在的相关表
                // 本方法将根据实际数据库模型进行适当处理
                
                // 保存所有更改
                await context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("所有相关记录转移完成");
            }
            catch (Exception ex)
            {
                // 详细记录转移过程中的错误
                System.Diagnostics.Debug.WriteLine($"转移相关记录时出错: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"内部异常: {ex.InnerException.Message}");
                }
                
                // 重新抛出异常，以便事务可以回滚
                throw new Exception("转移管理员相关记录到员工时出错", ex);
            }
        }
        #endregion

        #region 工具方法
        private void HandleDbUpdateException(DbUpdateException dbEx)
        {
            System.Diagnostics.Debug.WriteLine($"数据库更新错误: {dbEx.Message}");
            
            if (dbEx.InnerException is SqlException sqlEx)
            {
                // 处理SQL特定错误
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601) // 唯一约束冲突
                {
                    if (sqlEx.Message.Contains("IX_Admins_AdminName"))
                    {
                        Growl.Error("管理员姓名已存在");
                    }
                    else if (sqlEx.Message.Contains("IX_Admins_Account"))
                    {
                        Growl.Error("管理员账号已存在");
                    }
                    else
                    {
                        Growl.Error("数据冲突，请检查输入");
                    }
                }
                else
                {
                    Growl.Error($"SQL错误 {sqlEx.Number}: {sqlEx.Message}");
                }
            }
            else if (dbEx.InnerException != null)
            {
                Growl.Error($"内部错误: {dbEx.InnerException.Message}");
            }
            else
            {
                Growl.Error($"数据库更新错误: {dbEx.Message}");
            }
        }
        
        private void LogAndShowError(string message, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{message}: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"内部异常: {ex.InnerException.Message}");
                
                if (ex.InnerException.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"二级内部异常: {ex.InnerException.InnerException.Message}");
                    Growl.ErrorGlobal($"{message}: {ex.Message} - {ex.InnerException.Message} - {ex.InnerException.InnerException.Message}");
                }
                else
                {
                    Growl.ErrorGlobal($"{message}: {ex.Message} - {ex.InnerException.Message}");
                }
            }
            else
            {
                Growl.ErrorGlobal($"{message}: {ex.Message}");
            }
        }

        private void OnCancel()
        {
            var region = regionManager.Regions["AdminEditRegion"];
            region.RemoveAll();
            // 导航回到管理员管理视图
        }
        #endregion

        #endregion
    }
}
