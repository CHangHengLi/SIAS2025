using SIASGraduate.Common;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using SIASGraduate.Views.EditMessage.NominationLogViewer;
using SIASGraduate.ViewModels.EditMessage.NominationLogViewer;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO; // 添加文件操作
using System.Runtime.CompilerServices;
using System.Text; // 添加文本编码相关
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MediaColor = System.Windows.Media.Color; // 使用别名避免Color冲突
using SolidColorBrush = System.Windows.Media.SolidColorBrush; // 明确指定SolidColorBrush类型
using WindowNS = System.Windows.Window;
using System.Windows.Media;

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
            RejectCommand = new DelegateCommand<NominationDeclaration>(OnDeclarationRejected);

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
        private void OnSearchDeclaration()
        {
            try
            {
                if (Declarations == null) return;

                // 首先根据状态筛选
                var filteredByStatus = SelectedStatus.Key == -1
                    ? Declarations.ToList()
                    : Declarations.Where(d => d.Status == SelectedStatus.Key).ToList();

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

                TempViewDeclarations = new ObservableCollection<NominationDeclaration>(filteredByStatus);
                CurrentPage = 1;
                UpdateListViewData();
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"搜索申报失败: {ex.Message}");
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
        private void OnDeleteDeclaration(NominationDeclaration declaration)
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
                using (var context = new DataBaseContext())
                {
                    // 查询时加载关联实体，以便获取准确的奖项名称和被提名人姓名
                    var entity = context.NominationDeclarations
                        .Include(n => n.Award)
                        .Include(n => n.NominatedEmployee)
                        .Include(n => n.NominatedAdmin)
                        .FirstOrDefault(n => n.DeclarationId == declaration.DeclarationId);

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
                        context.SaveChanges();

                        Growl.SuccessGlobal("申报删除成功");

                        // 立即从UI集合中移除该项，而不是等待LoadDeclarationsAsync重新加载
                        App.Current.Dispatcher.Invoke(() =>
                        {
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
                        });

                        // 发布删除事件
                        eventAggregator.GetEvent<NominationDeclarationDeleteEvent>().Publish(declaration.DeclarationId);
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"删除申报失败：{ex.Message}");

                // 确保在出错时也会刷新数据
                LoadDeclarationsAsync();
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
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleLabel = new Label
            {
                Content = "确认通过此申报吗？",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var commentLabel = new Label
            {
                Content = "审核意见：",
                Margin = new Thickness(0, 5, 0, 5)
            };

            var commentTextBox = new System.Windows.Controls.TextBox
            {
                Height = 80,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var confirmButton = new Button
            {
                Content = "确认通过",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                Style = Application.Current.FindResource("LoginButtonStyle") as Style
            };

            var cancelButton = new Button
            {
                Content = "取消",
                Width = 80,
                Height = 30,
                Style = Application.Current.FindResource("LoginButtonStyle") as Style
            };

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(titleLabel, 0);
            Grid.SetRow(commentLabel, 1);
            Grid.SetRow(commentTextBox, 2);
            Grid.SetRow(buttonPanel, 3);

            grid.Children.Add(titleLabel);
            grid.Children.Add(commentLabel);
            grid.Children.Add(commentTextBox);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            // 绑定事件
            confirmButton.Click += (s, e) =>
            {
                try
                {
                    using (var context = new DataBaseContext())
                    {
                        var entity = context.NominationDeclarations.Find(declaration.DeclarationId);
                        if (entity != null)
                        {
                            entity.Status = 1; // 已通过
                            entity.ReviewComment = commentTextBox.Text;
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

                            context.SaveChanges();

                            // 添加审核日志
                            var log = new NominationLog
                            {
                                DeclarationId = entity.DeclarationId,
                                OperationType = 2, // 审核通过
                                OperationTime = DateTime.Now,
                                Content = $"审核意见：{commentTextBox.Text}"
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
                            context.SaveChanges();

                            declaration.Status = 1;
                            declaration.ReviewComment = commentTextBox.Text;
                            declaration.ReviewTime = DateTime.Now;

                            // 发布审核通过事件
                            eventAggregator.GetEvent<NominationDeclarationApproveEvent>().Publish(declaration);

                            Growl.SuccessGlobal("审核通过成功");
                            LoadDeclarationsAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Growl.ErrorGlobal($"审核操作失败：{ex.Message}");
                }

                dialog.Close();
            };

            cancelButton.Click += (s, e) => dialog.Close();

            dialog.ShowDialog();
        }

        private async void OnDeclarationRejected(NominationDeclaration declaration)
        {
            if (IsLoading) return;

            try
            {
                // 记录当前选中项
                var selectedId = SelectedDeclaration?.DeclarationId;

                // 开始加载
                IsLoading = true;

                // 完全刷新数据
                await Task.Run(async () => {
                    using (var context = new DataBaseContext())
                    {
                        var allDeclarations = await context.NominationDeclarations
                            .AsNoTracking()
                            .Where(d => d.Status != 4) // 明确排除已取消的记录
                            .Include(d => d.Award)
                            .Include(d => d.Department)
                            .Include(d => d.NominatedEmployee)
                            .Include(d => d.NominatedAdmin)
                            .Include(d => d.DeclarerEmployee)
                            .Include(d => d.DeclarerAdmin)
                            .Include(d => d.DeclarerSupAdmin)
                            .ToListAsync();

                        Application.Current.Dispatcher.Invoke(() => {
                            // 更新申报集合
                            Declarations = new ObservableCollection<NominationDeclaration>(allDeclarations);

                            // 应用当前筛选条件
                            OnSearchDeclaration();

                            // 尝试恢复选中状态
                            if (selectedId.HasValue)
                            {
                                SelectedDeclaration = ListViewDeclarations.FirstOrDefault(d => d.DeclarationId == selectedId);
                            }

                            System.Diagnostics.Debug.WriteLine("申报拒绝事件处理完成，已刷新界面");
                        });
                        }
                });
                }
                catch (Exception ex)
                {
                System.Diagnostics.Debug.WriteLine($"处理申报拒绝事件时出错: {ex.Message}");
                }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region 转为提名
        private void OnPromoteDeclaration(NominationDeclaration declaration)
        {
            if (declaration == null) return;

            // 验证状态
            if (declaration.Status != 1)
            {
                Growl.WarningGlobal("只能将已通过审核的申报转为正式提名");
                return;
            }

            // 验证权限
            if (CurrentUser.RoleId > 2)
            {
                Growl.WarningGlobal("您没有权限进行此操作");
                return;
            }

            // 管理员不能将任何角色为管理员的提名申报转为正式提名
            if (CurrentUser.RoleId == 2 && declaration.DeclarerAdminId != null)
            {
                Growl.WarningGlobal("管理员的提名申报只能由超级管理员转为正式提名");
                return;
            }

            // 确认转为提名
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                "确定要将此申报转为正式提名吗？此操作不可撤销，并将检查此奖项是否已有提名。",
                "转为提名确认",
                System.Windows.MessageBoxButton.OKCancel);

            if (result != System.Windows.MessageBoxResult.OK)
                return;

            try
            {
                using (var context = new DataBaseContext())
                {
                    // 检查该奖项是否已有提名，避重复提名
                    bool hasExistingNomination = context.Nominations
                        .Any(n => n.AwardId == declaration.AwardId &&
                                 ((n.NominatedEmployeeId != null && n.NominatedEmployeeId == declaration.NominatedEmployeeId) ||
                                  (n.NominatedAdminId != null && n.NominatedAdminId == declaration.NominatedAdminId)));

                    if (hasExistingNomination)
                    {
                        Growl.WarningGlobal("该奖项已有提名，不能重复提名");
                        return;
                    }

                    // 创建新提名
                    var nomination = new Nomination
                    {
                        AwardId = declaration.AwardId,
                        NominatedEmployeeId = declaration.NominatedEmployeeId,
                        NominatedAdminId = declaration.NominatedAdminId,
                        DepartmentId = declaration.DepartmentId,
                        Introduction = declaration.Introduction,
                        NominateReason = declaration.DeclarationReason,
                        CoverImage = declaration.CoverImage,
                        NominationTime = DateTime.Now
                    };

                    // 根据当前用户角色设置提议人
                    switch (CurrentUser.RoleId)
                    {
                        case 1: // 超级管理员
                            nomination.ProposerSupAdminId = CurrentUser.AdminId;
                            break;
                        case 2: // 管理员
                            nomination.ProposerAdminId = CurrentUser.AdminId;
                            break;
                    }
                    
                    // 添加额外逻辑：将原申报人信息也保留到提名记录中
                    // 仅当当前没有设置提名人时才进行设置（避免覆盖当前管理员作为提名人的情况）
                    if (nomination.ProposerEmployeeId == null && nomination.ProposerAdminId == null && nomination.ProposerSupAdminId == null)
                    {
                        nomination.ProposerEmployeeId = declaration.DeclarerEmployeeId;
                        nomination.ProposerAdminId = declaration.DeclarerAdminId;
                        nomination.ProposerSupAdminId = declaration.DeclarerSupAdminId;
                    }

                    context.Nominations.Add(nomination);
                    context.SaveChanges();

                    // 更新申报状态为已转为提名
                    var entity = context.NominationDeclarations.Find(declaration.DeclarationId);
                    if (entity != null)
                    {
                        entity.IsPromoted = true;
                        entity.PromotedNominationId = nomination.NominationId;
                        context.SaveChanges();

                        // 添加操作日志
                        var log = new NominationLog
                        {
                            DeclarationId = entity.DeclarationId,
                            OperationType = 4, // 转为提名
                            OperationTime = DateTime.Now,
                            Content = $"已转为提名ID：{nomination.NominationId}"
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
                        context.SaveChanges();

                        declaration.IsPromoted = true;
                        declaration.PromotedNominationId = nomination.NominationId;

                        // 发布转为提名事件
                        eventAggregator.GetEvent<NominationDeclarationPromoteEvent>().Publish(declaration);

                        Growl.SuccessGlobal("已成功转为正式提名");
                        LoadDeclarationsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"转为提名失败：{ex.Message}");
            }
        }
        #endregion

        #region 查看日志
        private async void OnViewLog()
        {
            try
            {
                // 创建日志查看窗口
                var logViewer = new NominationDeclarationLogWindow
                {
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                // 获取日志ViewModel
                if (logViewer.DataContext is NominationDeclarationLogViewModel viewModel)
                {
                    if (SelectedDeclaration != null)
                    {
                        // 设置查看特定申报的日志
                        viewModel.NominationDeclarationId = SelectedDeclaration.DeclarationId;

                        // 设置窗口的关联数据
                        viewModel.Award = SelectedDeclaration.Award;
                        viewModel.NominatedName = GetNomineeName(SelectedDeclaration);
                        viewModel.Department = SelectedDeclaration.Department;
                        viewModel.StatusText = GetStatusText(SelectedDeclaration.Status);

                        // 设置窗口标题
                        logViewer.Title = $"申报记录({SelectedDeclaration.DeclarationId})的日志";
                    }
                    else
                    {
                        // 设置查看所有日志
                        viewModel.NominationDeclarationId = -1;
                        logViewer.Title = "所有申报日志";

                        // 清空相关数据
                        viewModel.Award = null;
                        viewModel.NominatedName = "全部";
                        viewModel.Department = null;
                        viewModel.StatusText = "全部";
                    }
                }

                // 先显示窗口，避免界面卡顿
                logViewer.Show();

                // 窗口显示后再异步加载数据，提高界面响应性
                if (logViewer.DataContext is NominationDeclarationLogViewModel vm)
                {
                    // 使用Task.Run在后台启动加载过程
                    Task.Run(async () =>
                    {
                        await vm.LoadLogsAsync();
                    });
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"查看日志失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"查看日志异常: {ex}");
            }
        }
        #endregion

        #region 查看图片
        private bool _isViewingImage = false;

        private void OnViewImage(NominationDeclaration declaration)
        {
            if (_isViewingImage || declaration == null || declaration.CoverImage == null || declaration.CoverImage.Length == 0)
            {
                Growl.WarningGlobal("没有可查看的图片");
                return;
            }

            try
            {
                _isViewingImage = true;

                // 创建一个新窗口来展示图片
                var imageWindow = new WindowNS
                {
                    Title = $"申报图片 - {declaration.NominatedName} - ID: {declaration.DeclarationId}",
                    Width = 700,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush(MediaColor.FromRgb(240, 240, 240))
                };

                // 创建主布局
                var mainGrid = new Grid();
                var rowDef1 = new RowDefinition();
                rowDef1.Height = System.Windows.GridLength.Auto;
                mainGrid.RowDefinitions.Add(rowDef1);
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                // 创建顶部信息面板
                var infoBorder = new Border
                {
                    Background = new SolidColorBrush(MediaColor.FromRgb(230, 230, 230)),
                    Padding = new Thickness(10),
                    Margin = new Thickness(10, 10, 10, 5),
                    BorderBrush = new SolidColorBrush(MediaColor.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1),
                    Width = 650
                };

                var infoPanel = new StackPanel { Margin = new Thickness(5) };

                // 添加申报信息文本
                infoPanel.Children.Add(new TextBlock
                {
                    Text = $"奖项: {declaration.Award?.AwardName ?? "未知"}",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5),
                    FontSize = 14
                });

                infoPanel.Children.Add(new TextBlock
                {
                    Text = $"申报对象: {declaration.NominatedName}",
                    Margin = new Thickness(0, 0, 0, 5)
                });

                infoPanel.Children.Add(new TextBlock
                {
                    Text = $"部门: {declaration.Department?.DepartmentName ?? "未知"}",
                    Margin = new Thickness(0, 0, 0, 5)
                });

                infoPanel.Children.Add(new TextBlock
                {
                    Text = $"状态: {declaration.StatusText}",
                    Margin = new Thickness(0, 0, 0, 5)
                });

                if (!string.IsNullOrEmpty(declaration.Introduction))
                {
                    infoPanel.Children.Add(new TextBlock
                    {
                        Text = $"简介: {declaration.Introduction}",
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 5)
                    });
                }

                infoBorder.Child = infoPanel;
                Grid.SetRow(infoBorder, 0);
                mainGrid.Children.Add(infoBorder);

                // 创建图片控件
                var image = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(),
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    MaxWidth = 640,
                    MaxHeight = 390
                };

                // 将字节数组转换为图像源
                using (var ms = new System.IO.MemoryStream(declaration.CoverImage))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    image.Source = bitmap;
                }

                // 创建图片容器
                var dockPanel = new DockPanel
                {
                    LastChildFill = true,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var imageBorder = new Border
                {
                    Child = image,
                    BorderBrush = new SolidColorBrush(MediaColor.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(5),
                    Background = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(10),
                    Width = 650,
                    Height = 400,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                dockPanel.Children.Add(imageBorder);
                Grid.SetRow(dockPanel, 1);
                mainGrid.Children.Add(dockPanel);

                // 设置窗口内容
                imageWindow.Content = mainGrid;

                // 窗口关闭时重置标志位
                imageWindow.Closed += (s, e) => _isViewingImage = false;

                imageWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _isViewingImage = false;
                Growl.ErrorGlobal($"查看图片失败：{ex.Message}");
            }
        }
        #endregion

        #region 导出数据
        private void OnExportData()
        {
            // 记录开始时间，用于计算耗时
            var startTime = DateTime.Now;
            System.Diagnostics.Debug.WriteLine($"开始导出申报数据: {startTime:yyyy-MM-dd HH:mm:ss}");

            // 检查是否有数据可导出
            if (ListViewDeclarations == null || ListViewDeclarations.Count == 0)
            {
                Growl.WarningGlobal("当前没有数据可导出");
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV文件|*.csv",
                Title = "导出申报数据",
                FileName = $"申报数据_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 显示加载状态
                    IsLoading = true;

                    // CSV导出
                    ExportToCsv(saveFileDialog.FileName);

                    // 计算耗时
                    var endTime = DateTime.Now;
                    var timeSpan = endTime - startTime;

                    // 成功提示，显示导出记录数和耗时
                    Growl.SuccessGlobal($"成功导出 {ListViewDeclarations.Count} 条申报数据，耗时: {timeSpan.TotalSeconds:0.00} 秒");
                    System.Diagnostics.Debug.WriteLine($"申报数据导出完成: {endTime:yyyy-MM-dd HH:mm:ss}, 耗时: {timeSpan.TotalSeconds:0.00} 秒, 记录数: {ListViewDeclarations.Count}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"导出申报数据失败: {ex}");
                    Growl.ErrorGlobal($"导出失败: {ex.Message}");
                }
                finally
                {
                    // 关闭加载状态
                    IsLoading = false;
                }
            }
        }


        // CSV导出方法
        private void ExportToCsv(string filePath)
        {
            try 
            {
                // 使用UTF8编码，确保中文正确显示
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // 写入CSV头部
                    writer.WriteLine("申报ID,奖项名称,部门,被提名人,申报原因,申报人,申报时间,状态,审核人,审核时间");

                    // 写入每一行数据
                    foreach (var declaration in ListViewDeclarations)
                    {
                        // 处理CSV中的特殊字符，如逗号、换行符等
                        string reason = declaration.DeclarationReason?.Replace("\"", "\"\"").Replace(",", "，") ?? "";
                        
                        // 确保所有字段都有有效值
                        string awardName = declaration.Award?.AwardName?.Replace("\"", "\"\"") ?? "未设置";
                        string departmentName = declaration.Department?.DepartmentName?.Replace("\"", "\"\"") ?? "未设置";
                        string nominatedName = declaration.NominatedName?.Replace("\"", "\"\"") ?? "未设置";
                        string declarerName = declaration.DeclarerName?.Replace("\"", "\"\"") ?? "未设置";
                        string statusText = declaration.StatusText?.Replace("\"", "\"\"") ?? "未知";
                        string reviewerName = declaration.ReviewerName?.Replace("\"", "\"\"") ?? "";

                        writer.WriteLine(
                            $"{declaration.DeclarationId}," +
                            $"\"{awardName}\"," +
                            $"\"{departmentName}\"," +
                            $"\"{nominatedName}\"," +
                            $"\"{reason}\"," +
                            $"\"{declarerName}\"," +
                            $"{declaration.DeclarationTime:yyyy-MM-dd HH:mm:ss}," +
                            $"\"{statusText}\"," +
                            $"\"{reviewerName}\"," +
                            $"{(declaration.ReviewTime.HasValue ? declaration.ReviewTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}");
                    }
                }
                
                Growl.SuccessGlobal($"成功导出{ListViewDeclarations.Count}条申报记录到: {filePath}");
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"导出失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"导出申报数据出错: {ex.Message}\n{ex.StackTrace}");
            }
        }
        #endregion

        #region 分页功能
        private void OnPreviewTextInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return;

            int length = input.Length - LastInput.Length;
            for (int i = 0; i < length; i++)
            {
                var isDigit = char.IsDigit(input[^1]);
                if (!isDigit)
                {
                    SearchText = searchText[..^1];
                }
            }

            if (string.IsNullOrEmpty(SearchText))
            {
                LastInput = string.Empty;
                return;
            }

            bool isNumber = int.TryParse(SearchText, out int number);
            if (isNumber)
            {
                if (int.Parse(SearchText) > MaxPage)
                {
                    SearchText = MaxPage.ToString();
                }
            }
            else
            {
                SearchText = string.Empty;
            }

            LastInput = SearchText;
        }

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
            if (string.IsNullOrEmpty(SearchText))
            {
                Growl.WarningGlobal("请输入要跳转的页码");
                return;
            }

            if (!int.TryParse(SearchText, out int targetPage))
            {
                Growl.WarningGlobal("请输入有效的页码");
                return;
            }

            if (targetPage < 1 || targetPage > MaxPage)
            {
                Growl.WarningGlobal($"页码必须在1到{MaxPage}之间");
                return;
            }

            CurrentPage = targetPage;
            UpdateListViewData();
        }

        private void PageSizeChanged()
        {
            CurrentPage = 1;
            UpdateListViewData();
        }

        private void UpdateListViewData()
        {
            if (TempViewDeclarations == null || TempViewDeclarations.Count == 0)
            {
                ListViewDeclarations = new ObservableCollection<NominationDeclaration>();
                CurrentPage = 1;
                MaxPage = 1;
                TotalRecords = 0;
                return;
            }

            // 确保当前页不超过最大页数
            if (CurrentPage > MaxPage && MaxPage > 0)
            {
                CurrentPage = MaxPage;
            }

            // 计算起始索引和结束索引
            int startIndex = (CurrentPage - 1) * PageSize;

            // 使用Take/Skip优化内存使用，而不是创建新的列表
            var pagedData = TempViewDeclarations
                .Skip(startIndex)
                .Take(PageSize)
                .ToList();

            ListViewDeclarations = new ObservableCollection<NominationDeclaration>(pagedData);

            RefreshPaginationStatus();
        }

        // 刷新分页状态
        private void RefreshPaginationStatus()
        {
            // 更新页码相关UI状态
            IsFirstPage = CurrentPage == 1;
            IsPreviousEnabled = CurrentPage > 1;
            IsNextEnabled = CurrentPage < MaxPage;
            IsLastPage = CurrentPage == MaxPage;
        }
        #endregion

        #region 事件处理
        private async void OnDeclarationAdded()
        {
            if (IsLoading) return;

            try
            {
                // 记录当前选中项
                var selectedId = SelectedDeclaration?.DeclarationId;

                // 开始加载
                IsLoading = true;

                // 完全刷新数据
                await Task.Run(async () => {
                    using (var context = new DataBaseContext())
                    {
                        var allDeclarations = await context.NominationDeclarations
                            .AsNoTracking()
                            .Where(d => d.Status != 4) // 明确过滤掉已取消的记录
                            .Include(d => d.Award)
                            .Include(d => d.Department)
                            .Include(d => d.NominatedEmployee)
                            .Include(d => d.NominatedAdmin)
                            .Include(d => d.DeclarerEmployee)
                            .Include(d => d.DeclarerAdmin)
                            .Include(d => d.DeclarerSupAdmin)
                            .Include(d => d.ReviewerEmployee)
                            .Include(d => d.ReviewerAdmin)
                            .Include(d => d.ReviewerSupAdmin)
                            .ToListAsync();

                        Application.Current.Dispatcher.Invoke(() => {
                            // 更新申报集合
                            Declarations = new ObservableCollection<NominationDeclaration>(allDeclarations);

                            // 应用当前筛选条件
                            OnSearchDeclaration();

                            // 尝试恢复选中状态
                            if (selectedId.HasValue)
                            {
                                SelectedDeclaration = ListViewDeclarations.FirstOrDefault(d => d.DeclarationId == selectedId);
                            }

                            System.Diagnostics.Debug.WriteLine($"申报添加事件处理完成，已刷新界面，加载的数据类型: NominationDeclaration，数量: {allDeclarations.Count}");
                        });
                    }
                });

                // 提示用户
                Growl.InfoGlobal("有新申报已添加，列表已更新");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理申报添加事件时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnDeclarationUpdated()
        {
            if (IsLoading) return;

            try
            {
                // 记录当前选中项
                var selectedId = SelectedDeclaration?.DeclarationId;

                // 开始加载
                IsLoading = true;

                // 完全刷新数据
                await Task.Run(async () => {
                    using (var context = new DataBaseContext())
                    {
                        var allDeclarations = await context.NominationDeclarations
                            .AsNoTracking()
                            .Where(d => d.Status != 4) // 明确过滤掉已取消的记录
                            .Include(d => d.Award)
                            .Include(d => d.Department)
                            .Include(d => d.NominatedEmployee)
                            .Include(d => d.NominatedAdmin)
                            .Include(d => d.DeclarerEmployee)
                            .Include(d => d.DeclarerAdmin)
                            .Include(d => d.DeclarerSupAdmin)
                            .Include(d => d.ReviewerEmployee)
                            .Include(d => d.ReviewerAdmin)
                            .Include(d => d.ReviewerSupAdmin)
                            .ToListAsync();

                        Application.Current.Dispatcher.Invoke(() => {
                            // 更新申报集合
                            Declarations = new ObservableCollection<NominationDeclaration>(allDeclarations);

                            // 应用当前筛选条件
                            OnSearchDeclaration();

                            // 尝试恢复选中状态
                            if (selectedId.HasValue)
                            {
                                SelectedDeclaration = ListViewDeclarations.FirstOrDefault(d => d.DeclarationId == selectedId);
                            }

                            System.Diagnostics.Debug.WriteLine($"申报更新事件处理完成，已刷新界面，加载的数据类型: NominationDeclaration，数量: {allDeclarations.Count}");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理申报更新事件时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnDeclarationDeleted(int declarationId)
        {
            if (IsLoading) return;

            try
            {
                // 开始加载
                IsLoading = true;

                // 完全刷新数据
                await Task.Run(async () => {
                    using (var context = new DataBaseContext())
                    {
                        var allDeclarations = await context.NominationDeclarations
                            .AsNoTracking()
                            .Where(d => d.Status != 4) // 明确过滤掉已取消的记录
                            .Include(d => d.Award)
                            .Include(d => d.Department)
                            .Include(d => d.NominatedEmployee)
                            .Include(d => d.NominatedAdmin)
                            .Include(d => d.DeclarerEmployee)
                            .Include(d => d.DeclarerAdmin)
                            .Include(d => d.DeclarerSupAdmin)
                            .Include(d => d.ReviewerEmployee)
                            .Include(d => d.ReviewerAdmin)
                            .Include(d => d.ReviewerSupAdmin)
                            .ToListAsync();

                        Application.Current.Dispatcher.Invoke(() => {
                            // 更新申报集合
                            Declarations = new ObservableCollection<NominationDeclaration>(allDeclarations);

                            // 应用当前筛选条件
                            OnSearchDeclaration();

                            // 清除当前选择
                            SelectedDeclaration = null;

                            System.Diagnostics.Debug.WriteLine($"申报删除事件处理完成，已刷新界面，删除ID: {declarationId}，加载的数据类型: NominationDeclaration，数量: {allDeclarations.Count}");
                        });
                    }
                });

                // 提示用户
                Growl.InfoGlobal($"编号为 {declarationId} 的申报已被删除，列表已更新");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理申报删除事件时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnDeclarationApproved(NominationDeclaration declaration)
        {
            if (IsLoading) return;

            try
            {
                // 记录当前选中项
                var selectedId = SelectedDeclaration?.DeclarationId;

                // 开始加载
                IsLoading = true;

                // 完全刷新数据
                await Task.Run(async () => {
                    using (var context = new DataBaseContext())
                    {
                        var allDeclarations = await context.NominationDeclarations
                            .AsNoTracking()
                            .Where(d => d.Status != 4) // 明确排除已取消的记录
                            .Include(d => d.Award)
                            .Include(d => d.Department)
                            .Include(d => d.NominatedEmployee)
                            .Include(d => d.NominatedAdmin)
                            .Include(d => d.DeclarerEmployee)
                            .Include(d => d.DeclarerAdmin)
                            .Include(d => d.DeclarerSupAdmin)
                            .ToListAsync();

                        Application.Current.Dispatcher.Invoke(() => {
                            // 更新申报集合
                            Declarations = new ObservableCollection<NominationDeclaration>(allDeclarations);

                            // 应用当前筛选条件
                            OnSearchDeclaration();

                            // 尝试恢复选中状态
                            if (selectedId.HasValue)
                            {
                                SelectedDeclaration = ListViewDeclarations.FirstOrDefault(d => d.DeclarationId == selectedId);
                            }

                            System.Diagnostics.Debug.WriteLine($"申报审批通过事件处理完成，已刷新界面，加载的数据类型: NominationDeclaration，数量: {allDeclarations.Count}");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理申报审批通过事件时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

                // 已删除重复的OnDeclarationRejected方法

        private async void OnDeclarationPromoted(NominationDeclaration declaration)
        {
            if (IsLoading) return;

            try
            {
                // 记录当前选中项
                var selectedId = SelectedDeclaration?.DeclarationId;

                // 开始加载
                IsLoading = true;

                // 完全刷新数据
                await Task.Run(async () => {
                    using (var context = new DataBaseContext())
                    {
                        var allDeclarations = await context.NominationDeclarations
                            .AsNoTracking()
                            .Where(d => d.Status != 4) // 明确排除已取消的记录
                            .Include(d => d.Award)
                            .Include(d => d.Department)
                            .Include(d => d.NominatedEmployee)
                            .Include(d => d.NominatedAdmin)
                            .Include(d => d.DeclarerEmployee)
                            .Include(d => d.DeclarerAdmin)
                            .Include(d => d.DeclarerSupAdmin)
                            .ToListAsync();

                        Application.Current.Dispatcher.Invoke(() => {
                            // 更新申报集合
                            Declarations = new ObservableCollection<NominationDeclaration>(allDeclarations);

                            // 应用当前筛选条件
                            OnSearchDeclaration();

                            // 尝试恢复选中状态
                            if (selectedId.HasValue)
                            {
                                SelectedDeclaration = ListViewDeclarations.FirstOrDefault(d => d.DeclarationId == selectedId);
                            }

                            System.Diagnostics.Debug.WriteLine("申报升级事件处理完成，已刷新界面");
                        });
                    }
                });

                // 显示提示
                Growl.InfoGlobal($"申报 {declaration.DeclarationId} 已转为正式提名，列表已更新");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理申报升级事件时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void OnNominationDataChanged()
        {
            if (IsLoading) return;

            try
            {
                // 记录当前选中项
                var selectedId = SelectedDeclaration?.DeclarationId;

                // 开始加载
                IsLoading = true;

                // 完全刷新数据
                await Task.Run(async () => {
                    using (var context = new DataBaseContext())
                    {
                        // 明确只查询NominationDeclarations表
                        var allDeclarations = await context.NominationDeclarations
                            .AsNoTracking()
                            .Where(d => d.Status != 4) // 明确过滤掉已取消的记录
                            .Include(d => d.Award)
                            .Include(d => d.Department)
                            .Include(d => d.NominatedEmployee)
                            .Include(d => d.NominatedAdmin)
                            .Include(d => d.DeclarerEmployee)
                            .Include(d => d.DeclarerAdmin)
                            .Include(d => d.DeclarerSupAdmin)
                            .Include(d => d.ReviewerEmployee)
                            .Include(d => d.ReviewerAdmin)
                            .Include(d => d.ReviewerSupAdmin)
                            .ToListAsync();

                        Application.Current.Dispatcher.Invoke(() => {
                            // 更新申报集合
                            Declarations = new ObservableCollection<NominationDeclaration>(allDeclarations);

                            // 应用当前筛选条件
                            OnSearchDeclaration();

                            // 尝试恢复选中状态
                            if (selectedId.HasValue)
                            {
                                SelectedDeclaration = ListViewDeclarations.FirstOrDefault(d => d.DeclarationId == selectedId);
                            }

                            System.Diagnostics.Debug.WriteLine($"提名数据变更事件处理完成，已刷新界面，加载的数据类型: NominationDeclaration，数量: {allDeclarations.Count}");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理提名数据变更事件时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 添加SetProperty方法实现
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        #region Helper Methods
        private string GetNomineeName(NominationDeclaration declaration)
        {
            if (declaration.NominatedEmployee != null)
            {
                return declaration.NominatedEmployee.EmployeeName;
            }
            else if (declaration.NominatedAdmin != null)
            {
                return declaration.NominatedAdmin.AdminName;
            }
            else
            {
                return "未知";
            }
        }

        private string GetStatusText(int status)
        {
            switch (status)
            {
                case 0:
                    return "待审核";
                case 1:
                    return "已通过";
                case 2:
                    return "已拒绝";
                case 4:
                    return "已取消";
                default:
                    return "未知状态";
            }
        }
        #endregion

        #region Clear Selection
        private void ClearSelection()
        {
            // 检查是否有选中的申报项
            if (SelectedDeclaration != null)
            {
                // 保存被取消选择的申报名称，用于提示
                string nominationName = SelectedDeclaration.NominatedName;

                // 取消选择
                SelectedDeclaration = null;

                // 显示取消选择成功的提示
                Growl.InfoGlobal($"已取消选择【{nominationName}】的申报项");
            }
        }
        #endregion

        #region 自动刷新数据
        // 将自动刷新数据更改为手动刷新数据
        private async void OnRefreshData()
        {
            try
            {
                // 如果当前正在加载数据，则忽略刷新请求
                if (IsLoading)
                {
                    Growl.InfoGlobal("正在加载数据，请稍候...");
                    return;
                }

                // 记录当前选中的申报
                var selectedDeclarationId = SelectedDeclaration?.DeclarationId;

                // 显示加载状态
                IsLoading = true;

                // 显示正在刷新的提示
                Growl.InfoGlobal("正在刷新数据...");
                System.Diagnostics.Debug.WriteLine($"手动刷新数据开始: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // 重新加载数据
                await Task.Run(async () => {
                    using (var context = new DataBaseContext())
                    {
                        // 获取所有申报数据，注意只查询NominationDeclarations表
                        var allDeclarations = await context.NominationDeclarations
                            .AsNoTracking() // 提高性能
                            .Where(d => d.Status != 4) // 排除已取消的记录
                            .Include(d => d.Award)
                            .Include(d => d.Department)
                            .Include(d => d.NominatedEmployee)
                            .Include(d => d.NominatedAdmin)
                            .Include(d => d.DeclarerEmployee)
                            .Include(d => d.DeclarerAdmin)
                            .Include(d => d.DeclarerSupAdmin)
                            .Include(d => d.ReviewerEmployee)
                            .Include(d => d.ReviewerAdmin)
                            .Include(d => d.ReviewerSupAdmin)
                            .ToListAsync();

                        // 更新缓存时间
                        lastLoadTime = DateTime.Now;

                        // 在UI线程上更新数据集合
                        Application.Current.Dispatcher.Invoke(() => {
                            try
                            {
                                // 如果视图被销毁或不再活动，则停止更新UI
                                if (!IsViewActive) return;

                                // 判断数据是否有变化
                                bool hasChanges = Declarations == null ||
                                                  Declarations.Count != allDeclarations.Count ||
                                                  Declarations.Any(d => !allDeclarations.Any(ad => ad.DeclarationId == d.DeclarationId));

                                // 更新申报集合(不管有没有变化，都刷新以确保界面最新)
                                Declarations = new ObservableCollection<NominationDeclaration>(allDeclarations);

                                // 应用当前筛选条件
                                OnSearchDeclaration();

                                // 恢复之前选中的申报
                                if (selectedDeclarationId.HasValue)
                                {
                                    SelectedDeclaration = ListViewDeclarations.FirstOrDefault(d => d.DeclarationId == selectedDeclarationId);
                                }

                                // 输出调试信息
                                if (hasChanges)
                                {
                                    System.Diagnostics.Debug.WriteLine($"手动刷新完成，申报数据条数：{Declarations.Count}，发现数据变化");
                                    Growl.SuccessGlobal($"刷新完成，数据已更新");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"手动刷新完成，申报数据条数：{Declarations.Count}，数据无变化");
                                    Growl.SuccessGlobal("刷新完成，数据已是最新");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"数据刷新UI更新时出错: {ex.Message}");
                                Growl.ErrorGlobal($"数据刷新失败: {ex.Message}");
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新数据时出错: {ex.Message}");
                Growl.ErrorGlobal($"刷新数据失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 兼容旧的自动刷新调用，直接转发到手动刷新
        private void AutoRefreshData()
        {
            OnRefreshData();
        }
        #endregion

        // 添加视图活动状态属性
        private bool _isViewActive = true;
        public bool IsViewActive
        {
            get => _isViewActive;
            set => SetProperty(ref _isViewActive, value);
        }

        public void Cleanup()
        {
            // 标记视图不再活动
            IsViewActive = false;

            // 清理计时器资源
            timer?.Stop();
            timer = null;

            autoRefreshTimer?.Stop();
            autoRefreshTimer = null;

            // 移除事件订阅
            eventAggregator.GetEvent<NominationDeclarationAddEvent>().Unsubscribe(OnDeclarationAdded);
            eventAggregator.GetEvent<NominationDeclarationUpdateEvent>().Unsubscribe(OnDeclarationUpdated);
            eventAggregator.GetEvent<NominationDeclarationDeleteEvent>().Unsubscribe(OnDeclarationDeleted);
            eventAggregator.GetEvent<NominationDeclarationApproveEvent>().Unsubscribe(OnDeclarationApproved);
            eventAggregator.GetEvent<NominationDeclarationRejectEvent>().Unsubscribe(OnDeclarationRejected);
            eventAggregator.GetEvent<NominationDeclarationPromoteEvent>().Unsubscribe(OnDeclarationPromoted);
            eventAggregator.GetEvent<NominationDataChangedEvent>().Unsubscribe(OnNominationDataChanged);
        }
    }
}
