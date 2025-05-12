using _2025毕业设计.Context;
using _2025毕业设计.Models;
using _2025毕业设计.ViewModels.EditMessage.NominationDetailsWindows;
using _2025毕业设计.Common;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Linq;
using System.Reflection;

namespace _2025毕业设计.Views.EditMessage.NominationDetailsWindows
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
                    // 加载完整的提名信息包括投票记录
                    var nomination = context.Nominations
                        .Include(n => n.VoteRecords)
                            .ThenInclude(v => v.VoterEmployee)
                                .ThenInclude(e => e.Department)
                        .Include(n => n.VoteRecords)
                            .ThenInclude(v => v.VoterAdmin)
                                .ThenInclude(a => a.Department)
                        .AsNoTracking() // 使用AsNoTracking提高性能
                        .FirstOrDefault(n => n.NominationId == nominationId);
                    
                    if (nomination != null && this.DataContext is NominationDetailsViewModel vm)
                    {
                        // 显示加载结果
                        System.Diagnostics.Debug.WriteLine($"从数据库加载到 {nomination.VoteRecords?.Count ?? 0} 条投票记录");
                        
                        // 获取按时间排序的投票记录
                        var sortedRecords = nomination.VoteRecords
                            .OrderByDescending(v => v.VoteTime)
                            .ToList();
                        
                        // 清空当前集合并添加新的记录
                        vm.Nomination.VoteRecords.Clear();
                        foreach (var record in sortedRecords)
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
                using (var context = new DataBaseContext())
                {
                    // 使用Include加载提名关联的各种信息
                    var nomination = context.Nominations
                        .Include(n => n.Award)
                        .Include(n => n.Department)
                        .Include(n => n.NominatedEmployee)
                        .Include(n => n.NominatedAdmin)
                        .Include(n => n.ProposerEmployee)
                        .Include(n => n.ProposerAdmin)
                        .Include(n => n.VoteRecords)
                            .ThenInclude(v => v.VoterEmployee)
                                .ThenInclude(e => e.Department)
                        .Include(n => n.VoteRecords)
                            .ThenInclude(v => v.VoterAdmin)
                                .ThenInclude(a => a.Department)
                        .FirstOrDefault(n => n.NominationId == nominationId);
                        
                    if (nomination == null)
                    {
                        MessageBox.Show("未找到提名信息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // 设置数据上下文
                    var viewModel = new NominationDetailsViewModel(nomination);
                    
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
                using (var context = new DataBaseContext())
                {
                    // 尝试加载完整的Nomination对象
                    var nomination = context.Nominations
                        .Include(n => n.Award)
                        .Include(n => n.Department)
                        .Include(n => n.NominatedEmployee)
                        .Include(n => n.NominatedAdmin)
                        .Include(n => n.VoteRecords)
                            .ThenInclude(v => v.VoterEmployee)
                                .ThenInclude(e => e.Department)
                        .Include(n => n.VoteRecords)
                            .ThenInclude(v => v.VoterAdmin)
                                .ThenInclude(a => a.Department)
                        .FirstOrDefault(n => n.NominationId == voteDetail.NominationId);
                        
                    if (nomination != null)
                    {
                        // 创建ViewModel并设置为数据上下文
                        var viewModel = new NominationDetailsViewModel(nomination);
                        
                        // 设置超级管理员标志
                        SetSuperAdminFlag(viewModel);
                        
                        this.DataContext = viewModel;
                    }
                    else
                    {
                        MessageBox.Show("未找到对应的提名信息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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