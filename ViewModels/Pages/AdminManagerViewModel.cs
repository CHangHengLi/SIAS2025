using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using SIASGraduate.Services;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NLog;

namespace SIASGraduate.ViewModels.Pages
{
    public class AdminManagerViewModel : BindableBase, INavigationAware
    {
        #region 时间属性
        private DispatcherTimer timer;
        #endregion

        #region 服务
        private readonly IAdminService adminService;
        #endregion

        #region 区域管理器
        private IRegionManager regionManager;
        #endregion

        #region 日志
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 构造函数
        public AdminManagerViewModel(IAdminService adminService, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            #region 分页
            EnableButtons();
            PageSizeItems();
            #endregion

            #region 区域管理器
            this.regionManager = regionManager;
            #endregion

            #region 事件聚合器
            this.eventAggregator = eventAggregator;
            // 订阅事件
            eventAggregator.GetEvent<AdminAddEvent>().Subscribe(OnAdminAdded);
            eventAggregator.GetEvent<AdminUpdateEvent>().Subscribe(OnAdminUpdated);
            eventAggregator.GetEvent<AdminExportDataEvent>().Subscribe(OnExportData);
            #endregion

            #region 数据的导入和导出
            ExportDataCommand = new DelegateCommand(OnExport);
            #endregion

            #region 时间显示
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += async (s, e) => await ColorChangeAsync();
            timer.Start();
            // 初始化为默认颜色
            SearchBackground = new SolidColorBrush(Color.FromRgb(173, 216, 230));
            #endregion

            #region 初始化状态
            // Status会在View层通过ComboBox的SelectedIndex=0和IsSelected=true自动初始化
            #endregion

            #region 服务
            this.adminService = adminService;
            #endregion

            #region 初始化命令
            AddAdminCommand = new DelegateCommand(OnAddAdmin);
            DeleteAdminCommand = new DelegateCommand<Admin>(OnDeleteAdmin);
            UpdateAdminCommand = new DelegateCommand<Admin>(OnUpdateAdmin);
            SearchAdminCommand = new DelegateCommand(OnSearchAdmin);
            PreviewTextInputCommand = new DelegateCommand<string>(OnPreviewTextInput);
            RefreshCommand = new DelegateCommand(OnRefresh);
            #endregion

            // 使用异步方式初始化，避免界面卡顿
            InitializeDataAsync();
        }
        
        // 异步初始化数据，避免构造函数中的同步操作导致界面卡顿
        private async void InitializeDataAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    using (var context = new DataBaseContext())
                    {
                        // 在后台线程加载数据
                        var adminList = context.Admins.ToList();
                        var departmentList = context.Departments.ToList();
                        
                        // 切换回UI线程更新界面
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Admins = new ObservableCollection<Admin>(adminList);
                            Departments = new ObservableCollection<Department>(departmentList);
                            TempAdmins = new ObservableCollection<Admin>(Admins.Where(e => e.IsActive == true));
                            ListViewAdmins = new ObservableCollection<Admin>(TempAdmins.Take(PageSize));
                            TotalRecords = TempAdmins.Count;
                            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"加载数据失败: {ex.Message}");
            }
        }
        #endregion

        #region 搜索区域背景
        private byte colorOffset = 0;
        private SolidColorBrush searchBackground;
        public SolidColorBrush SearchBackground
        {
            get { return searchBackground; }
            set { SetProperty(ref searchBackground, value); }
        }
        #endregion

        #region 颜色变换   
        private async Task ColorChangeAsync()
        {
            await Task.Run(() =>
            {
                // 这里实际上不需要异步处理，因为颜色计算很快
                // 但是如果你有其他耗时操作，可以放在这里
                byte red = (byte)((colorOffset + 100) % 128 + 128);
                byte green = (byte)((colorOffset + 150) % 128 + 128);
                byte blue = (byte)((colorOffset + 200) % 128 + 128);
                // 需要在UI线程上更新UI元素
                // 所以使用Dispatcher.Invoke
                App.Current.Dispatcher.Invoke(() =>
                {
                    SearchBackground = new SolidColorBrush(Color.FromRgb(red, green, blue));
                });
                colorOffset += 5; // 增加偏移量以改变颜色
            });
        }
        #endregion

        #region 在职状态
        private object status;
        public object Status
        {
            get { return status; }
            set 
            { 
                SetProperty(ref status, value);
                // 同步更新StatusText
                if (value is System.Windows.Controls.ComboBoxItem comboBoxItem)
                {
                    StatusText = comboBoxItem.Content?.ToString() ?? "在职员工";
                }
                else if (value is string strStatus)
                {
                    StatusText = strStatus;
                }
                else
                {
                    StatusText = "在职员工";
                }
            }
        }

        private string statusText = "在职员工";
        public string StatusText
        {
            get { return statusText; }
            set { SetProperty(ref statusText, value); }
        }
        #endregion

        #region 搜索关键词
        private string searchKeyword;
        public string SearchKeyword
        {
            get { return searchKeyword; }
            set { SetProperty(ref searchKeyword, value); }
        }
        #endregion

        #region 搜索按钮是否启用
        private bool isSearchEnabled;
        public bool IsSearchEnabled
        {
            get => isSearchEnabled;
            set => SetProperty(ref isSearchEnabled, value);
        }
        #endregion

        #region 添加按钮是否启用
        private bool isAddEnabled;
        public bool IsAddEnabled
        {
            get => isAddEnabled;
            set => SetProperty(ref isAddEnabled, value);
        }
        #endregion

        #region 删除按钮是否启用
        private bool isDeleteEnabled;
        public bool IsDeleteEnabled
        {
            get => isDeleteEnabled;
            set => SetProperty(ref isDeleteEnabled, value);
        }
        #endregion

        #region 修改按钮是否启用
        private bool isUpdateEnabled;
        public bool IsUpdateEnabled
        {
            get => isUpdateEnabled;
            set => SetProperty(ref isUpdateEnabled, value);
        }
        #endregion

        #region 刷新按钮是否启用
        private bool isRefreshEnabled;
        public bool IsRefreshEnabled
        {
            get => isRefreshEnabled;
            set => SetProperty(ref isRefreshEnabled, value);
        }
        #endregion

        #region  增删改查命令
        public DelegateCommand AddAdminCommand { get; }
        public DelegateCommand<Admin> DeleteAdminCommand { get; }
        public DelegateCommand<Admin> UpdateAdminCommand { get; }
        public DelegateCommand SearchAdminCommand { get; }
        #endregion

        #region 原始的Admins集合
        private ObservableCollection<Admin> admins;
        public ObservableCollection<Admin> Admins
        {
            get => admins;
            set => SetProperty(ref admins, value);
        }
        #endregion

        #region 临时的Admins集合
        private ObservableCollection<Admin> tempAdmins;

        public ObservableCollection<Admin> TempAdmins { get => tempAdmins; set => SetProperty(ref tempAdmins, value); }
        #endregion

        #region 展示的Admins集合
        private ObservableCollection<Admin> listViewAdmins;

        public ObservableCollection<Admin> ListViewAdmins { get => listViewAdmins; set => SetProperty(ref listViewAdmins, value); }
        #endregion

        #region 选中的员工
        private Admin selectedAdmin;
        public Admin SelectedAdmin
        {
            get => selectedAdmin;
            set => SetProperty(ref selectedAdmin, value);
        }
        #endregion

        private ObservableCollection<Department> departments;
        public ObservableCollection<Department> Departments
        {
            get { return departments; }
            set { SetProperty(ref departments, value); }
        }

        #region 方法

        #region 禁用增删改查按钮
        private void DisableButtons()
        {
            IsAddEnabled = false;
            IsDeleteEnabled = false;
            IsUpdateEnabled = false;
            IsSearchEnabled = false;
        }
        #endregion

        #region 启用增删改查按钮
        private void EnableButtons()
        {
            IsAddEnabled = true;
            IsDeleteEnabled = true;
            IsUpdateEnabled = true;
            IsSearchEnabled = true;
        }
        #endregion

        #region 加载管理员列表数据
        private void LoadAdmins()
        {
            try
            {
                // 检查临时集合是否为空
                if (TempAdmins == null || !TempAdmins.Any())
                {
                    ListViewAdmins = new ObservableCollection<Admin>();
                    Growl.InfoGlobal("没有符合条件的管理员数据");
                    return;
                }

                // 计算分页参数
                TotalRecords = TempAdmins.Count;
                MaxPage = TotalRecords == 0 ? 1 : (TotalRecords % PageSize == 0 ? 
                          TotalRecords / PageSize : (TotalRecords / PageSize) + 1);

                // 确保当前页码有效
                if (CurrentPage > MaxPage)
                {
                    CurrentPage = MaxPage > 0 ? MaxPage : 1;
                }

                // 计算当前页数据范围
                int skip = (CurrentPage - 1) * PageSize;
                var pageData = TempAdmins.Skip(skip).Take(PageSize).ToList();

                // 批量更新UI集合
                App.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 创建新集合而不是清空后添加，提高性能
                    ListViewAdmins = new ObservableCollection<Admin>(pageData);
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"加载管理员列表失败: {ex.Message}");
                Debug.WriteLine($"加载管理员列表失败: {ex}");
            }
        }
        #endregion
        
        #region 刷新命令
        private bool isRefreshing;
        public bool IsRefreshing
        {
            get => isRefreshing;
            set => SetProperty(ref isRefreshing, value);
        }

        public DelegateCommand RefreshCommand { get; private set; }

        private void OnRefresh()
        {
            if (IsRefreshing)
                return;
            
            IsRefreshing = true;
            try
            {
                logger.Info("手动刷新管理员列表");
                OnAdminUpdated();
                Growl.InfoGlobal("管理员列表已刷新");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "手动刷新管理员列表失败");
                Growl.ErrorGlobal($"刷新失败: {ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
            }
        }
        #endregion

        #region 数据加载状态
        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }
        #endregion

        #region 管理员更新时刷新视图
        private async void OnAdminUpdated()
        {
            try
            {
                logger.Info("接收到管理员更新事件，开始刷新管理员列表");
                
                // 禁用按钮，防止并发操作
                IsSearchEnabled = false;
                IsRefreshEnabled = false;
                
                // 保存当前筛选条件
                string currentStatusText = StatusText;
                string currentKeyword = SearchKeyword;
                
                // 获取当前选中的管理员ID，用于在刷新后尝试恢复选中状态
                int? selectedAdminId = SelectedAdmin?.AdminId;
                
                // 使用新上下文和异步方法
                using (var freshContext = new DataBaseContext())
                {
                    freshContext.Database.SetCommandTimeout(120); // 设置较长的超时时间
                    
                    // 重新从数据库加载所有管理员
                    var adminsFromDb = await freshContext.Admins
                        .Include(a => a.Department)
                        .AsNoTracking() // 使用无跟踪查询提高性能
                        .ToListAsync();
                    
                    // 在UI线程上更新集合
                    App.Current.Dispatcher.Invoke(() => {
                        Admins = new ObservableCollection<Admin>(adminsFromDb);
                        logger.Debug($"已刷新管理员数据，加载了 {Admins.Count} 条记录");
                        
                        // 根据状态筛选
                        IEnumerable<Admin> filteredAdmins;
                        
                        if (currentStatusText.Contains("在职"))
                        {
                            filteredAdmins = Admins.Where(a => a.IsActive == true);
                        }
                        else if (currentStatusText.Contains("离职"))
                        {
                            filteredAdmins = Admins.Where(a => a.IsActive == false);
                        }
                        else
                        {
                            filteredAdmins = Admins;
                        }
                        
                        // 关键词筛选
                        if (!string.IsNullOrWhiteSpace(currentKeyword))
                        {
                            filteredAdmins = filteredAdmins.Where(a =>
                                (a.AdminName?.Contains(currentKeyword, StringComparison.OrdinalIgnoreCase) == true) ||
                                (a.Account?.Contains(currentKeyword, StringComparison.OrdinalIgnoreCase) == true) ||
                                a.AdminId.ToString().Contains(currentKeyword)
                            );
                        }
                        
                        // 更新临时集合
                        TempAdmins = new ObservableCollection<Admin>(filteredAdmins);
                        
                        // 更新分页信息
                        TotalRecords = TempAdmins.Count;
                        MaxPage = TotalRecords == 0 ? 1 : 
                                 (TotalRecords % PageSize == 0 ? 
                                 TotalRecords / PageSize : (TotalRecords / PageSize) + 1);
                        
                        // 确保当前页在有效范围内
                        if (CurrentPage > MaxPage)
                        {
                            CurrentPage = MaxPage > 0 ? MaxPage : 1;
                        }
                        
                        // 重新加载当前页数据
                        LoadAdmins();
                        
                        // 尝试恢复选中状态
                        if (selectedAdminId.HasValue && ListViewAdmins != null)
                        {
                            SelectedAdmin = ListViewAdmins.FirstOrDefault(a => a.AdminId == selectedAdminId);
                        }
                        
                        logger.Info($"管理员列表已刷新，当前显示 {TempAdmins.Count} 条记录");
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "刷新管理员列表失败");
                Growl.ErrorGlobal($"更新管理员列表失败: {ex.Message}");
            }
            finally
            {
                // 确保按钮状态恢复
                IsSearchEnabled = true;
                IsRefreshEnabled = true;
            }
        }
        #endregion

        #region 查询员工时
        public async void OnSearchAdmin()
        {
            try
            {
                DisableButtons();
                System.Diagnostics.Debug.WriteLine($"开始查询管理员，状态条件: '{Status}'");
                
                using (var context = new DataBaseContext())
                {
                    context.Database.SetCommandTimeout(180); // 设置更长的超时时间
                    
                    // 始终从数据库重新加载管理员列表，确保数据是最新的
                    Admins = new ObservableCollection<Admin>(
                        context.Admins
                              .Include(a => a.Department)
                              .ToList());
                    
                    System.Diagnostics.Debug.WriteLine($"从数据库加载了 {Admins.Count} 个管理员");
                    
                    // 加载部门信息
                    departments = new ObservableCollection<Department>(
                        context.Departments.ToList());
                }
                
                // 根据状态筛选
                if (Status == null)
                {
                    System.Diagnostics.Debug.WriteLine("Status为空，设置为默认值: 在职员工");
                    Status = "在职员工";
                }
                
                System.Diagnostics.Debug.WriteLine($"当前筛选条件: {StatusText}");
                
                if (StatusText.Contains("在职员工"))
                {
                    System.Diagnostics.Debug.WriteLine("筛选在职管理员");
                    TempAdmins = new ObservableCollection<Admin>(Admins.Where(a => a.IsActive == true));
                }
                else if (StatusText.Contains("离职员工"))
                {
                    System.Diagnostics.Debug.WriteLine("筛选离职管理员");
                    TempAdmins = new ObservableCollection<Admin>(Admins.Where(a => a.IsActive == false));
                }
                else // 全部员工
                {
                    System.Diagnostics.Debug.WriteLine("显示全部管理员");
                    TempAdmins = new ObservableCollection<Admin>(Admins);
                }
                
                System.Diagnostics.Debug.WriteLine($"状态筛选后剩余 {TempAdmins.Count} 个管理员");
                
                // 关键字筛选
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    string keyword = SearchKeyword.Trim().ToLower();
                    System.Diagnostics.Debug.WriteLine($"使用关键词筛选: '{keyword}'");
                    
                    TempAdmins = new ObservableCollection<Admin>(TempAdmins.Where(a =>
                        (a.AdminName != null && a.AdminName.ToLower().Contains(keyword)) ||
                        // 已移除对AdminPassword的匹配，提高安全性
                        (a.Email != null && a.Email.ToLower().Contains(keyword)) ||
                        (a.Account != null && a.Account.ToLower().Contains(keyword)) ||
                        a.AdminId.ToString().Contains(keyword) ||
                        (departments != null && 
                         a.DepartmentId.HasValue && 
                         departments.Any(d => d.DepartmentId == a.DepartmentId && 
                                         d.DepartmentName != null && 
                                         d.DepartmentName.ToLower().Contains(keyword))) ||
                        (a.HireDate.HasValue && a.HireDate.Value.ToString("yyyy-MM-dd").Contains(keyword))
                    ));
                    
                    System.Diagnostics.Debug.WriteLine($"关键词筛选后剩余 {TempAdmins.Count} 个管理员");
                }
                
                // 重置页码
                CurrentPage = 1;
                
                // 加载管理员数据
                LoadAdmins();
                
                EnableButtons();
            }
            catch (Exception ex)
            {
                EnableButtons();
                Growl.ErrorGlobal($"搜索管理员失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"搜索管理员出错: {ex.Message}\n{ex.StackTrace}");
            }
        }
        #endregion

        #region 添加员工时


        private void OnAddAdmin()

        {
            regionManager.RequestNavigate("AdminEditRegion", "AddAdmin");
        }
        #endregion

        #region 更改员工时
        private void OnUpdateAdmin(Admin admin)
        {
            //传入参数
            var parameters = new NavigationParameters()
            {
                { "Admin", admin }
            };
            regionManager.RequestNavigate("AdminEditRegion", "EditAdmin", parameters);
        }
        #endregion

        #region 删除员工时

        private async void OnDeleteAdmin(Admin admin)
        {
            if (admin == null)
            {
                Growl.WarningGlobal("请选择要删除的管理员");
                return;
            }

            try
            {
                // 检查管理员是否有关联记录
                using (var context = new DataBaseContext())
                {
                    // 查询管理员作为被提名者的提名记录
                    int nominatedCount = context.Nominations.Count(n => n.NominatedAdminId == admin.AdminId);
                    // 查询管理员作为提议人的提名记录
                    int proposerCount = context.Nominations.Count(n => n.ProposerAdminId == admin.AdminId);
                    // 查询管理员作为被提名者的申报记录
                    int declaredCount = context.NominationDeclarations.Count(n => n.NominatedAdminId == admin.AdminId);
                    // 查询管理员作为申报人的申报记录
                    int declarerCount = context.NominationDeclarations.Count(n => n.DeclarerAdminId == admin.AdminId);
                    // 查询管理员作为投票者的投票记录
                    int voterCount = context.VoteRecords.Count(v => v.VoterAdminId == admin.AdminId);
                    
                    int nominationCount = nominatedCount + proposerCount;
                    int declarationCount = declaredCount + declarerCount;
                    
                    // 如果有关联记录，显示特殊的确认对话框
                    if (nominationCount > 0 || declarationCount > 0 || voterCount > 0)
                    {
                        // 构建提示消息
                        string message = "当前管理员有关联记录:";
                        List<string> details = new List<string>();
                        
                        if (nominationCount > 0)
                            details.Add($"奖项提名({nominationCount}条)");
                        if (declarationCount > 0)
                            details.Add($"提名申报({declarationCount}条)");
                        if (voterCount > 0)
                            details.Add($"投票记录({voterCount}条)");
                            
                        message += string.Join("、", details);
                        message += "，是否删除该管理员及其所有关联记录？";
                        
                        // 显示Growl确认对话框
                        Growl.AskGlobal(message, (result) =>
                        {
                            if (result)
                            {
                                try
                                {
                                    DeleteAdminWithRelatedRecordsAsync(admin);
                                }
                                catch (Exception ex)
                                {
                                    Growl.ErrorGlobal($"管理员删除失败: {ex.Message}");
                                }
                            }
                            return true;
                        });
                        return;
                    }
                }

                // 如果没有关联记录，使用原有确认对话框
                Growl.AskGlobal($"确认删除管理员 {admin.AdminName} 吗？此操作不可逆", (result) =>
                {
                    if (result)
                    {
                        try
                        {
                            // 对于没有关联记录的管理员，使用简单的直接删除
                            using (var context = new DataBaseContext())
                            {
                                var dbAdmin = context.Admins.Find(admin.AdminId);
                                if (dbAdmin != null)
                                {
                                    context.Admins.Remove(dbAdmin);
                                    context.SaveChanges();
                                    Growl.SuccessGlobal($"管理员 {admin.AdminName} 删除成功");
                                    
                                    // 刷新列表
                                    Admins = new ObservableCollection<Admin>(context.Admins.ToList());
                                    TempAdmins = new ObservableCollection<Admin>(Admins.Where(e => e.IsActive == true));
                                    OnSearchAdmin();
                                }
                                else
                                {
                                    Growl.ErrorGlobal($"找不到管理员: ID={admin.AdminId}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"简单删除管理员失败，尝试级联删除: {ex.Message}");
                            Growl.WarningGlobal("简单删除失败，尝试级联删除...");
                            DeleteAdminWithRelatedRecordsAsync(admin);
                        }
                    }
                    return true;
                });
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"处理管理员删除时出错: {ex.Message}");
            }
        }
        #endregion

        #region 管理员级联删除
        private async void DeleteAdminWithRelatedRecordsAsync(Admin admin)
        {
            try
            {
                IsLoading = true;
                logger.Info($"开始级联删除管理员记录: ID={admin.AdminId}, 名称={admin.AdminName}");
                
                bool success = await adminService.DeleteAdminWithRelatedRecords(admin.AdminId);
                
                if (success)
                {
                    logger.Info($"管理员级联删除成功: ID={admin.AdminId}");
                    Growl.SuccessGlobal($"管理员 {admin.AdminName} 及其关联记录删除成功");
                    
                    // 刷新列表
                    using (var context = new DataBaseContext())
                    {
                        Admins = new ObservableCollection<Admin>(context.Admins.ToList());
                        TempAdmins = new ObservableCollection<Admin>(Admins.Where(e => e.IsActive == true));
                        OnSearchAdmin();
                    }
                }
                else
                {
                    logger.Error($"级联删除管理员失败: ID={admin.AdminId}");
                    Growl.ErrorGlobal($"管理员删除失败，请尝试更彻底的级联删除");
                    TryForceDeleteAdminAsync(admin);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"删除管理员时出错: {ex.Message}");
                Growl.ErrorGlobal($"删除管理员及关联记录时出错: {ex.Message}");
                TryForceDeleteAdminAsync(admin);
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region 尝试更彻底的删除
        /// <summary>
        /// 尝试强制删除管理员，绕过EF Core处理可能存在的问题
        /// </summary>
        private async void TryForceDeleteAdminAsync(Admin admin)
        {
            try
            {
                IsLoading = true;
                Growl.InfoGlobal("正在尝试更彻底的级联删除...");
                
                // 显示确认对话框
                Growl.AskGlobal("标准删除失败，是否尝试更彻底的级联删除？此操作将直接从数据库中删除所有关联记录，不可逆", (result) =>
                {
                    if (result)
                    {
                        ExecuteDirectSqlDeleteAsync(admin);
                    }
                    else
                    {
                        IsLoading = false;
                        Growl.InfoGlobal("已取消彻底级联删除");
                    }
                    return true;
                });
            }
            catch (Exception ex)
            {
                IsLoading = false;
                logger.Error($"尝试彻底级联删除时发生异常: {ex.Message}");
                Growl.ErrorGlobal($"处理删除请求时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行直接SQL语句删除
        /// </summary>
        private async void ExecuteDirectSqlDeleteAsync(Admin admin)
        {
            try
            {
                logger.Info($"开始执行管理员的彻底级联删除: ID={admin.AdminId}");
                bool success = await adminService.ExecuteDirectSqlDelete(admin.AdminId);
                
                if (success)
                {
                    logger.Info($"管理员彻底级联删除成功: ID={admin.AdminId}");
                    Growl.SuccessGlobal($"管理员 {admin.AdminName} 及其关联记录已成功删除");
                    
                    // 刷新列表
                    using (var context = new DataBaseContext())
                    {
                        Admins = new ObservableCollection<Admin>(context.Admins.ToList());
                        TempAdmins = new ObservableCollection<Admin>(Admins.Where(e => e.IsActive == true));
                        OnSearchAdmin();
                    }
                }
                else
                {
                    logger.Error($"管理员彻底级联删除失败: ID={admin.AdminId}");
                    Growl.ErrorGlobal("彻底级联删除失败，请联系系统管理员");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"彻底级联删除管理员时出错: {ex.Message}");
                Growl.ErrorGlobal($"彻底级联删除时出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region 员工(新增)更新时更新视图列表
        private void OnAdminAdded()
        {
            OnSearchAdmin();
        }
        #endregion

        #region 分页

        #region 最大页码
        private int maxPage;

        public int MaxPage { get => maxPage; set => SetProperty(ref maxPage, value); }
        #endregion

        #region 总记录数 TotalRecords
        private int totalRecords;

        public int TotalRecords { get => totalRecords; set => SetProperty(ref totalRecords, value); }
        #endregion

        #region 当前页码 CurrentPage
        private int currentPage = 1;

        public int CurrentPage { get => currentPage; set => SetProperty(ref currentPage, value); }
        #endregion

        #region  每页显示个数 PageSize

        private int pageSize = 10;

        public int PageSize { get => pageSize; set => SetProperty(ref pageSize, value); }

        #endregion

        #region 每页显示条数集合
        private void PageSizeItems()
        {
            PageSizeOptions = new List<int> { 10, 5, 3, 2 };
        }


        private IEnumerable pageSizeOptions;

        public IEnumerable PageSizeOptions { get => pageSizeOptions; set => SetProperty(ref pageSizeOptions, value); }


        #endregion

        #region  跳转查询框
        private string searchText;

        public string SearchText { get => searchText; set => SetProperty(ref searchText, value); }
        private string lastinput = string.Empty;

        public string Lastinput { get => lastinput; set => SetProperty(ref lastinput, value); }
        //限制输入框内只能输入数字
        public DelegateCommand<string> PreviewTextInputCommand { get; private set; }
        private void OnPreviewTextInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return;
            int length = input.Length - Lastinput.Length;
            for (int i = 0; i < length; i++)
            {
                var isDigit = char.IsDigit(input[input.Length - 1]);
                if (!isDigit)
                {
                    //如果输入的不是数字，则截取掉最后则截取掉最后一位字符
                    SearchText = searchText.Substring(0, searchText.Length - 1);
                    //如果输入的不是数字清空输入框
                    //暂时无法处理:类似于前面是非数字,最后为数字的粘贴过来的字符串情况
                    //SearchText = string.Empty;
                }
            }
            if (string.IsNullOrEmpty(SearchText))
            {
                Lastinput = string.Empty;
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
            Lastinput = SearchText;
            return;

        }
        #endregion

        #region 当每页个数变换时
        private DelegateCommand pageSizeChangedCommand;
        public ICommand PageSizeChangedCommand => pageSizeChangedCommand ??= new DelegateCommand(PageSizeChanged);

        private void PageSizeChanged()
        {
            CurrentPage = 1;
            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
            LoadAdmins();
        }
        #endregion

        #region 上一页 下一页


        private DelegateCommand previousPageCommand;
        public DelegateCommand PreviousPageCommand => previousPageCommand ??= new DelegateCommand(PreviousPage);

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadAdmins();
            }
        }

        private DelegateCommand nextPageCommand;
        public DelegateCommand NextPageCommand => nextPageCommand ??= new DelegateCommand(NextPage);

        private void NextPage()
        {

            if (CurrentPage < MaxPage)
            {
                CurrentPage++;
                LoadAdmins();
            }
        }

        private DelegateCommand jumpPageCommand;
        //跳转页面
        public ICommand JumpPageCommand => jumpPageCommand ??= new DelegateCommand(JumpPage);

        private void JumpPage()
        {

            if (int.TryParse(SearchText, out int number))
            {
                if (number > MaxPage)
                {
                    CurrentPage = MaxPage;
                }
                else
                {
                    CurrentPage = number;
                }
                LoadAdmins();
            }
        }
        #endregion

        #endregion

        #region 数据的导入导出

        //public DelegateCommand ImportDataCommand { get; }
        public DelegateCommand ExportDataCommand { get; }

        private void OnExport()
        {
            // 根据当前状态确定默认文件名
            string fileNamePrefix = StatusText.Contains("在职") ? "在职管理员" : 
                                   StatusText.Contains("离职") ? "离职管理员" : "全部管理员";
            string defaultFileName = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd}";
            
            // 打开文件对话框选择保存路径
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = defaultFileName
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                // 发布导出事件，传递文件路径
                eventAggregator.GetEvent<AdminExportDataEvent>().Publish(saveFileDialog.FileName);
            }
        }

        private void OnExportData(string filePath)
        {
            try
            {
                logger.Info($"开始导出{StatusText}列表...");
                
                // 导出当前筛选后的管理员列表(TempAdmins)，而不是所有管理员(Admins)
                if (TempAdmins == null || TempAdmins.Count == 0)
                {
                    Growl.InfoGlobal("当前筛选条件下没有可导出的管理员数据");
                    logger.Warn("导出取消：没有符合条件的管理员数据");
                    return;
                }
                
                // 实现导出逻辑
                bool result = adminService.ExportAdmins(TempAdmins.ToList(), filePath);
                
                if (result)
                {
                    // 添加导出成功提示，包含筛选条件和导出的记录数
                    string statusMsg = StatusText.Contains("在职") ? "在职" : 
                                      StatusText.Contains("离职") ? "离职" : "全部";
                    
                    Growl.SuccessGlobal($"成功导出{statusMsg}管理员记录 {TempAdmins.Count} 条");
                    logger.Info($"成功导出{statusMsg}管理员记录 {TempAdmins.Count} 条到 {filePath}");
                }
                else
                {
                    Growl.ErrorGlobal("导出失败，请检查文件路径是否可写");
                    logger.Error($"导出管理员记录失败: 路径={filePath}");
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"导出失败: {ex.Message}");
                logger.Error(ex, "导出管理员数据时发生异常");
            }
        }
        #endregion

        #region INavigatorAware接口实现
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 重新执行查询，确保数据是最新的
            OnSearchAdmin();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }
        #endregion

        #endregion
    }
}
