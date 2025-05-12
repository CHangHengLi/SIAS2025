using _2025毕业设计.Common;
using _2025毕业设计.Services;
using _2025毕业设计.ViewModels;
using _2025毕业设计.ViewModels.EditMessage.AdminManager;
using _2025毕业设计.ViewModels.EditMessage.AwardNominateManager;
using _2025毕业设计.ViewModels.EditMessage.AwardSettingManager;
using _2025毕业设计.ViewModels.EditMessage.DepartmentManager;
using _2025毕业设计.ViewModels.EditMessage.EmployeeManager;
using _2025毕业设计.ViewModels.EditMessage.NominationDeclarationManager;
using _2025毕业设计.ViewModels.EditMessage.NominationDetailsWindows;
using _2025毕业设计.ViewModels.EditMessage.NominationLogViewer;
using _2025毕业设计.ViewModels.EditMessage.PersonnallyManager;
using _2025毕业设计.ViewModels.Pages;
using _2025毕业设计.Views;
using _2025毕业设计.Views.EditMessage.AdminManager;
using _2025毕业设计.Views.EditMessage.AwardNominateManager;
using _2025毕业设计.Views.EditMessage.AwardSettingManager;
using _2025毕业设计.Views.EditMessage.DepartmentManager;
using _2025毕业设计.Views.EditMessage.EmployeeManager;
using _2025毕业设计.Views.EditMessage.NominationDeclarationManager;
using _2025毕业设计.Views.EditMessage.NominationDetailsWindows;
using _2025毕业设计.Views.EditMessage.NominationLogViewer;
using _2025毕业设计.Views.EditMessage.PersonnallyManager;
using _2025毕业设计.Views.Pages;
using System.Windows;

namespace _2025毕业设计
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        // 当前登录用户信息（仅用于存储引用，实际数据仍在静态CurrentUser类中）
        public static object Current_LoginUser { get; } = new object();
        
        protected override Window CreateShell()
        {
            // 创建并返回应用的主窗口
            return Container.Resolve<Home>();
        }
        
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 在这里注册应用的类型，如视图模型、服务等
            #region 主页
            containerRegistry.RegisterForNavigation<Home, HomeViewModel>();
            #endregion

            #region 登录页
            containerRegistry.RegisterDialog<Login, LoginViewModel>();
            #endregion

            #region 接口服务
            containerRegistry.RegisterSingleton<ISupAdminService, SupAdminService>();
            containerRegistry.RegisterSingleton<IAdminService, AdminService>();
            containerRegistry.RegisterSingleton<IEmployeeService, EmployeeService>();
            containerRegistry.RegisterSingleton<IDepartmentService, DepartmentService>();
            #endregion

            #region 管理员管理
            containerRegistry.RegisterForNavigation<AdminManager, AdminManagerViewModel>();
            //管理员新增,编辑页面
            containerRegistry.RegisterForNavigation<AddAdmin, AddAdminViewModel>();
            containerRegistry.RegisterForNavigation<EditAdmin, EditAdminViewModel>();
            #endregion

            #region 雇员管理
            containerRegistry.RegisterForNavigation<EmployeeManager, EmployeeManagerViewModel>();
            //雇员新增,编辑页面
            containerRegistry.RegisterForNavigation<AddEmployee, AddEmployeeViewModel>();
            containerRegistry.RegisterForNavigation<EditEmployee, EditEmployeeViewModel>();
            #endregion

            #region 部门管理
            containerRegistry.RegisterForNavigation<DepartmentManager, DepartmentManagerViewModel>();
            //部门新增,编辑页面
            containerRegistry.RegisterForNavigation<AddDepartment, AddDepartmentViewModel>();
            containerRegistry.RegisterForNavigation<EditDepartment, EditDepartmentViewModel>();
            #endregion

            #region 超级管理员个人信息页面
            containerRegistry.RegisterForNavigation<SupAdminPersonallyManager, SupAdminPersonallyManagerViewModel>();
            #endregion

            #region 管理员个人信息界面
            containerRegistry.RegisterForNavigation<AdminPersonallyManager, AdminPersonallyManagerViewModel>();
            #endregion

            #region 提名申报界面
            containerRegistry.RegisterForNavigation<NominationDeclaration, NominationDeclarationViewModel>();
            // 提名申报新增/编辑页面
            containerRegistry.RegisterForNavigation<AddNominationDeclaration, AddNominationDeclarationViewModel>();
            containerRegistry.RegisterForNavigation<EditNominationDeclaration, EditNominationDeclarationViewModel>();
            // 提名申报日志查看器
            containerRegistry.RegisterForNavigation<NominationLogViewer, NominationLogViewModel>();
            #endregion

            #region 雇员个人信息界面
            containerRegistry.RegisterForNavigation<EmployeePersonallyManager, EmployeePersonallyManagerViewModel>();
            #endregion

            #region 奖项设置界面
            containerRegistry.RegisterForNavigation<AwardSetting, AwardSettingViewModel>();
            //奖项设置新增,编辑页面
            containerRegistry.RegisterForNavigation<AddAwardSetting, AddAwardSettingViewModel>();
            containerRegistry.RegisterForNavigation<EditAwardSetting, EditAwardSettingViewModel>();
            #endregion

            #region 奖项提名界面
            containerRegistry.RegisterForNavigation<AwardNominate, AwardNominateViewModel>();
            //奖项提名新增,编辑页面
            containerRegistry.RegisterForNavigation<AddAwardNominate, AddAwardNominateViewModel>();
            containerRegistry.RegisterForNavigation<EditAwardNominate, EditAwardNominateViewModel>();
            #endregion

            #region 投票入口界面
            containerRegistry.RegisterForNavigation<VoteEntrance, VoteEntranceViewModel>();
            // 提名详情窗口
            containerRegistry.Register<NominationDetailsWindow>();
            containerRegistry.Register<NominationDetailsViewModel>();
            #endregion

            #region 投票结果界面
            containerRegistry.RegisterForNavigation<VoteResult, VoteResultViewModel>();
            #endregion
        }
        protected override void OnInitialized()
        {
            // 对话服务本身也受ioc管理
            ShowLoginDialog();
        }
        private void ShowLoginDialog()
        {
            var dialogService = Container.Resolve<IDialogService>(); // 从ioc容器中取对话服务

            dialogService.ShowDialog("Login", (dr) =>
            {
                if (dr.Result == ButtonResult.OK)
                {
                    // 用静态类LoginInfo把当前登录的信息存储一下。
                    if (dr.Parameters.ContainsKey("username") && dr.Parameters.ContainsKey("password") && dr.Parameters.ContainsKey("roleId") && dr.Parameters.ContainsKey("image"))
                    {
                        string username = dr.Parameters.GetValue<string>("username");
                        string password = dr.Parameters.GetValue<string>("password");
                        byte[] image = dr.Parameters.GetValue<byte[]>("image");
                        int roleId = dr.Parameters.GetValue<int>("roleId");
                        CurrentUser.Image = image;
                        CurrentUser.UserName = username;
                        CurrentUser.Password = password;
                        CurrentUser.RoleId = roleId;
                        
                        // 设置Account参数
                        if (dr.Parameters.ContainsKey("account"))
                        {
                            CurrentUser.Account = dr.Parameters.GetValue<string>("account");
                        }
                        
                        if (dr.Parameters.ContainsKey("adminId"))
                        {
                            CurrentUser.AdminId = dr.Parameters.GetValue<int>("adminId");
                        }
                        else if (roleId == 1) // 超级管理员
                        {
                            // 使用数据库上下文查找超级管理员ID
                            using (var context = new Context.DataBaseContext())
                            {
                                var supAdmin = context.SupAdmins.FirstOrDefault(s => s.SupAdminName == username);
                                if (supAdmin != null)
                                {
                                    CurrentUser.AdminId = supAdmin.SupAdminId;
                                }
                            }
                        }
                        if (dr.Parameters.ContainsKey("employeeId"))
                        {
                            CurrentUser.EmployeeId = dr.Parameters.GetValue<int>("employeeId");
                        }
                        base.OnInitialized();
                    }
                }
                else
                {
                    Current.Shutdown(); // 如果用户点击了取消按钮，则关闭应用
                }
            });
        }
    }
}
