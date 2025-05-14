using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO; // 添加文件操作
using System.Text; // 添加文本编码相关
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SIASGraduate.Common;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using SIASGraduate.ViewModels.EditMessage.NominationLogViewer;
using SIASGraduate.Views.EditMessage.NominationLogViewer;
using MediaColor = System.Windows.Media.Color; // 使用别名避免Color冲突
using SolidColorBrush = System.Windows.Media.SolidColorBrush; // 明确指定SolidColorBrush类型
using WindowNS = System.Windows.Window;

namespace SIASGraduate.ViewModels.Pages
{
    public class NominationDeclarationViewModel : INotifyPropertyChanged
    {
        #region 时间属性
        private DispatcherTimer timer;
        private DispatcherTimer autoRefreshTimer; // 自动刷新的计时器
        #endregion

        #region 区域管理器
        private readonly IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 构造函数
        public NominationDeclarationViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;



            #region 分页初始化
            EnableButtons();
            InitPageSizeOptions();
            #endregion

            #region 搜索区域颜色随时间变换
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += async (s, e) => await ColorChangeAsync();
            timer.Start();
            // 初始化为默认颜色
            SearchBackground = new SolidColorBrush(MediaColor.FromRgb(173, 216, 230));
            #endregion

            #region 自动刷新计时器
            // 自动刷新已改为手动刷新，不再使用计时器
            // autoRefreshTimer = new DispatcherTimer
            // {
            //     Interval = TimeSpan.FromSeconds(60) // 修改为60秒刷新一次
            // };
            // autoRefreshTimer.Tick += (s, e) => AutoRefreshData();
            // autoRefreshTimer.Start();
            #endregion

            #region 初始化状态选项
            InitStatusOptions();
            #endregion

            #region 加载申报数据
            LoadDeclarationsAsync();
            #endregion

            #region 初始化命令
            InitCommands();
            #endregion

            #region 根据当前用户角色设置权限
            SetUserPermissions();
            #endregion

            #region 订阅事件
            SubscribeEvents();
            #endregion
        }
        #endregion

        #region 初始化状态选项
        private void InitStatusOptions()
        {
            StatusOptions = new Dictionary<int, string>
            {
                { -1, "全部" },
                { 0, "待审核" },
                { 1, "已通过" },
                { 2, "已拒绝" }
            };
            SelectedStatus = StatusOptions.First();
        }
        #endregion

        #region 设置用户权限
        private void SetUserPermissions()
        {
            // 根据当前登录用户角色设置权限
            switch (CurrentUser.RoleId)
            {
                case 1: // 超级管理员
                    IsAddEnabled = false; // 禁用超级管理员的新增申报功能
                    IsAddButtonVisible = false; // 隐藏超级管理员的新增按钮
                    IsEditEnabled = true;
                    IsDeleteEnabled = true;
                    IsReviewButtonVisible = true;
                    IsPromoteButtonVisible = true;
                    break;
                case 2: // 管理员
                    IsAddEnabled = true;
                    IsAddButtonVisible = true;
                    IsEditEnabled = true;
                    IsDeleteEnabled = true;
                    IsReviewButtonVisible = true;
                    IsPromoteButtonVisible = true;
                    break;
                case 3: // 普通员工
                    IsAddEnabled = true;
                    IsAddButtonVisible = true;
                    IsEditEnabled = true;
                    IsDeleteEnabled = false;
                    IsReviewButtonVisible = false;
                    IsPromoteButtonVisible = false;
                    break;
                default:
                    IsAddEnabled = false;
                    IsAddButtonVisible = false;
                    IsEditEnabled = false;
                    IsDeleteEnabled = false;
                    IsReviewButtonVisible = false;
                    IsPromoteButtonVisible = false;
                    break;
            }
        }
        #endregion

        #region 订阅事件
        private void SubscribeEvents()
        {
            eventAggregator.GetEvent<NominationDeclarationAddEvent>().Subscribe(OnDeclarationAdded);
            eventAggregator.GetEvent<NominationDeclarationUpdateEvent>().Subscribe(OnDeclarationUpdated);
            eventAggregator.GetEvent<NominationDeclarationDeleteEvent>().Subscribe(OnDeclarationDeleted);
            eventAggregator.GetEvent<NominationDeclarationApproveEvent>().Subscribe(OnDeclarationApproved);
            eventAggregator.GetEvent<NominationDeclarationRejectEvent>().Subscribe(OnDeclarationRejected);
            eventAggregator.GetEvent<NominationDeclarationPromoteEvent>().Subscribe(OnDeclarationPromoted);
            eventAggregator.GetEvent<NominationDataChangedEvent>().Subscribe(OnNominationDataChanged);
        }
        #endregion

        #region 初始化命令
        private void InitCommands()
        {
            // 文本输入预览命令
            PreviewTextInputCommand = new DelegateCommand<string>(OnPreviewTextInput);

            // 查询命令
            SearchDeclarationCommand = new DelegateCommand(OnSearchDeclaration);

            // 新增命令
            AddDeclarationCommand = new DelegateCommand(OnAddDeclaration);

            // 编辑命令
            EditDeclarationCommand = new DelegateCommand<NominationDeclaration>(OnEditDeclaration);

            // 删除命令
            DeleteDeclarationCommand = new DelegateCommand<NominationDeclaration>(OnDeleteDeclaration);

            // 审核命令
            ApproveCommand = new DelegateCommand<NominationDeclaration>(OnApproveDeclaration);
            RejectCommand = new DelegateCommand<NominationDeclaration>(OnRejectDeclaration);

            // 转为提名命令
            PromoteCommand = new DelegateCommand<NominationDeclaration>(OnPromoteDeclaration);

            // 查看日志命令
            ViewLogCommand = new DelegateCommand(OnViewLog);

            // 查看图片命令
            ViewImageCommand = new DelegateCommand<NominationDeclaration>(OnViewImage);

            // 导出命令
            ExportDataCommand = new DelegateCommand(OnExportData);

            // 数据刷新命令
            RefreshDataCommand = new DelegateCommand(OnRefreshData);

            // 分页命令
            PreviousPageCommand = new DelegateCommand(PreviousPage);
            NextPageCommand = new DelegateCommand(NextPage);
            JumpPageCommand = new DelegateCommand(JumpPage);
            PageSizeChangedCommand = new DelegateCommand(PageSizeChanged);

            // 清除选择命令
            ClearSelectionCommand = new DelegateCommand(ClearSelection);
        }
        #endregion

        #region 属性

        #region 搜索关键词
        private string searchKeyword;
        public string SearchKeyword { get => searchKeyword; set => SetProperty(ref searchKeyword, value); }
        #endregion

        #region 状态选项
        private Dictionary<int, string> statusOptions;
        public Dictionary<int, string> StatusOptions { get => statusOptions; set => SetProperty(ref statusOptions, value); }

        private KeyValuePair<int, string> selectedStatus;
        public KeyValuePair<int, string> SelectedStatus { get => selectedStatus; set => SetProperty(ref selectedStatus, value); }
        #endregion

        #region 原始申报列表
        private ObservableCollection<NominationDeclaration> declarations;
        public ObservableCollection<NominationDeclaration> Declarations { get => declarations; set => SetProperty(ref declarations, value); }
        #endregion

        #region 临时申报列表
        private ObservableCollection<NominationDeclaration> tempViewDeclarations;
        public ObservableCollection<NominationDeclaration> TempViewDeclarations { get => tempViewDeclarations; set => SetProperty(ref tempViewDeclarations, value); }
        #endregion

        #region 视图展示申报列表
        private ObservableCollection<NominationDeclaration> listViewDeclarations;
        public ObservableCollection<NominationDeclaration> ListViewDeclarations { get => listViewDeclarations; set => SetProperty(ref listViewDeclarations, value); }
        #endregion

        #region 选中的申报
        private NominationDeclaration selectedDeclaration;
        public NominationDeclaration SelectedDeclaration { get => selectedDeclaration; set => SetProperty(ref selectedDeclaration, value); }
        #endregion

        #region 申报日志
        private ObservableCollection<NominationLog> logs;
        public ObservableCollection<NominationLog> Logs { get => logs; set => SetProperty(ref logs, value); }
        #endregion

        #region 操作按钮是否可见/启用
        private bool _isAddEnabled;
        public bool IsAddEnabled
        {
            get => _isAddEnabled;
            set => SetProperty(ref _isAddEnabled, value);
        }

        private bool _isAddButtonVisible;
        public bool IsAddButtonVisible
        {
            get => _isAddButtonVisible;
            set => SetProperty(ref _isAddButtonVisible, value);
        }

        private bool _isEditEnabled;
        public bool IsEditEnabled
        {
            get => _isEditEnabled;
            set => SetProperty(ref _isEditEnabled, value);
        }

        private bool isDeleteEnabled;
        public bool IsDeleteEnabled
        {
            get => isDeleteEnabled;
            set
            {
                isDeleteEnabled = value;
                OnPropertyChanged();
            }
        }

        private bool isSearchEnabled = true;
        public bool IsSearchEnabled { get => isSearchEnabled; set => SetProperty(ref isSearchEnabled, value); }

        private bool isReviewButtonVisible;
        public bool IsReviewButtonVisible
        {
            get => isReviewButtonVisible;
            set
            {
                isReviewButtonVisible = value;
                OnPropertyChanged();
            }
        }

        private bool isPromoteButtonVisible;
        public bool IsPromoteButtonVisible
        {
            get => isPromoteButtonVisible;
            set
            {
                isPromoteButtonVisible = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region 搜索区域背景色
        private byte colorOffset = 0;
        private SolidColorBrush searchBackground;
        public SolidColorBrush SearchBackground { get => searchBackground; set => SetProperty(ref searchBackground, value); }
        #endregion

        #region 分页相关属性
        private int maxPage = 1;
        public int MaxPage { get => maxPage; set => SetProperty(ref maxPage, value); }

        private int totalRecords;
        public int TotalRecords { get => totalRecords; set => SetProperty(ref totalRecords, value); }

        private int currentPage = 1;
        public int CurrentPage { get => currentPage; set => SetProperty(ref currentPage, value); }

        private int pageSize = 8;
        public int PageSize { get => pageSize; set => SetProperty(ref pageSize, value); }

        private IEnumerable pageSizeOptions;
        public IEnumerable PageSizeOptions { get => pageSizeOptions; set => SetProperty(ref pageSizeOptions, value); }

        private string searchText;
        public string SearchText { get => searchText; set => SetProperty(ref searchText, value); }

        private string lastInput = string.Empty;
        public string LastInput { get => lastInput; set => SetProperty(ref lastInput, value); }

        // 添加缺失的分页导航属性
        private bool _isFirstPage;
        public bool IsFirstPage
        {
            get => _isFirstPage;
            set => SetProperty(ref _isFirstPage, value);
        }

        private bool _isLastPage;
        public bool IsLastPage
        {
            get => _isLastPage;
            set => SetProperty(ref _isLastPage, value);
        }

        private bool _isNextEnabled;
        public bool IsNextEnabled
        {
            get => _isNextEnabled;
            set => SetProperty(ref _isNextEnabled, value);
        }

        private bool _isPreviousEnabled;
        public bool IsPreviousEnabled
        {
            get => _isPreviousEnabled;
            set => SetProperty(ref _isPreviousEnabled, value);
        }
        #endregion

        #region 成员变量
        private bool _isLoading;
        /// <summary>
        /// 是否正在加载数据
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // 缓存时间控制，减少不必要的数据刷新
        private DateTime lastLoadTime = DateTime.MinValue;
        private bool IsCacheValid => (DateTime.Now - lastLoadTime).TotalMinutes < 5; // 缓存5分钟有效
        #endregion

        #endregion

        #region 命令
        public DelegateCommand<string> PreviewTextInputCommand { get; private set; }
        public DelegateCommand SearchDeclarationCommand { get; private set; }
        public DelegateCommand AddDeclarationCommand { get; private set; }
        public DelegateCommand<NominationDeclaration> EditDeclarationCommand { get; private set; }
        public DelegateCommand<NominationDeclaration> DeleteDeclarationCommand { get; private set; }
        public DelegateCommand<NominationDeclaration> ApproveCommand { get; private set; }
        public DelegateCommand<NominationDeclaration> RejectCommand { get; private set; }
        public DelegateCommand<NominationDeclaration> PromoteCommand { get; private set; }
        public DelegateCommand ViewLogCommand { get; private set; }
        public DelegateCommand<NominationDeclaration> ViewImageCommand { get; private set; }
        public DelegateCommand ExportDataCommand { get; private set; }
        public DelegateCommand RefreshDataCommand { get; private set; } // 添加刷新命令
        public DelegateCommand PreviousPageCommand { get; private set; }
        public DelegateCommand NextPageCommand { get; private set; }
        public DelegateCommand JumpPageCommand { get; private set; }
        public DelegateCommand PageSizeChangedCommand { get; private set; }
        public DelegateCommand ClearSelectionCommand { get; private set; }
        #endregion

        #region 初始化PageSizeOptions
        private void InitPageSizeOptions()
        {
            PageSizeOptions = new List<int> { 8, 5, 3, 2 };
        }
        #endregion

        #region 启用按钮
        private void EnableButtons()
        {
            IsSearchEnabled = true;
        }
        #endregion

        #region 颜色变换
        private async Task ColorChangeAsync()
        {
            await Task.Run(() =>
            {
                byte red = (byte)((colorOffset + 100) % 128 + 128);
                byte green = (byte)((colorOffset + 150) % 128 + 128);
                byte blue = (byte)((colorOffset + 200) % 128 + 128);
                App.Current.Dispatcher.Invoke(() =>
                {
                    SearchBackground = new SolidColorBrush(MediaColor.FromRgb(red, green, blue));
                });
                colorOffset += 5;
            });
        }
        #endregion

        #region 加载申报数据
        private async void LoadDeclarationsAsync()
        {
            // 如果缓存有效，且数据已加载，则直接使用缓存数据
            if (IsCacheValid && Declarations != null && Declarations.Count > 0)
            {
                UpdateListViewData();
                return;
            }

            try
            {
                // 记录当前选中项的ID，以便在数据加载后恢复选中状态
                int? selectedDeclarationId = SelectedDeclaration?.DeclarationId;

                // 显示加载状态
                IsLoading = true;

                await Task.Run(async () =>
                {
                    using (var context = new DataBaseContext())
                    {
                        // 优化查询，加载必要的关联数据
                        var query = context.NominationDeclarations
                            .AsNoTracking()
                            .Where(d => d.Status != 4) // 明确排除已取消的记录
                            .Include(n => n.Award) // 加载奖项
                            .Include(n => n.Department) // 加载部门
                            .Include(n => n.NominatedEmployee)
                            .Include(n => n.NominatedAdmin)
                            .Include(n => n.DeclarerEmployee)
                            .Include(n => n.DeclarerAdmin)
                            .Include(n => n.DeclarerSupAdmin)
                            .Include(n => n.ReviewerEmployee)
                            .Include(n => n.ReviewerAdmin)
                            .Include(n => n.ReviewerSupAdmin); // 加载审核超级管理员

                        // 直接使用ToListAsync获取处理后的记录
                        var activeDeclarations = await query.ToListAsync();

                        // 在UI线程上更新数据集合
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Declarations = new ObservableCollection<NominationDeclaration>(activeDeclarations);
                            TempViewDeclarations = new ObservableCollection<NominationDeclaration>(activeDeclarations);

                            TotalRecords = TempViewDeclarations.Count;
                            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);

                            // 更新缓存时间
                            lastLoadTime = DateTime.Now;

                            UpdateListViewData();

                            // 恢复选中状态
                            if (selectedDeclarationId.HasValue)
                            {
                                SelectedDeclaration = ListViewDeclarations.FirstOrDefault(d => d.DeclarationId == selectedDeclarationId);

                                // 如果在当前页找不到，则尝试在整个数据集中查找
                                if (SelectedDeclaration == null)
                                {
                                    var declarationInAllData = Declarations.FirstOrDefault(d => d.DeclarationId == selectedDeclarationId);
                                    if (declarationInAllData != null)
                                    {
                                        // 计算该项应该在哪一页
                                        var listIndex = Declarations.IndexOf(declarationInAllData);
                                        if (listIndex >= 0)
                                        {
                                            int targetPage = (listIndex / PageSize) + 1;
                                            // 只有在页码有效时才进行切换
                                            if (targetPage > 0 && targetPage <= MaxPage && targetPage != CurrentPage)
                                            {
                                                CurrentPage = targetPage;
                                                UpdateListViewData();
                                                // 在新页面中再次查找并设置选中状态
                                                SelectedDeclaration = ListViewDeclarations.FirstOrDefault(d => d.DeclarationId == selectedDeclarationId);
                                            }
                                        }
                                    }
                                }
                            }

                            // 更新调试信息，确保加载的是正确类型的数据
                            System.Diagnostics.Debug.WriteLine($"加载完成，申报数据条数：{Declarations.Count}，类型: NominationDeclaration");

                            // 异步加载完整数据，包括关联实体的详细信息
                            LoadCompleteDataAsync();
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"加载申报数据失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"加载申报数据异常: {ex}");

                // 初始化为空集合，防止UI异常
                Declarations = new ObservableCollection<NominationDeclaration>();
                TempViewDeclarations = new ObservableCollection<NominationDeclaration>();
                ListViewDeclarations = new ObservableCollection<NominationDeclaration>();
                TotalRecords = 0;
                MaxPage = 1;
            }
            finally
            {
                // 隐藏加载状态
                IsLoading = false;
            }
        }

        // 异步加载完整数据
        private async void LoadCompleteDataAsync()
        {
            try
            {
                await Task.Run(async () =>
                {
                    using (var context = new DataBaseContext())
                    {
                        // 获取所有ID
                        var ids = Declarations.Select(d => d.DeclarationId).ToList();

                        // 分批加载详细数据，每次50条
                        const int batchSize = 50;
                        for (int i = 0; i < ids.Count; i += batchSize)
                        {
                            var batchIds = ids.Skip(i).Take(batchSize).ToList();
                            var detailedData = await context.NominationDeclarations
                                .AsNoTracking()
                                .Where(n => batchIds.Contains(n.DeclarationId))
                                .Include(n => n.Award)
                                .Include(n => n.NominatedEmployee)
                                .Include(n => n.NominatedAdmin)
                                .Include(n => n.Department)
                                .Include(n => n.DeclarerEmployee)
                                .Include(n => n.DeclarerAdmin)
                                .Include(n => n.DeclarerSupAdmin)
                                .Include(n => n.ReviewerEmployee)
                                .Include(n => n.ReviewerAdmin)
                                .Include(n => n.ReviewerSupAdmin)
                                .ToListAsync();

                            // 更新现有集合中的数据
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                foreach (var detail in detailedData)
                                {
                                    var existing = Declarations.FirstOrDefault(d => d.DeclarationId == detail.DeclarationId);
                                    if (existing != null)
                                    {
                                        // 更新详细信息
                                        UpdateDeclarationDetails(existing, detail);
                                    }
                                }
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载详细数据异常: {ex}");
            }
        }

        // 更新申报详细信息
        private void UpdateDeclarationDetails(NominationDeclaration target, NominationDeclaration source)
        {
            // 更新导航属性
            target.NominatedEmployee = source.NominatedEmployee;
            target.NominatedAdmin = source.NominatedAdmin;
            target.DeclarerEmployee = source.DeclarerEmployee;
            target.DeclarerAdmin = source.DeclarerAdmin;
            target.DeclarerSupAdmin = source.DeclarerSupAdmin;
            target.ReviewerEmployee = source.ReviewerEmployee;
            target.ReviewerAdmin = source.ReviewerAdmin;
            target.ReviewerSupAdmin = source.ReviewerSupAdmin;

            // 确保状态正确同步
            target.Status = source.Status;
            target.ReviewTime = source.ReviewTime;

            // 手动触发属性变更通知，确保UI更新
            if (target is INotifyPropertyChanged notifyTarget)
            {
                // 获取属性变更方法
                var method = notifyTarget.GetType().GetMethod("OnPropertyChanged",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);

                if (method != null)
                {
                    // 手动触发关键属性的变更通知
                    method.Invoke(notifyTarget, new object[] { "NominatedName" });
                    method.Invoke(notifyTarget, new object[] { "DeclarerName" });
                    method.Invoke(notifyTarget, new object[] { "ReviewerName" });
                    method.Invoke(notifyTarget, new object[] { "StatusText" });
                }
            }
        }
        #endregion

        #region 搜索申报
        private async void OnSearchDeclaration()
        {
            try
            {
                if (Declarations == null) return;

                // 显示加载状态
                IsLoading = true;

                // 使用Task.Run在后台线程执行筛选操作
                await Task.Run(() =>
                {
                    List<NominationDeclaration> filteredByStatus;

                    // 首先根据状态筛选
                    if (SelectedStatus.Key == -1)
                        filteredByStatus = Declarations.ToList();
                    else
                        filteredByStatus = Declarations.Where(d => d.Status == SelectedStatus.Key).ToList();

                    // 再根据关键词筛选
                    if (!string.IsNullOrWhiteSpace(SearchKeyword))
                    {
                        filteredByStatus = filteredByStatus.Where(d =>
                            (d.Award?.AwardName?.Contains(SearchKeyword) ?? false) ||
                            (d.NominatedEmployee?.EmployeeName?.Contains(SearchKeyword) ?? false) ||
                            (d.NominatedAdmin?.AdminName?.Contains(SearchKeyword) ?? false) ||
                            (d.Department?.DepartmentName?.Contains(SearchKeyword) ?? false) ||
                            (d.Introduction?.Contains(SearchKeyword) ?? false) ||
                            (d.DeclarationReason?.Contains(SearchKeyword) ?? false) ||
                            (d.DeclarerEmployee?.EmployeeName?.Contains(SearchKeyword) ?? false) ||
                            (d.DeclarerAdmin?.AdminName?.Contains(SearchKeyword) ?? false) ||
                            (d.ReviewerEmployee?.EmployeeName?.Contains(SearchKeyword) ?? false) ||
                            (d.ReviewerAdmin?.AdminName?.Contains(SearchKeyword) ?? false) ||
                            (d.ReviewerSupAdmin?.SupAdminName?.Contains(SearchKeyword) ?? false)
                        ).ToList();
                    }

                    // 在UI线程上更新集合
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        TempViewDeclarations = new ObservableCollection<NominationDeclaration>(filteredByStatus);
                        CurrentPage = 1;
                        UpdateListViewData();

                        // 在完成搜索后显示结果数量
                        if (filteredByStatus.Count == 0)
                        {
                            Growl.InfoGlobal($"没有找到符合条件的记录");
                        }
                        else
                        {
                            Growl.InfoGlobal($"找到 {filteredByStatus.Count} 条符合条件的记录");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"搜索申报失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"搜索异常详情: {ex}");
            }
            finally
            {
                // 隐藏加载状态
                IsLoading = false;
            }
        }
        #endregion

        #region 新增申报
        private void OnAddDeclaration()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("开始执行添加申报操作");

                // 检查用户权限
                if (!IsAddEnabled)
                {
                    Growl.WarningGlobal("您没有权限进行添加申报操作");
                    return;
                }

                // 验证是否有可用奖项
                using (var context = new DataBaseContext())
                {
                    if (!context.Awards.Any())
                    {
                        Growl.WarningGlobal("没有可用的奖项，请先在奖项设置中添加奖项！");
                        return;
                    }
                }

                // 检查区域是否已注册
                if (!regionManager.Regions.ContainsRegionWithName("NominationDeclarationEditRegion"))
                {
                    System.Diagnostics.Debug.WriteLine("错误：NominationDeclarationEditRegion 区域未注册");
                    Growl.ErrorGlobal("操作失败：申报编辑区域未注册");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("尝试导航到添加申报视图");

                try
                {
                    // 使用短名称进行导航，而不是完全限定名称
                    var viewName = "AddNominationDeclaration";
                    System.Diagnostics.Debug.WriteLine($"将要导航到的视图名称: {viewName}");

                    // 传递当前用户信息
                    var parameters = new NavigationParameters();

                    if (CurrentUser.RoleId == 3) // 员工只能申报自己
                    {
                        parameters.Add("NominatedEmployeeId", CurrentUser.EmployeeId);
                        parameters.Add("OnlySelfNomination", true);
                        System.Diagnostics.Debug.WriteLine($"添加员工ID参数: {CurrentUser.EmployeeId}");
                    }
                    else if (CurrentUser.RoleId == 2) // 管理员也只能申报自己
                    {
                        parameters.Add("NominatedAdminId", CurrentUser.AdminId);
                        parameters.Add("OnlySelfNomination", true);
                        System.Diagnostics.Debug.WriteLine($"添加管理员ID参数: {CurrentUser.AdminId}");
                    }

                    // 在导航前先清除区域内容，确保没有残留的视图
                    regionManager.Regions["NominationDeclarationEditRegion"].RemoveAll();

                    // 导航到视图
                    regionManager.RequestNavigate("NominationDeclarationEditRegion", viewName, parameters);

                    // 输出调试信息
                    System.Diagnostics.Debug.WriteLine("已发送导航请求到添加申报视图");
                    Growl.InfoGlobal("正在打开添加申报界面...");
                }
                catch (Exception navEx)
                {
                    System.Diagnostics.Debug.WriteLine($"导航到添加申报视图失败: {navEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"导航异常详情: {navEx}");
                    Growl.ErrorGlobal($"打开添加界面失败：{navEx.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加申报操作异常: {ex}");
                Growl.ErrorGlobal($"打开添加界面失败：{ex.Message}");
            }
        }
        #endregion

        #region 编辑申报
        private void OnEditDeclaration(NominationDeclaration declaration)
        {
            if (declaration == null)
            {
                Growl.WarningGlobal("请选择要修改的申报");
                return;
            }

            // 只能编辑状态为待审核的申报
            if (declaration.Status != 0)
            {
                Growl.WarningGlobal("只能编辑待审核状态的申报");
                return;
            }

            // 根据当前用户角色判断是否有权限编辑
            bool canEdit = false;
            switch (CurrentUser.RoleId)
            {
                case 1: // 超级管理员可以编辑所有
                    canEdit = true;
                    break;
                case 2: // 管理员可以编辑自己创建的申报（无论是以管理员还是员工身份创建的）
                    canEdit = (declaration.DeclarerAdminId == CurrentUser.AdminId) ||
                             (declaration.DeclarerEmployeeId == CurrentUser.EmployeeId);
                    break;
                case 3: // 普通员工只能编辑自己创建的
                    canEdit = declaration.DeclarerEmployeeId == CurrentUser.EmployeeId;
                    break;
            }

            if (!canEdit)
            {
                Growl.WarningGlobal("您没有权限编辑此申报");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"开始导航到编辑页面，申报ID：{declaration.DeclarationId}");
                var parameters = new NavigationParameters
                {
                    { "Declaration", declaration }
                };

                // 导航前清除区域内容
                regionManager.Regions["NominationDeclarationEditRegion"].RemoveAll();

                // 使用简短名称进行导航
                regionManager.RequestNavigate("NominationDeclarationEditRegion", "EditNominationDeclaration", parameters);

                Growl.InfoGlobal("正在打开编辑界面...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导航到编辑页面失败：{ex.Message}");
                Growl.ErrorGlobal($"打开编辑界面失败：{ex.Message}");
            }
        }
        #endregion

        #region 删除申报
        private async void OnDeleteDeclaration(NominationDeclaration declaration)
        {
            if (declaration == null) return;

            // 验证权限，超级管理员可以删除所有，管理员可以删除自己创建的和普通员工创建的，员工只能删除自己创建的
            bool canDelete = false;
            switch (CurrentUser.RoleId)
            {
                case 1: // 超级管理员
                    canDelete = true;
                    break;
                case 2: // 管理员
                    // 管理员可以删除自己创建的申报（无论是以管理员还是员工身份创建的）以及普通员工创建的申报
                    canDelete = (declaration.DeclarerAdminId == CurrentUser.AdminId) ||
                                (declaration.DeclarerEmployeeId == CurrentUser.EmployeeId) ||
                                (declaration.DeclarerEmployeeId != null);
                    break;
                case 3: // 普通员工
                    canDelete = declaration.DeclarerEmployeeId == CurrentUser.EmployeeId && declaration.Status == 0;
                    break;
            }

            if (!canDelete)
            {
                Growl.WarningGlobal("您没有权限删除此申报");
                return;
            }

            // 确认删除
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                $"确定要删除此申报吗？",
                "删除确认",
                System.Windows.MessageBoxButton.OKCancel);

            if (result != System.Windows.MessageBoxResult.OK)
                return;

            try
            {
                // 显示加载状态
                IsLoading = true;

                // 使用Task.Run在后台线程执行删除操作
                await Task.Run(async () =>
                {
                    using (var context = new DataBaseContext())
                    {
                        // 查询时加载关联实体，以便获取准确的奖项名称和被提名人姓名
                        var entity = await context.NominationDeclarations
                            .Include(n => n.Award)
                            .Include(n => n.NominatedEmployee)
                            .Include(n => n.NominatedAdmin)
                            .FirstOrDefaultAsync(n => n.DeclarationId == declaration.DeclarationId);

                        if (entity != null)
                        {
                            // 删除申报时保留日志记录
                            // 将申报标记为已取消状态，而不是物理删除
                            entity.Status = 4; // 设置为取消状态

                            // 添加删除操作日志
                            var log = new NominationLog
                            {
                                DeclarationId = entity.DeclarationId,
                                OperationType = 6, // 删除操作
                                OperationTime = DateTime.Now,
                                Content = $"删除操作：{entity.Award?.AwardName ?? "未知奖项"}-{entity.NominatedName}的申报"
                            };

                            // 根据当前用户角色设置操作者ID
                            switch (CurrentUser.RoleId)
                            {
                                case 1: // 超级管理员
                                    log.OperatorSupAdminId = CurrentUser.AdminId;
                                    log.OperatorAdminId = null;
                                    log.OperatorEmployeeId = null;
                                    break;
                                case 2: // 管理员
                                    log.OperatorAdminId = CurrentUser.AdminId;
                                    log.OperatorSupAdminId = null;
                                    log.OperatorEmployeeId = null;
                                    break;
                                case 3: // 普通员工
                                    log.OperatorEmployeeId = CurrentUser.EmployeeId;
                                    log.OperatorAdminId = null;
                                    log.OperatorSupAdminId = null;
                                    break;
                            }

                            context.NominationLogs.Add(log);
                            await context.SaveChangesAsync();

                            // 在UI线程上更新界面
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                Growl.SuccessGlobal("申报删除成功");

                                // 从原始数据集合中移除
                                var itemToRemove = Declarations.FirstOrDefault(d => d.DeclarationId == declaration.DeclarationId);
                                if (itemToRemove != null)
                                {
                                    Declarations.Remove(itemToRemove);
                                }

                                // 从过滤后的数据集合中移除
                                var tempItem = TempViewDeclarations.FirstOrDefault(d => d.DeclarationId == declaration.DeclarationId);
                                if (tempItem != null)
                                {
                                    TempViewDeclarations.Remove(tempItem);
                                }

                                // 从当前显示的集合中移除
                                var listItem = ListViewDeclarations.FirstOrDefault(d => d.DeclarationId == declaration.DeclarationId);
                                if (listItem != null)
                                {
                                    ListViewDeclarations.Remove(listItem);
                                }

                                // 更新分页信息
                                TotalRecords = TempViewDeclarations.Count;
                                MaxPage = TotalRecords % PageSize == 0 ?
                                    (TotalRecords / PageSize) :
                                    ((TotalRecords / PageSize) + 1);

                                // 如果当前页没有数据了但还有前一页，则自动跳转到前一页
                                if (ListViewDeclarations.Count == 0 && CurrentPage > 1)
                                {
                                    CurrentPage--;
                                    UpdateListViewData();
                                }
                                else if (MaxPage == 0)
                                {
                                    // 如果没有数据了，重置页码
                                    CurrentPage = 1;
                                    MaxPage = 1;
                                    RefreshPaginationStatus();
                                }
                                else
                                {
                                    // 刷新当前页数据
                                    UpdateListViewData();
                                }

                                // 发布删除事件
                                eventAggregator.GetEvent<NominationDeclarationDeleteEvent>().Publish(declaration.DeclarationId);
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"删除申报失败：{ex.Message}");
                System.Diagnostics.Debug.WriteLine($"删除异常详情: {ex}");

                // 确保在出错时也会刷新数据
                LoadDeclarationsAsync();
            }
            finally
            {
                // 隐藏加载状态
                IsLoading = false;
            }
        }
        #endregion

        #region 审核申报
        private void OnApproveDeclaration(NominationDeclaration declaration)
        {
            if (declaration == null) return;

            // 验证状态
            if (declaration.Status != 0)
            {
                Growl.WarningGlobal("只能审核待审核状态的申报");
                return;
            }

            // 验证权限
            if (CurrentUser.RoleId > 2)
            {
                Growl.WarningGlobal("您没有权限进行审核操作");
                return;
            }

            // 管理员不能审核自己创建的申报
            if (CurrentUser.RoleId == 2 && declaration.DeclarerAdminId == CurrentUser.AdminId)
            {
                Growl.WarningGlobal("您不能审核自己创建的申报");
                return;
            }

            // 管理员不能审核任何角色为管理员的提名申报
            if (CurrentUser.RoleId == 2 && declaration.DeclarerAdminId != null)
            {
                Growl.WarningGlobal("管理员的提名申报只能由超级管理员审核");
                return;
            }

            // 弹出审核确认对话框
            var dialog = new WindowNS
            {
                Title = "审核确认",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 更美观的标题文本块
            var titleTextBlock = new System.Windows.Controls.TextBlock
            {
                Text = "确认通过此申报吗？",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(0, 100, 0))
            };

            // 审核意见文本块，居中显示并去掉外边框
            var commentTextBlock = new System.Windows.Controls.TextBlock
            {
                Text = "审核意见：",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // 文本框添加边框和样式
            var commentTextBox = new System.Windows.Controls.TextBox
            {
                Height = 100,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(8),
                FontSize = 14,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(180, 220, 180))
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };

            var confirmButton = new Button
            {
                Content = "确认通过",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Style = Application.Current.FindResource("LoginButtonStyle") as Style
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 80,
                Height = 35,
                Style = Application.Current.FindResource("LoginButtonStyle") as Style
            };

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(titleTextBlock, 0);
            Grid.SetRow(commentTextBlock, 1);
            Grid.SetRow(commentTextBox, 2);
            Grid.SetRow(buttonPanel, 3);

            grid.Children.Add(titleTextBlock);
            grid.Children.Add(commentTextBlock);
            grid.Children.Add(commentTextBox);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            // 绑定事件
            confirmButton.Click += async (s, e) =>
            {
                try
                {
                    // 在UI线程获取TextBox的值，避免线程访问错误
                    string reviewComment = commentTextBox.Text;

                    // 显示加载状态
                    IsLoading = true;

                    // 异步执行审核操作
                    await Task.Run(async () =>
                    {
                        using (var context = new DataBaseContext())
                        {
                            var entity = await context.NominationDeclarations.FindAsync(declaration.DeclarationId);
                            if (entity != null)
                            {
                                entity.Status = 1; // 已通过
                                entity.ReviewComment = reviewComment; // 使用预先获取的文本值
                                entity.ReviewTime = DateTime.Now;

                                // 设置审核人
                                switch (CurrentUser.RoleId)
                                {
                                    case 1: // 超级管理员
                                        entity.ReviewerSupAdminId = CurrentUser.AdminId;
                                        break;
                                    case 2: // 管理员
                                        entity.ReviewerAdminId = CurrentUser.AdminId;
                                        break;
                                }

                                await context.SaveChangesAsync();

                                // 添加审核日志
                                var log = new NominationLog
                                {
                                    DeclarationId = entity.DeclarationId,
                                    OperationType = 2, // 审核通过
                                    OperationTime = DateTime.Now,
                                    Content = $"审核意见：{reviewComment}" // 使用预先获取的文本值
                                };

                                // 添加操作日志
                                switch (CurrentUser.RoleId)
                                {
                                    case 1: // 超级管理员
                                        log.OperatorSupAdminId = CurrentUser.AdminId;
                                        log.OperatorAdminId = null;
                                        log.OperatorEmployeeId = null;
                                        break;
                                    case 2: // 管理员
                                        log.OperatorAdminId = CurrentUser.AdminId;
                                        log.OperatorSupAdminId = null;
                                        log.OperatorEmployeeId = null;
                                        break;
                                }

                                context.NominationLogs.Add(log);
                                await context.SaveChangesAsync();

                                // 在UI线程更新界面
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    // 更新UI中的对象信息
                                    declaration.Status = 1;
                                    declaration.ReviewComment = reviewComment; // 使用预先获取的文本值
                                    declaration.ReviewTime = DateTime.Now;

                                    // 根据当前用户角色设置审核人ID和实体
                                    switch (CurrentUser.RoleId)
                                    {
                                        case 1: // 超级管理员
                                            declaration.ReviewerSupAdminId = CurrentUser.AdminId;
                                            // 从上下文加载最新的审核人信息
                                            declaration.ReviewerSupAdmin = context.SupAdmins.FirstOrDefault(a => a.SupAdminId == CurrentUser.AdminId);
                                            // 确保其他字段为空，防止冲突
                                            declaration.ReviewerAdmin = null;
                                            declaration.ReviewerAdminId = null;
                                            declaration.ReviewerEmployee = null;
                                            declaration.ReviewerEmployeeId = null;
                                            break;
                                        case 2: // 管理员
                                            declaration.ReviewerAdminId = CurrentUser.AdminId;
                                            // 从上下文加载最新的审核人信息
                                            declaration.ReviewerAdmin = context.Admins.FirstOrDefault(a => a.AdminId == CurrentUser.AdminId);
                                            // 确保其他字段为空，防止冲突
                                            declaration.ReviewerSupAdmin = null;
                                            declaration.ReviewerSupAdminId = null;
                                            declaration.ReviewerEmployee = null;
                                            declaration.ReviewerEmployeeId = null;
                                            break;
                                    }

                                    // 触发属性变更通知
                                    if (declaration is INotifyPropertyChanged notifyObj)
                                    {
                                        var method = notifyObj.GetType().GetMethod("OnPropertyChanged",
                                            System.Reflection.BindingFlags.Instance |
                                            System.Reflection.BindingFlags.NonPublic |
                                            System.Reflection.BindingFlags.Public);

                                        if (method != null)
                                        {
                                            // 通知关键属性已变更
                                            method.Invoke(notifyObj, new object[] { "Status" });
                                            method.Invoke(notifyObj, new object[] { "StatusText" });
                                            method.Invoke(notifyObj, new object[] { "ReviewComment" });
                                            method.Invoke(notifyObj, new object[] { "ReviewTime" });
                                            method.Invoke(notifyObj, new object[] { "ReviewerName" });
                                        }
                                    }

                                    // 发布申报审核通过事件
                                    eventAggregator.GetEvent<NominationDeclarationApproveEvent>().Publish(declaration);

                                    // 关闭对话框
                                    dialog.Close();

                                    // 显示成功提示
                                    Growl.SuccessGlobal("申报已通过审核");
                                });
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Growl.ErrorGlobal($"审核操作失败：{ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"审核异常详情: {ex}");
                }
                finally
                {
                    IsLoading = false;
                }
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.Close();
            };

            dialog.ShowDialog();
        }

        private void OnDeclarationRejected(NominationDeclaration declaration)
        {
            // 处理申报审核拒绝事件
            if (declaration == null) return;

            // 在UI线程上更新
            App.Current.Dispatcher.Invoke(() =>
            {
                // 更新原始数据集合
                var originalItem = Declarations.FirstOrDefault(d => d.DeclarationId == declaration.DeclarationId);
                if (originalItem != null)
                {
                    originalItem.Status = declaration.Status;
                    originalItem.ReviewComment = declaration.ReviewComment;
                    originalItem.ReviewTime = declaration.ReviewTime;
                    originalItem.ReviewerSupAdminId = declaration.ReviewerSupAdminId;
                    originalItem.ReviewerAdminId = declaration.ReviewerAdminId;
                    originalItem.ReviewerEmployeeId = declaration.ReviewerEmployeeId;
                    originalItem.ReviewerSupAdmin = declaration.ReviewerSupAdmin;
                    originalItem.ReviewerAdmin = declaration.ReviewerAdmin;
                    originalItem.ReviewerEmployee = declaration.ReviewerEmployee;

                    // 触发属性变更通知
                    if (originalItem is INotifyPropertyChanged notifyObj)
                    {
                        var method = notifyObj.GetType().GetMethod("OnPropertyChanged",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Public);

                        if (method != null)
                        {
                            // 通知关键属性已变更
                            method.Invoke(notifyObj, new object[] { "Status" });
                            method.Invoke(notifyObj, new object[] { "StatusText" });
                            method.Invoke(notifyObj, new object[] { "ReviewComment" });
                            method.Invoke(notifyObj, new object[] { "ReviewTime" });
                            method.Invoke(notifyObj, new object[] { "ReviewerName" });
                        }
                    }
                }

                // 处理临时集合和视图更新
                if (SelectedStatus.Key != -1 && declaration.Status != SelectedStatus.Key)
                {
                    // 如果不符合当前筛选条件，从临时集合中移除
                    var tempItem = TempViewDeclarations.FirstOrDefault(d => d.DeclarationId == declaration.DeclarationId);
                    if (tempItem != null)
                    {
                        TempViewDeclarations.Remove(tempItem);
                        // 更新分页信息
                        TotalRecords = TempViewDeclarations.Count;
                        MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
                        UpdateListViewData();
                    }
                }
                else
                {
                    // 符合筛选条件，更新视图
                    UpdateListViewData();
                }
            });
        }

        // 添加新的拒绝申报方法，用于处理拒绝命令
        private void OnRejectDeclaration(NominationDeclaration declaration)
        {
            if (declaration == null) return;

            // 验证状态
            if (declaration.Status != 0)
            {
                Growl.WarningGlobal("只能审核待审核状态的申报");
                return;
            }

            // 验证权限
            if (CurrentUser.RoleId > 2)
            {
                Growl.WarningGlobal("您没有权限进行审核操作");
                return;
            }

            // 管理员不能审核自己创建的申报
            if (CurrentUser.RoleId == 2 && declaration.DeclarerAdminId == CurrentUser.AdminId)
            {
                Growl.WarningGlobal("您不能审核自己创建的申报");
                return;
            }

            // 管理员不能审核任何角色为管理员的提名申报
            if (CurrentUser.RoleId == 2 && declaration.DeclarerAdminId != null)
            {
                Growl.WarningGlobal("管理员的提名申报只能由超级管理员审核");
                return;
            }

            // 弹出审核确认对话框
            var dialog = new WindowNS
            {
                Title = "审核确认",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 更美观的标题文本块
            var titleTextBlock = new System.Windows.Controls.TextBlock
            {
                Text = "确认拒绝此申报吗？",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(180, 0, 0))
            };

            // 审核意见文本块，居中显示并去掉外边框
            var commentTextBlock = new System.Windows.Controls.TextBlock
            {
                Text = "拒绝理由：",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // 文本框添加边框和样式
            var commentTextBox = new System.Windows.Controls.TextBox
            {
                Height = 100,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(8),
                FontSize = 14,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(220, 180, 180))
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };

            var confirmButton = new Button
            {
                Content = "确认拒绝",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Style = Application.Current.FindResource("LoginButtonStyle") as Style
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 80,
                Height = 35,
                Style = Application.Current.FindResource("LoginButtonStyle") as Style
            };

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(titleTextBlock, 0);
            Grid.SetRow(commentTextBlock, 1);
            Grid.SetRow(commentTextBox, 2);
            Grid.SetRow(buttonPanel, 3);

            grid.Children.Add(titleTextBlock);
            grid.Children.Add(commentTextBlock);
            grid.Children.Add(commentTextBox);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            // 绑定事件
            confirmButton.Click += async (s, e) =>
            {
                try
                {
                    // 在UI线程获取TextBox的值，避免线程访问错误
                    string rejectReason = commentTextBox.Text;

                    // 显示加载状态
                    IsLoading = true;

                    // 异步执行审核操作
                    await Task.Run(async () =>
                    {
                        using (var context = new DataBaseContext())
                        {
                            var entity = await context.NominationDeclarations.FindAsync(declaration.DeclarationId);
                            if (entity != null)
                            {
                                entity.Status = 2; // 已拒绝
                                entity.ReviewComment = rejectReason;
                                entity.ReviewTime = DateTime.Now;

                                // 设置审核人
                                switch (CurrentUser.RoleId)
                                {
                                    case 1: // 超级管理员
                                        entity.ReviewerSupAdminId = CurrentUser.AdminId;
                                        break;
                                    case 2: // 管理员
                                        entity.ReviewerAdminId = CurrentUser.AdminId;
                                        break;
                                }

                                await context.SaveChangesAsync();

                                // 添加审核日志
                                var log = new NominationLog
                                {
                                    DeclarationId = entity.DeclarationId,
                                    OperationType = 3, // 审核拒绝
                                    OperationTime = DateTime.Now,
                                    Content = $"拒绝理由：{rejectReason}"
                                };

                                // 添加操作日志
                                switch (CurrentUser.RoleId)
                                {
                                    case 1: // 超级管理员
                                        log.OperatorSupAdminId = CurrentUser.AdminId;
                                        log.OperatorAdminId = null;
                                        log.OperatorEmployeeId = null;
                                        break;
                                    case 2: // 管理员
                                        log.OperatorAdminId = CurrentUser.AdminId;
                                        log.OperatorSupAdminId = null;
                                        log.OperatorEmployeeId = null;
                                        break;
                                }

                                context.NominationLogs.Add(log);
                                await context.SaveChangesAsync();

                                // 在UI线程更新界面
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    // 更新UI中的对象信息
                                    declaration.Status = 2;
                                    declaration.ReviewComment = commentTextBox.Text;
                                    declaration.ReviewTime = DateTime.Now;

                                    // 根据当前用户角色设置审核人ID和实体
                                    switch (CurrentUser.RoleId)
                                    {
                                        case 1: // 超级管理员
                                            declaration.ReviewerSupAdminId = CurrentUser.AdminId;
                                            // 从上下文加载最新的审核人信息
                                            declaration.ReviewerSupAdmin = context.SupAdmins.FirstOrDefault(a => a.SupAdminId == CurrentUser.AdminId);
                                            // 确保其他字段为空，防止冲突
                                            declaration.ReviewerAdmin = null;
                                            declaration.ReviewerAdminId = null;
                                            declaration.ReviewerEmployee = null;
                                            declaration.ReviewerEmployeeId = null;
                                            break;
                                        case 2: // 管理员
                                            declaration.ReviewerAdminId = CurrentUser.AdminId;
                                            // 从上下文加载最新的审核人信息
                                            declaration.ReviewerAdmin = context.Admins.FirstOrDefault(a => a.AdminId == CurrentUser.AdminId);
                                            // 确保其他字段为空，防止冲突
                                            declaration.ReviewerSupAdmin = null;
                                            declaration.ReviewerSupAdminId = null;
                                            declaration.ReviewerEmployee = null;
                                            declaration.ReviewerEmployeeId = null;
                                            break;
                                    }

                                    // 触发属性变更通知
                                    if (declaration is INotifyPropertyChanged notifyObj)
                                    {
                                        var method = notifyObj.GetType().GetMethod("OnPropertyChanged",
                                            System.Reflection.BindingFlags.Instance |
                                            System.Reflection.BindingFlags.NonPublic |
                                            System.Reflection.BindingFlags.Public);

                                        if (method != null)
                                        {
                                            // 通知关键属性已变更
                                            method.Invoke(notifyObj, new object[] { "Status" });
                                            method.Invoke(notifyObj, new object[] { "StatusText" });
                                            method.Invoke(notifyObj, new object[] { "ReviewComment" });
                                            method.Invoke(notifyObj, new object[] { "ReviewTime" });
                                            method.Invoke(notifyObj, new object[] { "ReviewerName" });
                                        }
                                    }

                                    // 发布申报审核拒绝事件
                                    eventAggregator.GetEvent<NominationDeclarationRejectEvent>().Publish(declaration);

                                    // 关闭对话框
                                    dialog.Close();

                                    // 显示成功提示
                                    Growl.SuccessGlobal("申报已拒绝");
                                });
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Growl.ErrorGlobal($"拒绝操作失败：{ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"拒绝异常详情: {ex}");
                }
                finally
                {
                    IsLoading = false;
                }
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.Close();
            };

            dialog.ShowDialog();
        }
        #endregion

        #region 转为提名
        private void OnPromoteDeclaration(NominationDeclaration declaration)
        {
            if (declaration == null) return;

            // 验证状态 - 允许待审核和已通过的申报转为提名
            if (declaration.Status != 0 && declaration.Status != 1)
            {
                Growl.WarningGlobal("只能将待审核或已通过状态的申报转为提名");
                return;
            }

            // 验证权限
            if (CurrentUser.RoleId > 2)
            {
                Growl.WarningGlobal("您没有权限进行审核操作");
                return;
            }

            // 管理员不能审核自己创建的申报
            if (CurrentUser.RoleId == 2 && declaration.DeclarerAdminId == CurrentUser.AdminId)
            {
                Growl.WarningGlobal("您不能审核自己创建的申报");
                return;
            }

            // 管理员不能审核任何角色为管理员的提名申报
            if (CurrentUser.RoleId == 2 && declaration.DeclarerAdminId != null)
            {
                Growl.WarningGlobal("管理员的提名申报只能由超级管理员审核");
                return;
            }

            // 弹出审核确认对话框
            var dialog = new WindowNS
            {
                Title = "审核确认",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 更美观的标题文本块
            var titleTextBlock = new System.Windows.Controls.TextBlock
            {
                Text = "确认转为提名吗？",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(0, 100, 0))
            };

            // 审核意见文本块，居中显示并去掉外边框
            var commentTextBlock = new System.Windows.Controls.TextBlock
            {
                Text = "审核意见：",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // 文本框添加边框和样式
            var commentTextBox = new System.Windows.Controls.TextBox
            {
                Height = 100,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(8),
                FontSize = 14,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(180, 220, 180))
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };

            var confirmButton = new Button
            {
                Content = "确认转为提名",
                Width = 120,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Style = Application.Current.FindResource("LoginButtonStyle") as Style
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 80,
                Height = 35,
                Style = Application.Current.FindResource("LoginButtonStyle") as Style
            };

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(titleTextBlock, 0);
            Grid.SetRow(commentTextBlock, 1);
            Grid.SetRow(commentTextBox, 2);
            Grid.SetRow(buttonPanel, 3);

            grid.Children.Add(titleTextBlock);
            grid.Children.Add(commentTextBlock);
            grid.Children.Add(commentTextBox);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            // 绑定事件
            confirmButton.Click += async (s, e) =>
            {
                try
                {
                    // 在UI线程获取TextBox的值，避免线程访问错误
                    string promoteComment = commentTextBox.Text;

                    // 显示加载状态
                    IsLoading = true;

                    // 异步执行审核操作
                    await Task.Run(async () =>
                    {
                        using (var context = new DataBaseContext())
                        {
                            // 获取申报实体
                            var entity = await context.NominationDeclarations.FindAsync(declaration.DeclarationId);
                            if (entity != null)
                            {
                                // 只有待审核状态才需要先进行通过处理
                                if (entity.Status == 0)
                                {
                                    entity.Status = 1; // 已通过
                                    entity.ReviewComment = promoteComment;
                                    entity.ReviewTime = DateTime.Now;
                                }

                                // 设置审核人（如果是待审核状态或审核人未设置）
                                if (entity.Status == 0 || (entity.ReviewerSupAdminId == null && entity.ReviewerAdminId == null))
                                {
                                    switch (CurrentUser.RoleId)
                                    {
                                        case 1: // 超级管理员
                                            entity.ReviewerSupAdminId = CurrentUser.AdminId;
                                            break;
                                        case 2: // 管理员
                                            entity.ReviewerAdminId = CurrentUser.AdminId;
                                            break;
                                    }
                                }

                                await context.SaveChangesAsync();

                                // 添加审核日志
                                var log = new NominationLog
                                {
                                    DeclarationId = entity.DeclarationId,
                                    OperationType = 4, // 转为提名
                                    OperationTime = DateTime.Now,
                                    Content = $"审核意见：{promoteComment}"
                                };

                                // 添加操作日志
                                switch (CurrentUser.RoleId)
                                {
                                    case 1: // 超级管理员
                                        log.OperatorSupAdminId = CurrentUser.AdminId;
                                        log.OperatorAdminId = null;
                                        log.OperatorEmployeeId = null;
                                        break;
                                    case 2: // 管理员
                                        log.OperatorAdminId = CurrentUser.AdminId;
                                        log.OperatorSupAdminId = null;
                                        log.OperatorEmployeeId = null;
                                        break;
                                }

                                context.NominationLogs.Add(log);
                                await context.SaveChangesAsync();

                                // 调用转为提名的实际实现
                                try
                                {
                                    // 尝试使用EF Core创建提名
                                    PromoteWithEFCore(entity);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"使用EF Core转为提名失败：{ex.Message}，尝试使用SQL方式");

                                    // 如果EF Core失败，尝试使用SQL
                                    PromoteWithSQL(entity);
                                }

                                // 在UI线程更新界面
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    // 更新UI中的对象信息
                                    declaration.Status = 1;
                                    declaration.ReviewComment = promoteComment;
                                    declaration.ReviewTime = DateTime.Now;

                                    // 根据当前用户角色设置审核人ID和实体
                                    switch (CurrentUser.RoleId)
                                    {
                                        case 1: // 超级管理员
                                            declaration.ReviewerSupAdminId = CurrentUser.AdminId;
                                            // 从上下文加载最新的审核人信息
                                            declaration.ReviewerSupAdmin = context.SupAdmins.FirstOrDefault(a => a.SupAdminId == CurrentUser.AdminId);
                                            // 确保其他字段为空，防止冲突
                                            declaration.ReviewerAdmin = null;
                                            declaration.ReviewerAdminId = null;
                                            declaration.ReviewerEmployee = null;
                                            declaration.ReviewerEmployeeId = null;
                                            break;
                                        case 2: // 管理员
                                            declaration.ReviewerAdminId = CurrentUser.AdminId;
                                            // 从上下文加载最新的审核人信息
                                            declaration.ReviewerAdmin = context.Admins.FirstOrDefault(a => a.AdminId == CurrentUser.AdminId);
                                            // 确保其他字段为空，防止冲突
                                            declaration.ReviewerSupAdmin = null;
                                            declaration.ReviewerSupAdminId = null;
                                            declaration.ReviewerEmployee = null;
                                            declaration.ReviewerEmployeeId = null;
                                            break;
                                    }

                                    // 触发属性变更通知
                                    if (declaration is INotifyPropertyChanged notifyObj)
                                    {
                                        var method = notifyObj.GetType().GetMethod("OnPropertyChanged",
                                            System.Reflection.BindingFlags.Instance |
                                            System.Reflection.BindingFlags.NonPublic |
                                            System.Reflection.BindingFlags.Public);

                                        if (method != null)
                                        {
                                            // 通知关键属性已变更
                                            method.Invoke(notifyObj, new object[] { "Status" });
                                            method.Invoke(notifyObj, new object[] { "StatusText" });
                                            method.Invoke(notifyObj, new object[] { "ReviewComment" });
                                            method.Invoke(notifyObj, new object[] { "ReviewTime" });
                                            method.Invoke(notifyObj, new object[] { "ReviewerName" });
                                        }
                                    }

                                    // 发布申报转为提名事件
                                    eventAggregator.GetEvent<NominationDeclarationPromoteEvent>().Publish(declaration);

                                    // 发布提名数据变更事件，刷新提名列表
                                    eventAggregator.GetEvent<NominationDataChangedEvent>().Publish();

                                    // 关闭对话框
                                    dialog.Close();

                                    // 显示成功提示
                                    Growl.SuccessGlobal("申报已成功转为提名");
                                });
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Growl.ErrorGlobal($"转为提名操作失败：{ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"转为提名异常详情: {ex}");
                }
                finally
                {
                    IsLoading = false;
                }
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.Close();
            };

            dialog.ShowDialog();
        }

        // 使用EF Core方式转为提名
        private void PromoteWithEFCore(NominationDeclaration declaration)
        {
            using (var context = new DataBaseContext())
            {
                // 使用CreateExecutionStrategy创建一个执行策略
                var strategy = context.Database.CreateExecutionStrategy();

                strategy.Execute(() =>
                {
                    // 检查该奖项是否已有提名，避免重复提名
                    bool hasExistingNomination = false;

                    // 这个检查需要放在Execute内部的事务中
                    using (var checkTransaction = context.Database.BeginTransaction())
                    {
                        hasExistingNomination = context.Nominations
                            .Any(n => n.AwardId == declaration.AwardId &&
                                     ((n.NominatedEmployeeId != null && n.NominatedEmployeeId == declaration.NominatedEmployeeId) ||
                                      (n.NominatedAdminId != null && n.NominatedAdminId == declaration.NominatedAdminId)));

                        checkTransaction.Commit();
                    }

                    if (hasExistingNomination)
                    {
                        throw new InvalidOperationException("该奖项已有同一人的提名记录");
                    }

                    // 创建新的提名记录
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        try
                        {
                            // 清除缓存中的跟踪数据，以避免冲突
                            context.ChangeTracker.Clear();

                            // 确保使用新的无跟踪实例来创建提名记录
                            var newNomination = new Nomination
                            {
                                AwardId = declaration.AwardId,
                                NominatedEmployeeId = declaration.NominatedEmployeeId,
                                NominatedAdminId = declaration.NominatedAdminId,
                                DepartmentId = declaration.DepartmentId,
                                Introduction = declaration.Introduction,
                                NominateReason = declaration.DeclarationReason, // 确保使用正确的属性名
                                NominationTime = DateTime.Now,
                                ProposerEmployeeId = declaration.DeclarerEmployeeId,
                                ProposerAdminId = declaration.DeclarerAdminId,
                                ProposerSupAdminId = declaration.DeclarerSupAdminId,
                                // 设置CoverImage
                                CoverImage = declaration.CoverImage
                            };

                            // 标记为新增，避免EF Core尝试更新
                            context.Entry(newNomination).State = EntityState.Added;

                            // 保存提名记录
                            context.SaveChanges();

                            // 添加操作日志
                            var log = new NominationLog
                            {
                                DeclarationId = declaration.DeclarationId,
                                OperationType = 4, // 转为提名
                                OperationTime = DateTime.Now,
                                Content = $"申报已转为提名，提名ID: {newNomination.NominationId}"
                            };

                            // 设置日志操作人
                            switch (CurrentUser.RoleId)
                            {
                                case 1: // 超级管理员
                                    log.OperatorSupAdminId = CurrentUser.AdminId;
                                    break;
                                case 2: // 管理员
                                    log.OperatorAdminId = CurrentUser.AdminId;
                                    break;
                            }

                            // 标记日志为新增
                            context.Entry(log).State = EntityState.Added;
                            context.NominationLogs.Add(log);

                            // 更新申报记录，标记为已转为提名
                            var declarationEntity = context.NominationDeclarations.Find(declaration.DeclarationId);
                            if (declarationEntity != null)
                            {
                                declarationEntity.IsPromoted = true;
                                declarationEntity.PromotedNominationId = newNomination.NominationId;
                            }

                            context.SaveChanges();
                            transaction.Commit();

                            System.Diagnostics.Debug.WriteLine($"成功转为提名，提名ID: {newNomination.NominationId}");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"转为提名事务失败: {ex.Message}");
                            throw;
                        }
                    }
                });
            }
        }

        // 使用SQL方式转为提名
        private void PromoteWithSQL(NominationDeclaration declaration)
        {
            using (var context = new DataBaseContext())
            {
                // 使用SQL直接插入提名记录
                var sql = @"
                    DECLARE @NominationId INT;
                    
                    BEGIN TRY
                        BEGIN TRANSACTION;
                        
                        -- 检查是否已存在提名
                        IF EXISTS (
                            SELECT 1 FROM Nominations 
                            WHERE AwardId = @AwardId 
                                AND ((NominatedEmployeeId IS NOT NULL AND NominatedEmployeeId = @NominatedEmployeeId)
                                OR (NominatedAdminId IS NOT NULL AND NominatedAdminId = @NominatedAdminId))
                        )
                        BEGIN
                            RAISERROR('该奖项已有同一人的提名记录', 16, 1);
                        END
                        
                        -- 插入提名记录
                        INSERT INTO Nominations (
                            AwardId, NominatedEmployeeId, NominatedAdminId, DepartmentId,
                            Introduction, NominateReason, NominationTime, 
                            ProposerEmployeeId, ProposerAdminId, ProposerSupAdminId,
                            CoverImage
                        )
                        VALUES (
                            @AwardId, @NominatedEmployeeId, @NominatedAdminId, @DepartmentId,
                            @Introduction, @NominateReason, GETDATE(),
                            @ProposerEmployeeId, @ProposerAdminId, @ProposerSupAdminId,
                            @CoverImage
                        );
                        
                        -- 获取新插入的ID
                        SET @NominationId = SCOPE_IDENTITY();
                        
                        -- 更新申报记录，标记为已转为提名
                        UPDATE NominationDeclarations
                        SET IsPromoted = 1, PromotedNominationId = @NominationId
                        WHERE DeclarationId = @DeclarationId;
                        
                        -- 插入日志
                        INSERT INTO NominationLogs (
                            DeclarationId, OperationType, OperationTime, Content,
                            OperatorSupAdminId, OperatorAdminId, OperatorEmployeeId
                        )
                        VALUES (
                            @DeclarationId, 4, GETDATE(), 
                            '申报已转为提名，提名ID: ' + CAST(@NominationId AS NVARCHAR(20)),
                            @OperatorSupAdminId, @OperatorAdminId, @OperatorEmployeeId
                        );
                        
                        COMMIT TRANSACTION;
                        
                        -- 返回新提名ID
                        SELECT @NominationId AS NewNominationId;
                    END TRY
                    BEGIN CATCH
                        IF @@TRANCOUNT > 0
                            ROLLBACK TRANSACTION;
                        
                        -- 重新抛出错误
                        THROW;
                    END CATCH
                ";

                var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
                {
                    new Microsoft.Data.SqlClient.SqlParameter("@AwardId", declaration.AwardId),
                    new Microsoft.Data.SqlClient.SqlParameter("@NominatedEmployeeId", declaration.NominatedEmployeeId ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@NominatedAdminId", declaration.NominatedAdminId ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@DepartmentId", declaration.DepartmentId),
                    new Microsoft.Data.SqlClient.SqlParameter("@Introduction", declaration.Introduction ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@NominateReason", declaration.DeclarationReason ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@DeclarationId", declaration.DeclarationId),
                    new Microsoft.Data.SqlClient.SqlParameter("@ProposerEmployeeId", declaration.DeclarerEmployeeId ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@ProposerAdminId", declaration.DeclarerAdminId ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@ProposerSupAdminId", declaration.DeclarerSupAdminId ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@OperatorSupAdminId", CurrentUser.RoleId == 1 ? CurrentUser.AdminId : (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@OperatorAdminId", CurrentUser.RoleId == 2 ? CurrentUser.AdminId : (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@OperatorEmployeeId", DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@CoverImage", declaration.CoverImage ?? (object)DBNull.Value)
                };

                try
                {
                    // 执行SQL
                    var result = context.Database.ExecuteSqlRaw(sql, parameters.ToArray());
                    System.Diagnostics.Debug.WriteLine($"SQL执行结果: {result}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SQL执行失败: {ex.Message}");
                    throw;
                }
            }
        }
        #endregion

        #region 查看日志
        private void OnViewLog()
        {
            try
            {
                // 创建日志查看窗口
                var logWindow = new NominationDeclarationLogWindow();
                var viewModel = new NominationDeclarationLogViewModel();

                if (SelectedDeclaration == null)
                {
                    // 没有选中申报项，查看所有日志
                    viewModel.NominationDeclarationId = -1; // 设置为-1表示查看所有日志
                    viewModel.IsAllLogs = true;
                    logWindow.Title = "所有申报日志";
                }
                else
                {
                    // 设置查看的申报ID
                    viewModel.NominationDeclarationId = SelectedDeclaration.DeclarationId;

                    // 传递完整的申报项信息
                    viewModel.Award = SelectedDeclaration.Award;
                    viewModel.NominatedName = SelectedDeclaration.NominatedName;
                    viewModel.Department = SelectedDeclaration.Department;
                    viewModel.StatusText = SelectedDeclaration.StatusText;

                    // 设置窗口的标题
                    string nominatedName = SelectedDeclaration.NominatedName ?? "未知";
                    string awardName = SelectedDeclaration.Award?.AwardName ?? "未知奖项";
                    logWindow.Title = $"{awardName} - {nominatedName} 的申报日志";
                }

                // 加载日志
                viewModel.LoadLogsAsync();

                // 设置数据上下文
                logWindow.DataContext = viewModel;

                // 显示窗口
                logWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"查看日志失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"查看日志异常: {ex}");
            }
        }
        #endregion

        #region 查看图片
        private void OnViewImage(NominationDeclaration declaration)
        {
            if (declaration == null)
            {
                Growl.WarningGlobal("请选择要查看图片的申报");
                return;
            }

            try
            {
                // 检查是否有图片
                if (declaration.CoverImage == null || declaration.CoverImage.Length == 0)
                {
                    Growl.InfoGlobal("当前申报没有上传图片");
                    return;
                }

                // 创建图片查看窗口
                var imageWindow = new WindowNS
                {
                    Title = "查看申报图片",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.CanResize
                };

                // 创建容器
                var grid = new Grid();

                // 创建图片控件
                var image = new System.Windows.Controls.Image
                {
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(10)
                };

                // 转换字节数组为图片
                using (var ms = new MemoryStream(declaration.CoverImage))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze(); // UI线程访问
                    image.Source = bitmap;
                }

                // 添加关闭按钮
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 10, 10)
                };

                var closeButton = new Button
                {
                    Content = "关闭",
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                closeButton.Click += (s, e) => imageWindow.Close();
                buttonPanel.Children.Add(closeButton);

                // 添加控件到窗口
                grid.Children.Add(image);
                grid.Children.Add(buttonPanel);
                imageWindow.Content = grid;

                // 显示窗口
                imageWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"查看图片失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"查看图片异常: {ex}");
            }
        }
        #endregion

        #region 导出数据
        private void OnExportData()
        {
            try
            {
                if (TempViewDeclarations == null || TempViewDeclarations.Count == 0)
                {
                    Growl.WarningGlobal("没有数据可导出");
                    return;
                }

                // 创建保存文件对话框
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV文件 (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    Title = "导出申报数据",
                    FileName = $"申报数据_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;

                    // 导出CSV
                    ExportToCSV(saveFileDialog.FileName);

                    IsLoading = false;
                    Growl.SuccessGlobal("数据导出成功");
                }
            }
            catch (Exception ex)
            {
                IsLoading = false;
                Growl.ErrorGlobal($"导出数据失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"导出数据异常: {ex}");
            }
        }

        private void ExportToCSV(string fileName)
        {
            // 创建CSV内容
            var sb = new StringBuilder();

            // 添加表头
            sb.AppendLine("申报ID,奖项,被提名人,所属部门,申报理由,申报人,申报时间,状态,审核人,审核时间,审核意见");

            // 添加数据行
            foreach (var d in TempViewDeclarations)
            {
                // 转义字段中的逗号、引号和换行符
                string EscapeCSV(string field)
                {
                    if (string.IsNullOrEmpty(field)) return "";

                    // 如果包含逗号、引号或换行符，则用引号包裹并将内部引号转换为两个引号
                    if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
                    {
                        return "\"" + field.Replace("\"", "\"\"") + "\"";
                    }
                    return field;
                }

                sb.AppendLine(string.Join(",",
                    d.DeclarationId,
                    EscapeCSV(d.Award?.AwardName ?? "未知"),
                    EscapeCSV(d.NominatedName ?? "未知"),
                    EscapeCSV(d.Department?.DepartmentName ?? "未知"),
                    EscapeCSV(d.DeclarationReason ?? ""),
                    EscapeCSV(d.DeclarerName ?? "未知"),
                    EscapeCSV(d.DeclarationTime.ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeCSV(d.StatusText ?? "未知"),
                    EscapeCSV(d.ReviewerName ?? "-"),
                    EscapeCSV(d.ReviewTime.HasValue ? d.ReviewTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-"),
                    EscapeCSV(d.ReviewComment ?? "-")
                ));
            }

            // 写入文件
            File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
        }
        #endregion

        #region 刷新数据
        private async void OnRefreshData()
        {
            try
            {
                // 显示加载状态
                IsLoading = true;

                // 清除缓存标志
                lastLoadTime = DateTime.MinValue;

                // 保存当前选中项ID，以便在刷新后恢复选中状态
                int? selectedId = SelectedDeclaration?.DeclarationId;

                // 重新加载数据
                await Task.Run(() =>
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        LoadDeclarationsAsync();

                        // 显示刷新成功提示
                        Growl.InfoGlobal("数据已刷新");
                    });
                });

                // 恢复选中状态
                if (selectedId.HasValue)
                {
                    SelectedDeclaration = ListViewDeclarations.FirstOrDefault(d => d.DeclarationId == selectedId);
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"刷新数据失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"刷新数据异常: {ex}");
            }
            finally
            {
                // 隐藏加载状态
                IsLoading = false;
            }
        }
        #endregion

        #region 分页相关方法
        private void UpdateListViewData()
        {
            try
            {
                // 将临时集合按页数进行划分，只显示当前页的数据
                int start = (CurrentPage - 1) * PageSize;
                var displayData = TempViewDeclarations
                    .Skip(start)
                    .Take(PageSize)
                    .ToList();

                // 使用ObservableCollection更新UI
                ListViewDeclarations = new ObservableCollection<NominationDeclaration>(displayData);

                // 更新分页状态
                RefreshPaginationStatus();
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"更新列表视图数据失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"更新列表视图数据异常: {ex}");
            }
        }

        private void RefreshPaginationStatus()
        {
            // 计算总页数
            TotalRecords = TempViewDeclarations.Count;
            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
            if (MaxPage == 0) MaxPage = 1; // 至少有一页，即使没有数据

            // 确保当前页在有效范围内
            if (CurrentPage > MaxPage)
            {
                CurrentPage = MaxPage;
            }
            else if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }

            // 更新分页按钮状态
            IsFirstPage = CurrentPage == 1;
            IsLastPage = CurrentPage == MaxPage;
            IsPreviousEnabled = CurrentPage > 1;
            IsNextEnabled = CurrentPage < MaxPage;
        }
        #endregion

        #region 文本输入预览
        private void OnPreviewTextInput(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // 检查是否为数字输入
            bool isNumeric = int.TryParse(text, out _);

            // 保存上一次输入
            LastInput = text;

            // 仅允许在搜索框中输入数字
            if (!isNumeric)
            {
                // 可以在这里添加提示或其他处理
                System.Diagnostics.Debug.WriteLine("输入预览: 非数字输入 - " + text);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("输入预览: 数字输入 - " + text);
            }
        }
        #endregion

        #region 其他辅助方法
        private void SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region 事件处理方法
        private void OnDeclarationAdded()
        {
            // 申报添加事件处理（无参数）
            // 强制刷新数据
            App.Current.Dispatcher.Invoke(() =>
            {
                // 强制清除缓存并重新加载数据
                lastLoadTime = DateTime.MinValue;
                LoadDeclarationsAsync();
            });
        }

        private void OnDeclarationUpdated()
        {
            // 申报更新事件处理（无参数）
            // 强制刷新数据
            App.Current.Dispatcher.Invoke(() =>
            {
                // 强制清除缓存并重新加载数据
                lastLoadTime = DateTime.MinValue;
                LoadDeclarationsAsync();
            });
        }

        private void OnDeclarationDeleted(int declarationId)
        {
            // 处理删除申报事件
            // 在UI线程上更新
            App.Current.Dispatcher.Invoke(() =>
            {
                // 从原始数据集合中移除
                var originalItem = Declarations.FirstOrDefault(d => d.DeclarationId == declarationId);
                if (originalItem != null)
                {
                    Declarations.Remove(originalItem);
                }

                // 从临时集合中移除
                var tempItem = TempViewDeclarations.FirstOrDefault(d => d.DeclarationId == declarationId);
                if (tempItem != null)
                {
                    TempViewDeclarations.Remove(tempItem);
                    // 更新分页信息
                    TotalRecords = TempViewDeclarations.Count;
                    MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
                }

                // 从当前显示集合中移除
                var listItem = ListViewDeclarations?.FirstOrDefault(d => d.DeclarationId == declarationId);
                if (listItem != null)
                {
                    ListViewDeclarations.Remove(listItem);
                }

                // 如果当前页没有数据了但还有前一页，自动跳转到前一页
                if (ListViewDeclarations?.Count == 0 && CurrentPage > 1)
                {
                    CurrentPage--;
                }

                // 更新视图集合
                UpdateListViewData();
            });
        }

        private void OnDeclarationApproved(NominationDeclaration declaration)
        {
            // 处理申报审核通过事件
            if (declaration == null) return;

            // 在UI线程上更新
            App.Current.Dispatcher.Invoke(() =>
            {
                // 更新原始数据集合
                var originalItem = Declarations.FirstOrDefault(d => d.DeclarationId == declaration.DeclarationId);
                if (originalItem != null)
                {
                    originalItem.Status = declaration.Status;
                    originalItem.ReviewComment = declaration.ReviewComment;
                    originalItem.ReviewTime = declaration.ReviewTime;
                    originalItem.ReviewerSupAdminId = declaration.ReviewerSupAdminId;
                    originalItem.ReviewerAdminId = declaration.ReviewerAdminId;
                    originalItem.ReviewerEmployeeId = declaration.ReviewerEmployeeId;
                    originalItem.ReviewerSupAdmin = declaration.ReviewerSupAdmin;
                    originalItem.ReviewerAdmin = declaration.ReviewerAdmin;
                    originalItem.ReviewerEmployee = declaration.ReviewerEmployee;

                    // 触发属性变更通知
                    if (originalItem is INotifyPropertyChanged notifyObj)
                    {
                        var method = notifyObj.GetType().GetMethod("OnPropertyChanged",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Public);

                        if (method != null)
                        {
                            // 通知关键属性已变更
                            method.Invoke(notifyObj, new object[] { "Status" });
                            method.Invoke(notifyObj, new object[] { "StatusText" });
                            method.Invoke(notifyObj, new object[] { "ReviewComment" });
                            method.Invoke(notifyObj, new object[] { "ReviewTime" });
                            method.Invoke(notifyObj, new object[] { "ReviewerName" });
                        }
                    }
                }

                // 处理临时集合和视图更新
                if (SelectedStatus.Key != -1 && declaration.Status != SelectedStatus.Key)
                {
                    // 如果不符合当前筛选条件，从临时集合中移除
                    var tempItem = TempViewDeclarations.FirstOrDefault(d => d.DeclarationId == declaration.DeclarationId);
                    if (tempItem != null)
                    {
                        TempViewDeclarations.Remove(tempItem);
                        // 更新分页信息
                        TotalRecords = TempViewDeclarations.Count;
                        MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
                        UpdateListViewData();
                    }
                }
                else
                {
                    // 符合筛选条件，更新视图
                    UpdateListViewData();
                }
            });
        }

        private void OnDeclarationPromoted(NominationDeclaration declaration)
        {
            // 处理申报转为提名事件
            if (declaration == null) return;

            // 在UI线程上更新
            App.Current.Dispatcher.Invoke(() =>
            {
                // 更新原始数据集合
                var originalItem = Declarations.FirstOrDefault(d => d.DeclarationId == declaration.DeclarationId);
                if (originalItem != null)
                {
                    // 更新状态为已通过
                    originalItem.Status = 1;

                    // 处理临时集合和视图更新
                    if (SelectedStatus.Key != -1 && originalItem.Status != SelectedStatus.Key)
                    {
                        // 如果不符合当前筛选条件，从临时集合中移除
                        var tempItem = TempViewDeclarations.FirstOrDefault(d => d.DeclarationId == declaration.DeclarationId);
                        if (tempItem != null)
                        {
                            TempViewDeclarations.Remove(tempItem);
                            // 更新分页信息
                            TotalRecords = TempViewDeclarations.Count;
                            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
                            UpdateListViewData();
                        }
                    }
                    else
                    {
                        // 符合筛选条件，更新视图
                        UpdateListViewData();
                    }
                }
            });
        }

        private void OnNominationDataChanged()
        {
            // 处理提名数据变更事件，重新加载数据
            App.Current.Dispatcher.Invoke(() =>
            {
                // 强制清除缓存并重新加载数据
                lastLoadTime = DateTime.MinValue;
                LoadDeclarationsAsync();
            });
        }
        #endregion

        #region 分页和选择控制方法
        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdateListViewData();
            }
        }

        private void NextPage()
        {
            if (CurrentPage < MaxPage)
            {
                CurrentPage++;
                UpdateListViewData();
            }
        }

        private void JumpPage()
        {
            int pageNum;
            if (int.TryParse(SearchText, out pageNum))
            {
                if (pageNum >= 1 && pageNum <= MaxPage)
                {
                    CurrentPage = pageNum;
                    UpdateListViewData();
                }
                else
                {
                    Growl.WarningGlobal($"页码范围为 1-{MaxPage}");
                }
            }
            else
            {
                Growl.WarningGlobal("请输入有效的页码");
            }
        }

        private void PageSizeChanged()
        {
            // 更新页大小，重新计算最大页数
            TotalRecords = TempViewDeclarations.Count;
            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);

            // 确保当前页码有效
            if (CurrentPage > MaxPage)
            {
                CurrentPage = MaxPage > 0 ? MaxPage : 1;
            }

            UpdateListViewData();
        }

        private void ClearSelection()
        {
            SelectedDeclaration = null;
        }
        #endregion
    }
}
