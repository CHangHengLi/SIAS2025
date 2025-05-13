using SIASGraduate.Common;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using SIASGraduate.ViewModels.EditMessage.NominationDetailsWindows;
using SIASGraduate.Views.EditMessage.NominationDetailsWindows;

namespace SIASGraduate.ViewModels.Pages
{
    /// <summary>
    /// 投票入口视图模型
    /// 负责管理投票入口页面的数据和逻辑，包括:
    /// 1. 奖项列表加载和筛选
    /// 2. 提名数据加载和分页显示
    /// 3. 投票功能
    /// 4. 评论功能
    /// 5. 分页导航
    /// </summary>
    public class VoteEntranceViewModel : BindableBase
    {
        #region 字段和属性

        #region 奖项相关属性

        // 特殊奖项对象，表示"全部奖项"
        private Award _allAwards;
        /// <summary>
        /// 表示"全部奖项"的特殊选项
        /// </summary>
        public Award AllAwards
        {
            get { return _allAwards; }
            private set { SetProperty(ref _allAwards, value); }
        }

        private ObservableCollection<Award> _awards;
        /// <summary>
        /// 奖项列表
        /// </summary>
        public ObservableCollection<Award> Awards
        {
            get { return _awards; }
            set { SetProperty(ref _awards, value); }
        }

        private Award _selectedAward;
        /// <summary>
        /// 选中的奖项
        /// </summary>
        public Award SelectedAward
        {
            get { return _selectedAward; }
            set 
            { 
                SetProperty(ref _selectedAward, value);
                if (value != null)
                {
                    // 选中奖项后，加载提名并检查投票状态
                    LoadNominationsAndCheckVoteStatusAsync();
                }
            }
        }

        /// <summary>
        /// 用于存储当前筛选出的匹配奖项
        /// </summary>
        private List<Award> _filteredAwards = new List<Award>();

        #endregion

        #region 提名相关属性
        
        private ObservableCollection<Nomination> _nominations;
        /// <summary>
        /// 提名列表
        /// </summary>
        public ObservableCollection<Nomination> Nominations
        {
            get { return _nominations; }
            set { SetProperty(ref _nominations, value); }
        }

        private ObservableCollection<Nomination> _pagedNominations;
        /// <summary>
        /// 分页后的提名列表
        /// </summary>
        public ObservableCollection<Nomination> PagedNominations
        {
            get { return _pagedNominations; }
            set { SetProperty(ref _pagedNominations, value); }
        }

        private Nomination _selectedNomination;
        /// <summary>
        /// 选中的提名
        /// </summary>
        public Nomination SelectedNomination
        {
            get { return _selectedNomination; }
            set { SetProperty(ref _selectedNomination, value); }
        }

        #endregion

        #region 分页相关属性
        
        private int _currentPage = 1;
        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get { return _currentPage; }
            set 
            { 
                if (SetProperty(ref _currentPage, value))
                {
                    UpdatePagedNominations();
                }
            }
        }

        private int _pageSize = 5;
        /// <summary>
        /// 每页显示条数
        /// </summary>
        public int PageSize
        {
            get { return _pageSize; }
            set 
            { 
                SetProperty(ref _pageSize, value);
                // 页大小改变后，重置为第一页并刷新数据
                CurrentPage = 1;
                UpdatePagedNominations();
            }
        }

        private int _totalPages = 1;
        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get { return _totalPages; }
            set { SetProperty(ref _totalPages, value); }
        }

        private bool _isPaginationVisible = true;
        /// <summary>
        /// 是否显示分页控件
        /// </summary>
        public bool IsPaginationVisible
        {
            get { return _isPaginationVisible; }
            set { SetProperty(ref _isPaginationVisible, value); }
        }

        private ObservableCollection<int> _pageSizeOptions;
        /// <summary>
        /// 每页显示条数选项
        /// </summary>
        public ObservableCollection<int> PageSizeOptions
        {
            get { return _pageSizeOptions; }
            set { SetProperty(ref _pageSizeOptions, value); }
        }

        private int _totalItems;
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalItems
        {
            get { return _totalItems; }
            set { SetProperty(ref _totalItems, value); }
        }

        private string _searchText;
        /// <summary>
        /// 跳转页码输入框文本
        /// </summary>
        public string SearchText
        {
            get { return _searchText; }
            set { SetProperty(ref _searchText, value); }
        }

        #endregion

        #region 状态与搜索相关属性
        
        private bool _isLoading;
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }

        private string _statusMessage;
        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetProperty(ref _statusMessage, value); }
        }

        private string _searchKeyword;
        /// <summary>
        /// 搜索关键字
        /// </summary>
        public string SearchKeyword
        {
            get { return _searchKeyword; }
            set { SetProperty(ref _searchKeyword, value); }
        }

        private string _searchedAwardName;
        /// <summary>
        /// 在ComboBox中输入的奖项名称
        /// </summary>
        public string SearchedAwardName
        {
            get { return _searchedAwardName; }
            set 
            { 
                SetProperty(ref _searchedAwardName, value);
                
                if (!string.IsNullOrEmpty(value))
                {
                    // 当用户输入文本时，尝试在奖项列表中找到匹配项
                    var matchedAward = Awards?.FirstOrDefault(a => a.AwardName != null && 
                        a.AwardName.Contains(value, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchedAward != null && matchedAward != SelectedAward)
                    {
                        // 自动设置匹配的奖项为选中项
                        SelectedAward = matchedAward;
                    }
                }
            }
        }

        private bool _isActive = true;
        /// <summary>
        /// 指示ViewModel是否处于活动状态
        /// </summary>
        public bool IsActive
        {
            get { return _isActive; }
            private set { SetProperty(ref _isActive, value); }
        }

        #endregion

        #region 用户与投票相关属性
        
        private ObservableCollection<int> _votedAwardIds;
        /// <summary>
        /// 用户已投票的奖项ID集合
        /// </summary>
        public ObservableCollection<int> VotedAwardIds
        {
            get => _votedAwardIds;
            set => SetProperty(ref _votedAwardIds, value);
        }

        private Dictionary<int, int> _awardVoteCount = new Dictionary<int, int>();
        /// <summary>
        /// 用户对每个奖项的投票次数统计
        /// </summary>
        public Dictionary<int, int> AwardVoteCount
        {
            get => _awardVoteCount;
            set => SetProperty(ref _awardVoteCount, value);
        }

        private bool _hasVotedCurrentAward;
        /// <summary>
        /// 当前用户是否已对当前奖项投票
        /// </summary>
        public bool HasVotedCurrentAward
        {
            get { return _hasVotedCurrentAward; }
            set { SetProperty(ref _hasVotedCurrentAward, value); }
        }

        private int? _currentEmployeeId;
        /// <summary>
        /// 当前登录的员工ID
        /// </summary>
        public int? CurrentEmployeeId
        {
            get { return _currentEmployeeId; }
            set { SetProperty(ref _currentEmployeeId, value); }
        }

        private int? _currentAdminId;
        /// <summary>
        /// 当前登录的管理员ID
        /// </summary>
        public int? CurrentAdminId
        {
            get { return _currentAdminId; }
            set { SetProperty(ref _currentAdminId, value); }
        }

        private int? _currentSupAdminId;
        /// <summary>
        /// 当前登录的超级管理员ID
        /// </summary>
        public int? CurrentSupAdminId
        {
            get { return _currentSupAdminId; }
            set { SetProperty(ref _currentSupAdminId, value); }
        }

        private bool _isSuperAdmin;
        /// <summary>
        /// 是否是超级管理员
        /// </summary>
        public bool IsSuperAdmin
        {
            get { return _isSuperAdmin; }
            private set { SetProperty(ref _isSuperAdmin, value); }
        }

        private bool _isAdmin;
        /// <summary>
        /// 是否是管理员(包括超级管理员和普通管理员)
        /// </summary>
        public bool IsAdmin
        {
            get { return _isAdmin; }
            private set { SetProperty(ref _isAdmin, value); }
        }
        
        #endregion

        #endregion

        #region 命令

        #region 基础操作命令
        
        /// <summary>
        /// 投票命令
        /// </summary>
        public DelegateCommand<Nomination> VoteCommand { get; private set; }

        /// <summary>
        /// 处理投票按钮点击事件，即使按钮被禁用也会执行
        /// </summary>
        public DelegateCommand<Nomination> VoteButtonClickCommand { get; private set; }

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; private set; }

        /// <summary>
        /// 筛选奖项命令，用于输入时实时筛选
        /// </summary>
        public DelegateCommand<string> FilterAwardsCommand { get; private set; }

        /// <summary>
        /// Tab键自动完成命令
        /// </summary>
        public DelegateCommand<string> TabCompleteCommand { get; private set; }

        /// <summary>
        /// 处理特殊按键事件
        /// </summary>
        public DelegateCommand<object> HandlePreviewKeyDownCommand { get; private set; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; private set; }

        #endregion

        #region 评论相关命令
        
        /// <summary>
        /// 显示评论区命令
        /// </summary>
        public DelegateCommand<Nomination> ShowCommentsCommand { get; private set; }

        /// <summary>
        /// 隐藏评论区命令
        /// </summary>
        public DelegateCommand<Nomination> HideCommentsCommand { get; private set; }

        /// <summary>
        /// 添加评论命令
        /// </summary>
        public DelegateCommand<Nomination> AddCommentCommand { get; private set; }

        /// <summary>
        /// 删除评论命令
        /// </summary>
        public DelegateCommand<CommentRecord> DeleteCommentCommand { get; private set; }

        /// <summary>
        /// 加载更多评论命令
        /// </summary>
        public DelegateCommand<Nomination> LoadMoreCommentsCommand { get; private set; }

        #endregion

        #region 分页导航命令
        
        /// <summary>
        /// 首页命令
        /// </summary>
        public DelegateCommand FirstPageCommand { get; private set; }

        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand { get; private set; }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand { get; private set; }

        /// <summary>
        /// 末页命令
        /// </summary>
        public DelegateCommand LastPageCommand { get; private set; }

        /// <summary>
        /// 页大小改变命令
        /// </summary>
        public DelegateCommand<object> PageSizeChangedCommand { get; private set; }

        /// <summary>
        /// 跳转到指定页命令
        /// </summary>
        public DelegateCommand JumpPageCommand { get; private set; }

        /// <summary>
        /// 输入验证命令
        /// </summary>
        public DelegateCommand<string> PreviewTextInputCommand { get; private set; }

        #endregion

        #region 页面事件处理命令
        
        private DelegateCommand<int> _pageUpdatedCommand;
        /// <summary>
        /// 页码更新命令
        /// </summary>
        public DelegateCommand<int> PageUpdatedCommand =>
            _pageUpdatedCommand ?? (_pageUpdatedCommand = new DelegateCommand<int>(ExecutePageUpdatedCommand));
        
        private DelegateCommand _scrollToTopCommand;
        /// <summary>
        /// 滚动到顶部命令
        /// </summary>
        public DelegateCommand ScrollToTopCommand =>
            _scrollToTopCommand ?? (_scrollToTopCommand = new DelegateCommand(ExecuteScrollToTopCommand));

        /// <summary>
        /// 滚动到顶部请求事件
        /// </summary>
        public event Action ScrollToTopRequested;
        
        /// <summary>
        /// 执行页码更新命令
        /// </summary>
        private void ExecutePageUpdatedCommand(int pageNumber)
        {
            if (pageNumber > 0 && pageNumber <= TotalPages)
            {
                CurrentPage = pageNumber;
                // 更新页面数据
                UpdatePagedNominations();
                
                // 触发滚动到顶部命令
                ScrollToTopCommand.Execute();
            }
        }
        
        /// <summary>
        /// 执行滚动到顶部命令
        /// </summary>
        private void ExecuteScrollToTopCommand()
        {
            // 通过事件通知View滚动到顶部
            ScrollToTopRequested?.Invoke();
        }
        
        private DelegateCommand<object> _comboBoxPreviewKeyDownCommand;
        /// <summary>
        /// ComboBox预览键盘按下命令
        /// </summary>
        public DelegateCommand<object> ComboBoxPreviewKeyDownCommand =>
            _comboBoxPreviewKeyDownCommand ?? (_comboBoxPreviewKeyDownCommand = new DelegateCommand<object>(ExecuteComboBoxPreviewKeyDownCommand));
        
        private DelegateCommand<object> _comboBoxKeyDownCommand;
        /// <summary>
        /// ComboBox键盘按下命令
        /// </summary>
        public DelegateCommand<object> ComboBoxKeyDownCommand =>
            _comboBoxKeyDownCommand ?? (_comboBoxKeyDownCommand = new DelegateCommand<object>(ExecuteComboBoxKeyDownCommand));
        
        /// <summary>
        /// 处理ComboBox的PreviewKeyDown事件
        /// </summary>
        private void ExecuteComboBoxPreviewKeyDownCommand(object parameter)
        {
            // 直接接收KeyEventArgs
            if (parameter is System.Windows.Input.KeyEventArgs keyArgs)
            {
                if (keyArgs.Key == System.Windows.Input.Key.Tab)
                {
                    Debug.WriteLine("捕获PreviewKeyDown Tab键");
                    
                    // 获取当前ComboBox (通过事件源)
                    var comboBox = keyArgs.Source as System.Windows.Controls.ComboBox;
                    if (comboBox != null)
                    {
                        // 调用TabCompleteCommand命令
                        if (TabCompleteCommand != null)
                        {
                            // 直接调用命令，传入当前文本
                            TabCompleteCommand.Execute(comboBox.Text);
                            Debug.WriteLine($"直接执行TabCompleteCommand，参数：{comboBox.Text}");
                        }
                        
                        // 标记事件为已处理，阻止默认Tab行为
                        keyArgs.Handled = true;
                        
                        // 确保焦点留在当前控件上
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                            var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as System.Windows.Controls.TextBox;
                            if (textBox != null)
                            {
                                textBox.Focus();
                            }
                            else
                            {
                                comboBox.Focus();
                            }
                        }), System.Windows.Threading.DispatcherPriority.Input);
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理ComboBox的KeyDown事件
        /// </summary>
        private void ExecuteComboBoxKeyDownCommand(object parameter)
        {
            // 转换参数
            if (parameter is System.Windows.Input.KeyEventArgs keyArgs)
            {
                if (keyArgs.Key == System.Windows.Input.Key.Tab)
                {
                    Debug.WriteLine("Tab键被按下 - KeyDown阶段");
                    
                    // 事件已在PreviewKeyDown阶段处理，这里只做备份处理
                    keyArgs.Handled = true;
                }
            }
        }
        
        #endregion

        #endregion

        #region 构造函数与初始化

        /// <summary>
        /// 构造函数
        /// </summary>
        public VoteEntranceViewModel()
        {
            // 初始化集合
            Awards = new ObservableCollection<Award>();
            Nominations = new ObservableCollection<Nomination>();
            PagedNominations = new ObservableCollection<Nomination>();
            VotedAwardIds = new ObservableCollection<int>();
            
            // 初始化分页选项，默认选中5
            PageSizeOptions = new ObservableCollection<int> { 5, 10, 20, 50 };
            PageSize = 5;
            
            // 设置初始状态
            IsLoading = true;
            IsPaginationVisible = false;
            
            // 初始化"全部奖项"选项
            AllAwards = new Award
            {
                AwardId = -1, // 使用一个不可能存在的ID
                AwardName = "全部奖项",
                AwardDescription = "显示所有奖项的提名"
            };
            
            // 初始化筛选后的奖项列表
            _filteredAwards = new List<Award>();
            
            // 初始化命令
            VoteCommand = new DelegateCommand<Nomination>(ExecuteVoteCommand, CanExecuteVoteCommand);
            VoteButtonClickCommand = new DelegateCommand<Nomination>(ExecuteVoteButtonClickCommand);
            SearchCommand = new DelegateCommand(ExecuteSearchCommand);
            FilterAwardsCommand = new DelegateCommand<string>(ExecuteFilterAwardsCommand);
            TabCompleteCommand = new DelegateCommand<string>(ExecuteTabCompleteCommand);
            HandlePreviewKeyDownCommand = new DelegateCommand<object>(ExecuteHandlePreviewKeyDownCommand);
            RefreshCommand = new DelegateCommand(ExecuteRefreshCommand);
            ShowCommentsCommand = new DelegateCommand<Nomination>(ExecuteShowCommentsCommand);
            HideCommentsCommand = new DelegateCommand<Nomination>(ExecuteHideCommentsCommand);
            AddCommentCommand = new DelegateCommand<Nomination>(ExecuteAddCommentCommand);
            DeleteCommentCommand = new DelegateCommand<CommentRecord>(ExecuteDeleteCommentCommand);
            LoadMoreCommentsCommand = new DelegateCommand<Nomination>(ExecuteLoadMoreCommentsCommand);
            
            // 初始化分页命令
            FirstPageCommand = new DelegateCommand(ExecuteFirstPageCommand, CanExecuteFirstPageCommand);
            PreviousPageCommand = new DelegateCommand(ExecutePreviousPageCommand, CanExecutePreviousPageCommand);
            NextPageCommand = new DelegateCommand(ExecuteNextPageCommand, CanExecuteNextPageCommand);
            LastPageCommand = new DelegateCommand(ExecuteLastPageCommand, CanExecuteLastPageCommand);
            PageSizeChangedCommand = new DelegateCommand<object>(ExecutePageSizeChangedCommand);
            JumpPageCommand = new DelegateCommand(ExecuteJumpPageCommand);
            PreviewTextInputCommand = new DelegateCommand<string>(ExecutePreviewTextInputCommand);
            
            // 异步初始化数据
            InitializeDataAsync();
        }
        
        /// <summary>
        /// 异步初始化数据
        /// </summary>
        private async void InitializeDataAsync()
        {
            try
            {
            // 获取当前登录用户信息
            GetCurrentUserInfo();
            
                // 加载奖项数据（等待完成）
                await LoadAwardsAsync();
                
                // 加载提名数据
                await LoadNominationsAsync();
            
            // 检查用户是否已投票
                await CheckIfUserHasVotedAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"初始化数据失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"初始化数据失败: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 获取当前登录用户信息
        /// </summary>
        private void GetCurrentUserInfo()
        {
            // 默认不是超级管理员
            IsSuperAdmin = false;
            IsAdmin = false;
            
            try 
            {
                // 使用全局静态类属性直接获取ID，而不是从数据库再次查询
                CurrentEmployeeId = CurrentUser.EmployeeId;
                CurrentAdminId = CurrentUser.AdminId;
                
                // 使用CurrentUser静态类获取当前登录用户角色信息
                if (CurrentUser.RoleId == 3) // 员工角色
                {
                    // 员工角色不设置管理标志
                }
                else if (CurrentUser.RoleId == 2) // 管理员角色
                {
                    IsAdmin = true;
                }
                else if (CurrentUser.RoleId == 1) // 超级管理员角色
                {
                    // 设置为超级管理员
                    IsSuperAdmin = true;
                    IsAdmin = true;
                    
                    // 超级管理员的ID存储在AdminId中
                    CurrentSupAdminId = CurrentUser.AdminId;
                }
            }
            catch (Exception ex)
            {
                // 发生异常，但不影响操作，只在调试时输出
                System.Diagnostics.Debug.WriteLine($"获取当前用户信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载奖项数据
        /// </summary>
        private async Task LoadAwardsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载奖项...";
                
                // 清空当前集合
                Awards.Clear();
                
                // 异步加载所有奖项
                using (var dbContext = new DataBaseContext())
                {
                    // 强制使用AsNoTracking()并设置EF不缓存查询结果
                    var awards = await dbContext.Awards
                        .AsNoTracking()
                        .OrderBy(a => a.AwardName)
                        .ToListAsync();
                    
                    // 将数据存储在完整的筛选集合中（缓存）
                    _filteredAwards = new List<Award>(awards);
                    
                    // 添加全部奖项选项
                    Awards.Add(AllAwards);
                    
                    // 添加所有奖项到集合
                    foreach (var award in awards)
                    {
                        Awards.Add(award);
                    }
                    
                    StatusMessage = $"已加载 {Awards.Count - 1} 个奖项"; // 减去"全部奖项"选项
                }
                
                // 默认选择"全部奖项"
                SelectedAward = AllAwards;
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载奖项数据时出错: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 更新当前页的提名数据
        /// </summary>
        private void UpdatePagedNominations()
        {
            if (Nominations == null || Nominations.Count == 0)
            {
                PagedNominations.Clear();
                TotalPages = 1;
                TotalItems = 0;
                IsPaginationVisible = false;
                return;
            }

            // 设置总记录数
            TotalItems = Nominations.Count;
            
            // 计算总页数
            TotalPages = (int)Math.Ceiling(Nominations.Count / (double)PageSize);
            
            // 确保当前页在有效范围内
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
            else if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            // 计算当前页数据
            int startIndex = (CurrentPage - 1) * PageSize;
            int endIndex = Math.Min(startIndex + PageSize, Nominations.Count);
            
            PagedNominations.Clear();
            for (int i = startIndex; i < endIndex; i++)
            {
                PagedNominations.Add(Nominations[i]);
            }

            // 更新分页控件的可见性
            IsPaginationVisible = TotalPages > 1;
            
            // 更新命令状态
            FirstPageCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
            LastPageCommand.RaiseCanExecuteChanged();
            
            // 更新显示的搜索文本框为当前页码
            SearchText = CurrentPage.ToString();
        }

        /// <summary>
        /// 加载提名数据
        /// </summary>
        private async Task LoadNominationsAsync()
        {
            try
            {
                using (var context = new DataBaseContext())
                {
                    try
                    {
                        #region 构建查询

                        // 构建基本查询，从提名表中选择所有记录
                        var query = context.Nominations
                            .AsNoTracking() // 只读操作，提高性能
                            .AsQueryable();
                        
                        // 如果选择了特定奖项，则只加载该奖项的提名
                        if (SelectedAward != null && SelectedAward.AwardId != AllAwards.AwardId)
                        {
                            query = query.Where(n => n.AwardId == SelectedAward.AwardId);
                        }
                        
                        #endregion

                        #region 关键词搜索筛选
                        
                        // 根据搜索关键词筛选
                        if (!string.IsNullOrWhiteSpace(SearchKeyword))
                        {
                            var keyword = SearchKeyword.ToLower();
                            
                            // 尝试将关键词转换为ID进行搜索
                            bool isNumeric = int.TryParse(keyword, out int searchId);
                            
                            query = query.Where(n =>
                                // 搜索提名ID
                                (isNumeric && n.NominationId == searchId) ||
                                // 搜索奖项名称
                                (n.Award != null && n.Award.AwardName != null && n.Award.AwardName.ToLower().Contains(keyword)) ||
                                // 搜索被提名者姓名
                                (n.NominatedEmployee != null && n.NominatedEmployee.EmployeeName != null && n.NominatedEmployee.EmployeeName.ToLower().Contains(keyword)) ||
                                (n.NominatedAdmin != null && n.NominatedAdmin.AdminName != null && n.NominatedAdmin.AdminName.ToLower().Contains(keyword)) ||
                                // 搜索部门名称
                                (n.Department != null && n.Department.DepartmentName != null && n.Department.DepartmentName.ToLower().Contains(keyword)) ||
                                // 搜索介绍和理由
                                (n.Introduction != null && n.Introduction.ToLower().Contains(keyword)) ||
                                (n.NominateReason != null && n.NominateReason.ToLower().Contains(keyword))
                            );
                        }
                        
                        #endregion
                        
                        #region 计算总记录数和限制记录数
                        
                        // 首先计算总数 - 单独执行以优化性能
                        var totalCount = await query.CountAsync();
                        
                        // 设置TotalItems属性
                        TotalItems = totalCount;
                        
                        // 限制最大记录数，避免加载过多数据
                        const int maxItems = 500;
                        bool hasMoreItems = totalCount > maxItems;
                        if (hasMoreItems)
                        {
                            // 如果记录数超过限制，显示警告
                            Application.Current.Dispatcher.Invoke(() => {
                                Growl.WarningGlobal($"查询结果超过{maxItems}条记录，只显示前{maxItems}条。请使用筛选功能缩小范围。");
                            });
                        }
                        
                        #endregion
                        
                        #region 使用高效查询优化数据获取
                        
                        // 使用投影查询只获取需要的字段，避免加载完整的关联实体
                        var nominationsFromDb = await query
                            .Select(n => new 
                            {
                                NominationId = n.NominationId,
                                CoverImage = n.CoverImage,
                                Introduction = n.Introduction,
                                NominateReason = n.NominateReason,
                                AwardId = n.AwardId,
                                DepartmentId = n.DepartmentId,
                                AwardName = n.Award.AwardName,
                                DepartmentName = n.Department != null ? n.Department.DepartmentName : null,
                                EmployeeId = n.NominatedEmployee != null ? n.NominatedEmployee.EmployeeId : (int?)null,
                                EmployeeName = n.NominatedEmployee != null ? n.NominatedEmployee.EmployeeName : null,
                                AdminId = n.NominatedAdmin != null ? n.NominatedAdmin.AdminId : (int?)null,
                                AdminName = n.NominatedAdmin != null ? n.NominatedAdmin.AdminName : null
                            })
                            .OrderBy(n => n.NominationId) // 按提名ID从小到大排序
                            .Take(maxItems)
                            .ToListAsync();
                        
                        #endregion
                        
                        #region 处理评论数据
                        
                        // 在内存中处理UI相关字段
                        var processedNominations = new List<Nomination>();
                        
                        // 获取提名ID集合用于查询评论计数
                        var nominationIds = nominationsFromDb.Select(n => n.NominationId).ToList();
                        
                        // 批量查询所有提名的评论数量
                        var commentCounts = await context.CommentRecords
                            .Where(c => nominationIds.Contains(c.NominationId) && !c.IsDeleted)
                            .GroupBy(c => c.NominationId)
                            .Select(g => new { NominationId = g.Key, Count = g.Count() })
                            .ToDictionaryAsync(k => k.NominationId, v => v.Count);
                        
                        // 获取所有相关的奖项数据，包含MaxVoteCount
                        var awardIds = nominationsFromDb.Select(n => n.AwardId).Distinct().ToList();
                        var awardsDict = await context.Awards
                            .AsNoTracking()
                            .Where(a => awardIds.Contains(a.AwardId))
                            .Select(a => new { a.AwardId, a.AwardName, a.MaxVoteCount }) // 只选择需要的字段
                            .ToDictionaryAsync(a => a.AwardId, a => new { a.AwardName, a.MaxVoteCount });
                        
                        #endregion
                        
                        #region 构建提名对象列表
                        
                        // 在内存中构建对象
                        foreach (var item in nominationsFromDb)
                        {
                            // 获取正确的MaxVoteCount
                            int maxVoteCount = 1; // 默认值
                            string awardName = item.AwardName ?? "未知奖项";
                            if (awardsDict.TryGetValue(item.AwardId, out var awardData))
                            {
                                maxVoteCount = awardData.MaxVoteCount;
                                awardName = awardData.AwardName ?? awardName;
                            }
                            
                            var nomination = new Nomination
                            {
                                NominationId = item.NominationId,
                                CoverImage = item.CoverImage,
                                Introduction = item.Introduction,
                                NominateReason = item.NominateReason,
                                AwardId = item.AwardId,
                                DepartmentId = item.DepartmentId,
                                
                                // 创建关联对象，包含必要字段
                                Award = new Award 
                                { 
                                    AwardId = item.AwardId, 
                                    AwardName = awardName,
                                    MaxVoteCount = maxVoteCount // 使用最新获取的值
                                },
                                
                                Department = item.DepartmentName == null ? null : new Department 
                                { 
                                    DepartmentId = item.DepartmentId ?? 0, 
                                    DepartmentName = item.DepartmentName 
                                },
                                
                                NominatedEmployee = item.EmployeeId == null ? null : new Employee
                                {
                                    EmployeeId = item.EmployeeId.Value,
                                    EmployeeName = item.EmployeeName,
                                    EmployeePassword = "placeholder" // 填充required字段
                                },
                                
                                NominatedAdmin = item.AdminId == null ? null : new Admin
                                {
                                    AdminId = item.AdminId.Value,
                                    AdminName = item.AdminName,
                                    AdminPassword = "placeholder" // 填充required字段
                                },
                                
                                // 设置UI相关属性
                                IsActive = true,
                                IsCommentSectionVisible = false,
                                UIComments = new ObservableCollection<CommentRecord>(),
                                NewCommentText = string.Empty,
                                
                                // 设置投票状态 - 修改为只在达到最大投票次数时才标记为已投票
                                IsUserVoted = false, // 初始化为false，后续由CheckIfUserHasVotedAsync方法更新
                                
                                // 设置评论数量
                                CommentCount = commentCounts.TryGetValue(item.NominationId, out int count) ? count : 0
                            };
                            
                            processedNominations.Add(nomination);
                        }
                        
                        #endregion
                        
                        #region 在UI线程更新数据
                        
                        // 在UI线程中更新集合
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Nominations = new ObservableCollection<Nomination>(processedNominations);
                            UpdatePagedNominations();
                        });
                        
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"加载提名数据失败: {ex.Message}";
                        HandyControl.Controls.MessageBox.Error($"加载提名数据失败: {ex.Message}", "错误");
                        System.Diagnostics.Debug.WriteLine($"加载提名数据异常详情: {ex}");
                        
                        // 尝试进行数据加载的恢复处理
                        try
                        {
                            if (Nominations == null || Nominations.Count == 0)
                            {
                                Nominations = new ObservableCollection<Nomination>();
                                PagedNominations = new ObservableCollection<Nomination>();
                            }
                            IsPaginationVisible = TotalPages > 1;
                        }
                        catch { /* 忽略恢复过程中的任何错误 */ }
                    }
                }
            }
            finally
            {
                // 确保无论方法如何结束，都会重置加载状态
                IsLoading = false;
            }
        }
        /// <summary>
        /// 检查用户是否已投票
        /// </summary>
        private async Task CheckIfUserHasVotedAsync()
        {
            try
            {
                await Task.Run(async () =>
                {
                    using (var context = new DataBaseContext())
                    {
                        #region 初始化和清空数据
                        
                        // 清空已投票的奖项列表
                        VotedAwardIds.Clear();
                        AwardVoteCount.Clear();
                        bool hasVotedCurrentAward = false;
                        
                        #endregion
                        
                        #region 根据用户类型查询投票记录
                        
                        // 获取用户已投票的记录
                        List<VoteRecord> userVotes = new List<VoteRecord>();
                        
                        if (CurrentEmployeeId.HasValue)
                        {
                            // 查询员工的投票记录
                            userVotes = await context.VoteRecords
                                .AsNoTracking()
                                .Where(v => v.VoterEmployeeId == CurrentEmployeeId)
                                .ToListAsync();
                        }
                        else if (CurrentAdminId.HasValue)
                        {
                            // 查询管理员的投票记录
                            userVotes = await context.VoteRecords
                                .AsNoTracking()
                                .Where(v => v.VoterAdminId == CurrentAdminId)
                                .ToListAsync();
                        }
                        
                        #endregion
                        
                        // 获取最新的奖项数据
                        var latestAwards = await context.Awards.AsNoTracking().ToListAsync();
                        var awardsDict = latestAwards.ToDictionary(a => a.AwardId, a => a);
                        
                        #region 处理投票数据并更新状态
                        
                        // 记录用户已投票的奖项ID
                        foreach (var vote in userVotes)
                        {
                            // 统计用户对每个奖项的投票次数
                            if (AwardVoteCount.ContainsKey(vote.AwardId))
                            {
                                AwardVoteCount[vote.AwardId]++;
                            }
                            else
                            {
                                AwardVoteCount[vote.AwardId] = 1;
                            }
                            
                            // 获取奖项的最大投票次数
                            int maxVotes = 1; // 默认为1
                            if (awardsDict.TryGetValue(vote.AwardId, out var award))
                            {
                                maxVotes = award.MaxVoteCount;
                            }
                            
                            // 只有当投票次数达到最大值时，才添加到已投票奖项列表
                            if (AwardVoteCount[vote.AwardId] >= maxVotes && !VotedAwardIds.Contains(vote.AwardId))
                            {
                                Application.Current.Dispatcher.Invoke(() => {
                                    VotedAwardIds.Add(vote.AwardId);
                                });
                            }
                            
                            // 检查是否对当前选择的奖项投过票
                            if (SelectedAward != null && vote.AwardId == SelectedAward.AwardId)
                            {
                                hasVotedCurrentAward = true;
                            }
                        }
                        
                        #endregion
                        
                        #region 更新UI状态
                        
                        // 创建一个已达到最大投票数的奖项ID集合
                        var maxVotedAwardIds = new HashSet<int>();
                        foreach(var awardId in AwardVoteCount.Keys)
                        {
                            int maxVotes = 1;
                            if (awardsDict.TryGetValue(awardId, out var award))
                            {
                                maxVotes = award.MaxVoteCount;
                            }
                            
                            if (AwardVoteCount[awardId] >= maxVotes)
                            {
                                maxVotedAwardIds.Add(awardId);
                            }
                        }
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 更新状态消息
                            if (SelectedAward != null)
                            {
                                // 从最新数据获取奖项
                                Award latestAward = null;
                                if (awardsDict.TryGetValue(SelectedAward.AwardId, out var award))
                                {
                                    latestAward = award;
                                }
                                else
                                {
                                    latestAward = SelectedAward;
                                }

                                int maxVotes = latestAward.MaxVoteCount;
                                int usedVotes = AwardVoteCount.ContainsKey(SelectedAward.AwardId) ? AwardVoteCount[SelectedAward.AwardId] : 0;
                                int remainingVotes = maxVotes - usedVotes;
                                
                                if (usedVotes >= maxVotes)
                                {
                                    StatusMessage = $"您已经对奖项 \"{SelectedAward.AwardName}\" 投了 {usedVotes} 次票，已达到最大次数 {maxVotes}";
                                    HasVotedCurrentAward = true;
                                }
                                else
                                {
                                    StatusMessage = $"您对奖项 \"{SelectedAward.AwardName}\" 还有 {remainingVotes} 次投票机会";
                                    HasVotedCurrentAward = (remainingVotes <= 0);
                                }
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"已投票奖项数: {VotedAwardIds.Count}, 当前奖项已投票次数: {(SelectedAward != null && AwardVoteCount.ContainsKey(SelectedAward.AwardId) ? AwardVoteCount[SelectedAward.AwardId] : 0)}");
                            
                            // 更新所有当前显示提名的投票状态 - 直接批量更新，避免一个一个设置导致的UI闪烁
                            if (Nominations != null && Nominations.Count > 0)
                            {
                                // 首先创建一个字典，将提名按奖项ID分组
                                var nominationsByAward = Nominations
                                    .GroupBy(n => n.AwardId)
                                    .ToDictionary(g => g.Key, g => g.ToList());
                                
                                // 批量更新已达到最大投票次数的奖项下的所有提名
                                foreach (var awardId in maxVotedAwardIds)
                                {
                                    if (nominationsByAward.ContainsKey(awardId))
                                    {
                                        foreach (var nom in nominationsByAward[awardId])
                                        {
                                            // 统一设置为true，表示该奖项已达到最大投票次数
                                            nom.IsUserVoted = true;
                                        }
                                    }
                                }
                                
                                // 批量更新未达到最大投票次数的奖项下的所有提名
                                foreach (var nomination in Nominations)
                                {
                                    if (nomination.AwardId != null && !maxVotedAwardIds.Contains(nomination.AwardId))
                                    {
                                        // 统一设置为false，表示该奖项未达到最大投票次数
                                        nomination.IsUserVoted = false;
                                    }
                                }
                            }
                        });
                        
                        #endregion
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"检查投票状态失败: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"检查投票状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行投票按钮点击事件命令
        /// </summary>
        private void ExecuteVoteButtonClickCommand(Nomination nomination)
        {
            if (nomination == null)
                return;

            // 检查投票按钮是否被禁用（不能投票）
            if (!CanExecuteVoteCommand(nomination))
            {
                // 获取奖项的最大投票次数和已投票次数
                int maxVoteCount = nomination.Award?.MaxVoteCount ?? 1;
                int usedVotes = AwardVoteCount.ContainsKey(nomination.AwardId) ? AwardVoteCount[nomination.AwardId] : 0;
                
                if (usedVotes >= maxVoteCount)
                {
                    // 显示投票上限提示
                    string nomineeName = nomination.NominatedEmployee?.EmployeeName ?? 
                                       nomination.NominatedAdmin?.AdminName ?? 
                                       "未知提名人";
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = $"您已经对奖项 '{nomination.Award?.AwardName}' 投票达到最大次数 {maxVoteCount}";
                        HandyControl.Controls.Growl.InfoGlobal($"当前奖项 '{nomination.Award?.AwardName}' 能够进行的最大投票数量为：{maxVoteCount}，你已用完所有投票次数");
                    });
                }
                else if (IsSuperAdmin)
                {
                    // 超级管理员不能投票
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = $"超级管理员不能参与投票，仅可查看";
                        HandyControl.Controls.Growl.InfoGlobal($"超级管理员不能参与投票，仅可查看");
                    });
                }
            }
            else
            {
                // 如果按钮可用，执行正常的投票命令
                ExecuteVoteCommand(nomination);
            }
        }

        /// <summary>
        /// 执行投票命令
        /// </summary>
        private async void ExecuteVoteCommand(Nomination nomination)
        {
            if (nomination == null)
                return;

            #region 权限检查
            
            // 超级管理员显示提示信息，但不进行实际投票操作
            if (IsSuperAdmin)
            {
                string nomineeName = nomination.NominatedEmployee?.EmployeeName ?? 
                                    nomination.NominatedAdmin?.AdminName ?? 
                                    "未知提名人";
                StatusMessage = $"超级管理员不能参与投票，仅可查看";
                HandyControl.Controls.Growl.InfoGlobal($"超级管理员不能参与投票，仅可查看");
                return; // 返回，不执行实际的投票操作
            }

            #endregion
            
            #region 投票验证
            
            // 获取奖项的最新数据，确保使用最新的MaxVoteCount
            Award latestAward = null;
            int maxVoteCount = 1;
            
            using (var context = new DataBaseContext())
            {
                try
                {
                    // 查询最新的奖项数据
                latestAward = await context.Awards
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AwardId == nomination.AwardId);
                
                if (latestAward != null)
                {
                    maxVoteCount = latestAward.MaxVoteCount;
                }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取奖项最新数据失败: {ex.Message}");
                }
            }
            
            #endregion

            #region 投票操作
            
            // 执行实际的投票操作
                    using (var context = new DataBaseContext())
                    {
                try
                {
                    // 检查是否超过了最大投票次数
                    int currentVoteCount = 0;
                    if (AwardVoteCount.ContainsKey(nomination.AwardId))
                    {
                        currentVoteCount = AwardVoteCount[nomination.AwardId];
                    }
                    
                    if (currentVoteCount >= maxVoteCount)
                    {
                        // 已达到最大投票次数，显示提示信息
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                            StatusMessage = $"您已经对奖项 '{nomination.Award?.AwardName}' 投票达到最大次数 {maxVoteCount}";
                            HandyControl.Controls.Growl.WarningGlobal($"当前奖项 '{nomination.Award?.AwardName}' 能够进行的最大投票数量为：{maxVoteCount}，你已用完所有投票次数");
                            });
                            return;
                        }
                        
                        // 创建新的投票记录
                        var voteRecord = new VoteRecord
                        {
                            AwardId = nomination.AwardId,
                        NominationId = nomination.NominationId,
                        VoteTime = DateTime.Now,
                        VoterEmployeeId = CurrentEmployeeId,
                        VoterAdminId = CurrentAdminId
                        };
                        
                    // 添加到数据库并保存
                        context.VoteRecords.Add(voteRecord);
                    await context.SaveChangesAsync();
                        
                    // 更新UI
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 更新当前奖项的投票次数统计
                            if (AwardVoteCount.ContainsKey(nomination.AwardId))
                            {
                                AwardVoteCount[nomination.AwardId]++;
                            }
                            else
                            {
                                AwardVoteCount[nomination.AwardId] = 1;
                            }
                            
                            // 获取更新后的投票次数
                            int updatedVoteCount = AwardVoteCount[nomination.AwardId];
                            
                            // 检查是否已达到最大投票次数
                            bool hasReachedMaxVotes = (updatedVoteCount >= maxVoteCount);
                            
                            // 更新当前奖项的投票状态
                            HasVotedCurrentAward = hasReachedMaxVotes;
                            
                            // 更新UI和投票状态
                            if (hasReachedMaxVotes)
                            {
                                // 如果达到最大投票次数，将该奖项的所有提名标记为已投票状态（显示红色边框）
                                foreach (var nom in Nominations.Where(n => n.AwardId == nomination.AwardId))
                                {
                                    // 避免触发不必要的UI更新，只在状态真正改变时才设置
                                    if (!nom.IsUserVoted)
                                    {
                                        nom.IsUserVoted = true;
                                    }
                                }
                                
                                // 添加到已投票奖项列表
                                if (!VotedAwardIds.Contains(nomination.AwardId))
                                {
                                    VotedAwardIds.Add(nomination.AwardId);
                                }
                            }
                            
                            // 更新状态信息
                            string nomineeName = nomination.NominatedEmployee?.EmployeeName ?? 
                                               nomination.NominatedAdmin?.AdminName ?? 
                                               "未知提名人";
                            
                            // 更新状态消息
                            if (hasReachedMaxVotes)
                            {
                                StatusMessage = $"您已对 {nomineeName} 投票成功，已达到奖项 '{nomination.Award?.AwardName}' 的最大投票次数 {maxVoteCount}";
                                HandyControl.Controls.Growl.SuccessGlobal($"投票成功！当前奖项 '{nomination.Award?.AwardName}' 能够进行的最大投票数量为：{maxVoteCount}，你已用完所有投票次数");
                            }
                            else
                            {
                                int remainingVotes = maxVoteCount - updatedVoteCount;
                                StatusMessage = $"您已对 {nomineeName} 投票成功，对奖项 '{nomination.Award?.AwardName}' 还有 {remainingVotes} 次投票机会";
                                HandyControl.Controls.Growl.SuccessGlobal($"投票成功！当前奖项 '{nomination.Award?.AwardName}' 能够进行的最大投票数量为：{maxVoteCount}，你还剩余投票次数为：{remainingVotes}");
                            }
                            
                            // 刷新命令状态
                            VoteCommand.RaiseCanExecuteChanged();
                        });
                    }
                catch (Exception ex)
                {
                    // 投票失败
                    StatusMessage = $"投票失败: {ex.Message}";
                    HandyControl.Controls.Growl.ErrorGlobal($"投票失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"投票失败: {ex}");
                }
            }
            
            #endregion
        }

        /// <summary>
        /// 投票命令的CanExecute方法
        /// </summary>
        private bool CanExecuteVoteCommand(Nomination nomination)
        {
            // 超级管理员不能投票
            if (IsSuperAdmin || nomination == null)
                return false;

            // 检查奖项是否已达到最大投票次数
            if (nomination.Award != null && AwardVoteCount.ContainsKey(nomination.AwardId))
            {
                int maxVoteCount = nomination.Award.MaxVoteCount;
                int currentVoteCount = AwardVoteCount[nomination.AwardId];
                
                // 如果已达到最大投票次数，则禁用投票按钮
                if (currentVoteCount >= maxVoteCount)
                    return false;
            }

            // 允许投票
            return true;
        }

        #endregion

        #region 搜索和筛选命令
        
        /// <summary>
        /// 执行搜索命令
        /// </summary>
        private void ExecuteSearchCommand()
        {
            // 加载提名数据，会使用SearchKeyword参数进行过滤
            LoadNominationsAsync();
        }
        
        /// <summary>
        /// 执行奖项筛选命令
        /// </summary>
        private void ExecuteFilterAwardsCommand(string keyword)
        {
            // 在实际项目中实现奖项筛选逻辑
        }

        /// <summary>
        /// 执行Tab自动完成命令
        /// </summary>
        private void ExecuteTabCompleteCommand(string text)
        {
            // 在实际项目中实现Tab自动完成逻辑
        }

        /// <summary>
        /// 执行按键预览命令
        /// </summary>
        private void ExecuteHandlePreviewKeyDownCommand(object parameter)
        {
            // 在实际项目中实现按键预览处理逻辑
        }
        
        /// <summary>
        /// 执行刷新命令
        /// </summary>
        private void ExecuteRefreshCommand()
        {
            // 重新加载提名数据
            LoadNominationsAsync();
        }

        #endregion

        #region 评论相关命令

        /// <summary>
        /// 执行显示评论命令
        /// </summary>
        private void ExecuteShowCommentsCommand(Nomination nomination)
        {
            // 在实际项目中实现显示评论逻辑
            if (nomination != null)
            {
                nomination.IsCommentSectionVisible = true;
            }
        }

        /// <summary>
        /// 执行隐藏评论命令
        /// </summary>
        private void ExecuteHideCommentsCommand(Nomination nomination)
        {
            // 在实际项目中实现隐藏评论逻辑
            if (nomination != null)
            {
                nomination.IsCommentSectionVisible = false;
            }
        }

        /// <summary>
        /// 执行添加评论命令
        /// </summary>
        private void ExecuteAddCommentCommand(Nomination nomination)
        {
            // 在实际项目中实现添加评论逻辑
        }

        /// <summary>
        /// 执行删除评论命令
        /// </summary>
        private void ExecuteDeleteCommentCommand(CommentRecord comment)
        {
            // 在实际项目中实现删除评论逻辑
        }
        
        /// <summary>
        /// 执行加载更多评论命令
        /// </summary>
        private void ExecuteLoadMoreCommentsCommand(Nomination nomination)
        {
            // 在实际项目中实现加载更多评论逻辑
        }

        #endregion

        #region 分页命令

        /// <summary>
        /// 执行首页命令
        /// </summary>
        private void ExecuteFirstPageCommand()
            {
                CurrentPage = 1;
        }

        /// <summary>
        /// 首页命令是否可执行
        /// </summary>
        private bool CanExecuteFirstPageCommand()
        {
            return CurrentPage > 1;
        }

        /// <summary>
        /// 执行上一页命令
        /// </summary>
        private void ExecutePreviousPageCommand()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        /// <summary>
        /// 上一页命令是否可执行
        /// </summary>
        private bool CanExecutePreviousPageCommand()
        {
            return CurrentPage > 1;
        }

        /// <summary>
        /// 执行下一页命令
        /// </summary>
        private void ExecuteNextPageCommand()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        /// <summary>
        /// 下一页命令是否可执行
        /// </summary>
        private bool CanExecuteNextPageCommand()
        {
            return CurrentPage < TotalPages;
        }

        /// <summary>
        /// 执行末页命令
        /// </summary>
        private void ExecuteLastPageCommand()
        {
            CurrentPage = TotalPages;
        }

        /// <summary>
        /// 末页命令是否可执行
        /// </summary>
        private bool CanExecuteLastPageCommand()
        {
            return CurrentPage < TotalPages;
        }

        /// <summary>
        /// 执行页大小改变命令
        /// </summary>
        private void ExecutePageSizeChangedCommand(object parameter)
        {
            // 在实际项目中实现页大小改变逻辑
            // 这里已经在PageSize属性的setter中实现了
        }

        /// <summary>
        /// 执行跳转页命令
        /// </summary>
        private void ExecuteJumpPageCommand()
        {
            if (int.TryParse(SearchText, out int pageNumber))
            {
                if (pageNumber > 0 && pageNumber <= TotalPages)
                {
                    CurrentPage = pageNumber;
                }
            }
        }

        /// <summary>
        /// 执行预览文本输入命令
        /// </summary>
        private void ExecutePreviewTextInputCommand(string text)
        {
            // 仅允许输入数字
            SearchText = new string(text.Where(char.IsDigit).ToArray());
        }

        #endregion

        /// <summary>
        /// 加载提名并按顺序检查投票状态，确保UI状态一致
        /// </summary>
        private async void LoadNominationsAndCheckVoteStatusAsync()
        {
            // 先加载提名
            await LoadNominationsAsync();
            
            // 然后检查投票状态
            await CheckIfUserHasVotedAsync();
        }
    }
}
