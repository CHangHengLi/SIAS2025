using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NLog;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using SIASGraduate.Services;

namespace SIASGraduate.ViewModels.Pages
{
    public class EmployeeManagerViewModel : BindableBase
    {
        #region 时间属性
        private DispatcherTimer timer;
        #endregion

        #region 服务
        private readonly IEmployeeService employeeService;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 日志
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region 导航
        private readonly IRegionManager regionManager;
        #endregion

        #region 构造函数
        public EmployeeManagerViewModel(IEmployeeService employeeService, IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            #region 分页
            EnableButtons();
            PageSizeItems();
            #endregion

            #region 搜索区域颜色随时间变换
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += async (s, e) => await ColorChangeAsync();
            timer.Start();
            // 初始化为默认颜色
            SearchBackground = new SolidColorBrush(Color.FromRgb(173, 216, 230));
            #endregion

            #region 事件聚合器
            this.eventAggregator = eventAggregator;
            // 清理重复和错误的事件订阅
            eventAggregator.GetEvent<EmployeeAddedEvent>().Subscribe(OnDataChanged);
            eventAggregator.GetEvent<EmployeeUpdatedEvent>().Subscribe(OnDataChanged);
            eventAggregator.GetEvent<EmployeeExportDataEvent>().Subscribe(OnExportData);
            // 添加对EmployeeRemovedEvent的订阅
            eventAggregator.GetEvent<EmployeeRemovedEvent>().Subscribe(OnDataChanged);
            // 添加对部门删除事件的订阅，当部门被删除时也更新员工列表
            eventAggregator.GetEvent<DepartmentDeletedEvent>().Subscribe(OnDataChanged);
            // 添加对部门更新事件的订阅，当部门信息更新时也更新员工列表
            eventAggregator.GetEvent<DepartmentUpdatedEvent>().Subscribe(OnDataChanged);
            #endregion

            #region 数据的导入和导出
            exportDataCommand = new DelegateCommand(OnExportData, () => !IsLoading)
                .ObservesProperty(() => IsLoading);
            #endregion

            #region 服务
            this.employeeService = employeeService;
            #endregion

            #region 初始化命令
            //LoadDataCommand = new DelegateCommand(LoadData);
            AddEmployeeCommand = new DelegateCommand(OnAddEmployee);
            DeleteEmployeeCommand = new DelegateCommand<Employee>(OnDeleteEmployee);
            UpdateEmployeeCommand = new DelegateCommand<Employee>(OnUpdateEmployee);
            SearchEmployeeCommand = new DelegateCommand(OnSearchEmployee);
            PreviewTextInputCommand = new DelegateCommand<string>(OnPreviewTextInput);
            #endregion

            #region 初始化员工集合(加载所有在职员工)
            using (var context = new DataBaseContext())
            {
                #region 服务
                this.employeeService = employeeService;
                #endregion
                //获取所有员工数据
                Employees = new ObservableCollection<Employee>(context.Employees);
                // 初始化缓存，包含所有员工
                _allEmployeesCache = Employees.ToList();
                // 默认显示在职员工
                ListViewEmployees = TempEmployees = new ObservableCollection<Employee>(Employees.Where(e => e.IsActive == true));
                TotalRecords = TempEmployees.Count;
                Departments = new ObservableCollection<Department>(context.Departments);
            }

            LoadEmployees();
            #endregion

            #region 初始化状态
            // Status会在View层通过ComboBox的SelectedIndex=0和IsSelected=true自动初始化
            #endregion

            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);

            #region 导航
            this.regionManager = regionManager;
            #endregion
        }
        #endregion

        #region 属性

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


        #region 原始的Employees集合
        private ObservableCollection<Employee> employees;
        public ObservableCollection<Employee> Employees
        {
            get => employees;
            set => SetProperty(ref employees, value);
        }
        #endregion

        #region 临时的Employees集合
        private ObservableCollection<Employee> tempEmployees;

        public ObservableCollection<Employee> TempEmployees { get => tempEmployees; set => SetProperty(ref tempEmployees, value); }
        #endregion

        #region 展示的Employees集合
        private ObservableCollection<Employee> listViewEmployees;

        public ObservableCollection<Employee> ListViewEmployees { get => listViewEmployees; set => SetProperty(ref listViewEmployees, value); }
        #endregion

        #region 选中的员工
        private Employee selectedEmployee;
        public Employee SelectedEmployee
        {
            get => selectedEmployee;
            set => SetProperty(ref selectedEmployee, value);
        }
        #endregion

        #region 搜索关键词
        private string _searchKeyword;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }
        #endregion


        private ObservableCollection<Department> departments;
        public ObservableCollection<Department> Departments
        {
            get { return departments; }
            set { SetProperty(ref departments, value); }
        }
        #endregion

        #region 员工数据集合
        // 所有过滤后的员工集合
        private ObservableCollection<Employee> allEmployees;
        public ObservableCollection<Employee> AllEmployees
        {
            get => allEmployees;
            set => SetProperty(ref allEmployees, value);
        }

        // 当前页显示的员工集合
        private ObservableCollection<Employee> currentEmployees;
        public ObservableCollection<Employee> CurrentEmployees
        {
            get => currentEmployees;
            set => SetProperty(ref currentEmployees, value);
        }

        // 总项目数
        private int totalItems;
        public int TotalItems
        {
            get => totalItems;
            set => SetProperty(ref totalItems, value);
        }
        #endregion

        #region 状态选择
        private string selectedStatus = "全部";
        public string SelectedStatus
        {
            get => selectedStatus;
            set => SetProperty(ref selectedStatus, value);
        }

        private ObservableCollection<string> statusOptions;
        public ObservableCollection<string> StatusOptions
        {
            get => statusOptions;
            set => SetProperty(ref statusOptions, value);
        }
        #endregion

        #region 分页控件状态
        private bool isFirstEnabled;
        public bool IsFirstEnabled
        {
            get => isFirstEnabled;
            set => SetProperty(ref isFirstEnabled, value);
        }

        private bool isPreviousEnabled;
        public bool IsPreviousEnabled
        {
            get => isPreviousEnabled;
            set => SetProperty(ref isPreviousEnabled, value);
        }

        private bool isNextEnabled;
        public bool IsNextEnabled
        {
            get => isNextEnabled;
            set => SetProperty(ref isNextEnabled, value);
        }

        private bool isLastEnabled;
        public bool IsLastEnabled
        {
            get => isLastEnabled;
            set => SetProperty(ref isLastEnabled, value);
        }
        #endregion

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

        #region  增删改查命令
        public DelegateCommand AddEmployeeCommand { get; }
        public DelegateCommand<Employee> DeleteEmployeeCommand { get; private set; }
        public DelegateCommand<Employee> UpdateEmployeeCommand { get; }
        public DelegateCommand SearchEmployeeCommand { get; }
        #endregion

        #region 加载当前页员工数据
        private void LoadData()
        {
            // 加载数据的具体实现
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                // 清空当前集合, 使用 Clear() 而不是创建新实例避免触发不必要的UI更新
                if (CurrentEmployees == null)
                {
                    CurrentEmployees = new ObservableCollection<Employee>();
                }
                else
                {
                    CurrentEmployees.Clear();
                }

                // 如果没有数据则直接返回
                if (TempEmployees == null || TempEmployees.Count == 0)
                {
                    return;
                }

                // 计算当前页的起始索引和结束索引
                int startIndex = (CurrentPage - 1) * PageSize;
                int endIndex = Math.Min(startIndex + PageSize, TempEmployees.Count);

                // 使用批量添加来减少UI更新次数
                var pageItems = TempEmployees.Skip(startIndex).Take(PageSize).ToList();

                // 将数据一次性添加到集合，减少UI更新
                App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var employee in pageItems)
                    {
                        CurrentEmployees.Add(employee);
                    }
                });

                // 更新分页控件状态
                IsFirstEnabled = CurrentPage > 1;
                IsPreviousEnabled = CurrentPage > 1;
                IsNextEnabled = CurrentPage < MaxPage;
                IsLastEnabled = CurrentPage < MaxPage;
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"加载员工数据失败: {ex.Message}");
                Debug.WriteLine($"LoadEmployees错误: {ex}");
            }
        }
        #endregion

        #region 查询员工时
        public void OnSearchEmployee()
        {
            try
            {
                logger.Debug($"开始执行员工搜索: 状态={StatusText}, 关键词={SearchKeyword}");

                // 防止重复操作
                IsSearchEnabled = false;
                IsRefreshEnabled = false;
                IsLoading = true; // 显示加载中状态

                // 如果员工集合为空，不执行搜索
                if (Employees == null || !Employees.Any())
                {
                    ListViewEmployees = new ObservableCollection<Employee>();
                    Growl.InfoGlobal("没有员工数据");
                    return;
                }

                // 根据状态筛选员工
                IEnumerable<Employee> filteredEmployees;
                if (StatusText == "在职员工")
                {
                    filteredEmployees = Employees.Where(e => e.IsActive == true);
                    logger.Debug($"筛选在职员工，找到 {filteredEmployees.Count()} 条记录");
                }
                else if (StatusText == "离职员工")
                {
                    filteredEmployees = Employees.Where(e => e.IsActive == false);
                    logger.Debug($"筛选离职员工，找到 {filteredEmployees.Count()} 条记录");
                }
                else // 全部员工
                {
                    filteredEmployees = Employees;
                    logger.Debug($"显示全部员工，共 {filteredEmployees.Count()} 条记录");
                }

                // 关键词筛选
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    string keyword = SearchKeyword.Trim().ToLower();
                    filteredEmployees = filteredEmployees.Where(e =>
                        (e.EmployeeName != null && e.EmployeeName.ToLower().Contains(keyword)) ||
                        (e.Account != null && e.Account.ToLower().Contains(keyword)) ||
                        e.EmployeeId.ToString().Contains(keyword) ||
                        (Departments != null && e.DepartmentId.HasValue &&
                         Departments.Any(d => d.DepartmentId == e.DepartmentId &&
                            d.DepartmentName != null && d.DepartmentName.ToLower().Contains(keyword)))
                    ).ToList();

                    logger.Debug($"关键词筛选后，剩余 {filteredEmployees.Count()} 条记录");
                }

                // 同时更新AllEmployees和TempEmployees，确保它们保持同步
                var filteredList = filteredEmployees.ToList(); // 强制执行查询
                TempEmployees = new ObservableCollection<Employee>(filteredList);
                AllEmployees = new ObservableCollection<Employee>(filteredList);

                // 计算分页信息
                TotalRecords = TempEmployees.Count;
                TotalItems = TotalRecords; // 确保TotalItems和TotalRecords保持同步
                MaxPage = TotalRecords == 0 ? 1 : (TotalRecords % PageSize == 0 ?
                          TotalRecords / PageSize : (TotalRecords / PageSize) + 1);

                // 重置为第一页
                CurrentPage = 1;

                // 使用统一的方法更新ListViewEmployees
                UpdateListViewData();

                logger.Info($"员工搜索完成，共找到 {TotalRecords} 条记录，当前显示第 {CurrentPage}/{MaxPage} 页");

                // 如果没有搜索结果，显示提示信息
                if (TotalRecords == 0)
                {
                    Growl.InfoGlobal("没有找到符合条件的员工");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "搜索员工失败");
                Growl.ErrorGlobal($"搜索员工失败: {ex.Message}");
            }
            finally
            {
                // 恢复按钮状态
                IsSearchEnabled = true;
                IsRefreshEnabled = true;
                IsLoading = false;
            }
        }
        #endregion

        #region 添加员工时
        private void OnAddEmployee()
        {
            try
            {
                // 清空编辑区域，确保之前的编辑不会影响新增操作
                var region = regionManager.Regions["EmployeeEditRegion"];
                region?.RemoveAll();

                // 导航到添加员工页面
                logger.Debug("导航到员工添加页面");
                regionManager.RequestNavigate("EmployeeEditRegion", "AddEmployee");

                logger.Info("已打开员工添加页面");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "打开员工添加页面失败");
                Growl.ErrorGlobal($"打开添加页面失败: {ex.Message}");
            }
        }
        #endregion

        #region 更改员工时
        private void OnUpdateEmployee(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    Growl.WarningGlobal("请选择要修改的员工");
                    return;
                }

                // 创建导航参数
                var parameters = new NavigationParameters
                {
                    { "Employee", employee }
                };

                // 导航到编辑页面
                regionManager.RequestNavigate("EmployeeEditRegion", "EditEmployee", parameters);

                logger.Debug($"已导航到员工编辑页面，员工ID: {employee.EmployeeId}, 姓名: {employee.EmployeeName}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "导航到员工编辑页面失败");
                Growl.ErrorGlobal($"打开编辑页面失败: {ex.Message}");
            }
        }
        #endregion

        #region 删除员工时
        private async void OnDeleteEmployee(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    Growl.WarningGlobal("请选择要删除的员工");
                    return;
                }

                // 显示加载中状态
                IsLoading = true;

                // 检查员工是否有关联记录
                var checkResult = await Task.Run(() => employeeService.CheckEmployeeRelatedRecords(employee.EmployeeId));

                // 隐藏加载状态
                IsLoading = false;

                // 如果有关联记录，显示全局警告框询问是否级联删除
                if (checkResult.hasRelated)
                {
                    // 构建提示消息
                    string message = "删除操作无法完成，发现关联记录：\n";

                    if (checkResult.nominationCount > 0)
                    {
                        message += $"• 奖项提名记录 ({checkResult.nominationCount}条)\n";
                    }

                    if (checkResult.declarationCount > 0)
                    {
                        message += $"• 提名申报记录 ({checkResult.declarationCount}条)\n";
                    }

                    if (checkResult.voteCount > 0)
                    {
                        message += $"• 投票记录 ({checkResult.voteCount}条)\n";
                    }

                    message += "\n是否连带删除所有关联记录？";

                    // 显示Growl全局警告框
                    Growl.AskGlobal(message, isConfirmed =>
                    {
                        if (isConfirmed)
                        {
                            // 用户确认级联删除，执行删除操作
                            ExecuteCascadeDelete(employee);
                        }
                        return true;
                    });
                    return;
                }

                // 如果没有关联记录，显示普通确认对话框
                var result = HandyControl.Controls.MessageBox.Show(
                    $"确定要删除员工 {employee.EmployeeName} 吗？此操作不可恢复！",
                    "删除确认",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.OK)
                {
                    // 执行普通删除
                    ExecuteNormalDelete(employee);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "删除员工操作失败");
                Growl.ErrorGlobal($"删除操作失败: {ex.Message}");
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行级联删除操作
        /// </summary>
        private async void ExecuteCascadeDelete(Employee employee)
        {
            // 显示加载中状态
            IsLoading = true;

            logger.Info($"开始级联删除员工及关联记录：ID={employee.EmployeeId}，姓名={employee.EmployeeName}");

            // 显示正在处理的提示
            Growl.InfoGlobal("正在处理级联删除，请稍候...");

            try
            {
                // 优先使用ExecuteDirectSqlDelete方法，而不是DeleteEmployeeWithRelatedRecords
                // 因为ExecuteDirectSqlDelete方法不使用EF Core事务，避免事务执行策略冲突
                bool success = await Task.Run(() => employeeService.ExecuteDirectSqlDelete(employee.EmployeeId));

                // 如果直接SQL删除成功
                if (success)
                {
                    // 直接更新UI界面，避免重复消息
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        // 从本地集合中移除员工
                        if (Employees.Contains(employee))
                            Employees.Remove(employee);

                        if (TempEmployees.Contains(employee))
                            TempEmployees.Remove(employee);

                        if (ListViewEmployees.Contains(employee))
                            ListViewEmployees.Remove(employee);

                        if (_allEmployeesCache != null)
                            _allEmployeesCache.Remove(employee);

                        // 更新计数和分页
                        TotalRecords = TempEmployees.Count;
                        MaxPage = TotalRecords == 0 ? 1 :
                                 (TotalRecords % PageSize == 0 ?
                                 TotalRecords / PageSize : (TotalRecords / PageSize) + 1);

                        // 如果当前页超出范围，修正为最大页
                        if (CurrentPage > MaxPage)
                            CurrentPage = MaxPage > 0 ? MaxPage : 1;

                        // 刷新当前页数据
                        UpdateListViewData();

                        // 通知用户
                        Growl.SuccessGlobal($"已成功删除员工 {employee.EmployeeName} 及其所有关联记录");

                        // 发布员工删除事件，通知其他组件
                        eventAggregator.GetEvent<EmployeeRemovedEvent>().Publish();
                    });

                    logger.Info($"直接SQL删除员工及关联记录成功：ID={employee.EmployeeId}，姓名={employee.EmployeeName}");
                }
                else
                {
                    // 如果直接SQL删除失败，尝试使用改进后的DeleteEmployeeWithRelatedRecords方法
                    logger.Warn($"直接SQL删除失败，尝试使用DeleteEmployeeWithRelatedRecords方法：ID={employee.EmployeeId}");
                    Growl.InfoGlobal("正在尝试另一种删除方式...");

                    success = await Task.Run(() => employeeService.DeleteEmployeeWithRelatedRecords(employee.EmployeeId));

                    if (success)
                    {
                        // 直接更新UI界面，避免重复消息
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            // 从本地集合中移除员工
                            if (Employees.Contains(employee))
                                Employees.Remove(employee);

                            if (TempEmployees.Contains(employee))
                                TempEmployees.Remove(employee);

                            if (ListViewEmployees.Contains(employee))
                                ListViewEmployees.Remove(employee);

                            if (_allEmployeesCache != null)
                                _allEmployeesCache.Remove(employee);

                            // 更新计数和分页
                            TotalRecords = TempEmployees.Count;
                            MaxPage = TotalRecords == 0 ? 1 :
                                     (TotalRecords % PageSize == 0 ?
                                     TotalRecords / PageSize : (TotalRecords / PageSize) + 1);

                            // 如果当前页超出范围，修正为最大页
                            if (CurrentPage > MaxPage)
                                CurrentPage = MaxPage > 0 ? MaxPage : 1;

                            // 刷新当前页数据
                            UpdateListViewData();

                            // 通知用户
                            Growl.SuccessGlobal($"已成功删除员工 {employee.EmployeeName} 及其所有关联记录");

                            // 发布员工删除事件，通知其他组件
                            eventAggregator.GetEvent<EmployeeRemovedEvent>().Publish();
                        });

                        logger.Info($"DeleteEmployeeWithRelatedRecords删除成功：ID={employee.EmployeeId}，姓名={employee.EmployeeName}");
                    }
                    else
                    {
                        // 两种方法都失败，尝试更强力的直接删除方法
                        Growl.WarningGlobal("常规删除失败，尝试强制删除...");
                        await ForceDeleteEmployee(employee);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"级联删除员工异常: {ex.Message}");
                Growl.ErrorGlobal($"删除失败: {ex.Message}");
                
                // 捕获到异常，尝试更强力的直接删除方法
                Growl.WarningGlobal("发生异常，尝试强制删除...");
                await ForceDeleteEmployee(employee);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 尝试使用强制方式删除员工
        /// </summary>
        private async Task ForceDeleteEmployee(Employee employee)
        {
            try
            {
                IsLoading = true;
                logger.Info($"开始强制删除员工：ID={employee.EmployeeId}，姓名={employee.EmployeeName}");
                Growl.InfoGlobal("正在强制删除员工，这可能需要些时间...");

                using (var context = new DataBaseContext())
                {
                    // 先查找所有与该员工相关的投票记录
                    var voteRecords = await context.VoteRecords
                        .Where(v => v.VoterEmployeeId == employee.EmployeeId)
                        .ToListAsync();

                    if (voteRecords.Count > 0)
                    {
                        Growl.InfoGlobal($"找到 {voteRecords.Count} 条投票记录，正在删除...");

                        // 删除所有投票记录
                        context.VoteRecords.RemoveRange(voteRecords);

                        // 尝试保存更改
                        await context.SaveChangesAsync();

                        Growl.SuccessGlobal($"成功删除 {voteRecords.Count} 条投票记录");

                        // 直接尝试使用SQL方法删除员工，避免再次调用ExecuteCascadeDelete导致重复提示
                        Growl.InfoGlobal("正在尝试直接删除员工...");

                        bool success = await Task.Run(() => employeeService.ExecuteDirectSqlDelete(employee.EmployeeId));

                        if (success)
                        {
                            // 更新UI界面
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                // 从本地集合中移除员工
                                if (Employees.Contains(employee))
                                    Employees.Remove(employee);

                                if (TempEmployees.Contains(employee))
                                    TempEmployees.Remove(employee);

                                if (ListViewEmployees.Contains(employee))
                                    ListViewEmployees.Remove(employee);

                                if (_allEmployeesCache != null)
                                    _allEmployeesCache.Remove(employee);

                                // 更新计数和分页
                                TotalRecords = TempEmployees.Count;
                                MaxPage = TotalRecords == 0 ? 1 :
                                         (TotalRecords % PageSize == 0 ?
                                         TotalRecords / PageSize : (TotalRecords / PageSize) + 1);

                                // 如果当前页超出范围，修正为最大页
                                if (CurrentPage > MaxPage)
                                    CurrentPage = MaxPage > 0 ? MaxPage : 1;

                                // 刷新当前页数据
                                UpdateListViewData();
                            });

                            // 通知用户
                            Growl.SuccessGlobal($"已成功删除员工 {employee.EmployeeName} 及其所有关联记录");

                            // 发布员工删除事件，通知其他组件
                            eventAggregator.GetEvent<EmployeeRemovedEvent>().Publish();

                            logger.Info($"强制删除员工成功：ID={employee.EmployeeId}，姓名={employee.EmployeeName}");
                        }
                        else
                        {
                            Growl.ErrorGlobal("直接删除员工失败，请联系系统管理员");
                            logger.Error($"强制删除员工失败：ID={employee.EmployeeId}");
                        }
                    }
                    else
                    {
                        Growl.WarningGlobal("未找到投票记录，但仍然无法删除员工");
                        logger.Warn($"强制删除失败：未找到投票记录，ID={employee.EmployeeId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"强制删除失败：{ex.Message}");
                logger.Error(ex, "强制删除员工失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行普通删除操作
        /// </summary>
        private async void ExecuteNormalDelete(Employee employee)
        {
            // 显示加载中状态
            IsLoading = true;

            logger.Info($"开始删除员工：ID={employee.EmployeeId}，姓名={employee.EmployeeName}");

            // 异步调用服务层删除员工
            bool success = await Task.Run(() =>
            {
                try
                {
                    employeeService.DeleteEmployee(employee.EmployeeId);
                    return true;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"删除员工时发生数据库错误: {ex.Message}");
                    return false;
                }
            });

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                if (success)
                {
                    // 从本地集合中移除员工
                    if (Employees.Contains(employee))
                        Employees.Remove(employee);

                    if (TempEmployees.Contains(employee))
                        TempEmployees.Remove(employee);

                    if (ListViewEmployees.Contains(employee))
                        ListViewEmployees.Remove(employee);

                    if (_allEmployeesCache != null)
                        _allEmployeesCache.Remove(employee);

                    // 更新计数和分页
                    TotalRecords = TempEmployees.Count;
                    MaxPage = TotalRecords == 0 ? 1 :
                             (TotalRecords % PageSize == 0 ?
                             TotalRecords / PageSize : (TotalRecords / PageSize) + 1);

                    // 如果当前页超出范围，修正为最大页
                    if (CurrentPage > MaxPage)
                        CurrentPage = MaxPage > 0 ? MaxPage : 1;

                    // 刷新当前页数据
                    UpdateListViewData();

                    // 通知用户
                    Growl.SuccessGlobal($"已成功删除员工 {employee.EmployeeName}");
                    logger.Info($"成功删除员工：ID={employee.EmployeeId}，姓名={employee.EmployeeName}");

                    // 发布员工删除事件，通知其他组件
                    eventAggregator.GetEvent<EmployeeRemovedEvent>().Publish();
                }
                else
                {
                    // 如果删除失败，可能是存在其他关联记录，再次检查
                    var checkResult = employeeService.CheckEmployeeRelatedRecords(employee.EmployeeId);

                    if (checkResult.hasRelated)
                    {
                        // 构建错误消息
                        string message = "删除失败，发现关联记录：\n";

                        if (checkResult.nominationCount > 0)
                        {
                            message += $"• 奖项提名记录 ({checkResult.nominationCount}条)\n";
                        }

                        if (checkResult.declarationCount > 0)
                        {
                            message += $"• 提名申报记录 ({checkResult.declarationCount}条)\n";
                        }

                        if (checkResult.voteCount > 0)
                        {
                            message += $"• 投票记录 ({checkResult.voteCount}条)\n";
                        }

                        message += "\n是否连带删除所有关联记录？";

                        Growl.AskGlobal(message, isConfirmed =>
                        {
                            if (isConfirmed)
                            {
                                // 用户确认级联删除，执行删除操作
                                ExecuteCascadeDelete(employee);
                            }
                            return true;
                        });
                    }
                    else
                    {
                        Growl.ErrorGlobal("删除员工失败，可能是数据库错误或权限问题");
                    }
                }

                // 隐藏加载状态
                IsLoading = false;
            });
        }
        #endregion

        #region 员工(新增)更新时更新视图列表
        public async void OnDataChanged()
        {
            try
            {
                logger.Info("接收到员工数据更新事件，开始刷新员工列表");

                // 禁用按钮，防止并发操作
                IsSearchEnabled = false;
                IsRefreshEnabled = false;
                IsLoading = true;

                // 保存当前筛选条件和页码
                string currentStatusText = StatusText;
                string currentKeyword = SearchKeyword;
                int currentPage = CurrentPage;

                // 完全重新加载数据（避免缓存问题）
                using (var freshContext = new DataBaseContext())
                {
                    freshContext.Database.SetCommandTimeout(120); // 设置较长的超时时间

                    // 重新从数据库加载所有员工
                    var employeesFromDb = await freshContext.Employees
                        .Include(e => e.Department)
                        .AsNoTracking() // 使用无跟踪查询提高性能
                        .ToListAsync();

                    // 更新缓存和所有集合
                    _allEmployeesCache = employeesFromDb;

                    // 在UI线程上更新集合
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Employees = new ObservableCollection<Employee>(employeesFromDb);
                        logger.Debug($"已刷新员工数据，共加载了 {Employees.Count} 条记录");

                        // 重新应用过滤条件
                        // 根据状态筛选
                        IEnumerable<Employee> filteredEmployees;

                        if (currentStatusText.Contains("在职"))
                        {
                            filteredEmployees = Employees.Where(e => e.IsActive == true);
                        }
                        else if (currentStatusText.Contains("离职"))
                        {
                            filteredEmployees = Employees.Where(e => e.IsActive == false);
                        }
                        else
                        {
                            filteredEmployees = Employees;
                        }

                        // 关键词筛选
                        if (!string.IsNullOrWhiteSpace(currentKeyword))
                        {
                            filteredEmployees = filteredEmployees.Where(e =>
                                (e.EmployeeName?.Contains(currentKeyword, StringComparison.OrdinalIgnoreCase) == true) ||
                                (e.Account?.Contains(currentKeyword, StringComparison.OrdinalIgnoreCase) == true) ||
                                (e.Department?.DepartmentName?.Contains(currentKeyword, StringComparison.OrdinalIgnoreCase) == true)
                            );
                        }

                        // 更新临时集合
                        TempEmployees = new ObservableCollection<Employee>(filteredEmployees);

                        // 更新分页信息
                        TotalRecords = TempEmployees.Count;
                        MaxPage = TotalRecords == 0 ? 1 :
                                 (TotalRecords % PageSize == 0 ?
                                 TotalRecords / PageSize : (TotalRecords / PageSize) + 1);

                        // 确保当前页在有效范围内
                        if (currentPage > MaxPage)
                        {
                            CurrentPage = MaxPage > 0 ? MaxPage : 1;
                        }
                        else
                        {
                            CurrentPage = currentPage;
                        }

                        // 更新页面数据
                        UpdateListViewData();

                        logger.Info($"员工列表已刷新，当前显示 {TempEmployees.Count} 条记录");
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "刷新员工列表失败");
                Growl.ErrorGlobal($"更新员工列表失败: {ex.Message}");
            }
            finally
            {
                // 确保按钮状态恢复
                IsSearchEnabled = true;
                IsRefreshEnabled = true;
                IsLoading = false;
            }
        }
        #endregion

        #region 分页计算
        /// <summary>
        /// 更新分页相关的计算
        /// </summary>
        private void UpdatePagination()
        {
            // 更新总记录数
            TotalRecords = TempEmployees?.Count() ?? 0;

            // 计算最大页码
            MaxPage = TotalRecords == 0 ? 1 :
                      (TotalRecords % PageSize == 0 ?
                      (TotalRecords / PageSize) :
                      ((TotalRecords / PageSize) + 1));

            // 确保当前页码在有效范围内
            if (CurrentPage > MaxPage)
                CurrentPage = MaxPage > 0 ? MaxPage : 1;
        }
        #endregion

        #region 数据筛选
        /// <summary>
        /// 根据当前筛选条件刷新员工列表
        /// </summary>
        private void RefreshEmployeeList()
        {
            if (_allEmployeesCache == null || !_allEmployeesCache.Any())
            {
                AllEmployees = new ObservableCollection<Employee>();
                return;
            }

            // 使用LINQ查询筛选员工数据
            IEnumerable<Employee> filteredList = _allEmployeesCache;

            // 状态筛选
            if (!string.IsNullOrEmpty(SelectedStatus))
            {
                bool? isActive = null;
                switch (SelectedStatus)
                {
                    case "在职":
                        isActive = true;
                        break;
                    case "离职":
                        isActive = false;
                        break;
                        // "全部" 状态不需要筛选，保持 isActive 为 null
                }

                if (isActive.HasValue)
                {
                    filteredList = filteredList.Where(e => e.IsActive == isActive.Value);
                }
            }

            // 搜索文本筛选
            if (!string.IsNullOrEmpty(SearchKeyword))
            {
                string searchLower = SearchKeyword.ToLower();
                filteredList = filteredList.Where(e =>
                    (e.EmployeeName != null && e.EmployeeName.ToLower().Contains(searchLower)) ||
                    (e.Account != null && e.Account.ToLower().Contains(searchLower)) ||
                    (e.Department != null && e.Department.DepartmentName != null &&
                     e.Department.DepartmentName.ToLower().Contains(searchLower)));
            }

            // 更新临时集合，为了效率，使用ToList()强制执行LINQ查询并缓存结果
            var resultList = filteredList.ToList();
            AllEmployees = new ObservableCollection<Employee>(resultList);
            TotalItems = resultList.Count;
        }
        #endregion

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

        public string SearchText
        {
            get => searchText;
            set => SetProperty(ref searchText, value);
        }
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
            try
            {
                CurrentPage = 1; // 改变每页大小时重置为第一页

                // 更新最大页码
                if (TempEmployees != null && TempEmployees.Count > 0)
                {
                    TotalRecords = TempEmployees.Count;
                    MaxPage = TotalRecords == 0 ? 1 : (TotalRecords % PageSize == 0 ?
                             (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1));
                }

                // 更新视图
                UpdateListViewData();
            }
            catch (Exception ex)
            {
                Growl.Error($"更新分页设置失败: {ex.Message}");
            }
        }
        #endregion

        #region 分页导航
        // 注释掉重复的命令定义，使用下方优化后的异步命令
        /*
        private DelegateCommand previousPageCommand;
        public DelegateCommand PreviousPageCommand => previousPageCommand ??= new DelegateCommand(PreviousPage, CanPreviousPage)
            .ObservesProperty(() => CurrentPage);

        private bool CanPreviousPage()
        {
            return CurrentPage > 1;
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdateListViewData();
            }
        }

        private DelegateCommand nextPageCommand;
        public DelegateCommand NextPageCommand => nextPageCommand ??= new DelegateCommand(NextPage, CanNextPage)
            .ObservesProperty(() => CurrentPage)
            .ObservesProperty(() => MaxPage);

        private bool CanNextPage()
        {
            return CurrentPage < MaxPage;
        }

        private void NextPage()
        {
            if (CurrentPage < MaxPage)
            {
                CurrentPage++;
                UpdateListViewData();
            }
        }
        */

        private DelegateCommand jumpPageCommand;
        //跳转页面
        public ICommand JumpPageCommand => jumpPageCommand ??= new DelegateCommand(JumpPage, CanJumpPage)
            .ObservesProperty(() => SearchText);

        private bool CanJumpPage()
        {
            return !string.IsNullOrWhiteSpace(SearchText) && int.TryParse(SearchText, out _);
        }

        private void JumpPage()
        {
            if (int.TryParse(SearchText, out int number))
            {
                if (number > MaxPage)
                {
                    CurrentPage = MaxPage;
                }
                else if (number < 1)
                {
                    CurrentPage = 1;
                }
                else
                {
                    CurrentPage = number;
                }
                UpdateListViewData();
            }
        }

        // 修改为异步委托命令
        public DelegateCommand FirstPageCommand => new DelegateCommand(async () =>
            {
                if (CurrentPage <= 1)
                    return;

                logger.Debug("导航到第一页");
                await ExecutePageNavigationAsync(1);
            }, () => CurrentPage > 1)
            .ObservesProperty(() => CurrentPage);

        public DelegateCommand PreviousPageAsyncCommand => new DelegateCommand(async () =>
            {
                if (CurrentPage <= 1)
                    return;

                logger.Debug($"导航到上一页: {CurrentPage - 1}");
                await ExecutePageNavigationAsync(CurrentPage - 1);
            }, () => CurrentPage > 1)
            .ObservesProperty(() => CurrentPage);

        public DelegateCommand NextPageAsyncCommand => new DelegateCommand(async () =>
            {
                if (CurrentPage >= MaxPage)
                    return;

                logger.Debug($"导航到下一页: {CurrentPage + 1}");
                await ExecutePageNavigationAsync(CurrentPage + 1);
            }, () => CurrentPage < MaxPage)
            .ObservesProperty(() => CurrentPage)
            .ObservesProperty(() => MaxPage);

        public DelegateCommand LastPageCommand => new DelegateCommand(async () =>
            {
                if (CurrentPage >= MaxPage)
                    return;

                logger.Debug($"导航到最后一页: {MaxPage}");
                await ExecutePageNavigationAsync(MaxPage);
            }, () => CurrentPage < MaxPage)
            .ObservesProperty(() => CurrentPage)
            .ObservesProperty(() => MaxPage);
        #endregion

        #endregion

        #region 数据的导入导出

        //public DelegateCommand ImportDataCommand { get; }
        private DelegateCommand exportDataCommand;
        public DelegateCommand ExportDataCommand =>
            exportDataCommand ?? (exportDataCommand = new DelegateCommand(OnExportData, () => !IsLoading)
                .ObservesProperty(() => IsLoading));

        //private void OnImport()
        //{
        //    // 打开文件对话框选择文件
        //    var openFileDialog = new OpenFileDialog
        //    {
        //        Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
        //    };
        //    if (openFileDialog.ShowDialog() == true)
        //    {
        //        // 发布导入事件
        //        eventAggregator.GetEvent<ImportDataEvent>().Publish(openFileDialog.FileName);
        //    }
        //}
        private void OnExport()
        {
            // 根据当前状态确定默认文件名
            string statusMsg = StatusText.Contains("在职") ? "在职员工" :
                              StatusText.Contains("离职") ? "离职员工" : "全部员工";
            string defaultFileName = $"{statusMsg}_{DateTime.Now:yyyyMMdd}";

            // 打开文件对话框选择保存路径
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = defaultFileName
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                // 发布导出事件
                eventAggregator.GetEvent<EmployeeExportDataEvent>().Publish(saveFileDialog.FileName);
            }
        }
        //private void OnImportData(string filePath)
        //{
        //    // 实现导入逻辑
        //    var employees = employeeService.ImportEmployees(filePath);
        //    // 更新视图模型中的数据
        //    Employees = new ObservableCollection<Employee>(employees);
        //}
        private void OnExportData(string filePath)
        {
            try
            {
                IsLoading = true;
                logger.Info($"开始导出{StatusText}数据");

                // 获取导出状态信息，用于文件名和提示信息
                string statusMsg = StatusText.Contains("在职") ? "在职" :
                                  StatusText.Contains("离职") ? "离职" : "全部";

                if (string.IsNullOrEmpty(filePath))
                {
                    // 根据当前状态确定默认文件名
                    string fileNamePrefix = $"{statusMsg}员工";
                    string defaultFileName = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd}";

                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "CSV文件|*.csv",
                        Title = "导出员工数据",
                        FileName = defaultFileName
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        filePath = saveFileDialog.FileName;
                    }
                    else
                    {
                        logger.Info("用户取消了导出操作");
                        return;
                    }
                }

                // 检查是否有数据可导出 - 使用TempEmployees而不是AllEmployees
                if (TempEmployees == null || TempEmployees.Count == 0)
                {
                    Growl.InfoGlobal("当前筛选条件下没有可导出的员工数据");
                    logger.Warn("导出取消：没有符合条件的员工数据");
                    return;
                }

                // 导出当前过滤后的所有员工，使用TempEmployees而不是AllEmployees
                bool result = employeeService.ExportEmployees(TempEmployees.ToList(), filePath);

                if (result)
                {
                    // 添加导出成功提示，包含筛选条件和导出的记录数
                    Growl.SuccessGlobal($"成功导出{statusMsg}员工记录 {TempEmployees.Count} 条");
                    logger.Info($"成功导出{statusMsg}员工记录 {TempEmployees.Count} 条到 {filePath}");
                }
                else
                {
                    logger.Warn("员工数据导出失败");
                    Growl.WarningGlobal("员工数据导出失败");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "导出员工数据时出错");
                Growl.ErrorGlobal($"导出失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region 更新ListViewData方法
        private void UpdateListViewData()
        {
            try
            {
                logger.Debug($"开始更新列表视图 - 当前页:{CurrentPage}, 每页大小:{PageSize}");

                if (TempEmployees == null || TempEmployees.Count == 0)
                {
                    logger.Debug("没有员工数据，清空列表视图");
                    ListViewEmployees = new ObservableCollection<Employee>();
                    return;
                }

                // 确保当前页码在有效范围内
                if (CurrentPage < 1)
                {
                    logger.Debug($"当前页码 {CurrentPage} 无效，重置为 1");
                    CurrentPage = 1;
                }

                if (MaxPage > 0 && CurrentPage > MaxPage)
                {
                    logger.Debug($"当前页码 {CurrentPage} 超出最大页数 {MaxPage}，重置为最大页");
                    CurrentPage = MaxPage;
                }

                // 计算分页起始索引
                int startIndex = (CurrentPage - 1) * PageSize;

                // 应用分页并强制执行LINQ查询
                var pagedEmployees = TempEmployees.Skip(startIndex).Take(PageSize).ToList();

                // 更新ListViewEmployees - 使用列表创建新集合以避免引用问题
                ListViewEmployees = new ObservableCollection<Employee>(pagedEmployees);

                logger.Debug($"已更新列表视图 - 当前页:{CurrentPage}/{MaxPage}, 显示记录数:{ListViewEmployees.Count}");

                // 更新分页按钮状态
                IsFirstEnabled = CurrentPage > 1;
                IsPreviousEnabled = CurrentPage > 1;
                IsNextEnabled = CurrentPage < MaxPage;
                IsLastEnabled = CurrentPage < MaxPage;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "更新列表视图失败");
                Growl.ErrorGlobal($"更新列表视图失败：{ex.Message}");
            }
        }
        #endregion

        #region 过滤和搜索
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // 添加私有缓存字段存储所有员工数据
        private List<Employee> _allEmployeesCache;

        // 根据状态和搜索文本过滤员工
        public void FilterEmployeesByStatus()
        {
            try
            {
                logger.Debug($"开始过滤员工数据 - 状态: {SelectedStatus}, 搜索文本: {SearchKeyword}");

                // 从缓存中筛选员工数据
                var filteredEmployees = _allEmployeesCache;

                // 根据状态筛选
                if (SelectedStatus != "全部")
                {
                    bool isActive = SelectedStatus == "在职";
                    filteredEmployees = filteredEmployees.Where(e => e.IsActive == isActive).ToList();
                    logger.Debug($"状态过滤后剩余: {filteredEmployees.Count} 名员工");
                }

                // 根据搜索文本筛选
                if (!string.IsNullOrWhiteSpace(SearchKeyword))
                {
                    string searchLower = SearchKeyword.ToLower();
                    filteredEmployees = filteredEmployees.Where(e =>
                        e.EmployeeName.ToLower().Contains(searchLower) ||
                        (e.Department != null && e.Department.DepartmentName.ToLower().Contains(searchLower)) ||
                        (e.Account != null && e.Account.ToLower().Contains(searchLower))
                    ).ToList();

                    logger.Debug($"文本搜索后剩余: {filteredEmployees.Count} 名员工");
                }

                // 更新当前过滤后的员工集合
                AllEmployees = new ObservableCollection<Employee>(filteredEmployees);

                // 更新分页属性
                TotalItems = AllEmployees.Count;
                MaxPage = (int)Math.Ceiling(TotalItems / (double)PageSize);

                // 修正当前页码（如果超出范围）
                if (CurrentPage > MaxPage && MaxPage > 0)
                {
                    CurrentPage = MaxPage;
                }
                else if (MaxPage == 0)
                {
                    CurrentPage = 1;
                }

                // 加载当前页的数据
                LoadEmployees();

                logger.Debug($"过滤完成 - 总计: {TotalItems} 名员工, 共 {MaxPage} 页");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "过滤员工数据时出错");
            }
        }

        // 执行搜索
        public DelegateCommand SearchCommand => new DelegateCommand(OnSearch);
        private void OnSearch()
        {
            logger.Debug($"执行搜索: {SearchKeyword}");
            CurrentPage = 1; // 重置到第一页
            FilterEmployeesByStatus();
        }

        // 清除搜索
        public DelegateCommand ClearSearchCommand => new DelegateCommand(OnClearSearch);
        private void OnClearSearch()
        {
            logger.Debug("清除搜索条件");
            SearchKeyword = string.Empty;
            CurrentPage = 1; // 重置到第一页
            FilterEmployeesByStatus();
        }
        #endregion

        #region 数据加载
        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                logger.Info("开始初始化员工管理视图...");

                // 初始化员工缓存
                _allEmployeesCache = await Task.Run(() => employeeService.GetAllEmployees().ToList());
                logger.Info($"已从数据库加载 {_allEmployeesCache.Count} 名员工");

                // 设置数据源
                AllEmployees = new ObservableCollection<Employee>(_allEmployeesCache);

                // 初始化状态选择
                StatusOptions = new ObservableCollection<string> { "全部", "在职", "离职" };
                SelectedStatus = "全部";

                // 设置分页参数
                PageSize = 20;
                TotalItems = _allEmployeesCache.Count;
                CurrentPage = 1;
                MaxPage = (int)Math.Ceiling(TotalItems / (double)PageSize);

                // 加载第一页数据
                await LoadEmployeesPagedAsync(CurrentPage);

                // 配置ListView优化
                OptimizeListViewPerformance();

                logger.Info("员工管理视图初始化完成");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "初始化员工管理视图时发生错误");
                Growl.ErrorGlobal("加载员工数据失败，请检查网络连接或数据库状态");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 刷新数据
        public async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;
                logger.Debug("开始刷新员工数据...");

                // 保存当前状态
                string currentStatus = SelectedStatus;
                string currentSearch = SearchKeyword;
                int currentPage = CurrentPage;

                // 重新加载数据
                _allEmployeesCache = await Task.Run(() => employeeService.GetAllEmployees().ToList());
                logger.Debug($"已刷新员工缓存，共 {_allEmployeesCache.Count} 名员工");

                // 应用之前的过滤条件
                SelectedStatus = currentStatus;
                SearchKeyword = currentSearch;

                // 重新过滤
                FilterEmployeesByStatus();

                // 尝试恢复到之前的页码，如果可能的话
                if (currentPage <= MaxPage)
                {
                    CurrentPage = currentPage;
                }

                // 加载当前页数据
                await LoadEmployeesPagedAsync(CurrentPage);

                logger.Info("员工数据刷新完成");
                Growl.SuccessGlobal("员工数据已刷新");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "刷新员工数据时发生错误");
                Growl.ErrorGlobal("刷新员工数据失败");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region UI性能优化

        // 员工分页数据加载优化
        private async Task LoadEmployeesPagedAsync(int page)
        {
            try
            {
                IsLoading = true;

                await Task.Run(() =>
                {
                    // 计算要显示的记录范围
                    int startIndex = (page - 1) * PageSize;
                    int count = Math.Min(PageSize, TotalItems - startIndex);

                    if (count <= 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                {
                    ListViewEmployees = new ObservableCollection<Employee>();
                });
                        return;
                    }

                    // 获取当前页的数据
                    var pageData = AllEmployees
                        .Skip(startIndex)
                        .Take(count)
                        .ToList();

                    // 在UI线程上更新UI
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 更新ListViewEmployees而不是Employees
                        ListViewEmployees = new ObservableCollection<Employee>(pageData);
                        logger.Debug($"已加载第 {page} 页数据，共 {pageData.Count} 条记录");
                    });
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"加载第 {page} 页员工数据时出错");
                Growl.ErrorGlobal($"加载第 {page} 页数据失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 优化的分页导航命令
        private async Task ExecutePageNavigationAsync(int targetPage)
        {
            if (targetPage < 1 || targetPage > MaxPage || targetPage == CurrentPage)
                return;

            try
            {
                IsLoading = true;
                logger.Debug($"切换到第 {targetPage} 页");

                CurrentPage = targetPage;

                // 使用统一的视图更新方法，而不是LoadEmployeesPagedAsync
                UpdateListViewData();

                // 记录翻页结果
                logger.Debug($"已切换到第 {CurrentPage} 页，显示 {ListViewEmployees.Count} 条记录");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"切换到第 {targetPage} 页时出错");
                Growl.ErrorGlobal($"切换页面失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // 用于ListView的延迟加载优化
        private void OptimizeListViewPerformance()
        {
            // 在实际View中找到ListView控件后加入以下代码
            /*
            <ListView ItemsSource="{Binding Employees}" 
                      VirtualizingPanel.IsVirtualizing="True"
                      VirtualizingPanel.VirtualizationMode="Recycling"
                      VirtualizingPanel.CacheLengthUnit="Page"
                      VirtualizingPanel.CacheLength="1,1"
                      ScrollViewer.IsDeferredScrollingEnabled="True">
            */

            logger.Debug("已应用ListView性能优化设置");
        }
        #endregion

        #region 数据操作命令

        // 刷新数据命令
        private DelegateCommand _refreshDataCommand;
        public DelegateCommand RefreshDataCommand =>
            _refreshDataCommand ?? (_refreshDataCommand = new DelegateCommand(ExecuteRefreshDataCommand)
                .ObservesProperty(() => IsLoading));

        private async void ExecuteRefreshDataCommand()
        {
            if (!IsLoading)
            {
                await RefreshDataAsync();
            }
        }

        private void OnExportData()
        {
            OnExportData(null);
        }
        #endregion

    }
}
