using System.Reflection;
using System.Windows;
using _2025毕业设计.Models;
using Microsoft.EntityFrameworkCore;
using SIASGraduate.Common;
using SIASGraduate.Context;
using SIASGraduate.Models;
using SIASGraduate.ViewModels.EditMessage.NominationDetailsWindows;

namespace SIASGraduate.Views.EditMessage.NominationDetailsWindows
{
    /// <summary>
    /// NominationDetailsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NominationDetailsWindow : Window
    {
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public NominationDetailsWindow()
        {
            InitializeComponent();

            // 添加窗口加载完成事件
            this.Loaded += NominationDetailsWindow_Loaded;
        }

        /// <summary>
        /// 窗口加载完成事件
        /// </summary>
        private void NominationDetailsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 检查数据加载状态
            if (this.DataContext is NominationDetailsViewModel vm)
            {
                // 记录数据状态
                System.Diagnostics.Debug.WriteLine($"窗口加载完成 - 投票记录数: {vm.Nomination?.VoteRecords?.Count ?? 0}");

                // 如果没有数据，尝试再次加载
                if ((vm.Nomination?.VoteRecords == null || vm.Nomination.VoteRecords.Count == 0) &&
                    vm.Nomination?.NominationId > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"窗口已加载但没有投票数据，尝试再次加载数据...");
                    // 尝试重新加载提名数据
                    EnsureVoteRecordsLoaded(vm.Nomination.NominationId);
                }
                else
                {
                    // 即使有数据也重新触发属性变更通知，确保UI更新
                    System.Diagnostics.Debug.WriteLine("窗口已加载且有投票数据，触发属性变更通知...");
                    NotifyPropertyChanged(vm, nameof(vm.Nomination));
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("窗口加载完成，但DataContext不是NominationDetailsViewModel类型");
            }
        }

        /// <summary>
        /// 辅助方法，通过反射调用受保护的RaisePropertyChanged方法
        /// </summary>
        /// <param name="viewModel">视图模型对象</param>
        /// <param name="propertyName">属性名称</param>
        private void NotifyPropertyChanged(object viewModel, string propertyName)
        {
            try
            {
                // 通过手动刷新UI元素来解决属性变更通知问题
                if (VoteRecordsItemsControl != null)
                {
                    // 对于ItemsControl没有Items.Refresh()方法，可以通过刷新ItemsSource来更新
                    var itemsSource = VoteRecordsItemsControl.ItemsSource;
                    VoteRecordsItemsControl.ItemsSource = null;
                    VoteRecordsItemsControl.ItemsSource = itemsSource;
                }

                // 使用反射获取并调用OnPropertyChanged方法
                var methodInfo = viewModel.GetType().GetMethod("OnPropertyChanged",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                if (methodInfo != null)
                {
                    methodInfo.Invoke(viewModel, new object[] { propertyName });
                    System.Diagnostics.Debug.WriteLine($"已调用OnPropertyChanged方法: {propertyName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("无法找到OnPropertyChanged方法");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通知属性变更时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 确保投票记录被加载
        /// </summary>
        private void EnsureVoteRecordsLoaded(int nominationId)
        {
            try
            {
                using (var context = new DataBaseContext())
                {
                    // 使用分步方式加载数据，避免NotMapped属性问题

                    // 单独加载投票记录（不通过导航属性加载）
                    var voteRecords = context.VoteRecords
                        .AsNoTracking()
                        .Where(v => v.NominationId == nominationId)
                        .OrderByDescending(v => v.VoteTime)
                        .ToList();

                    if (this.DataContext is NominationDetailsViewModel vm)
                    {
                        // 手动加载关联实体
                        foreach (var record in voteRecords)
                        {
                            // 加载投票者(员工)信息
                            if (record.VoterEmployeeId.HasValue)
                            {
                                record.VoterEmployee = context.Employees
                                    .AsNoTracking()
                                    .FirstOrDefault(e => e.EmployeeId == record.VoterEmployeeId);

                                // 如果有员工，再加载其部门
                                if (record.VoterEmployee != null && record.VoterEmployee.DepartmentId.HasValue)
                                {
                                    record.VoterEmployee.Department = context.Departments
                                        .AsNoTracking()
                                        .FirstOrDefault(d => d.DepartmentId == record.VoterEmployee.DepartmentId.Value);
                                }
                            }

                            // 加载投票者(管理员)信息
                            if (record.VoterAdminId.HasValue)
                            {
                                record.VoterAdmin = context.Admins
                                    .AsNoTracking()
                                    .FirstOrDefault(a => a.AdminId == record.VoterAdminId);

                                // 如果有管理员，再加载其部门
                                if (record.VoterAdmin != null && record.VoterAdmin.DepartmentId.HasValue)
                                {
                                    record.VoterAdmin.Department = context.Departments
                                        .AsNoTracking()
                                        .FirstOrDefault(d => d.DepartmentId == record.VoterAdmin.DepartmentId.Value);
                                }
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"从数据库加载到 {voteRecords.Count} 条投票记录");

                        // 清空当前集合并添加新的记录
                        vm.Nomination.VoteRecords.Clear();
                        foreach (var record in voteRecords)
                        {
                            vm.Nomination.VoteRecords.Add(record);
                        }

                        System.Diagnostics.Debug.WriteLine($"已重新加载 {vm.Nomination.VoteRecords.Count} 条投票记录");

                        // 刷新ItemsControl显示
                        if (VoteRecordsItemsControl != null)
                        {
                            // 对于ItemsControl没有Items.Refresh()方法，可以通过刷新ItemsSource来更新
                            var itemsSource = VoteRecordsItemsControl.ItemsSource;
                            VoteRecordsItemsControl.ItemsSource = null;
                            VoteRecordsItemsControl.ItemsSource = itemsSource;
                        }

                        // 通过反射调用OnPropertyChanged方法更新其他属性
                        NotifyPropertyChanged(vm, nameof(vm.Nomination));
                        NotifyPropertyChanged(vm, nameof(vm.EmployeeVoteCount));
                        NotifyPropertyChanged(vm, nameof(vm.AdminVoteCount));
                        NotifyPropertyChanged(vm, nameof(vm.TotalVoteCount));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载投票记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从Nomination对象构造
        /// </summary>
        /// <param name="nomination">提名对象</param>
        public NominationDetailsWindow(Nomination nomination)
        {
            InitializeComponent();

            // 设置数据上下文为ViewModel
            var viewModel = new NominationDetailsViewModel(nomination);

            // 设置超级管理员标志
            SetSuperAdminFlag(viewModel);

            this.DataContext = viewModel;
        }

        /// <summary>
        /// 使用提名ID加载提名详情
        /// </summary>
        /// <param name="nominationId">提名ID</param>
        public void LoadNominationDetails(int nominationId)
        {
            try
            {
                // 预先声明viewModel变量
                NominationDetailsViewModel viewModel;

                using (var context = new DataBaseContext())
                {
                    // 使用分步方式加载数据，避免NotMapped属性问题

                    // 1. 首先获取基本的提名信息（只获取非导航属性）
                    var nomination = context.Nominations
                        .AsNoTracking()
                        .Where(n => n.NominationId == nominationId)
                        .Select(n => new Nomination
                        {
                            NominationId = n.NominationId,
                            AwardId = n.AwardId,
                            DepartmentId = n.DepartmentId,
                            NominatedEmployeeId = n.NominatedEmployeeId,
                            NominatedAdminId = n.NominatedAdminId,
                            ProposerEmployeeId = n.ProposerEmployeeId,
                            ProposerAdminId = n.ProposerAdminId,
                            Introduction = n.Introduction,
                            NominateReason = n.NominateReason,
                            CoverImage = n.CoverImage,
                            NominationTime = n.NominationTime
                        })
                        .FirstOrDefault();

                    if (nomination == null)
                    {
                        MessageBox.Show("未找到提名信息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 2. 单独加载各个关联实体
                    // 加载奖项
                    if (nomination.AwardId > 0)
                    {
                        nomination.Award = context.Awards
                            .AsNoTracking()
                            .FirstOrDefault(a => a.AwardId == nomination.AwardId);
                    }

                    // 加载部门
                    if (nomination.DepartmentId.HasValue && nomination.DepartmentId.Value > 0)
                    {
                        nomination.Department = context.Departments
                            .AsNoTracking()
                            .FirstOrDefault(d => d.DepartmentId == nomination.DepartmentId.Value);
                    }

                    // 加载被提名员工
                    if (nomination.NominatedEmployeeId.HasValue && nomination.NominatedEmployeeId.Value > 0)
                    {
                        nomination.NominatedEmployee = context.Employees
                            .AsNoTracking()
                            .FirstOrDefault(e => e.EmployeeId == nomination.NominatedEmployeeId.Value);
                    }

                    // 加载被提名管理员
                    if (nomination.NominatedAdminId.HasValue && nomination.NominatedAdminId.Value > 0)
                    {
                        nomination.NominatedAdmin = context.Admins
                            .AsNoTracking()
                            .FirstOrDefault(a => a.AdminId == nomination.NominatedAdminId.Value);
                    }

                    // 加载提名人(员工)
                    if (nomination.ProposerEmployeeId.HasValue && nomination.ProposerEmployeeId.Value > 0)
                    {
                        nomination.ProposerEmployee = context.Employees
                            .AsNoTracking()
                            .FirstOrDefault(e => e.EmployeeId == nomination.ProposerEmployeeId.Value);
                    }

                    // 加载提名人(管理员)
                    if (nomination.ProposerAdminId.HasValue && nomination.ProposerAdminId.Value > 0)
                    {
                        nomination.ProposerAdmin = context.Admins
                            .AsNoTracking()
                            .FirstOrDefault(a => a.AdminId == nomination.ProposerAdminId.Value);
                    }

                    // 3. 单独加载投票记录
                    var voteRecords = context.VoteRecords
                        .AsNoTracking()
                        .Where(v => v.NominationId == nominationId)
                        .OrderByDescending(v => v.VoteTime)
                        .ToList();

                    // 4. 加载投票记录的关联实体
                    foreach (var record in voteRecords)
                    {
                        if (record.VoterEmployeeId.HasValue)
                        {
                            record.VoterEmployee = context.Employees
                                .AsNoTracking()
                                .FirstOrDefault(e => e.EmployeeId == record.VoterEmployeeId);

                            // 如果有员工，再加载其部门
                            if (record.VoterEmployee != null && record.VoterEmployee.DepartmentId.HasValue)
                            {
                                record.VoterEmployee.Department = context.Departments
                                    .AsNoTracking()
                                    .FirstOrDefault(d => d.DepartmentId == record.VoterEmployee.DepartmentId.Value);
                            }
                        }

                        if (record.VoterAdminId.HasValue)
                        {
                            record.VoterAdmin = context.Admins
                                .AsNoTracking()
                                .FirstOrDefault(a => a.AdminId == record.VoterAdminId);

                            // 如果有管理员，再加载其部门
                            if (record.VoterAdmin != null && record.VoterAdmin.DepartmentId.HasValue)
                            {
                                record.VoterAdmin.Department = context.Departments
                                    .AsNoTracking()
                                    .FirstOrDefault(d => d.DepartmentId == record.VoterAdmin.DepartmentId.Value);
                            }
                        }
                    }

                    // 5. 将加载的投票记录设置到提名对象
                    nomination.VoteRecords = new System.Collections.ObjectModel.ObservableCollection<VoteRecord>(voteRecords);

                    // 设置数据上下文
                    viewModel = new NominationDetailsViewModel(nomination);

                    // 设置超级管理员标志
                    SetSuperAdminFlag(viewModel);

                    this.DataContext = viewModel;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载提名详情时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"加载提名详情错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// 使用提名详情DTO对象加载详情
        /// </summary>
        /// <param name="voteDetail">提名详情DTO</param>
        public void LoadNominationDetails(VoteDetailDto voteDetail)
        {
            if (voteDetail == null) return;

            try
            {
                // 预先声明viewModel变量，避免在不同作用域重复声明
                NominationDetailsViewModel viewModel;

                using (var context = new DataBaseContext())
                {
                    // 使用分步加载方式避免NotMapped属性问题

                    // 1. 首先获取基本的提名信息，使用Select投影只获取非导航属性
                    var nomination = context.Nominations
                        .AsNoTracking()
                        .Where(n => n.NominationId == voteDetail.NominationId)
                        .Select(n => new Nomination
                        {
                            NominationId = n.NominationId,
                            AwardId = n.AwardId,
                            DepartmentId = n.DepartmentId,
                            NominatedEmployeeId = n.NominatedEmployeeId,
                            NominatedAdminId = n.NominatedAdminId,
                            ProposerEmployeeId = n.ProposerEmployeeId,
                            ProposerAdminId = n.ProposerAdminId,
                            Introduction = n.Introduction,
                            NominateReason = n.NominateReason,
                            CoverImage = n.CoverImage,
                            NominationTime = n.NominationTime
                        })
                        .FirstOrDefault();

                    if (nomination == null)
                    {
                        // 如果找不到数据，则直接使用DTO创建ViewModel
                        viewModel = new NominationDetailsViewModel(voteDetail);
                        SetSuperAdminFlag(viewModel);
                        this.DataContext = viewModel;
                        return;
                    }

                    // 2. 单独加载各个关联实体
                    // 加载奖项
                    if (nomination.AwardId > 0)
                    {
                        nomination.Award = context.Awards
                            .AsNoTracking()
                            .FirstOrDefault(a => a.AwardId == nomination.AwardId);
                    }

                    // 加载部门
                    if (nomination.DepartmentId.HasValue && nomination.DepartmentId.Value > 0)
                    {
                        nomination.Department = context.Departments
                            .AsNoTracking()
                            .FirstOrDefault(d => d.DepartmentId == nomination.DepartmentId.Value);
                    }

                    // 加载被提名员工
                    if (nomination.NominatedEmployeeId.HasValue && nomination.NominatedEmployeeId.Value > 0)
                    {
                        nomination.NominatedEmployee = context.Employees
                            .AsNoTracking()
                            .FirstOrDefault(e => e.EmployeeId == nomination.NominatedEmployeeId.Value);
                    }

                    // 加载被提名管理员
                    if (nomination.NominatedAdminId.HasValue && nomination.NominatedAdminId.Value > 0)
                    {
                        nomination.NominatedAdmin = context.Admins
                            .AsNoTracking()
                            .FirstOrDefault(a => a.AdminId == nomination.NominatedAdminId.Value);
                    }

                    // 3. 单独加载投票记录
                    var voteRecords = context.VoteRecords
                        .AsNoTracking()
                        .Where(v => v.NominationId == voteDetail.NominationId)
                        .OrderByDescending(v => v.VoteTime)
                        .ToList();

                    // 4. 加载投票记录的关联实体
                    foreach (var record in voteRecords)
                    {
                        if (record.VoterEmployeeId.HasValue)
                        {
                            record.VoterEmployee = context.Employees
                                .AsNoTracking()
                                .FirstOrDefault(e => e.EmployeeId == record.VoterEmployeeId);

                            // 如果有员工，再加载其部门
                            if (record.VoterEmployee != null && record.VoterEmployee.DepartmentId.HasValue)
                            {
                                record.VoterEmployee.Department = context.Departments
                                    .AsNoTracking()
                                    .FirstOrDefault(d => d.DepartmentId == record.VoterEmployee.DepartmentId.Value);
                            }
                        }

                        if (record.VoterAdminId.HasValue)
                        {
                            record.VoterAdmin = context.Admins
                                .AsNoTracking()
                                .FirstOrDefault(a => a.AdminId == record.VoterAdminId);

                            // 如果有管理员，再加载其部门
                            if (record.VoterAdmin != null && record.VoterAdmin.DepartmentId.HasValue)
                            {
                                record.VoterAdmin.Department = context.Departments
                                    .AsNoTracking()
                                    .FirstOrDefault(d => d.DepartmentId == record.VoterAdmin.DepartmentId.Value);
                            }
                        }
                    }

                    // 5. 设置投票记录到提名对象
                    nomination.VoteRecords = new System.Collections.ObjectModel.ObservableCollection<VoteRecord>(voteRecords);

                    // 设置数据上下文
                    viewModel = new NominationDetailsViewModel(nomination);

                    // 设置超级管理员标志
                    SetSuperAdminFlag(viewModel);

                    this.DataContext = viewModel;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载提名详情时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"加载提名详情错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// 设置超级管理员标志
        /// </summary>
        /// <param name="viewModel">要设置的视图模型</param>
        private void SetSuperAdminFlag(NominationDetailsViewModel viewModel)
        {
            // 检查当前用户是否为超级管理员
            if (CurrentUser.RoleId == 1) // 假设1是超级管理员角色ID
            {
                viewModel.IsSuperAdmin = true;
            }
            else
            {
                viewModel.IsSuperAdmin = false;
            }
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
