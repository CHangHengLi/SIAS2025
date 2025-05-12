using _2025毕业设计.Common;
using _2025毕业设计.Context;
using _2025毕业设计.Event;
using _2025毕业设计.Models;
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
using _2025毕业设计.ViewModels.EditMessage.NominationDetailsWindows;
using _2025毕业设计.Views.EditMessage.NominationDetailsWindows;

namespace _2025毕业设计.ViewModels.Pages
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
                    // 选中奖项后，重新加载提名以筛选结果
                    LoadNominationsAsync();
                    
                    // 检查用户是否已对当前选择的奖项投过票
                    CheckIfUserHasVotedAsync();
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
                                IsUserVoted = AwardVoteCount.ContainsKey(item.AwardId) && AwardVoteCount[item.AwardId] >= maxVoteCount,
                                
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
                            
                            // 更新所有当前显示提名的投票状态
                            if (Nominations != null)
                            {
                                foreach (var nomination in Nominations)
                                {
                                    // 如果已经达到最大投票次数，则标记为已投票
                                    if (nomination.Award != null && AwardVoteCount.ContainsKey(nomination.AwardId))
                                    {
                                        // 获取最新的MaxVoteCount
                                        int maxVotes = 1; // 默认为1
                                        if (awardsDict.TryGetValue(nomination.AwardId, out var award))
                                        {
                                            maxVotes = award.MaxVoteCount;
                                        }
                                        else if (nomination.Award != null)
                                        {
                                            maxVotes = nomination.Award.MaxVoteCount;
                                        }
                                        
                                        nomination.IsUserVoted = AwardVoteCount[nomination.AwardId] >= maxVotes;
                                    }
                                    else
                                    {
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
            int maxVoteCount = 1; // 默认为1
            
            using (var context = new DataBaseContext())
            {
                // 从数据库获取最新的奖项数据
                latestAward = await context.Awards
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AwardId == nomination.AwardId);
                
                // 如果找到奖项，使用其MaxVoteCount
                if (latestAward != null)
                {
                    maxVoteCount = latestAward.MaxVoteCount;
                }
                else if (nomination.Award != null)
                {
                    // 如果数据库没有找到，但nomination对象有Award属性，则使用它
                    maxVoteCount = nomination.Award.MaxVoteCount;
                }
            }
            
            // 检查用户当前对该奖项的投票次数
            int currentVoteCount = 0;
            if (AwardVoteCount.ContainsKey(nomination.AwardId))
            {
                currentVoteCount = AwardVoteCount[nomination.AwardId];
            }
            
            // 检查是否达到最大投票次数
            if (currentVoteCount >= maxVoteCount)
            {
                string nomineeName = nomination.NominatedEmployee?.EmployeeName ?? 
                                   nomination.NominatedAdmin?.AdminName ?? 
                                   "未知提名人";
                
                // 使用最新获取的奖项名称
                string awardName = latestAward?.AwardName ?? nomination.Award?.AwardName ?? "未知奖项";
                
                StatusMessage = $"您已经对奖项 \"{awardName}\" 投了 {currentVoteCount} 次票，已达到最大次数 {maxVoteCount}";
                HandyControl.Controls.Growl.InfoGlobal($"您已经对奖项 \"{awardName}\" 投票达到最大次数 {maxVoteCount}");
                return; // 返回，不执行实际的投票操作
            }
            
            #endregion

            try
            {
                #region 初始化投票操作
                
                IsLoading = true;
                StatusMessage = "正在提交投票...";
                
                #endregion

                await Task.Run(() =>
                {
                    using (var context = new DataBaseContext())
                    {
                        #region 创建并保存投票记录
                        
                        // 获取完整的用户信息，包括部门信息
                        Employee currentEmployee = null;
                        Admin currentAdmin = null;
                        
                        // 根据用户类型查询完整的用户信息
                        if (CurrentEmployeeId.HasValue)
                        {
                            currentEmployee = context.Employees
                                .Include(e => e.Department)
                                .FirstOrDefault(e => e.EmployeeId == CurrentEmployeeId);
                        }
                        else if (CurrentAdminId.HasValue)
                        {
                            currentAdmin = context.Admins
                                .Include(a => a.Department)
                                .FirstOrDefault(a => a.AdminId == CurrentAdminId);
                        }
                        
                        // 验证特定提名的投票次数
                        int nominationVoteCount = context.VoteRecords
                            .Count(v => v.NominationId == nomination.NominationId && 
                                   ((CurrentEmployeeId.HasValue && v.VoterEmployeeId == CurrentEmployeeId) ||
                                    (CurrentAdminId.HasValue && v.VoterAdminId == CurrentAdminId)));
                                    
                        // 检查是否已经对该提名投过票
                        if (nominationVoteCount > 0)
                        {
                            var nomineeName = nomination.NominatedEmployee?.EmployeeName ?? 
                                            nomination.NominatedAdmin?.AdminName ?? 
                                            "未知提名人";
                            
                            // 同步UI线程通知用户
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusMessage = $"您已经为 {nomineeName} 投过票了，不能重复投票";
                                HandyControl.Controls.Growl.InfoGlobal($"您已经为 {nomineeName} 投过票了，不能重复投票");
                                IsLoading = false;
                            });
                            
                            return;
                        }
                        
                        // 创建新的投票记录
                        var voteRecord = new VoteRecord
                        {
                            VoterEmployeeId = CurrentEmployeeId,
                            VoterAdminId = CurrentAdminId,
                            VoteTime = DateTime.Now,
                            AwardId = nomination.AwardId,
                            NominationId = nomination.NominationId
                        };
                        
                        // 添加到数据库
                        context.VoteRecords.Add(voteRecord);
                        context.SaveChanges();
                        
                        #endregion
                        
                        #region 更新UI状态
                        
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
                            
                            // 更新当前投票计数并使用最新的maxVoteCount
                            int updatedVoteCount = AwardVoteCount[nomination.AwardId];
                            
                            // 只有在达到最大投票次数时才标记为已投票
                            HasVotedCurrentAward = (updatedVoteCount >= maxVoteCount);
                            
                            // 添加到已投票奖项列表（如果已达到最大投票次数）
                            if (HasVotedCurrentAward && !VotedAwardIds.Contains(nomination.AwardId))
                            {
                                VotedAwardIds.Add(nomination.AwardId);
                            }
                            
                            // 获取提名对象名称
                            string nomineeName = nomination.NominatedEmployee?.EmployeeName ?? 
                                               nomination.NominatedAdmin?.AdminName ?? 
                                               "未知提名人";
                            
                            // 使用Growl代替MessageBox
                            if (updatedVoteCount < maxVoteCount)
                            {
                                int remainingVotes = maxVoteCount - updatedVoteCount;
                                StatusMessage = $"您已成功为 {nomineeName} 在奖项 \"{latestAward?.AwardName ?? nomination.Award?.AwardName}\" 中投票，还剩 {remainingVotes} 次投票机会";
                            }
                            else
                            {
                                StatusMessage = $"您已成功为 {nomineeName} 在奖项 \"{latestAward?.AwardName ?? nomination.Award?.AwardName}\" 中投票，已用完所有投票机会";
                            }
                            
                            // 更新Growl提示消息，添加最高投票数和剩余投票次数的信息
                            int remainingVotesForGrowl = maxVoteCount - updatedVoteCount;
                            string votesInfoText = remainingVotesForGrowl > 0
                                ? $"（最高投票数: {maxVoteCount}，已使用: {updatedVoteCount}，剩余: {remainingVotesForGrowl}）"
                                : $"（已用完所有 {maxVoteCount} 次投票机会）";
                                
                            HandyControl.Controls.Growl.SuccessGlobal($"投票成功！您已成功为 {nomineeName} 在奖项 \"{latestAward?.AwardName ?? nomination.Award?.AwardName}\" 中投票 {votesInfoText}");
                            
                            // 刷新提名列表，更新投票状态
                            LoadNominationsAsync();
                            
                            // 重新检查用户投票状态，确保UI状态更新
                            CheckIfUserHasVotedAsync();
                        });
                        
                        #endregion
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"投票失败: {ex.Message}";
                HandyControl.Controls.Growl.ErrorGlobal($"投票失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"投票失败: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行刷新命令
        /// </summary>
        private void ExecuteRefreshCommand()
        {
            LoadAwardsAsync();
            LoadNominationsAsync();
            CheckIfUserHasVotedAsync();
        }

        /// <summary>
        /// 检查指定奖项是否已被投票
        /// </summary>
        public bool CheckIfAwardVoted(int awardId)
        {
            return VotedAwardIds.Contains(awardId);
        }

        #region 评论功能相关方法

        /// <summary>
        /// 显示评论区
        /// </summary>
        private async void ExecuteShowCommentsCommand(Nomination nomination)
        {
            if (nomination == null) return;
            
            try
            {
                // 明确关闭所有打开的评论区并打印日志以便调试
                System.Diagnostics.Debug.WriteLine($"关闭所有评论区，保留提名ID: {nomination.NominationId}");
                CloseAllCommentSections(nomination.NominationId);
                
                // 先设置评论区可见，提供更快的UI响应
                nomination.IsCommentSectionVisible = true;
                System.Diagnostics.Debug.WriteLine($"已打开提名ID: {nomination.NominationId} 的评论区");
                
                // 强制UI立即更新
                Application.Current.Dispatcher.Invoke(() => {
                    // 手动触发一次PropertyChanged事件
                    var temp = nomination.IsCommentSectionVisible;
                    nomination.IsCommentSectionVisible = false;
                    nomination.IsCommentSectionVisible = temp;
                });
                
                // 确保UIComments已初始化
                if (nomination.UIComments == null)
                {
                    nomination.UIComments = new ObservableCollection<CommentRecord>();
                }
                
                // 显示加载指示器
                IsLoading = true;
                StatusMessage = "正在加载评论...";
                
                try
                {
                    // 加载评论
                    await LoadCommentsAsync(nomination);
                    
                    // 评论加载完成后的操作
                    StatusMessage = $"已加载 {nomination.CommentCount} 条评论";
                    
                    // 保证UI立即更新
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                        // 强制更新UI - 有时数据绑定不会立即刷新，这里手动触发刷新
                        var index = PagedNominations.IndexOf(nomination);
                        if (index != -1)
                        {
                            // 使用重新赋值的方式强制刷新UI
                            var temp = PagedNominations[index];
                            PagedNominations.RemoveAt(index);
                            PagedNominations.Insert(index, temp);
                        }
                    });
                    
                    // 使用Dispatcher延迟显示Growl通知，避免UI阻塞
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        HandyControl.Controls.Growl.SuccessGlobal($"已加载 {nomination.CommentCount} 条评论");
                    }));
                }
                catch (Exception ex)
                {
                    // 如果加载评论失败，显示错误信息
                    StatusMessage = $"加载评论失败: {ex.Message}";
                    HandyControl.Controls.Growl.ErrorGlobal($"加载评论失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"加载评论失败: {ex}");
                    
                    // 尝试恢复
                    nomination.UIComments.Clear();
                    nomination.CommentCount = 0;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"打开评论区失败: {ex.Message}";
                HandyControl.Controls.Growl.ErrorGlobal($"打开评论区失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"打开评论区失败: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 隐藏评论区
        /// </summary>
        private void ExecuteHideCommentsCommand(Nomination nomination)
        {
            if (nomination == null) return;
            
            // 设置当前提名的评论区不可见
            nomination.IsCommentSectionVisible = false;
            
            // 强制更新UI
            Application.Current.Dispatcher.Invoke(() => 
            {
                var index = PagedNominations.IndexOf(nomination);
                if (index != -1)
                {
                    // 使用重新赋值的方式强制刷新UI
                    var temp = PagedNominations[index];
                    PagedNominations.RemoveAt(index);
                    PagedNominations.Insert(index, temp);
                }
            });
        }
        
        /// <summary>
        /// 加载评论（支持分页）
        /// </summary>
        private async Task LoadCommentsAsync(Nomination nomination, int page = 1, int pageSize = 10)
        {
            if (nomination == null) return;
            
            using (var context = new DataBaseContext())
            {
                try
                {
                    // 计算跳过的记录数
                    int skip = (page - 1) * pageSize;
                    
                    // 查询该提名的评论 - 使用分页查询以提高性能
                    var comments = await context.CommentRecords
                        .AsNoTracking() // 提高性能，因为我们只是读取数据
                        .Where(c => c.NominationId == nomination.NominationId && !c.IsDeleted)
                        .OrderByDescending(c => c.CommentTime)
                        .Skip(skip)
                        .Take(pageSize)
                        .Select(c => new CommentRecord 
                        {
                            CommentId = c.CommentId,
                            CommentTime = c.CommentTime,
                            Content = c.Content,
                            NominationId = c.NominationId,
                            AwardId = c.AwardId,
                            IsDeleted = c.IsDeleted,
                            CommenterEmployeeId = c.CommenterEmployeeId,
                            CommenterAdminId = c.CommenterAdminId, 
                            CommenterSupAdminId = c.CommenterSupAdminId,
                            CommenterEmployee = c.CommenterEmployee != null ? new Employee 
                            {
                                EmployeeId = c.CommenterEmployee.EmployeeId,
                                EmployeeName = c.CommenterEmployee.EmployeeName,
                                EmployeeImage = c.CommenterEmployee.EmployeeImage,
                                EmployeePassword = c.CommenterEmployee.EmployeePassword // 必须包含required字段
                            } : null,
                            CommenterAdmin = c.CommenterAdmin != null ? new Admin 
                            {
                                AdminId = c.CommenterAdmin.AdminId,
                                AdminName = c.CommenterAdmin.AdminName,
                                AdminImage = c.CommenterAdmin.AdminImage,
                                AdminPassword = c.CommenterAdmin.AdminPassword // 必须包含required字段
                            } : null,
                            CommenterSupAdmin = c.CommenterSupAdmin != null ? new SupAdmin 
                            {
                                SupAdminId = c.CommenterSupAdmin.SupAdminId,
                                SupAdminName = c.CommenterSupAdmin.SupAdminName,
                                SupAdminImage = c.CommenterSupAdmin.SupAdminImage,
                                SupAdminPassword = c.CommenterSupAdmin.SupAdminPassword // 必须包含required字段
                            } : null
                        })
                        .ToListAsync();
                    
                    // 获取总评论数
                    int totalComments = await context.CommentRecords
                        .AsNoTracking()
                        .Where(c => c.NominationId == nomination.NominationId && !c.IsDeleted)
                        .CountAsync();
                    
                    // 在UI线程上更新UI集合
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                        // 确保UIComments已初始化
                        if (nomination.UIComments == null)
                        {
                            nomination.UIComments = new ObservableCollection<CommentRecord>();
                        }
                        
                        // 如果是第一页，清空集合；否则追加数据
                        if (page == 1)
                        {
                            nomination.UIComments.Clear();
                        }
                        
                        // 添加新加载的评论
                        foreach (var comment in comments)
                        {
                            nomination.UIComments.Add(comment);
                        }
                        
                        // 更新本地评论数量
                        nomination.CommentCount = totalComments;
                        
                        // 如果评论数量大于页面显示数量，显示加载更多按钮
                        nomination.HasMoreComments = totalComments > nomination.UIComments.Count;
                        nomination.CurrentCommentPage = page;
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载评论时发生错误: {ex}");
                    throw; // 重新抛出异常，让调用者处理
                }
            }
        }
        
        /// <summary>
        /// 添加评论
        /// </summary>
        private async void ExecuteAddCommentCommand(Nomination nomination)
        {
            if (nomination == null) return;
            
            // 检查评论内容是否为空
            if (string.IsNullOrWhiteSpace(nomination.NewCommentText))
            {
                HandyControl.Controls.Growl.InfoGlobal("评论内容不能为空");
                return;
            }
            
            // 检查是否登录，任意一个ID不为空即表示已登录
            bool isLoggedIn = CurrentEmployeeId.HasValue || CurrentAdminId.HasValue || CurrentSupAdminId.HasValue;
            if (!isLoggedIn)
            {
                HandyControl.Controls.Growl.WarningGlobal("您需要登录后才能发表评论");
                return;
            }

            IsLoading = true;
            StatusMessage = "正在发表评论...";
            
            try
            {
                await Task.Run(async () => 
                {
                    using (var context = new DataBaseContext())
                    {
                        try 
                        {
                            // 创建执行策略来处理事务
                            var strategy = context.Database.CreateExecutionStrategy();
                            
                            // 使用执行策略来包装事务操作
                            await strategy.ExecuteAsync(async () =>
                            {
                                // 在执行策略内部使用事务
                                using (var transaction = await context.Database.BeginTransactionAsync())
                                {
                                    try
                                    {
                                        // 检查奖项和提名是否存在
                                        var nominationEntity = await context.Nominations
                                            .FirstOrDefaultAsync(n => n.NominationId == nomination.NominationId);
                                        
                                        if (nominationEntity == null)
                                        {
                                            Application.Current.Dispatcher.Invoke(() => 
                                            {
                                                StatusMessage = "提名不存在或已被删除";
                                                HandyControl.Controls.Growl.WarningGlobal("提名不存在或已被删除");
                                            });
                                            return;
                                        }
                                        
                                        // 先获取发表评论的用户名称，用于显示消息
                                        string commenterName = "未知用户";
                                        
                                        // 创建新评论 - 仅设置必要的字段
                                        var newComment = new CommentRecord
                                        {
                                            Content = nomination.NewCommentText.Trim(), // 移除前后空格
                                            CommentTime = DateTime.Now,
                                            NominationId = nomination.NominationId,
                                            AwardId = nomination.AwardId,
                                            IsDeleted = false
                                        };
                                        
                                        // 根据当前用户类型设置评论者ID
                                        if (CurrentSupAdminId.HasValue)
                                        {
                                            // 超级管理员评论
                                            newComment.CommenterSupAdminId = CurrentSupAdminId.Value;
                                            var supAdmin = await context.SupAdmins
                                                .AsNoTracking()
                                                .Where(s => s.SupAdminId == CurrentSupAdminId.Value)
                                                .Select(s => new { s.SupAdminName })
                                                .FirstOrDefaultAsync();
                                            if (supAdmin != null)
                                            {
                                                commenterName = supAdmin.SupAdminName;
                                            }
                                            
                                            // 更新提名表的最新评论者字段
                                            nominationEntity.LastCommenterEmployeeId = null;
                                            nominationEntity.LastCommenterAdminId = null;
                                            nominationEntity.LastCommenterSupAdminId = CurrentSupAdminId.Value;
                                        }
                                        else if (CurrentAdminId.HasValue)
                                        {
                                            // 管理员评论
                                            newComment.CommenterAdminId = CurrentAdminId.Value;
                                            var admin = await context.Admins
                                                .AsNoTracking()
                                                .Where(a => a.AdminId == CurrentAdminId.Value)
                                                .Select(a => new { a.AdminName })
                                                .FirstOrDefaultAsync();
                                            if (admin != null)
                                            {
                                                commenterName = admin.AdminName;
                                            }
                                            
                                            // 更新提名表的最新评论者字段
                                            nominationEntity.LastCommenterEmployeeId = null;
                                            nominationEntity.LastCommenterAdminId = CurrentAdminId.Value;
                                            nominationEntity.LastCommenterSupAdminId = null;
                                        }
                                        else if (CurrentEmployeeId.HasValue)
                                        {
                                            // 员工评论
                                            newComment.CommenterEmployeeId = CurrentEmployeeId.Value;
                                            var employee = await context.Employees
                                                .AsNoTracking()
                                                .Where(e => e.EmployeeId == CurrentEmployeeId.Value)
                                                .Select(e => new { e.EmployeeName })
                                                .FirstOrDefaultAsync();
                                            if (employee != null)
                                            {
                                                commenterName = employee.EmployeeName;
                                            }
                                            
                                            // 更新提名表的最新评论者字段
                                            nominationEntity.LastCommenterEmployeeId = CurrentEmployeeId.Value;
                                            nominationEntity.LastCommenterAdminId = null;
                                            nominationEntity.LastCommenterSupAdminId = null;
                                        }
                                        else
                                        {
                                            // 未登录用户不能评论（前面已经检查过，正常不会走到这里）
                                            Application.Current.Dispatcher.Invoke(() => 
                                            {
                                                StatusMessage = "您需要登录后才能发表评论";
                                                HandyControl.Controls.Growl.WarningGlobal("您需要登录后才能发表评论");
                                            });
                                            return;
                                        }
                                        
                                        // 更新提名表中的评论相关字段
                                        nominationEntity.CommentCount = nominationEntity.CommentCount + 1;
                                        nominationEntity.LastCommentTime = newComment.CommentTime;
                                        // 存储评论内容预览（最多80个字符）
                                        nominationEntity.LastCommentPreview = newComment.Content.Length > 80 
                                            ? newComment.Content.Substring(0, 80) + "..." 
                                            : newComment.Content;
                                        
                                        // 保存评论并更新提名
                                        context.CommentRecords.Add(newComment);
                                        await context.SaveChangesAsync();
                                        
                                        // 提交事务
                                        await transaction.CommitAsync();
                                        
                                        // 保存成功后，再次查询评论以获取完整数据（包括用户信息）
                                        var savedComment = await context.CommentRecords
                                            .AsNoTracking()
                                            .Include(c => c.CommenterEmployee)
                                            .Include(c => c.CommenterAdmin)
                                            .Include(c => c.CommenterSupAdmin)
                                            .FirstOrDefaultAsync(c => c.CommentId == newComment.CommentId);
                                        
                                        if (savedComment == null)
                                        {
                                            Application.Current.Dispatcher.Invoke(() => 
                                            {
                                                StatusMessage = "评论保存成功但无法加载";
                                                HandyControl.Controls.Growl.WarningGlobal("评论保存成功但无法加载");
                                            });
                                            return;
                                        }
                                        
                                        // 在UI线程上更新界面
                                        Application.Current.Dispatcher.Invoke(() => 
                                        {
                                            // 清空评论输入框
                                            nomination.NewCommentText = string.Empty;
                                            
                                            // 添加新评论到界面集合
                                            nomination.UIComments.Insert(0, savedComment);
                                            
                                            // 更新本地评论数据和计数
                                            nomination.CommentCount = nominationEntity.CommentCount;
                                            nomination.LastCommentTime = nominationEntity.LastCommentTime;
                                            nomination.LastCommentPreview = nominationEntity.LastCommentPreview;
                                            nomination.LastCommenterEmployeeId = nominationEntity.LastCommenterEmployeeId;
                                            nomination.LastCommenterAdminId = nominationEntity.LastCommenterAdminId;
                                            nomination.LastCommenterSupAdminId = nominationEntity.LastCommenterSupAdminId;
                                            
                                            StatusMessage = $"{commenterName} 的评论发表成功";
                                            HandyControl.Controls.Growl.SuccessGlobal($"{commenterName} 的评论发表成功");
                                            
                                            // 强制刷新UI
                                            var index = PagedNominations.IndexOf(nomination);
                                            if (index != -1)
                                            {
                                                var temp = PagedNominations[index];
                                                PagedNominations.RemoveAt(index);
                                                PagedNominations.Insert(index, temp);
                                            }
                                        });
                                    }
                                    catch
                                    {
                                        // 发生异常时回滚事务
                                        await transaction.RollbackAsync();
                                        throw;
                                    }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            // 处理异常
                            Application.Current.Dispatcher.Invoke(() => 
                            {
                                StatusMessage = $"发表评论失败: {ex.Message}";
                                HandyControl.Controls.Growl.ErrorGlobal($"发表评论失败: {ex.Message}");
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"发表评论失败: {ex.Message}";
                HandyControl.Controls.Growl.ErrorGlobal($"发表评论失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        /// <summary>
        /// 删除评论
        /// </summary>
        private async void ExecuteDeleteCommentCommand(CommentRecord comment)
        {
            if (comment == null) return;
            
            // 只有管理员和超级管理员可以删除评论
            if (!IsAdmin)
            {
                HandyControl.Controls.Growl.WarningGlobal("您没有权限删除评论");
                return;
            }
            
            // 确认是否删除
            var result = HandyControl.Controls.MessageBox.Ask("确定要删除这条评论吗？", "确认");
            if (result != MessageBoxResult.OK) return;
            
            IsLoading = true;
            StatusMessage = "正在删除评论...";
            
            try
            {
                // 找到评论所属的提名对象，用于后续刷新UI
                Nomination nomination = null;
                Application.Current.Dispatcher.Invoke(() => {
                    nomination = Nominations.FirstOrDefault(n => n.NominationId == comment.NominationId);
                });
                
                if (nomination == null)
                {
                    StatusMessage = "找不到评论所属的提名";
                    HandyControl.Controls.Growl.WarningGlobal("找不到评论所属的提名");
                    IsLoading = false;
                    return;
                }
                
                await Task.Run(async () =>
                {
                    using (var context = new DataBaseContext())
                    {
                        try
                        {
                            // 创建执行策略来处理事务
                            var strategy = context.Database.CreateExecutionStrategy();
                            
                            // 使用执行策略来包装事务操作
                            await strategy.ExecuteAsync(async () =>
                            {
                                // 在执行策略内部使用事务
                                using (var transaction = await context.Database.BeginTransactionAsync())
                                {
                                    try
                                    {
                                        // 查找评论
                                        var commentToDelete = await context.CommentRecords.FindAsync(comment.CommentId);
                                        if (commentToDelete == null)
                                        {
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                HandyControl.Controls.Growl.WarningGlobal("评论不存在或已被删除");
                                                StatusMessage = "评论不存在或已被删除";
                                            });
                                            return;
                                        }
                                        
                                        // 设置评论为已删除
                                        commentToDelete.IsDeleted = true;
                                        commentToDelete.DeletedTime = DateTime.Now;
                                        
                                        // 根据当前用户类型设置删除者信息
                                        if (CurrentAdminId.HasValue)
                                        {
                                            commentToDelete.DeletedByAdminId = CurrentAdminId;
                                        }
                                        else if (CurrentSupAdminId.HasValue)
                                        {
                                            commentToDelete.DeletedBySupAdminId = CurrentSupAdminId;
                                        }
                                        
                                        // 更新提名表中的评论计数
                                        var nominationEntity = await context.Nominations.FindAsync(comment.NominationId);
                                        if (nominationEntity != null)
                                        {
                                            nominationEntity.CommentCount = Math.Max(0, nominationEntity.CommentCount - 1);
                                            
                                            // 如果删除的是最新评论，需要更新最新评论相关信息
                                            if (comment.CommentTime == nominationEntity.LastCommentTime)
                                            {
                                                // 查找最新的未删除评论
                                                var latestComment = await context.CommentRecords
                                                    .Where(c => c.NominationId == comment.NominationId && !c.IsDeleted)
                                                    .OrderByDescending(c => c.CommentTime)
                                                    .FirstOrDefaultAsync();
                                                
                                                if (latestComment != null)
                                                {
                                                    // 更新最新评论信息
                                                    nominationEntity.LastCommentTime = latestComment.CommentTime;
                                                    nominationEntity.LastCommenterEmployeeId = latestComment.CommenterEmployeeId;
                                                    nominationEntity.LastCommenterAdminId = latestComment.CommenterAdminId;
                                                    nominationEntity.LastCommenterSupAdminId = latestComment.CommenterSupAdminId;
                                                    nominationEntity.LastCommentPreview = latestComment.Content.Length > 80 
                                                        ? latestComment.Content.Substring(0, 80) + "..." 
                                                        : latestComment.Content;
                                                }
                                                else
                                                {
                                                    // 如果没有剩余评论，清空最新评论信息
                                                    nominationEntity.LastCommentTime = null;
                                                    nominationEntity.LastCommenterEmployeeId = null;
                                                    nominationEntity.LastCommenterAdminId = null;
                                                    nominationEntity.LastCommenterSupAdminId = null;
                                                    nominationEntity.LastCommentPreview = null;
                                                }
                                            }
                                        }
                                        
                                        // 保存更改
                                        await context.SaveChangesAsync();
                                        
                                        // 提交事务
                                        await transaction.CommitAsync();
                                        
                                        // 获取删除者名称
                                        string deleterName = "管理员";
                                        if (CurrentAdminId.HasValue)
                                        {
                                            var admin = await context.Admins
                                                .AsNoTracking()
                                                .Where(a => a.AdminId == CurrentAdminId.Value)
                                                .Select(a => new { a.AdminName })
                                                .FirstOrDefaultAsync();
                                            if (admin != null)
                                            {
                                                deleterName = admin.AdminName;
                                            }
                                        }
                                        else if (CurrentSupAdminId.HasValue)
                                        {
                                            var supAdmin = await context.SupAdmins
                                                .AsNoTracking()
                                                .Where(s => s.SupAdminId == CurrentSupAdminId.Value)
                                                .Select(s => new { s.SupAdminName })
                                                .FirstOrDefaultAsync();
                                            if (supAdmin != null)
                                            {
                                                deleterName = supAdmin.SupAdminName;
                                            }
                                        }
                                        
                                        // 在UI线程中完全刷新评论区
                                        Application.Current.Dispatcher.Invoke(async () =>
                                        {
                                            try
                                            {
                                                // 更新本地评论计数
                                                if (nominationEntity != null)
                                                {
                                                    nomination.CommentCount = nominationEntity.CommentCount;
                                                    nomination.LastCommentTime = nominationEntity.LastCommentTime;
                                                    nomination.LastCommenterEmployeeId = nominationEntity.LastCommenterEmployeeId;
                                                    nomination.LastCommenterAdminId = nominationEntity.LastCommenterAdminId;
                                                    nomination.LastCommenterSupAdminId = nominationEntity.LastCommenterSupAdminId;
                                                    nomination.LastCommentPreview = nominationEntity.LastCommentPreview;
                                                }
                                                
                                                // 重新加载该提名的所有评论
                                                await LoadCommentsAsync(nomination);
                                                
                                                // 确保评论区依然保持打开状态
                                                nomination.IsCommentSectionVisible = true;
                                                
                                                // 强制刷新UI
                                                var index = PagedNominations.IndexOf(nomination);
                                                if (index != -1)
                                                {
                                                    var temp = PagedNominations[index];
                                                    PagedNominations.RemoveAt(index);
                                                    PagedNominations.Insert(index, temp);
                                                }
                                                
                                                StatusMessage = $"评论已被{deleterName}删除";
                                                HandyControl.Controls.Growl.SuccessGlobal($"评论已被{deleterName}删除");
                                            }
                                            catch (Exception ex)
                                            {
                                                StatusMessage = $"刷新评论区失败: {ex.Message}";
                                                System.Diagnostics.Debug.WriteLine($"刷新评论区失败: {ex}");
                                            }
                                        });
                                    }
                                    catch
                                    {
                                        // 发生异常时回滚事务
                                        await transaction.RollbackAsync();
                                        throw;
                                    }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"删除评论异常: {ex}");
                            
                            Application.Current.Dispatcher.Invoke(() => 
                            {
                                StatusMessage = $"删除评论失败: {ex.Message}";
                                HandyControl.Controls.MessageBox.Error($"删除评论失败: {ex.Message}", "错误");
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"评论删除失败: {ex.Message}";
                HandyControl.Controls.MessageBox.Error($"评论删除失败: {ex.Message}", "错误");
                System.Diagnostics.Debug.WriteLine($"评论删除失败: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行首页命令
        /// </summary>
        private void ExecuteFirstPageCommand()
        {
            if (CurrentPage > 1)
            {
                CurrentPage = 1;
                UpdatePagedNominations();
            }
        }

        /// <summary>
        /// 执行上一页命令
        /// </summary>
        private void ExecutePreviousPageCommand()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdatePagedNominations();
            }
        }

        /// <summary>
        /// 执行下一页命令
        /// </summary>
        private void ExecuteNextPageCommand()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                UpdatePagedNominations();
            }
        }

        /// <summary>
        /// 执行末页命令
        /// </summary>
        private void ExecuteLastPageCommand()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage = TotalPages;
                UpdatePagedNominations();
            }
        }

        /// <summary>
        /// 首页命令的CanExecute方法
        /// </summary>
        private bool CanExecuteFirstPageCommand()
        {
            return CurrentPage > 1;
        }

        /// <summary>
        /// 上一页命令的CanExecute方法
        /// </summary>
        private bool CanExecutePreviousPageCommand()
        {
            return CurrentPage > 1;
        }

        /// <summary>
        /// 下一页命令的CanExecute方法
        /// </summary>
        private bool CanExecuteNextPageCommand()
        {
            return CurrentPage < TotalPages;
        }

        /// <summary>
        /// 末页命令的CanExecute方法
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
            if (parameter is int pageSize)
            {
                PageSize = pageSize;
                // 切换页面大小后返回第一页
                CurrentPage = 1;
                UpdatePagedNominations();
            }
        }

        /// <summary>
        /// 执行跳转页命令
        /// </summary>
        private void ExecuteJumpPageCommand()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return;

            if (int.TryParse(SearchText, out int pageNumber))
            {
                // 确保页码在有效范围内
                if (pageNumber >= 1 && pageNumber <= TotalPages)
                {
                    CurrentPage = pageNumber;
                    UpdatePagedNominations();
                }
                else
                {
                    HandyControl.Controls.Growl.WarningGlobal($"页码超出范围，有效范围：1-{TotalPages}");
                    // 自动修正页码
                    if (pageNumber < 1)
                        SearchText = "1";
                    else if (pageNumber > TotalPages)
                        SearchText = TotalPages.ToString();
                }
            }
            else
            {
                HandyControl.Controls.Growl.WarningGlobal("请输入有效的页码");
                SearchText = CurrentPage.ToString();
            }
        }

        /// <summary>
        /// 执行输入验证命令
        /// </summary>
        private void ExecutePreviewTextInputCommand(string text)
        {
            // 监控按键，如果是Enter键则执行跳转
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Enter))
            {
                ExecuteJumpPageCommand();
            }
            
            // 只允许输入数字
            if (!string.IsNullOrEmpty(text))
            {
                foreach (char c in text)
                {
                    if (!char.IsDigit(c))
                    {
                        SearchText = text.Where(char.IsDigit).Aggregate(string.Empty, (current, c) => current + c);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 关闭所有评论区，除了指定ID的提名
        /// </summary>
        private void CloseAllCommentSections(int exceptNominationId)
        {
            // 在所有提名记录中检查并关闭评论区
            if (Nominations != null)
            {
                foreach (var nomination in Nominations)
                {
                    if (nomination.NominationId != exceptNominationId && nomination.IsCommentSectionVisible)
                    {
                        nomination.IsCommentSectionVisible = false;
                    }
                }
            }
            
            // 在分页数据中再次检查，确保可见内容也被关闭
            if (PagedNominations != null)
            {
                foreach (var nomination in PagedNominations)
                {
                    if (nomination.NominationId != exceptNominationId && nomination.IsCommentSectionVisible)
                    {
                        nomination.IsCommentSectionVisible = false;
                    }
                }
            }
        }

        /// <summary>
        /// 执行加载更多评论命令
        /// </summary>
        private async void ExecuteLoadMoreCommentsCommand(Nomination nomination)
        {
            if (nomination == null) return;
            
            // 已经没有更多评论
            if (!nomination.HasMoreComments) return;
            
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载更多评论...";
                
                // 增加页码
                int nextPage = nomination.CurrentCommentPage + 1;
                
                // 调用加载评论方法，传递递增的页码
                await LoadCommentsAsync(nomination, nextPage);
                
                StatusMessage = "评论加载完成";
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载更多评论失败: {ex.Message}";
                HandyControl.Controls.Growl.ErrorGlobal($"加载更多评论失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"加载更多评论失败: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 清理资源并重置状态，在页面导航离开时调用以避免多用户数据混乱
        /// </summary>
        public void Cleanup()
        {
            // 清空集合，避免内存泄漏
            if (Nominations != null)
            {
                foreach (var nomination in Nominations)
                {
                    // 清空评论相关数据
                    if (nomination.UIComments != null)
                    {
                        nomination.UIComments.Clear();
                    }
                }
                Nominations.Clear();
            }
            
            if (PagedNominations != null)
            {
                PagedNominations.Clear();
            }
            
            if (VotedAwardIds != null)
            {
                VotedAwardIds.Clear();
            }
            
            // 重置状态
            IsLoading = false;
            CurrentPage = 1;
            
            // 标记当前ViewModel已清理，避免在用户切换后继续处理事件
            IsActive = false;
        }
        
        /// <summary>
        /// 验证当前ViewModel是否可以执行操作，避免在页面导航离开后继续处理事件
        /// </summary>
        private bool CanExecuteOperation()
        {
            return IsActive && !IsLoading;
        }

        /// <summary>
        /// 处理标签页完成加载事件
        /// </summary>
        private void ExecuteTabCompleteCommand(string tabName)
        {
            // ... existing code ...
        }
        
        /// <summary>
        /// 执行筛选奖项的命令
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        private void ExecuteFilterAwardsCommand(string searchText)
        {
            // 只保存搜索文本，不执行搜索操作
            SearchedAwardName = searchText;
        }
        
        /// <summary>
        /// 处理键盘输入事件
        /// </summary>
        private void ExecuteHandlePreviewKeyDownCommand(object parameter)
        {
            // 尝试将参数转换为KeyEventArgs
            if (parameter is System.Windows.Input.KeyEventArgs keyArgs)
            {
                // 检查是否按下了Tab键
                if (keyArgs.Key == System.Windows.Input.Key.Tab)
                {
                    // 如果有匹配的奖项，选择第一个
                    if (_filteredAwards.Count > 0)
                    {
                        var bestMatch = _filteredAwards.FirstOrDefault();
                        if (bestMatch != null)
                        {
                            // 在UI线程上执行，因为可能在事件处理中
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                // 设置为选中项
                                SelectedAward = bestMatch;
                                // 更新输入框文本
                                SearchedAwardName = bestMatch.AwardName;
                                
                                // 触发搜索
                                ExecuteSearchCommand();
                            }));
                            
                            // 标记事件已处理，防止默认Tab行为
                            keyArgs.Handled = true;
                        }
                    }
                }
            }
        }

        #endregion

        // 添加查看提名详情命令
        private DelegateCommand<Nomination> _viewNominationDetailsCommand;
        /// <summary>
        /// 查看提名详情命令
        /// </summary>
        public DelegateCommand<Nomination> ViewNominationDetailsCommand =>
            _viewNominationDetailsCommand ?? (_viewNominationDetailsCommand = new DelegateCommand<Nomination>(ExecuteViewNominationDetailsCommand));

        /// <summary>
        /// 执行查看提名详情命令
        /// </summary>
        private async void ExecuteViewNominationDetailsCommand(Nomination nomination)
        {
            if (nomination == null)
                return;
            
            // 显示加载状态
            IsLoading = true;
            
            try
            {
                // 从数据库加载完整的提名数据，包括所有关联实体
                using (var context = new DataBaseContext())
                {
                    // 加载完整的提名数据，包括关联实体
                    var fullNomination = await context.Nominations
                        .AsNoTracking()
                        .Include(n => n.NominatedEmployee)
                        .Include(n => n.NominatedAdmin)
                        .Include(n => n.Award)
                        .Include(n => n.Department)
                        .Include(n => n.VoteRecords)
                            .ThenInclude(vr => vr.VoterEmployee)
                                .ThenInclude(e => e.Department)
                        .Include(n => n.VoteRecords)
                            .ThenInclude(vr => vr.VoterAdmin)
                                .ThenInclude(a => a.Department)
                        .FirstOrDefaultAsync(n => n.NominationId == nomination.NominationId);
                    
                    if (fullNomination != null)
                    {
                        // 确保CoverImage被正确加载
                        fullNomination.CoverImage = await context.Nominations
                            .Where(n => n.NominationId == nomination.NominationId)
                            .Select(n => n.CoverImage)
                            .FirstOrDefaultAsync();
                        
                        // 创建并显示详情窗口
                        var detailsWindow = new NominationDetailsWindow(fullNomination);
                        
                        // 显示窗口
                        detailsWindow.ShowDialog();
                    }
                    else
                    {
                        HandyControl.Controls.Growl.WarningGlobal("未能加载提名详情");
                    }
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.Growl.ErrorGlobal($"查看提名详情失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"查看提名详情异常详情: {ex}");
            }
            finally
            {
                // 隐藏加载状态
                IsLoading = false;
            }
        }

        /// <summary>
        /// 投票命令的CanExecute方法
        /// </summary>
        private bool CanExecuteVoteCommand(Nomination nomination)
        {
            // 超级管理员不能投票
            if (IsSuperAdmin || nomination == null)
                return false;

            // 只需验证nomination存在，实际投票逻辑将在ExecuteVoteCommand中进行
            // 这样即使用户已经投过票，按钮也会可点击，但点击时会显示提示信息
            return nomination != null;
        }

        /// <summary>
        /// 执行搜索命令
        /// </summary>
        private void ExecuteSearchCommand()
        {
            if (!CanExecuteOperation())
                return;
            
            // 更新状态
            IsLoading = true;
            StatusMessage = "正在搜索...";

            // 首先处理奖项筛选
            if (!string.IsNullOrEmpty(SearchedAwardName) && SearchedAwardName != AllAwards.AwardName)
            {
                // 从缓存的奖项列表中查找匹配的奖项
                var exactMatch = _filteredAwards.FirstOrDefault(a => 
                    a.AwardName.Equals(SearchedAwardName, StringComparison.OrdinalIgnoreCase));
                
                if (exactMatch != null)
                {
                    // 找到精确匹配
                    SelectedAward = exactMatch;
                }
                else
                {
                    // 尝试模糊匹配
                    var fuzzyMatch = _filteredAwards.FirstOrDefault(a => 
                        a.AwardName.Contains(SearchedAwardName, StringComparison.OrdinalIgnoreCase));
                    
                    if (fuzzyMatch != null)
                    {
                        SelectedAward = fuzzyMatch;
                    }
                }
            }

            // 然后应用关键字搜索 - LoadNominationsAsync方法会自动设置IsLoading=false
            LoadNominationsAsync();
        }

        #endregion
    }
}
