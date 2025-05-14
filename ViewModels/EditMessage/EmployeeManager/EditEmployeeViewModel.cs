using System.Collections.ObjectModel;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using SIASGraduate.Services;

namespace SIASGraduate.ViewModels.EditMessage.EmployeeManager
{
    public class EditEmployeeViewModel : BindableBase, INavigationAware
    {
        #region 服务
        private readonly IEmployeeService employeeService;
        private readonly ISupAdminService supAdminService;
        private readonly IAdminService adminService;
        #endregion

        #region 区域导航
        private readonly IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region  属性

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
        private int employeeId;
        public int EmployeeId
        {
            get { return employeeId; }
            set { SetProperty(ref employeeId, value); }
        }
        #endregion

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

        #region 修改员工
        private Employee updateEmployee;
        public Employee UpdateEmployee
        {
            get
            { return updateEmployee; }
            set { SetProperty(ref updateEmployee, value); }
        }
        #endregion

        #region 员工账号
        private string account;
        public string Account
        {
            get { return account; }
            set { SetProperty(ref account, value); }
        }
        #endregion

        #region 员工姓名
        private string employeeName;
        public string EmployeeName
        {
            get { return employeeName; }
            set { SetProperty(ref employeeName, value); }
        }
        #endregion

        #region 员工密码
        private string employeePassword;
        public string EmployeePassword
        {
            get { return employeePassword; }
            set { SetProperty(ref employeePassword, value); }
        }

        #endregion

        #region 员工邮箱
        private string? email;
        public string? Email
        {
            get { return email; }
            set { SetProperty(ref email, value); }
        }
        #endregion

        #region 员工部门
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
                    System.Diagnostics.Debug.WriteLine($"员工状态从在职变为离职，自动设置部门为无部门");
                }
                // 更新在职状态
                SetProperty(ref isActive, value);
                System.Diagnostics.Debug.WriteLine($"IsActive已变更为: {value}");
            }
        }
        #endregion

        #region 员工权限
        private int? roleId = 3;
        public int? RoleId
        {
            get { return roleId; }
            set { SetProperty(ref roleId, value); }
        }
        #endregion

        #region 角色是否可编辑
        private bool isRoleEditable = true;
        public bool IsRoleEditable
        {
            get { return isRoleEditable; }
            set { SetProperty(ref isRoleEditable, value); }
        }
        #endregion

        #endregion

        #region 构造函数
        public EditEmployeeViewModel(IEmployeeService employeeService, IRegionManager regionManager, ISupAdminService supAdminService, IAdminService adminService, IEventAggregator eventAggregator)
        {
            this.employeeService = employeeService;
            this.regionManager = regionManager;
            this.supAdminService = supAdminService;
            this.adminService = adminService;
            this.eventAggregator = eventAggregator;
            SaveCommand = new DelegateCommand(OnSave, CanSave).ObservesProperty(() => IsSaveEnabled);
            CancelCommand = new DelegateCommand(OnCancel, CanCancel).ObservesProperty(() => IsCancelEnabled);
            using (var context = new DataBaseContext())
            {
                var departmentNames = context.Departments.Select(d => d.DepartmentName).ToList();
                // 添加"无部门"选项
                departmentNames.Insert(0, "无部门");
                DepartmentNames = new ObservableCollection<string>(departmentNames);
            }

            // 根据当前用户角色设置是否可以编辑角色
            IsRoleEditable = SIASGraduate.Common.CurrentUser.RoleId == 1; // 只有超级管理员才能编辑角色
        }

        private bool CanSave()
        {
            return IsSaveEnabled;
        }

        private bool CanCancel()
        {
            return IsCancelEnabled;
        }

        #endregion

        #region 方法
        public DelegateCommand SaveCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        private async Task OnSaveAsync()
        {
            //账号和姓名不能为空
            if (string.IsNullOrEmpty(Account) || string.IsNullOrEmpty(EmployeeName) || string.IsNullOrEmpty(EmployeePassword))
            {
                Growl.Warning("账号、姓名或密码不能为空");
                return;
            }

            //验证账号格式必须为6位
            if (Account.Length != 6)
            {
                Growl.Warning("账号必须为6位");
                return;
            }

            //如果修改了账号，需要验证账号是否已存在
            if (Account != BaseAccount)
            {
                bool accountExists = await Task.Run(() =>
                {
                    return supAdminService.IsSupAdminAccountExist(Account) ||
                           adminService.IsAdminAccountExist(Account) ||
                           employeeService.IsEmployeeAccountExist(Account);
                });
                if (accountExists)
                {
                    Growl.Warning("账号已存在");
                    return;
                }
            }

            //如果修改了姓名，需要验证姓名是否已存在
            if (EmployeeName != BaseName)
            {
                bool nameExists = await Task.Run(() =>
                {
                    return supAdminService.IsSupAdminNameExist(EmployeeName) ||
                           adminService.IsAdminNameExist(EmployeeName) ||
                           employeeService.IsEmployeeNameExist(EmployeeName);
                });
                if (nameExists)
                {
                    Growl.Warning("姓名已存在");
                    return;
                }
            }

            // 检查当前用户权限，如果不是超级管理员但尝试将员工设置为管理员
            if (RoleId == 2 && SIASGraduate.Common.CurrentUser.RoleId != 1)
            {
                Growl.WarningGlobal("您没有权限将员工提升为管理员");
                return;
            }

            // 如果将员工角色修改为管理员
            if (RoleId == 2)
            {
                // 检查员工是否离职
                if (IsActive == false)
                {
                    Growl.WarningGlobal("无法将已离职的员工转换为管理员");
                    return;
                }

                try
                {
                    // 禁用按钮防止重复点击
                    IsSaveEnabled = false;
                    IsCancelEnabled = false;

                    using (var context = new DataBaseContext())
                    {
                        // 使用执行策略
                        var strategy = context.Database.CreateExecutionStrategy();

                        await strategy.ExecuteAsync(async () =>
                        {
                            // 使用事务
                            using (var transaction = await context.Database.BeginTransactionAsync())
                            {
                                try
                                {
                                    // 获取当前员工
                                    var currentEmployee = await context.Employees
                                        .FirstOrDefaultAsync(e => e.EmployeeId == EmployeeId);

                                    if (currentEmployee == null)
                                    {
                                        throw new Exception($"无法找到ID为 {EmployeeId} 的员工");
                                    }

                                    // 获取部门ID
                                    int? tempDepartmentId = null;
                                    if (!string.IsNullOrEmpty(DepartmentName) && DepartmentName != "无部门")
                                    {
                                        var department = await context.Departments
                                            .FirstOrDefaultAsync(d => d.DepartmentName == DepartmentName);
                                        if (department != null)
                                        {
                                            tempDepartmentId = department.DepartmentId;
                                        }
                                    }

                                    // 创建新管理员对象
                                    Admin newAdmin = new Admin
                                    {
                                        Account = Account,
                                        AdminName = EmployeeName,
                                        AdminPassword = EmployeePassword,
                                        Email = Email,
                                        HireDate = HireDate,
                                        IsActive = IsActive,
                                        RoleId = RoleId,
                                        AdminImage = currentEmployee.EmployeeImage,
                                        DepartmentId = tempDepartmentId
                                    };

                                    // 添加新管理员
                                    context.Admins.Add(newAdmin);
                                    await context.SaveChangesAsync();

                                    // 一次性处理所有外键引用
                                    await TransferRelatedRecords(EmployeeId, newAdmin.AdminId, context);

                                    // 删除原员工
                                    context.Employees.Remove(currentEmployee);
                                    await context.SaveChangesAsync();

                                    // 提交事务
                                    await transaction.CommitAsync();

                                    // 发布事件通知视图更新
                                    eventAggregator.GetEvent<EmployeeUpdatedEvent>().Publish();

                                    // 添加发布AdminAddEvent事件，确保管理员列表视图更新
                                    eventAggregator.GetEvent<AdminAddEvent>().Publish();

                                    // 添加发布EmployeeRemovedEvent事件，确保员工列表视图删除该员工
                                    eventAggregator.GetEvent<EmployeeRemovedEvent>().Publish();

                                    // 清空编辑区域
                                    var region = regionManager.Regions["EmployeeEditRegion"];
                                    region.RemoveAll();

                                    // 显示成功消息
                                    Growl.SuccessGlobal($"已成功将员工 {EmployeeName} 提升为管理员");

                                    // 不再需要通过OnCancel返回，因为已经手动清空区域
                                    return;
                                }
                                catch (Exception ex)
                                {
                                    // 回滚事务
                                    await transaction.RollbackAsync();
                                    throw new Exception($"转换为管理员失败: {ex.Message}", ex);
                                }
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Growl.ErrorGlobal($"转换为管理员失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"转换为管理员失败: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"内部异常: {ex.InnerException.Message}");
                    }
                }
                finally
                {
                    // 确保按钮状态恢复
                    IsSaveEnabled = true;
                    IsCancelEnabled = true;
                }
                return;
            }
            else if (RoleId == 3)
            {
                try
                {
                    using (var context = new DataBaseContext())
                    {
                        var strategy = context.Database.CreateExecutionStrategy();

                        await strategy.ExecuteAsync(async () =>
                        {
                            using (var transaction = await context.Database.BeginTransactionAsync())
                            {
                                try
                                {
                                    var currentEmployee = await context.Employees
                                        .Include(e => e.Department)
                                        .FirstOrDefaultAsync(e => e.EmployeeId == EmployeeId);

                                    if (currentEmployee == null)
                                    {
                                        throw new Exception("未找到员工信息");
                                    }

                                    // 更新员工信息
                                    currentEmployee.Account = Account; // 添加账号更新
                                    currentEmployee.EmployeeName = EmployeeName;
                                    currentEmployee.EmployeePassword = EmployeePassword;
                                    currentEmployee.Email = Email;
                                    currentEmployee.HireDate = HireDate;
                                    currentEmployee.IsActive = IsActive;
                                    currentEmployee.RoleId = RoleId;

                                    // 如果员工设置为离职状态，自动将部门设置为空
                                    if (IsActive == false)
                                    {
                                        DepartmentName = "无部门";
                                    }

                                    // 先获取之前的部门ID，用于后面判断是否部门发生了变化
                                    int? oldDepartmentId = currentEmployee.DepartmentId;

                                    // 设置新的部门ID
                                    if (!DepartmentName.IsNullOrEmpty() && DepartmentName != "无部门" && IsActive == true)
                                    {
                                        var department = await context.Departments
                                            .FirstOrDefaultAsync(d => d.DepartmentName == DepartmentName);

                                        if (department != null)
                                        {
                                            currentEmployee.DepartmentId = department.DepartmentId;
                                            System.Diagnostics.Debug.WriteLine($"部门已设置: {department.DepartmentName}, ID={department.DepartmentId}");
                                        }
                                    }
                                    else
                                    {
                                        // 如果选择"无部门"或状态为离职，则清除部门关联
                                        currentEmployee.DepartmentId = null;
                                        System.Diagnostics.Debug.WriteLine("员工部门已设置为空");
                                    }

                                    // 更新相关的提名记录中的部门信息
                                    if (oldDepartmentId != currentEmployee.DepartmentId)
                                    {
                                        // 直接使用SQL语句更新提名记录的部门信息
                                        System.Diagnostics.Debug.WriteLine($"开始更新提名记录的部门信息");

                                        // 先获取受影响的提名记录数量
                                        var nominationCount = await context.Nominations
                                            .Where(n => n.NominatedEmployeeId == EmployeeId || n.ProposerEmployeeId == EmployeeId)
                                            .CountAsync();

                                        // 使用原生SQL直接更新，完全绕过EF Core的实体跟踪
                                        await context.Database.ExecuteSqlInterpolatedAsync(
                                            $"UPDATE Nominations SET DepartmentId = {currentEmployee.DepartmentId} WHERE NominatedEmployeeId = {EmployeeId} OR ProposerEmployeeId = {EmployeeId}");

                                        System.Diagnostics.Debug.WriteLine($"使用SQL直接更新了 {nominationCount} 条提名记录的部门信息");
                                    }

                                    // 清理所有可能被跟踪的Nomination实体，避免保存时尝试更新不存在的列
                                    foreach (var entry in context.ChangeTracker.Entries<Nomination>())
                                    {
                                        entry.State = EntityState.Detached;
                                    }

                                    System.Diagnostics.Debug.WriteLine("正在保存员工信息更改");
                                    await context.SaveChangesAsync();

                                    // 提交事务
                                    System.Diagnostics.Debug.WriteLine("正在提交事务");
                                    await transaction.CommitAsync();

                                    // 发布正确的事件通知左侧视图更新
                                    eventAggregator.GetEvent<EmployeeUpdatedEvent>().Publish();
                                    OnCancel();

                                    // 显示成功消息
                                    Growl.SuccessGlobal($"员工 {EmployeeName} 信息已成功更新");
                                }
                                catch (Exception ex)
                                {
                                    // 回滚事务
                                    System.Diagnostics.Debug.WriteLine($"发生错误，正在回滚事务: {ex.Message}");
                                    await transaction.RollbackAsync();
                                    throw new Exception($"更新员工信息失败: {ex.Message}", ex);
                                }
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"更新员工信息失败: {ex.Message}", ex);
                }
            }
        }

        private void OnCancel()
        {
            var region = regionManager.Regions["EmployeeEditRegion"];
            region.RemoveAll();
            // 导航回到员工管理视图
        }

        private async Task TransferRelatedRecords(int employeeId, int adminId, DataBaseContext context)
        {
            try
            {
                // 一次性获取所有需要处理的关联记录
                var nominationsAsProposer = await context.Nominations
                    .Where(n => n.ProposerEmployeeId == employeeId)
                    .ToListAsync();

                var nominationsAsNominated = await context.Nominations
                    .Where(n => n.NominatedEmployeeId == employeeId)
                    .ToListAsync();

                var voteRecords = await context.VoteRecords
                    .Where(v => v.VoterEmployeeId == employeeId)
                    .ToListAsync();

                var commentRecords = await context.CommentRecords
                    .Where(c => c.CommenterEmployeeId == employeeId)
                    .ToListAsync();

                var nominationLogs = await context.NominationLogs
                    .Where(nl => nl.OperatorEmployeeId == employeeId)
                    .ToListAsync();

                // 批量更新所有关联记录

                // 1. 更新提名表中的提名人引用
                foreach (var nomination in nominationsAsProposer)
                {
                    nomination.ProposerEmployeeId = null;
                    nomination.ProposerAdminId = adminId;
                }

                // 2. 更新提名表中的被提名人引用
                foreach (var nomination in nominationsAsNominated)
                {
                    nomination.NominatedEmployeeId = null;
                    nomination.NominatedAdminId = adminId;
                }

                // 3. 更新投票记录中的引用
                foreach (var vote in voteRecords)
                {
                    vote.VoterEmployeeId = null;
                    vote.VoterAdminId = adminId;
                }

                // 4. 更新评论记录中的引用
                foreach (var comment in commentRecords)
                {
                    comment.CommenterEmployeeId = null;
                    comment.CommenterAdminId = adminId;
                }

                // 5. 更新提名日志中的引用
                foreach (var log in nominationLogs)
                {
                    log.OperatorEmployeeId = null;
                    log.OperatorAdminId = adminId;
                }

                // 保存所有更改
                if (nominationsAsProposer.Any() || nominationsAsNominated.Any() ||
                    voteRecords.Any() || commentRecords.Any() || nominationLogs.Any())
                {
                    await context.SaveChangesAsync();
                }

                // 为确保所有外键关系已断开，执行SQL更新
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE NominationLogs SET OperatorEmployeeId = NULL WHERE OperatorEmployeeId = {employeeId}");
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE Nominations SET ProposerEmployeeId = NULL WHERE ProposerEmployeeId = {employeeId}");
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE Nominations SET NominatedEmployeeId = NULL WHERE NominatedEmployeeId = {employeeId}");
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE VoteRecords SET VoterEmployeeId = NULL WHERE VoterEmployeeId = {employeeId}");
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE CommentRecords SET CommenterEmployeeId = NULL WHERE CommenterEmployeeId = {employeeId}");
            }
            catch (Exception ex)
            {
                throw new Exception($"转移关联记录时出错: {ex.Message}", ex);
            }
        }

        #endregion

        #region INavigationAware接口的实现

        #region  OnNavigatedTo方法: 当导航到该视图模型时调用
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            try
            {
                if (navigationContext?.Parameters == null)
                {
                    Growl.ErrorGlobal("导航参数为空");
                    return;
                }

                if (navigationContext.Parameters.ContainsKey("Employee"))
                {
                    UpdateEmployee = navigationContext.Parameters.GetValue<Employee>("Employee");
                    if (UpdateEmployee == null)
                    {
                        Growl.ErrorGlobal("员工信息为空");
                        return;
                    }

                    EmployeeId = UpdateEmployee.EmployeeId;
                    Account = UpdateEmployee.Account; // 设置账号
                    EmployeeName = UpdateEmployee.EmployeeName;
                    EmployeePassword = UpdateEmployee.EmployeePassword;
                    Email = UpdateEmployee.Email;
                    HireDate = UpdateEmployee.HireDate;
                    IsActive = UpdateEmployee.IsActive;
                    RoleId = UpdateEmployee.RoleId;
                    BaseName = UpdateEmployee.EmployeeName;
                    BaseAccount = UpdateEmployee.Account; // 设置基础账号用于后续比较

                    // 根据当前用户角色设置是否可以编辑角色
                    IsRoleEditable = SIASGraduate.Common.CurrentUser.RoleId == 1; // 只有超级管理员才能编辑角色

                    if (UpdateEmployee.DepartmentId.HasValue)
                    {
                        using var context = new DataBaseContext();
                        var department = context.Departments.FirstOrDefault(d => d.DepartmentId == UpdateEmployee.DepartmentId);
                        DepartmentName = department?.DepartmentName ?? "无部门";
                    }
                    else
                    {
                        DepartmentName = "无部门";
                    }
                }
                else
                {
                    Growl.ErrorGlobal("未找到员工信息");
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"加载员工信息失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"OnNavigatedTo错误: {ex.Message}\n{ex.StackTrace}");
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
            System.Diagnostics.Debug.WriteLine("离开EditEmployeeViewModel");
        }
        #endregion

        #region OnSave方法: 保存按钮点击事件
        private async void OnSave()
        {
            try
            {
                // 验证密码长度
                if (EmployeePassword.Length < 6 || EmployeePassword.Length > 20)
                {
                    HandyControl.Controls.Growl.Warning("密码长度必须在6-20位之间");
                    return;
                }

                // 禁用保存按钮，防止重复提交
                IsSaveEnabled = false;

                try
                {
                    // 执行实际的保存操作
                    System.Diagnostics.Debug.WriteLine("开始调用OnSaveAsync()");
                    await OnSaveAsync();
                    System.Diagnostics.Debug.WriteLine("OnSaveAsync()执行完成");
                }
                catch (DbUpdateException dbEx)
                {
                    // 特别处理数据库更新异常
                    System.Diagnostics.Debug.WriteLine($"数据库更新错误: {dbEx.Message}");

                    // 检查是否为IsUserVoted列错误
                    if (dbEx.Message.Contains("IsUserVoted"))
                    {
                        System.Diagnostics.Debug.WriteLine("检测到IsUserVoted列错误，尝试修复...");

                        try
                        {
                            // 尝试直接更新员工表，避开提名记录
                            using (var context = new DataBaseContext())
                            {
                                var employee = await context.Employees.FindAsync(EmployeeId);
                                if (employee != null)
                                {
                                    // 只更新员工基本信息
                                    employee.EmployeeName = EmployeeName;
                                    employee.Account = Account;
                                    employee.EmployeePassword = EmployeePassword;
                                    employee.Email = Email;
                                    employee.HireDate = HireDate;
                                    employee.IsActive = IsActive;

                                    // 更新部门ID
                                    if (!string.IsNullOrEmpty(DepartmentName) && DepartmentName != "无部门" && IsActive == true)
                                    {
                                        var department = await context.Departments
                                            .FirstOrDefaultAsync(d => d.DepartmentName == DepartmentName);
                                        if (department != null)
                                        {
                                            employee.DepartmentId = department.DepartmentId;
                                        }
                                    }
                                    else
                                    {
                                        employee.DepartmentId = null;
                                    }

                                    // 保存员工信息变更
                                    await context.SaveChangesAsync();

                                    // 单独更新提名记录部门
                                    await context.Database.ExecuteSqlInterpolatedAsync(
                                        $"UPDATE Nominations SET DepartmentId = {employee.DepartmentId} WHERE NominatedEmployeeId = {EmployeeId} OR ProposerEmployeeId = {EmployeeId}");

                                    // 提示成功
                                    Growl.SuccessGlobal($"员工 {EmployeeName} 信息已成功更新");

                                    // 发布事件通知视图更新
                                    eventAggregator.GetEvent<EmployeeUpdatedEvent>().Publish();
                                    OnCancel();
                                    return;
                                }
                            }
                        }
                        catch (Exception fixEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"尝试修复IsUserVoted错误失败: {fixEx.Message}");
                            Growl.ErrorGlobal($"修复数据库错误失败: {fixEx.Message}");
                        }
                    }

                    if (dbEx.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"内部异常: {dbEx.InnerException.Message}");
                        Growl.ErrorGlobal($"保存失败(数据库更新错误): {dbEx.Message} - {dbEx.InnerException.Message}");
                    }
                    else
                    {
                        Growl.ErrorGlobal($"保存失败(数据库更新错误): {dbEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"保存操作发生错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"OnSave方法发生错误: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // 确保按钮重新启用
                IsSaveEnabled = true;
                IsCancelEnabled = true;

                System.Diagnostics.Debug.WriteLine("保存操作完成，已重新启用按钮");
            }
        }
        #endregion

        #endregion
    }
}
