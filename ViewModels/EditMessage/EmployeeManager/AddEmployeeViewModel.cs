using System.Collections.ObjectModel;
using HandyControl.Controls;
using Microsoft.IdentityModel.Tokens;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using SIASGraduate.Services;

namespace SIASGraduate.ViewModels.EditMessage.EmployeeManager
{
    public class AddEmployeeViewModel : BindableBase, INavigationAware
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

        #region 添加员工
        private Department addEmployee;
        public Department AddEmployee
        {
            get
            { return addEmployee; }
            set { SetProperty(ref addEmployee, value); }
        }
        #endregion

        #region 员工账号
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
                if (string.IsNullOrEmpty(employeeName) || (oldAccount != null && employeeName == oldAccount))
                {
                    EmployeeName = value;
                }
            }
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
        private string employeePassword = "123456";
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

        #region 员工权限
        private int roleId = 3;
        public int RoleId
        {
            get { return roleId; }
            set { SetProperty(ref roleId, value); }
        }
        #endregion

        #endregion

        #region 构造函数
        public AddEmployeeViewModel(IEmployeeService employeeService, ISupAdminService supAdminService, IAdminService adminService, IRegionManager regionManager, IEventAggregator eventAggregator)
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
                // 添加"无部门"选项
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
            // 生成格式: E + 年份个位数 + 4位随机数字，总长度为6
            string prefix = "E" + DateTime.Now.Year.ToString().Substring(3, 1); // 只取年份的最后一位
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
            EmployeePassword = "123456";
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
            //将新增的Employee类型的SelectedEmployee传递回去

        }
        #endregion

        #region OnSave方法: 保存按钮点击事件
        private async void OnSave()
        {
            //账号密码不能为空
            if (string.IsNullOrEmpty(Account) || EmployeePassword.IsNullOrEmpty())
            {
                Growl.Warning("账号或密码不能为空");
                return;
            }
            
            if (string.IsNullOrEmpty(EmployeeName))
            {
                Growl.Warning("员工姓名不能为空");
                return;
            }
            
            //员工账号不能够和超级管理员,管理员,员工重复评审员重复
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

            //员工名称不能够和超级管理员,管理员,员工重复评审员重复
            bool nameExists = await Task.Run(() =>
            {
                return supAdminService.IsSupAdminNameExist(EmployeeName) ||
                       adminService.IsAdminNameExist(EmployeeName) ||
                       employeeService.IsEmployeeNameExist(EmployeeName);
            });
            if (nameExists)
            {
                Growl.Warning("员工姓名已存在");
                return;
            }

            var AddEmployee = new Employee()
            {
                Account = Account,
                EmployeeName = EmployeeName,
                EmployeePassword = EmployeePassword,
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
                    AddEmployee.DepartmentId = departmentId;
                }
            }
            // 如果选择"无部门"，DepartmentId保持为null

            employeeService.AddEmployee(AddEmployee);
            Growl.SuccessGlobal("员工添加成功");
            
            // 发布正确的事件通知左侧视图更新
            eventAggregator.GetEvent<EmployeeAddedEvent>().Publish();
            OnCancel();
        }
        private void OnCancel()
        {
            var region = regionManager.Regions["EmployeeEditRegion"];
            region.RemoveAll();
            // 导航回到员工管理视图
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
