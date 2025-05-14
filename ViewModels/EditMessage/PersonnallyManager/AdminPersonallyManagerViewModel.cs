using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using Microsoft.IdentityModel.Tokens;
using SIASGraduate.Common;
using SIASGraduate.Context;
using SIASGraduate.Models;
using SIASGraduate.Services;
using ConverterImage = SIASGraduate.Converter.ConVerterImage;

namespace SIASGraduate.ViewModels.EditMessage.PersonnallyManager
{
    public class AdminPersonallyManagerViewModel : BindableBase, INavigationAware

    {
        #region 数据库上下文
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

        #region 当前管理员
        private Admin currentAdmin;
        public Admin CurrentAdmin { get => currentAdmin; set => SetProperty(ref currentAdmin, value); }
        #endregion

        #region 服务
        private readonly IEmployeeService employeeService;
        private readonly ISupAdminService supAdminService;
        private readonly IAdminService adminService;
        #endregion

        #region 构造函数
        public AdminPersonallyManagerViewModel(IEmployeeService employeeService, ISupAdminService supAdminService, IAdminService adminService)
        {
            this.employeeService = employeeService;
            this.supAdminService = supAdminService;
            this.adminService = adminService;
            using (context = new DataBaseContext())
            {
                // 优先根据Account查找，如果找不到再根据UserName查找
                CurrentAdmin = null;

                // 如果Account有值，优先尝试用Account查询
                if (!string.IsNullOrEmpty(CurrentUser.Account))
                {
                    CurrentAdmin = context.Admins.FirstOrDefault(a => a.Account == CurrentUser.Account);
                }

                // 如果通过Account没找到，再尝试用UserName查询
                if (CurrentAdmin == null && !string.IsNullOrEmpty(CurrentUser.UserName))
                {
                    CurrentAdmin = context.Admins.FirstOrDefault(a => a.AdminName == CurrentUser.UserName);
                }

                // 如果还是没找到，创建一个新对象
                if (CurrentAdmin == null)
                {
                    CurrentAdmin = new Admin
                    {
                        Account = CurrentUser.Account ?? "",  // 确保Account有值
                        AdminName = CurrentUser.UserName,
                        AdminPassword = CurrentUser.Password,
                        AdminImage = CurrentUser.Image
                    };
                }

                // 确保Account属性已设置（防止数据库中的记录没有Account值）
                if (string.IsNullOrEmpty(CurrentAdmin.Account))
                {
                    CurrentAdmin.Account = CurrentUser.Account ?? "";
                }
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
                //账号密码不能为空
                if (CurrentAdmin.AdminName.IsNullOrEmpty() || CurrentAdmin.AdminPassword.IsNullOrEmpty())
                {
                    Growl.Warning("姓名或密码不能为空");
                    return;
                }

                // 验证密码长度
                if (CurrentAdmin.AdminPassword.Length < 6 || CurrentAdmin.AdminPassword.Length > 20)
                {
                    Growl.Warning("密码长度必须在6-20位之间");
                    return;
                }

                // 如果用户名没有变化，直接更新
                if (CurrentAdmin.AdminName == CurrentUser.UserName)
                {
                    if (ByteImage != null)
                    {
                        CurrentAdmin.AdminImage = ByteImage;
                    }
                    context.Admins.Update(CurrentAdmin);
                    context.SaveChanges();
                    // 更新CurrentUser的信息
                    CurrentUser.UserName = CurrentAdmin.AdminName;
                    CurrentUser.Password = CurrentAdmin.AdminPassword;
                    CurrentUser.Image = CurrentAdmin.AdminImage;
                    CurrentUser.Account = CurrentAdmin.Account;
                    Growl.Success("修改成功");
                    return;
                }

                // 检查用户名是否已存在，排除当前用户自己
                bool nameExists = await Task.Run(() =>
                {
                    // 检查超级管理员中是否有同名用户
                    bool existsInSupAdmin = supAdminService.IsSupAdminNameExist(CurrentAdmin.AdminName);

                    // 检查管理员中是否有同名用户（排除自己）
                    bool existsInAdmin = false;
                    using (var tempContext = new DataBaseContext())
                    {
                        existsInAdmin = tempContext.Admins
                            .Any(a => a.AdminName == CurrentAdmin.AdminName && a.AdminId != CurrentAdmin.AdminId);
                    }

                    // 检查员工中是否有同名用户
                    bool existsInEmployee = employeeService.IsEmployeeNameExist(CurrentAdmin.AdminName);

                    return existsInSupAdmin || existsInAdmin || existsInEmployee;
                });

                if (nameExists)
                {
                    Growl.Warning("用户名已存在");
                    return;
                }

                if (ByteImage != null)
                {
                    CurrentAdmin.AdminImage = ByteImage;
                }
                context.Admins.Update(CurrentAdmin);
                context.SaveChanges();
                // 更新CurrentUser的信息
                CurrentUser.UserName = CurrentAdmin.AdminName;
                CurrentUser.Password = CurrentAdmin.AdminPassword;
                CurrentUser.Image = CurrentAdmin.AdminImage;
                CurrentUser.Account = CurrentAdmin.Account;
                Growl.Success("修改成功");
            }
        }
        #endregion

        #region 修改头像
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
            if (CurrentAdmin.AdminImage != null)
            {
                Image = ConverterImage.ConvertByteArrayToBitmapImage(CurrentAdmin.AdminImage);
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
