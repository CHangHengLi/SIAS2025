using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SIASGraduate.Models;
using SIASGraduate.Services;

namespace SIASGraduate.ViewModels
{
    public class LoginViewModel : BindableBase, IDialogAware
    {

        #region 构造函数
        public LoginViewModel(ISupAdminService supAdminService, IAdminService adminService, IEmployeeService employeeService) //构造函数,传入ISupAdminService, IAdminService,IEmployeeService
        {
            Loading = Visibility.Hidden;
            SubmitCommand = new DelegateCommand(OnSubmit);
            _supAdminService = supAdminService;
            _adminService = adminService;
            _employeeService = employeeService;
            LoadSupAdmins();
            LoadAdmins();
            LoadEmployees();
        }
        #endregion

        #region 属性
        private string title = "登录";
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }
        private string account;
        public string Account
        {
            get { return account; }
            set { SetProperty(ref account, value); }
        }
        private string password;
        public string Password
        {
            get { return password; }
            set { SetProperty(ref password, value); }
        }
        private byte[] image;
        public byte[] Image
        {
            get { return image; }
            set { SetProperty(ref image, value); }
        }
        #endregion

        #region 命令

        public DelegateCommand SubmitCommand { get; private set; }
        private void OnSubmit()
        {
            // 先显示加载旋转条
            Loading = Visibility.Visible;

            // 执行在按下回车键时触发的逻辑
            LoginCommand.Execute();
        }

        #region SupAdminsService
        private readonly ISupAdminService _supAdminService;
        private ObservableCollection<SupAdmin> _supAdmins;
        public ObservableCollection<SupAdmin> SupAdmins
        {
            get { return _supAdmins; }
            set { SetProperty(ref _supAdmins, value); }
        }
        private void LoadSupAdmins()
        {
            var supAdmins = _supAdminService.GetAllSupAdmins(); // 获取所有管理员
            SupAdmins = new ObservableCollection<SupAdmin>(supAdmins); // 将管理员集合绑定到UI
        }

        private SupAdmin GetSupAdminByAccount(string account)
        {
            return _supAdminService.GetSupAdminByAccount(account);
        }
        #endregion

        #region AdminService
        private readonly IAdminService _adminService;
        private ObservableCollection<Admin> _admins;
        public ObservableCollection<Admin> Admins
        {
            get { return _admins; }
            set { SetProperty(ref _admins, value); }
        }
        private async void LoadAdmins()
        {
            var admins = await _adminService.GetAllAdminsAsync(); // 获取所有管理员
            Admins = new ObservableCollection<Admin>(admins); // 将管理员集合绑定到UI
        }

        private async Task<Admin> GetAdminByAccountAsync(string account)
        {
            return await _adminService.GetAdminByAccountAsync(account);
        }

        #endregion

        #region EmployeeService
        private readonly IEmployeeService _employeeService;
        private ObservableCollection<Employee> _employees;
        public ObservableCollection<Employee> Employees
        {
            get { return _employees; }
            set { SetProperty(ref _employees, value); }
        }
        private void LoadEmployees()
        {
            var employees = _employeeService.GetAllEmployees(); // 获取所有管理员
            Employees = new ObservableCollection<Employee>(employees); // 将管理员集合绑定到UI
        }

        private Employee GetEmployeeByAccount(string account)
        {
            return _employeeService.GetEmployeeByAccount(account);
        }

        #endregion

        public DelegateCommand LoginCommand
        {
            get
            {
                return new DelegateCommand(async () =>
                {

                    // 登录逻辑
                    var supAdmin = GetSupAdminByAccount(Account);
                    var admin = await GetAdminByAccountAsync(Account);
                    var employee = GetEmployeeByAccount(Account);

                    if ((supAdmin != null && supAdmin.SupAdminPassword == Password) || (admin != null && admin.AdminPassword == Password) && (admin.IsActive == true) || (employee != null && employee.EmployeePassword == Password) && (employee.IsActive == true))
                    {
                        DialogParameters parameters = new() {
                            { "username", Account},
                            { "password", Password},
                            { "account", Account},
                        };
                        if (supAdmin != null && supAdmin.SupAdminPassword == Password)
                        {
                            parameters.Add("roleId", 1);
                            parameters.Add("image", supAdmin.SupAdminImage);
                            parameters.Add("adminId", supAdmin.SupAdminId);
                            parameters.Add("userName", supAdmin.SupAdminName);
                        }
                        else if (admin != null && admin.AdminPassword == Password)
                        {
                            parameters.Add("roleId", 2);
                            parameters.Add("image", admin.AdminImage);
                            parameters.Add("adminId", admin.AdminId);
                            parameters.Add("userName", admin.AdminName);
                        }
                        else if (employee != null && employee.EmployeePassword == Password)
                        {
                            parameters.Add("roleId", 3);
                            parameters.Add("image", employee.EmployeeImage);
                            parameters.Add("employeeId", employee.EmployeeId);
                            parameters.Add("userName", employee.EmployeeName);
                        }
                        else
                        {
                            parameters.Add("roleId", 4);
                        }
                        RequestClose.Invoke(parameters, ButtonResult.OK);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("账号或密码错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    Loading = Visibility.Hidden;
                });
            }
        }



        public DialogCloseListener RequestClose { get; } // 关闭对话框
        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }

        private DelegateCommand exitCommand;
        public ICommand ExitCommand => exitCommand ??= new DelegateCommand(Exit);

        private void Exit()
        {
            RequestClose.Invoke(ButtonResult.Cancel);
        }

        private Visibility loading;

        public Visibility Loading { get => loading; set => SetProperty(ref loading, value); }

        private DelegateCommand previewMouseDownLoginCommand;
        public ICommand PreviewMouseDownLoginCommand => previewMouseDownLoginCommand ??= new DelegateCommand(PreviewMouseDownLogin);

        private void PreviewMouseDownLogin()
        {
            Loading = Visibility.Visible;
        }
        #endregion

    }
}
