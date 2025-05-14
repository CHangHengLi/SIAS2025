using System.Collections.ObjectModel;
using HandyControl.Controls;
using Microsoft.IdentityModel.Tokens;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using SIASGraduate.Services;

namespace SIASGraduate.ViewModels.EditMessage.AdminManager
{
    public class AddAdminViewModel : BindableBase, INavigationAware
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

        #region 添加管理员
        private Admin addAdmin;
        public Admin AddAdmin
        {
            get
            { return addAdmin; }
            set { SetProperty(ref addAdmin, value); }
        }
        #endregion

        #region 管理员账号
        private string account;
        public string Account
        {
            get { return account; }
            set
            {
                // 保存旧账号值
                string oldAccount = account;
                SetProperty(ref account, value);

                // 如果姓名为空或与旧账号相同，则将姓名设置为新账号
                if (string.IsNullOrEmpty(adminName) || (oldAccount != null && adminName == oldAccount))
                {
                    AdminName = value;
                }
            }
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
        private string adminPassword = "123456";
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
            set { SetProperty(ref isActive, value); }
        }
        #endregion

        #region 管理员权限
        private int roleId = 2;
        public int RoleId
        {
            get { return roleId; }
            set { SetProperty(ref roleId, value); }
        }
        #endregion

        #endregion

        #region 构造函数
        public AddAdminViewModel(IEmployeeService employeeService, ISupAdminService supAdminService, IAdminService adminService, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            #region 服务
            this.employeeService = employeeService;
            this.supAdminService = supAdminService;
            this.adminService = adminService;
            #endregion

            SubmitCommand = new DelegateCommand(OnSubmit);

            this.eventAggregator = eventAggregator;
            this.regionManager = regionManager;
            SaveCommand = new DelegateCommand(OnSave);
            CancelCommand = new DelegateCommand(OnCancel);

            using (var context = new DataBaseContext())
            {
                var departmentNames = context.Departments.Select(d => d.DepartmentName).ToList();
                departmentNames.Insert(0, "无部门");
                DepartmentNames = new ObservableCollection<string>(departmentNames);
            }
        }

        #endregion

        #region 方法
        public DelegateCommand SaveCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        // 生成唯一账号
        private string GenerateUniqueAccount()
        {
            // 生成格式: A + 年份个位数 + 4位随机数字，总长度为6
            string prefix = "A" + DateTime.Now.Year.ToString().Substring(3, 1); // 只取年份的最后一位
            Random random = new Random();
            string randomPart = random.Next(1000, 9999).ToString();
            string account = prefix + randomPart;

            // 检查账号是否已存在
            bool accountExists = supAdminService.IsSupAdminAccountExist(account) ||
                                adminService.IsAdminAccountExist(account) ||
                                employeeService.IsEmployeeAccountExist(account);

            // 如果账号已存在，重新生成
            if (accountExists)
            {
                return GenerateUniqueAccount();
            }

            return account;
        }
        #endregion

        #region INavigationAware接口的实现   

        #region  OnNavigatedTo方法: 当导航到该视图模型时调用
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 自动生成唯一账号
            Account = GenerateUniqueAccount();
            // 设置默认密码
            AdminPassword = "123456";
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

                //账号密码不能为空
                if (string.IsNullOrEmpty(Account) || AdminPassword.IsNullOrEmpty())
                {
                    Growl.Warning("账号或密码不能为空");
                    return;
                }

                if (string.IsNullOrEmpty(AdminName))
                {
                    Growl.Warning("管理员姓名不能为空");
                    return;
                }

                //管理员账号不能够与已有账号重复
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

                //管理员姓名不能重复
                bool nameExists = await Task.Run(() =>
                {
                    return supAdminService.IsSupAdminNameExist(AdminName) ||
                           adminService.IsAdminNameExist(AdminName) ||
                           employeeService.IsEmployeeNameExist(AdminName);
                });
                if (nameExists)
                {
                    Growl.Warning("管理员姓名已存在");
                    return;
                }

                var AddAdmin = new Admin()
                {
                    Account = Account,
                    AdminName = AdminName,
                    AdminPassword = AdminPassword,
                    Email = Email,
                    HireDate = HireDate,
                    IsActive = IsActive,
                    RoleId = RoleId
                };

                if (!DepartmentName.IsNullOrEmpty() && DepartmentName != "无部门")
                {
                    using var context = new DataBaseContext();
                    var department = context.Departments.FirstOrDefault(d => d.DepartmentName == DepartmentName);
                    var departmentId = department?.DepartmentId;
                    if (department != null)
                    {
                        AddAdmin.DepartmentId = departmentId;
                    }
                }
                adminService.AddAdmin(AddAdmin);
                // 发布事件通知左侧视图更新
                eventAggregator.GetEvent<AdminAddEvent>().Publish();
                Growl.SuccessGlobal("添加成功");
                OnCancel();
            }
            catch (Exception ex)
            {
                Growl.Warning(ex.Message);
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

        #region Enter
        public DelegateCommand SubmitCommand { get; private set; }
        private void OnSubmit()
        {
            // 执行在按下回车键时触发的逻辑
            SaveCommand.Execute();
        }
        #endregion
    }
}
