using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using SIASGraduate.Services;
using CsvHelper;
using CsvHelper.Configuration;
using HandyControl.Controls;
using Microsoft.Win32;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SIASGraduate.ViewModels.Pages
{
    public class DepartmentManagerViewModel : BindableBase

    {
        #region 时间属性
        private DispatcherTimer timer;
        #endregion

        #region 服务
        private readonly IDepartmentService departmentService;
        #endregion

        #region 区域管理器
        private IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 构造函数
        public DepartmentManagerViewModel(IDepartmentService departmentService, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            #region 分页
            EnableButtons();
            PageSizeItems();
            #endregion

            #region 颜色随时间变换
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += async (s, e) => await ColorChangeAsync();
            timer.Start();
            // 初始化为默认颜色
            SearchBackground = new SolidColorBrush(Color.FromRgb(173, 216, 230));
            #endregion

            #region 区域管理器
            this.regionManager = regionManager;
            #endregion

            #region 事件聚合器
            this.eventAggregator = eventAggregator;
            // 订阅事件
            eventAggregator.GetEvent<DepartmentAddEvent>().Subscribe(OnDepartmentAdded);
            eventAggregator.GetEvent<DepartmentUpdatedEvent>().Subscribe(OnDepartmentUpdated);
            #endregion

            #region 数据的导入和导出
            ExportDataCommand = new DelegateCommand(OnExport);
            #endregion

            #region 服务
            this.departmentService = departmentService;
            #endregion

            #region 初始化部门集合
            using (var context = new DataBaseContext())
            {

                //获取所有在职员工
                ListViewDepartments = TempDepartments = Departments = new ObservableCollection<Department>(context.Departments);

                TotalRecords = TempDepartments.Count;
            }

            LoadDepartments();
            #endregion

            #region 初始化命令
            AddDepartmentCommand = new DelegateCommand(OnAddDepartment);
            DeleteDepartmentCommand = new DelegateCommand<Department>(OnDeleteDepartment);
            UpdateDepartmentCommand = new DelegateCommand<Department>(OnUpdateDepartment);
            SearchDepartmentCommand = new DelegateCommand(OnSearchDepartment);
            PreviewTextInputCommand = new DelegateCommand<string>(OnPreviewTextInput);
            #endregion

            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);


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

        #region 原始的Department集合
        private ObservableCollection<Department> departments;
        public ObservableCollection<Department> Departments
        {
            get => departments;
            set => SetProperty(ref departments, value);
        }
        #endregion

        #region 临时的Department集合
        private ObservableCollection<Department> tempDepartments;

        public ObservableCollection<Department> TempDepartments { get => tempDepartments; set => SetProperty(ref tempDepartments, value); }
        #endregion

        #region 展示的Department集合
        private ObservableCollection<Department> listViewDepartments;

        public ObservableCollection<Department> ListViewDepartments { get => listViewDepartments; set => SetProperty(ref listViewDepartments, value); }
        #endregion

        #region 选中的部门
        private Department selectedDepartment;
        public Department SelectedDepartment
        {
            get => selectedDepartment;
            set => SetProperty(ref selectedDepartment, value);
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
        public DelegateCommand AddDepartmentCommand { get; }
        public DelegateCommand<Department> DeleteDepartmentCommand { get; }
        public DelegateCommand<Department> UpdateDepartmentCommand { get; }
        public DelegateCommand SearchDepartmentCommand { get; }
        #endregion

        #region 加载所有部门
        public void LoadDepartments()
        {
            TotalRecords = TempDepartments.Count;
            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
            ListViewDepartments = new ObservableCollection<Department>(TempDepartments.Skip((CurrentPage - 1) * PageSize).Take(PageSize));
        }
        #endregion

        #region 查询部门时
        public void OnSearchDepartment()
        {
            using (var context = new DataBaseContext())
            {
                TempDepartments = Departments = new ObservableCollection<Department>(departmentService.GetAllDepartments());
            }

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                TempDepartments = new ObservableCollection<Department>(TempDepartments.Where(e =>
                        (e.DepartmentName != null && e.DepartmentName.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase))
                   ));
            }
            CurrentPage = 1;
            LoadDepartments();
        }
        #endregion

        #region 添加部门时


        private void OnAddDepartment()

        {
            regionManager.RequestNavigate("DepartmentEditRegion", "AddDepartment");
        }
        #endregion

        #region 更改部门时
        private void OnUpdateDepartment(Department department)
        {
            //传入参数
            var parameters = new NavigationParameters()
            {
                { "Department", department }
            };
            regionManager.RequestNavigate("DepartmentEditRegion", "EditDepartment", parameters);
        }
        #endregion

        #region 删除部门时
        private async void OnDeleteDepartment(Department department)
        {
            bool isExist = await Task.Run(() => { return department != null; });
            if (isExist)
            {
                Growl.AskGlobal("确认删除该部门吗?此操作不可逆", (result) =>
                {
                    if (result)
                    {
                        try
                        {
                            // 使用departmentService进行删除操作
                            bool success = departmentService.DeleteDepartment(department.DepartmentId);
                            
                            if (success)
                            {
                                Growl.SuccessGlobal("部门删除成功");
                                // 刷新部门列表
                                using (var context = new DataBaseContext())
                                {
                                    TempDepartments = Departments = new ObservableCollection<Department>(context.Departments);
                                }
                                OnSearchDepartment();
                                
                                // 发布部门删除事件，通知其他视图更新
                                eventAggregator.GetEvent<DepartmentDeletedEvent>().Publish();
                            }
                            else
                            {
                                Growl.ErrorGlobal("部门删除失败");
                            }
                        }
                        catch (Exception ex)
                        {
                            Growl.ErrorGlobal($"部门删除失败: {ex.Message}");
                        }
                    }
                    return true;
                });
            }
        }
        #endregion

        #region 员工(新增)更新时更新视图列表
        private void OnDepartmentAdded()
        {
            OnSearchDepartment();
        }
        private void OnDepartmentUpdated()
        {
            OnSearchDepartment();
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
            LoadDepartments();
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
                LoadDepartments();
            }
        }

        private DelegateCommand nextPageCommand;
        public DelegateCommand NextPageCommand => nextPageCommand ??= new DelegateCommand(NextPage);

        private void NextPage()
        {

            if (CurrentPage < MaxPage)
            {
                CurrentPage++;
                LoadDepartments();
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
                LoadDepartments();
            }
        }
        #endregion

        #endregion

        #region 数据的导出
        public DelegateCommand ExportDataCommand { get; }

        private void OnExport()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("===== 开始部门导出操作 =====");

                // 检查是否有数据可导出
                if (TempDepartments == null || TempDepartments.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("错误: 没有可导出的部门数据");
                    Growl.InfoGlobal("当前没有可导出的部门数据");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"准备导出部门数据，共 {TempDepartments.Count} 条记录");

                // 创建默认文件名
                string defaultFileName = $"部门列表_{DateTime.Now:yyyyMMdd}.csv";
                System.Diagnostics.Debug.WriteLine($"默认文件名: {defaultFileName}");

                // 打开文件对话框选择保存路径
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                    FileName = defaultFileName,
                    Title = "保存部门列表"
                };

                System.Diagnostics.Debug.WriteLine("显示保存文件对话框");

                // 如果用户选择了保存路径
                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        string filePath = saveFileDialog.FileName;
                        System.Diagnostics.Debug.WriteLine($"用户选择的文件路径: {filePath}");

                        // 确保目录存在
                        string directory = Path.GetDirectoryName(filePath);
                        System.Diagnostics.Debug.WriteLine($"文件目录: {directory}");

                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            System.Diagnostics.Debug.WriteLine($"创建目录: {directory}");
                            Directory.CreateDirectory(directory);
                        }

                        System.Diagnostics.Debug.WriteLine("开始创建CSV文件");

                        // 创建一个自定义的CsvConfiguration
                        var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            HasHeaderRecord = true,
                            Delimiter = ",",
                        };

                        System.Diagnostics.Debug.WriteLine("CSV配置已创建");

                        // 使用UTF-8编码（带BOM）确保Excel可以正确识别中文
                        using (var writer = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true)))
                        {
                            System.Diagnostics.Debug.WriteLine("StreamWriter已创建");

                            using (var csv = new CsvWriter(writer, config))
                            {
                                System.Diagnostics.Debug.WriteLine("CsvWriter已创建");

                                // 准备数据 - 使用专门的导出模型类
                                var exportData = TempDepartments.Select(d => new DepartmentExport
                                {
                                    DepartmentId = d.DepartmentId,
                                    DepartmentName = d.DepartmentName ?? string.Empty
                                }).ToList();

                                System.Diagnostics.Debug.WriteLine($"已准备导出数据，共 {exportData.Count} 条记录");
                                System.Diagnostics.Debug.WriteLine($"第一条记录: 部门编号={exportData.FirstOrDefault()?.DepartmentId}, 部门名称={exportData.FirstOrDefault()?.DepartmentName}");

                                // 注册映射
                                System.Diagnostics.Debug.WriteLine("注册列映射");
                                csv.Context.RegisterClassMap<DepartmentExportMap>();

                                // 写入标题
                                System.Diagnostics.Debug.WriteLine("写入CSV标题");
                                csv.WriteHeader<DepartmentExport>();
                                csv.NextRecord();

                                // 写入数据
                                System.Diagnostics.Debug.WriteLine("开始写入CSV数据");
                                csv.WriteRecords(exportData);
                                System.Diagnostics.Debug.WriteLine("CSV数据写入完成");
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"导出成功，文件路径: {filePath}");
                        Growl.SuccessGlobal($"成功导出部门记录 {TempDepartments.Count} 条");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"导出过程中发生异常: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                        Growl.ErrorGlobal($"导出失败: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("用户取消了文件保存对话框");
                }

                System.Diagnostics.Debug.WriteLine("===== 部门导出操作结束 =====");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出操作发生未处理异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                Growl.ErrorGlobal($"导出操作失败: {ex.Message}");
            }
        }
        #endregion
    }
}
