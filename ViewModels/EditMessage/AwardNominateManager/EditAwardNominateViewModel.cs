using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using Microsoft.Win32;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using ConverterImage = SIASGraduate.Converter.ConVerterImage;

namespace SIASGraduate.ViewModels.EditMessage.AwardNominateManager
{
    public class EditAwardNominateViewModel : BindableBase, INavigationAware
    {
        #region 区域导航
        private readonly IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 构造函数
        public EditAwardNominateViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;
            LoadAwards();
            LoadEmployees();
            LoadAdmins();
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

        #region 当前提名
        private Nomination currentNomination;
        public Nomination CurrentNomination
        {
            get { return currentNomination; }
            set { SetProperty(ref currentNomination, value); }
        }
        #endregion

        #region 是否已有投票
        private bool hasVotes;
        public bool HasVotes
        {
            get { return hasVotes; }
            set
            {
                SetProperty(ref hasVotes, value);
                RaisePropertyChanged(nameof(CanEditAward));
                RaisePropertyChanged(nameof(CanEditNominee));
            }
        }
        #endregion

        #region 是否可以修改奖项
        public bool CanEditAward
        {
            get { return !HasVotes; }
        }
        #endregion

        #region 是否可以修改提名对象
        public bool CanEditNominee
        {
            get { return !HasVotes; }
        }
        #endregion

        #region 选中的Tab索引
        private int selectedTabIndex;
        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set { SetProperty(ref selectedTabIndex, value); }
        }
        #endregion

        #endregion

        #region 命令

        #region 保存命令
        private DelegateCommand saveCommand;
        public ICommand SaveCommand => saveCommand ??= new DelegateCommand(Save);

        private async void Save()
        {
            if (SelectedAward == null)
            {
                Growl.WarningGlobal("请选择要提名的奖项");
                return;
            }

            if (SelectedNominee == null)
            {
                Growl.WarningGlobal("请选择提名对象");
                return;
            }

            // 检查是否选择了离职人员
            bool isInactive = false;
            string nomineeType = "";
            string nomineeName = "";

            if (SelectedNominee is Employee employee && employee.IsActive == false)
            {
                isInactive = true;
                nomineeType = "员工";
                nomineeName = employee.EmployeeName;
            }
            else if (SelectedNominee is Admin admin && admin.IsActive == false)
            {
                isInactive = true;
                nomineeType = "管理员";
                nomineeName = admin.AdminName;
            }

            // 如果选择了离职人员，弹出确认
            if (isInactive)
            {
                var confirmResult = HandyControl.Controls.MessageBox.Show(
                    $"您选择的{nomineeType} {nomineeName} 已离职，确定要继续提名吗？",
                    "确认提名",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                {
                    // 用户选择不继续，退出保存
                    return;
                }
            }

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

            if (string.IsNullOrWhiteSpace(NominateReason))
            {
                Growl.WarningGlobal("提名理由不能为空");
                return;
            }

            try
            {
                using (var context = new DataBaseContext())
                {
                    var nomination = context.Nominations.Find(CurrentNomination.NominationId);
                    if (nomination == null)
                    {
                        Growl.ErrorGlobal("未找到要修改的提名记录");
                        return;
                    }

                    // 检查是否有投票记录
                    int voteCount = context.VoteRecords.Count(v => v.NominationId == nomination.NominationId);

                    // 如果已有投票，则不允许修改奖项和提名对象
                    if (voteCount > 0)
                    {
                        // 检查是否试图修改奖项
                        if (SelectedAward.AwardId != CurrentNomination.AwardId)
                        {
                            Growl.WarningGlobal("此提名已有投票，不能修改奖项");
                            return;
                        }

                        // 检查是否试图修改提名对象
                        bool isNomineeChanged = false;

                        if (SelectedNominee is Employee selectedEmployee && CurrentNomination.NominatedEmployeeId != selectedEmployee.EmployeeId)
                        {
                            isNomineeChanged = true;
                        }
                        else if (SelectedNominee is Admin selectedAdmin && CurrentNomination.NominatedAdminId != selectedAdmin.AdminId)
                        {
                            isNomineeChanged = true;
                        }
                        else if ((SelectedNominee is Employee && CurrentNomination.NominatedEmployeeId == null) ||
                                 (SelectedNominee is Admin && CurrentNomination.NominatedAdminId == null))
                        {
                            isNomineeChanged = true;
                        }

                        if (isNomineeChanged)
                        {
                            Growl.WarningGlobal("此提名已有投票，不能修改提名对象");
                            return;
                        }

                        // 只更新允许的字段
                        nomination.Introduction = Introduction;
                        nomination.NominateReason = NominateReason;
                        nomination.CoverImage = CoverImage;
                    }
                    else
                    {
                        // 如果没有投票，允许完全更新
                        nomination.AwardId = SelectedAward.AwardId;
                        nomination.NominateReason = NominateReason;
                        nomination.Introduction = Introduction;
                        nomination.CoverImage = CoverImage;

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
                    }

                    // 更新提名
                    context.Nominations.Update(nomination);
                    await context.SaveChangesAsync();
                }

                eventAggregator.GetEvent<NominationUpdateEvent>().Publish();
                Growl.SuccessGlobal("修改成功");
                Cancel();
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"修改失败：{ex.Message}");
            }
        }
        #endregion

        #region 取消命令
        private DelegateCommand cancelCommand;
        public ICommand CancelCommand => cancelCommand ??= new DelegateCommand(Cancel);

        private void Cancel()
        {
            var region = regionManager.Regions["AwardNominateEditRegion"];
            region.RemoveAll();
        }
        #endregion

        #region 上传图片命令
        private DelegateCommand uploadImageCommand;
        public ICommand UploadImageCommand => uploadImageCommand ??= new DelegateCommand(UploadImage);

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
        #endregion

        #endregion

        #region 方法
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

        private void ShowDeclaration()
        {
            // 设置预览图片
            CoverImagePreview = ConverterImage.ConvertByteArrayToBitmapImage(CoverImage);
        }
        #endregion

        #region INavigationAware接口实现
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("Nomination"))
            {
                try
                {
                    CurrentNomination = navigationContext.Parameters.GetValue<Nomination>("Nomination");
                    if (CurrentNomination == null)
                    {
                        Growl.ErrorGlobal("未找到要编辑的提名记录");
                        Cancel();
                        return;
                    }

                    NominationId = CurrentNomination.NominationId;

                    // 查询当前提名是否已有投票 - 先查询再设置其他属性
                    using (var context = new DataBaseContext())
                    {
                        int voteCount = context.VoteRecords.Count(v => v.NominationId == CurrentNomination.NominationId);
                        HasVotes = voteCount > 0;

                        if (HasVotes)
                        {
                            Growl.InfoGlobal($"此提名已有{voteCount}个投票，奖项和提名对象不能修改");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("无投票，可编辑所有字段");
                        }
                    }

                    // 设置基本属性
                    SelectedAward = Awards.FirstOrDefault(a => a.AwardId == CurrentNomination.AwardId);
                    Introduction = CurrentNomination.Introduction;
                    NominateReason = CurrentNomination.NominateReason;
                    CoverImage = CurrentNomination.CoverImage;
                    if (CoverImage != null)
                    {
                        ShowDeclaration();
                    }

                    // 设置被提名人 - 处理离职情况
                    using (var context = new DataBaseContext())
                    {
                        // 如果是员工
                        if (CurrentNomination.NominatedEmployeeId.HasValue)
                        {
                            // 从数据库中查询员工，包括离职的
                            var employeeInDb = context.Employees.FirstOrDefault(e =>
                                e.EmployeeId == CurrentNomination.NominatedEmployeeId);

                            if (employeeInDb != null)
                            {
                                // 如果员工已离职且未在列表中
                                if (employeeInDb.IsActive == false &&
                                    !Employees.Any(e => e.EmployeeId == employeeInDb.EmployeeId))
                                {
                                    // 提示用户原提名对象已离职
                                    Growl.WarningGlobal($"提名的员工 {employeeInDb.EmployeeName} 已离职。您可以选择其他在职员工或继续使用该员工。");

                                    // 添加到当前列表以便选择
                                    Employees.Add(employeeInDb);
                                }

                                // 设置为当前选中项
                                SelectedNominee = Employees.FirstOrDefault(e => e.EmployeeId == employeeInDb.EmployeeId);
                                // 设置TabControl选中员工标签页
                                SelectedTabIndex = 0;
                            }
                        }
                        // 如果是管理员
                        else if (CurrentNomination.NominatedAdminId.HasValue)
                        {
                            // 从数据库中查询管理员，包括离职的
                            var adminInDb = context.Admins.FirstOrDefault(a =>
                                a.AdminId == CurrentNomination.NominatedAdminId);

                            if (adminInDb != null)
                            {
                                // 如果管理员已离职且未在列表中
                                if (adminInDb.IsActive == false &&
                                    !Admins.Any(a => a.AdminId == adminInDb.AdminId))
                                {
                                    // 提示用户原提名对象已离职
                                    Growl.WarningGlobal($"提名的管理员 {adminInDb.AdminName} 已离职。您可以选择其他在职管理员或继续使用该管理员。");

                                    // 添加到当前列表以便选择
                                    Admins.Add(adminInDb);
                                }

                                // 设置为当前选中项
                                SelectedNominee = Admins.FirstOrDefault(a => a.AdminId == adminInDb.AdminId);
                                // 设置TabControl选中管理员标签页
                                SelectedTabIndex = 1;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Growl.ErrorGlobal($"加载提名信息失败：{ex.Message}");
                    Cancel();
                }
            }
            else
            {
                Growl.WarningGlobal("未指定要编辑的提名记录");
                Cancel();
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
