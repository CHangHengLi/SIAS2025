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
    public class EmployeePersonallyManagerViewModel : BindableBase, INavigationAware
    {
        #region 数据库上下文
        private DataBaseContext context;
        #endregion

        #region 二进制图片存储ByteImage
        private byte[] byteImage;
        public byte[] ByteImage
        {
            get => byteImage;
            set
            {
                if (SetProperty(ref byteImage, value))
                {
                    Image = ConverterImage.ConvertByteArrayToBitmapImage(ByteImage);
                }
            }
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

        #region 当前雇员
        private Employee currentEmployee;

        public Employee CurrentEmployee { get => currentEmployee; set => SetProperty(ref currentEmployee, value); }
        #endregion
        
        #region 部门列表
        private List<Department> departments;
        public List<Department> Departments
        {
            get { return departments; }
            set { SetProperty(ref departments, value); }
        }
        #endregion

        #region 服务
        private readonly IEmployeeService employeeService;
        private readonly ISupAdminService supAdminService;
        private readonly IAdminService adminService;
        #endregion

        #region 构造函数
        public EmployeePersonallyManagerViewModel(IEmployeeService employeeService, ISupAdminService supAdminService, IAdminService adminService)
        {
            this.employeeService = employeeService;
            this.supAdminService = supAdminService;
            this.adminService = adminService;
            using (context = new DataBaseContext())
            {
                // 优先根据Account查找，如果找不到再根据UserName查找
                CurrentEmployee = null;
                
                // 如果Account有值，优先尝试用Account查询
                if (!string.IsNullOrEmpty(CurrentUser.Account))
                {
                    CurrentEmployee = context.Employees.FirstOrDefault(a => a.Account == CurrentUser.Account);
                }
                
                // 如果通过Account没找到，再尝试用UserName查询
                if (CurrentEmployee == null && !string.IsNullOrEmpty(CurrentUser.UserName))
                {
                    CurrentEmployee = context.Employees.FirstOrDefault(a => a.EmployeeName == CurrentUser.UserName);
                }
                
                // 如果还是没找到，创建一个新对象
                if (CurrentEmployee == null)
                {
                    CurrentEmployee = new Employee
                    {
                        Account = CurrentUser.Account ?? "",  // 确保Account有值
                        EmployeeName = CurrentUser.UserName,
                        EmployeePassword = CurrentUser.Password,
                        EmployeeImage = CurrentUser.Image
                    };
                }
                
                // 确保Account属性已设置（防止数据库中的记录没有Account值）
                if (string.IsNullOrEmpty(CurrentEmployee.Account))
                {
                    CurrentEmployee.Account = CurrentUser.Account ?? "";
                }
                
                Departments = context.Departments.ToList();
            }
        }
        #endregion

        #region 保存信息
        private DelegateCommand saveCommand;
        public DelegateCommand SaveCommand => saveCommand ??= new DelegateCommand(Save);

        private async void Save()
        {
            using (context = new DataBaseContext())
            {
                //姓名密码不能为空
                if (CurrentEmployee.EmployeeName.IsNullOrEmpty() || CurrentEmployee.EmployeePassword.IsNullOrEmpty())
                {
                    Growl.Warning("姓名或密码不能为空");
                    return;
                }
                // 如果用户名没有变化，直接更新
                if (CurrentEmployee.EmployeeName == CurrentUser.UserName)
                {
                    if (ByteImage != null)
                    {
                        CurrentEmployee.EmployeeImage = ByteImage;
                    }
                    context.Employees.Update(CurrentEmployee);
                    context.SaveChanges();
                    // 更新CurrentUser的信息
                    CurrentUser.UserName = CurrentEmployee.EmployeeName;
                    CurrentUser.Password = CurrentEmployee.EmployeePassword;
                    CurrentUser.Image = CurrentEmployee.EmployeeImage;
                    CurrentUser.Account = CurrentEmployee.Account;
                    Growl.Success("保存成功");
                    return;
                }
                
                // 检查用户名是否已存在，排除当前用户自己
                bool nameExists = await Task.Run(() =>
                {
                    // 检查超级管理员中是否有同名用户
                    bool existsInSupAdmin = supAdminService.IsSupAdminNameExist(CurrentEmployee.EmployeeName);
                    
                    // 检查管理员中是否有同名用户
                    bool existsInAdmin = adminService.IsAdminNameExist(CurrentEmployee.EmployeeName);
                    
                    // 检查员工中是否有同名用户（排除自己）
                    bool existsInEmployee = false;
                    using (var tempContext = new DataBaseContext())
                    {
                        existsInEmployee = tempContext.Employees
                            .Any(e => e.EmployeeName == CurrentEmployee.EmployeeName 
                                 && e.EmployeeId != CurrentEmployee.EmployeeId);
                    }
                    
                    return existsInSupAdmin || existsInAdmin || existsInEmployee;
                });
                
                //员工姓名不能够和超级管理员,管理员,员工重复
                if (nameExists)
                {
                    Growl.Warning("用户名已存在");
                    return;
                }
                
                if (ByteImage != null)
                {
                    CurrentEmployee.EmployeeImage = ByteImage;
                }

                context.Employees.Update(CurrentEmployee);
                context.SaveChanges();
                // 更新CurrentUser的信息
                CurrentUser.UserName = CurrentEmployee.EmployeeName;
                CurrentUser.Password = CurrentEmployee.EmployeePassword;
                CurrentUser.Image = CurrentEmployee.EmployeeImage;
                CurrentUser.Account = CurrentEmployee.Account;
                Growl.Success("保存成功");
            }
        }
        #endregion

        #region 图片修改
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

        #region INavigationAware 接口实现
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (CurrentEmployee.EmployeeImage != null)
            {
                Image = ConverterImage.ConvertByteArrayToBitmapImage(CurrentEmployee.EmployeeImage);
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
