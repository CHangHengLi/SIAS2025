using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using Microsoft.Win32;
using SIASGraduate.Common;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;

namespace SIASGraduate.ViewModels.EditMessage.AwardNominateManager
{
    public class AddAwardNominateViewModel : BindableBase
    {
        #region 区域导航
        private readonly IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 构造函数
        public AddAwardNominateViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            // 获取下一个提名编号
            using (var context = new DataBaseContext())
            {
                var maxId = context.Nominations.Any() ? context.Nominations.Select(n => n.NominationId).Max() : 0;
                NominationId = maxId + 1;
            }

            // 加载数据
            LoadAwards();
            LoadEmployees();

            // 只有超级管理员可以提名管理员
            if (CurrentUser.RoleId == 1) // 超级管理员
            {
                LoadAdmins();
                CanNominateAdmins = true;
            }
            else
            {
                // 对于管理员和普通员工角色
                Admins = new ObservableCollection<Admin>(); // 空集合
                CanNominateAdmins = false;
            }
        }
        #endregion

        #region 属性

        #region 提名编号
        private int nominationId;
        public int NominationId
        {
            get { return nominationId; }
            set { SetProperty(ref nominationId, value); }
        }
        #endregion

        #region 奖项列表
        private ObservableCollection<Award> awards;
        public ObservableCollection<Award> Awards
        {
            get { return awards; }
            set { SetProperty(ref awards, value); }
        }
        #endregion

        #region 选中的奖项
        private Award selectedAward;
        public Award SelectedAward
        {
            get { return selectedAward; }
            set { SetProperty(ref selectedAward, value); }
        }
        #endregion

        #region 员工列表
        private ObservableCollection<Employee> employees;
        public ObservableCollection<Employee> Employees
        {
            get { return employees; }
            set { SetProperty(ref employees, value); }
        }
        #endregion

        #region 管理员列表
        private ObservableCollection<Admin> admins;
        public ObservableCollection<Admin> Admins
        {
            get { return admins; }
            set { SetProperty(ref admins, value); }
        }
        #endregion

        #region 选中的提名对象
        private object selectedNominee;
        public object SelectedNominee
        {
            get { return selectedNominee; }
            set
            {
                SetProperty(ref selectedNominee, value);
                UpdateDepartment();
            }
        }
        #endregion

        #region 所属部门
        private string departmentName;
        public string DepartmentName
        {
            get { return departmentName; }
            set { SetProperty(ref departmentName, value); }
        }
        #endregion

        #region 一句话介绍
        private string introduction;
        public string Introduction
        {
            get { return introduction; }
            set { SetProperty(ref introduction, value); }
        }
        #endregion

        #region 提名理由
        private string nominateReason;
        public string NominateReason
        {
            get { return nominateReason; }
            set { SetProperty(ref nominateReason, value); }
        }
        #endregion

        #region 封面图片
        private byte[] coverImage;
        public byte[] CoverImage
        {
            get { return coverImage; }
            set { SetProperty(ref coverImage, value); }
        }
        #endregion

        #region 封面图片预览
        private BitmapImage coverImagePreview;
        public BitmapImage CoverImagePreview
        {
            get { return coverImagePreview; }
            set { SetProperty(ref coverImagePreview, value); }
        }
        #endregion

        #region 是否可以提名管理员
        private bool canNominateAdmins;
        public bool CanNominateAdmins
        {
            get { return canNominateAdmins; }
            set { SetProperty(ref canNominateAdmins, value); }
        }
        #endregion

        #endregion

        #region 命令
        private DelegateCommand saveCommand;
        public ICommand SaveCommand => saveCommand ??= new DelegateCommand(Save);

        private DelegateCommand cancelCommand;
        public ICommand CancelCommand => cancelCommand ??= new DelegateCommand(Cancel);

        private DelegateCommand uploadImageCommand;
        public ICommand UploadImageCommand => uploadImageCommand ??= new DelegateCommand(UploadImage);
        #endregion

        #region 方法
        private async void Save()
        {
            // 验证奖项
            if (SelectedAward == null)
            {
                Growl.WarningGlobal("请选择要提名的奖项");
                return;
            }

            // 验证提名对象
            if (SelectedNominee == null)
            {
                Growl.WarningGlobal("请选择提名对象");
                return;
            }

            // 检查是否选择了离职人员 - 新增提名时不应该选择离职人员
            bool isInactive = false;
            string nomineeType = "";
            string nomineeName = "";

            if (SelectedNominee is Employee employee)
            {
                if (employee.IsActive == false)
                {
                    isInactive = true;
                    nomineeType = "员工";
                    nomineeName = employee.EmployeeName;
                }
            }
            else if (SelectedNominee is Admin admin)
            {
                if (admin.IsActive == false)
                {
                    isInactive = true;
                    nomineeType = "管理员";
                    nomineeName = admin.AdminName;
                }
            }

            // 如果选择了离职人员，拒绝保存
            if (isInactive)
            {
                Growl.WarningGlobal($"您选择的{nomineeType} {nomineeName} 已离职，无法提名。请选择在职的员工或管理员。");
                return;
            }

            // 验证所属部门（如果是必填项）
            if (string.IsNullOrWhiteSpace(DepartmentName))
            {
                Growl.WarningGlobal("选择的提名对象未关联部门，请选择有部门的对象");
                return;
            }

            // 验证一句话介绍
            if (string.IsNullOrWhiteSpace(Introduction))
            {
                Growl.WarningGlobal("一句话介绍不能为空");
                return;
            }

            if (Introduction.Length > 50)
            {
                Growl.WarningGlobal("一句话介绍不能超过50字");
                return;
            }

            // 验证提名理由
            if (string.IsNullOrWhiteSpace(NominateReason))
            {
                Growl.WarningGlobal("提名理由不能为空");
                return;
            }

            // 验证当前用户信息（保证能正确设置提议人）
            if (CurrentUser.RoleId != 1 && CurrentUser.RoleId != 2 && CurrentUser.RoleId != 3)
            {
                Growl.WarningGlobal("当前用户身份不明确，无法设置提议人信息");
                return;
            }

            try
            {
                using (var context = new DataBaseContext())
                {
                    // 检查是否已存在该员工对该奖项的提名
                    bool duplicateNomination = false;

                    if (SelectedNominee is Employee selectedEmployee)
                    {
                        duplicateNomination = context.Nominations.Any(n =>
                            n.AwardId == SelectedAward.AwardId &&
                            n.NominatedEmployeeId == selectedEmployee.EmployeeId);
                        nomineeType = "员工";
                    }
                    else if (SelectedNominee is Admin selectedAdmin)
                    {
                        duplicateNomination = context.Nominations.Any(n =>
                            n.AwardId == SelectedAward.AwardId &&
                            n.NominatedAdminId == selectedAdmin.AdminId);
                        nomineeType = "管理员";
                    }

                    if (duplicateNomination)
                    {
                        Growl.WarningGlobal($"同一{nomineeType}只能申请一次同一奖项");
                        return;
                    }

                    var nomination = new Nomination
                    {
                        AwardId = SelectedAward.AwardId,
                        NominateReason = NominateReason,
                        Introduction = Introduction,
                        NominationTime = DateTime.Now,
                        CoverImage = CoverImage
                    };

                    if (SelectedNominee is Employee selectedEmployee1)
                    {
                        nomination.NominatedEmployeeId = selectedEmployee1.EmployeeId;
                        nomination.NominatedAdminId = null;
                        nomination.DepartmentId = selectedEmployee1.DepartmentId;
                    }
                    else if (SelectedNominee is Admin selectedAdmin)
                    {
                        nomination.NominatedAdminId = selectedAdmin.AdminId;
                        nomination.NominatedEmployeeId = null;
                        nomination.DepartmentId = selectedAdmin.DepartmentId;
                    }

                    // 设置提议人信息
                    if (CurrentUser.RoleId == 1) // 超级管理员
                    {
                        System.Diagnostics.Debug.WriteLine($"当前登录的超级管理员 ID: {CurrentUser.AdminId}, 用户名: {CurrentUser.UserName}, 账号: {CurrentUser.Account}");

                        // 使用超级管理员作为提议人
                        using (var lookupContext = new DataBaseContext())
                        {
                            // 尝试通过用户名或账号查找超级管理员
                            var supAdmin = lookupContext.SupAdmins.FirstOrDefault(s => s.SupAdminName == CurrentUser.UserName ||
                                                                                      s.Account == CurrentUser.Account ||
                                                                                      s.SupAdminId == CurrentUser.AdminId);
                            if (supAdmin != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"设置超级管理员作为提议人: {supAdmin.SupAdminId}, 用户名: {supAdmin.SupAdminName}");
                                nomination.ProposerSupAdminId = supAdmin.SupAdminId;
                                nomination.ProposerAdminId = null;
                                nomination.ProposerEmployeeId = null;
                            }
                            else
                            {
                                // 直接使用CurrentUser中的AdminId作为备选方案
                                if (CurrentUser.AdminId.HasValue)
                                {
                                    System.Diagnostics.Debug.WriteLine($"使用CurrentUser中的AdminId作为超级管理员ID: {CurrentUser.AdminId}");
                                    nomination.ProposerSupAdminId = CurrentUser.AdminId.Value;
                                    nomination.ProposerAdminId = null;
                                    nomination.ProposerEmployeeId = null;
                                }
                                else
                                {
                                    Growl.WarningGlobal("无法获取当前超级管理员信息");
                                    return;
                                }
                            }
                        }
                    }
                    else if (CurrentUser.RoleId == 2) // 管理员
                    {
                        if (CurrentUser.AdminId.HasValue)
                        {
                            nomination.ProposerAdminId = CurrentUser.AdminId.Value;
                            nomination.ProposerEmployeeId = null;
                        }
                        else
                        {
                            Growl.WarningGlobal("无法获取当前管理员ID");
                            return;
                        }
                    }
                    else if (CurrentUser.RoleId == 3) // 员工
                    {
                        if (CurrentUser.EmployeeId.HasValue)
                        {
                            nomination.ProposerEmployeeId = CurrentUser.EmployeeId.Value;
                            nomination.ProposerAdminId = null;
                        }
                        else
                        {
                            Growl.WarningGlobal("无法获取当前员工ID");
                            return;
                        }
                    }

                    context.Nominations.Add(nomination);
                    await context.SaveChangesAsync();
                }

                eventAggregator.GetEvent<NominationAddEvent>().Publish();
                Growl.SuccessGlobal("添加成功");
                Cancel();
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"添加失败：{ex.Message}");
            }
        }

        private void Cancel()
        {
            var region = regionManager.Regions["AwardNominateEditRegion"];
            region.RemoveAll();
        }

        private void UploadImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 读取图片文件
                    CoverImage = File.ReadAllBytes(openFileDialog.FileName);

                    // 创建图片预览
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = new MemoryStream(CoverImage);
                    image.EndInit();
                    CoverImagePreview = image;
                }
                catch (Exception ex)
                {
                    Growl.ErrorGlobal($"图片加载失败：{ex.Message}");
                }
            }
        }

        private void LoadAwards()
        {
            using (var context = new DataBaseContext())
            {
                Awards = new ObservableCollection<Award>(context.Awards.ToList());
            }
        }

        private void LoadEmployees()
        {
            using (var context = new DataBaseContext())
            {
                // 只加载在职员工（IsActive = true）
                Employees = new ObservableCollection<Employee>(
                    context.Employees
                    .Where(e => e.IsActive == true)
                    .ToList());
                System.Diagnostics.Debug.WriteLine($"已加载 {Employees.Count} 个在职员工");
            }
        }

        private void LoadAdmins()
        {
            using (var context = new DataBaseContext())
            {
                // 只加载在职管理员（IsActive = true）
                Admins = new ObservableCollection<Admin>(
                    context.Admins
                    .Where(a => a.IsActive == true)
                    .ToList());
                System.Diagnostics.Debug.WriteLine($"已加载 {Admins.Count} 个在职管理员");
            }
        }

        private void UpdateDepartment()
        {
            if (SelectedNominee == null) return;

            using (var context = new DataBaseContext())
            {
                if (SelectedNominee is Employee employee)
                {
                    var department = context.Departments.FirstOrDefault(d => d.DepartmentId == employee.DepartmentId);
                    DepartmentName = department?.DepartmentName ?? "未知部门";
                }
                else if (SelectedNominee is Admin admin)
                {
                    var department = context.Departments.FirstOrDefault(d => d.DepartmentId == admin.DepartmentId);
                    DepartmentName = department?.DepartmentName ?? "未知部门";
                }
            }
        }
        #endregion
    }
}
