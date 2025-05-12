using _2025毕业设计.Context;
using _2025毕业设计.Event;
using _2025毕业设计.Models;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WindowNS = System.Windows.Window;
using System.IO;
using System.Globalization;
using CsvHelper;

namespace _2025毕业设计.ViewModels.Pages
{
    public class AwardNominateViewModel : BindableBase
    {
        #region 时间属性
        private DispatcherTimer timer;
        #endregion

        #region 区域管理器
        private readonly IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 构造函数
        public AwardNominateViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            #region 分页
            EnableButtons();
            PageSizeItems();
            #endregion

            #region 搜索区域颜色随时间变换
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += async (s, e) => await ColorChangeAsync();
            timer.Start();
            // 初始化为默认颜色
            SearchBackground = new SolidColorBrush(Color.FromRgb(173, 216, 230));
            #endregion

            #region 初始化集合
            // 先初始化空集合，避免空引用
            Nominates = new ObservableCollection<Nomination>();
            TempViewNominates = new ObservableCollection<Nomination>();
            ListViewNominates = new ObservableCollection<Nomination>();
            
            // 加载数据并应用分页（内部会设置TotalRecords和MaxPage）
            LoadNominates(); 
            #endregion

            PreviewTextInputCommand = new DelegateCommand<string>(OnPreviewTextInput);

            // 订阅事件
            eventAggregator.GetEvent<NominationAddEvent>().Subscribe(OnAwardAdded);
            eventAggregator.GetEvent<NominationUpdateEvent>().Subscribe(OnAwardUpdated);
            eventAggregator.GetEvent<AwardDeletedEvent>().Subscribe(OnAwardDeleted);

            // 初始化命令
            AddAwardCommand = new DelegateCommand(OnAddAward);
            DeleteAwardCommand = new DelegateCommand<Nomination>(OnDeleteAward);
            EditAwardCommand = new DelegateCommand<Nomination>(OnEditAward);
            SearchAwardCommand = new DelegateCommand(OnSearchAward);
            ViewImageCommand = new DelegateCommand<Nomination>(OnViewImage);
        }
        #endregion

        #region 加载所有奖项提名
        private async void LoadNominates()
        {
            try
            {
                using (var context = new DataBaseContext())
                {
                    try
                    {
                        // 使用投影查询，只获取需要的字段，并添加null检查
                        var query = context.Nominations
                            .AsNoTracking()
                            .Select(n => new
                            {
                                n.NominationId,
                                n.AwardId,
                                n.Introduction,
                                n.NominateReason,
                                n.CoverImage,
                                n.NominationTime,
                                n.DepartmentId,
                                n.NominatedEmployeeId,
                                n.NominatedAdminId,
                                Award = n.Award != null ? new { n.Award.AwardId, n.Award.AwardName } : null,
                                Department = n.Department != null ? new { n.Department.DepartmentId, n.Department.DepartmentName } : null,
                                NominatedEmployee = n.NominatedEmployee != null ? new { n.NominatedEmployee.EmployeeId, n.NominatedEmployee.EmployeeName } : null
                            });
                            
                        var nominationsFromDb = await query.ToListAsync();
                        
                        // 转换为Nomination对象并设置UI属性，添加null检查
                        var processedNominations = nominationsFromDb.Select(n => new Nomination
                        {
                            NominationId = n.NominationId,
                            AwardId = n.AwardId,
                            Introduction = n.Introduction ?? string.Empty,
                            NominateReason = n.NominateReason ?? string.Empty,
                            CoverImage = n.CoverImage,
                            NominationTime = n.NominationTime,
                            DepartmentId = n.DepartmentId,
                            NominatedEmployeeId = n.NominatedEmployeeId,
                            NominatedAdminId = n.NominatedAdminId,
                            Award = n.Award != null ? new Award { AwardId = n.Award.AwardId, AwardName = n.Award.AwardName } : null,
                            Department = n.Department != null ? new Department { DepartmentId = n.Department.DepartmentId, DepartmentName = n.Department.DepartmentName } : null,
                            NominatedEmployee = n.NominatedEmployee != null ? new Employee 
                            { 
                                EmployeeId = n.NominatedEmployee.EmployeeId, 
                                EmployeeName = n.NominatedEmployee.EmployeeName,
                                EmployeePassword = string.Empty // 添加必需的密码字段
                            } : null,
                            // 设置UI相关属性
                            IsActive = true,
                            IsCommentSectionVisible = false,
                            UIComments = new ObservableCollection<CommentRecord>(),
                            NewCommentText = string.Empty,
                            CommentCount = 0,
                            IsUserVoted = false
                        }).ToList();

                        TempViewNominates = new ObservableCollection<Nomination>(processedNominations);
                        Nominates = new ObservableCollection<Nomination>(processedNominations);
                        TotalRecords = TempViewNominates.Count;
                        MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
                        
                        // 使用 Skip 和 Take 进行分页
                        var pagedNominations = TempViewNominates.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
                        ListViewNominates = new ObservableCollection<Nomination>(pagedNominations);
                        System.Diagnostics.Debug.WriteLine($"成功加载{processedNominations.Count}条提名数据");
                    }
                    catch (Exception ex)
                    {
                        Growl.ErrorGlobal($"加载提名数据失败: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"加载提名详细数据异常: {ex}");
                        
                        // 如果详细加载失败，回退到更简单的查询
                        var basicQuery = context.Nominations
                            .AsNoTracking()
                            .Select(n => new
                            {
                                n.NominationId,
                                n.AwardId,
                                n.Introduction,
                                n.NominateReason
                            });
                        var basicNominationsFromDb = await basicQuery.ToListAsync();
                        
                        // 转换为Nomination对象并设置UI属性
                        var basicProcessedNominations = basicNominationsFromDb.Select(n => new Nomination
                        {
                            NominationId = n.NominationId,
                            AwardId = n.AwardId,
                            Introduction = n.Introduction ?? string.Empty,
                            NominateReason = n.NominateReason ?? string.Empty,
                            // 设置UI相关属性
                            IsActive = true,
                            IsCommentSectionVisible = false,
                            UIComments = new ObservableCollection<CommentRecord>(),
                            NewCommentText = string.Empty,
                            CommentCount = 0,
                            IsUserVoted = false
                        }).ToList();
                            
                        TempViewNominates = new ObservableCollection<Nomination>(basicProcessedNominations);
                        Nominates = new ObservableCollection<Nomination>(basicProcessedNominations);
                        TotalRecords = TempViewNominates.Count;
                        MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
                        
                        var pagedNominations = TempViewNominates.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
                        ListViewNominates = new ObservableCollection<Nomination>(pagedNominations);
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"加载数据失败：{ex.Message}");
                System.Diagnostics.Debug.WriteLine($"加载提名数据失败: {ex}");
                
                // 初始化空集合以防止UI异常
                TempViewNominates = new ObservableCollection<Nomination>();
                Nominates = new ObservableCollection<Nomination>();
                ListViewNominates = new ObservableCollection<Nomination>();
                TotalRecords = 0;
                MaxPage = 1;
            }
        }
        #endregion

        #region 属性

        #region 搜索关键词
        private string searchKeyword;
        public string SearchKeyword { get => searchKeyword; set => SetProperty(ref searchKeyword, value); }
        #endregion

        #region 原始提名列表

        private ObservableCollection<Nomination> nominates;

        public ObservableCollection<Nomination> Nominates { get => nominates; set => SetProperty(ref nominates, value); }
        #endregion

        #region 临时提名列表
        private ObservableCollection<Nomination> tempViewNominates;

        public ObservableCollection<Nomination> TempViewNominates { get => tempViewNominates; set => SetProperty(ref tempViewNominates, value); }
        #endregion

        #region 视图展示提名列表
        private ObservableCollection<Nomination> listViewNominates;

        public ObservableCollection<Nomination> ListViewNominates { get => listViewNominates; set => SetProperty(ref listViewNominates, value); }
        #endregion

        #region 选中的提名
        private Nomination selectedNominate;
        public Nomination SelectedNominate { get => selectedNominate; set => SetProperty(ref selectedNominate, value); }

        #endregion

        #endregion

        #region 搜索区域背景颜色
        private byte colorOffset = 0;
        private SolidColorBrush searchBackground;
        public SolidColorBrush SearchBackground { get => searchBackground; set => SetProperty(ref searchBackground, value); }
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

        #region 增删改查命令

        #region 添加奖项命令
        public DelegateCommand AddAwardCommand { get; }
        private void OnAddAward()
        {
            try
            {
                // 验证是否有可用奖项
                using (var context = new DataBaseContext())
                {
                    if (!context.Awards.Any())
                    {
                        Growl.WarningGlobal("没有可用的奖项，请先在奖项设置中添加奖项！");
                        return;
                    }
                }
                
                // 导航到添加视图
                regionManager.RequestNavigate("AwardNominateEditRegion", "AddAwardNominate");
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"打开添加界面失败：{ex.Message}");
            }
        }
        #endregion

        #region 判断能否删除奖项
        private bool CanUpdateOrDelete()
        {
            return SelectedNominate != null;
        }
        #endregion

        #region 删除奖项命令
        public DelegateCommand<Nomination> DeleteAwardCommand { get; }
        private void OnDeleteAward(Nomination nomination)
        {
            if (nomination == null) return;
            
            // 使用完全限定名称确保不会有命名空间冲突
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                $"删除提名「{nomination.NominationId}」将同时删除所有关联的评论和投票记录！\n确定要继续吗？", 
                "删除确认", 
                System.Windows.MessageBoxButton.OKCancel);
        
            if (result != System.Windows.MessageBoxResult.OK) 
                return;
        
            try
            {
                using (var context = new DataBaseContext())
                {
                    // 先查找并删除所有关联的评论记录
                    var relatedComments = context.CommentRecords
                        .Where(c => c.NominationId == nomination.NominationId)
                        .ToList();
                
                    if (relatedComments.Any())
                    {
                        context.CommentRecords.RemoveRange(relatedComments);
                    }
                
                    // 查找并删除所有关联的投票记录
                    var relatedVotes = context.VoteRecords
                        .Where(v => v.NominationId == nomination.NominationId)
                        .ToList();
                
                    if (relatedVotes.Any())
                    {
                        context.VoteRecords.RemoveRange(relatedVotes);
                    }
                
                    // 最后删除提名记录本身
                    context.Nominations.Remove(nomination);
                    context.SaveChanges();
                
                    // 刷新列表
                    TempViewNominates = Nominates = new ObservableCollection<Nomination>(context.Nominations.ToList());
                }
            
                Growl.SuccessGlobal("提名删除成功");
                OnSearchAward();
            }
            catch (Exception ex)
            {
                Growl.Error($"删除提名失败：{ex.Message}");
            }
        }
        #endregion

        #region 编辑奖项命令
        public DelegateCommand<Nomination> EditAwardCommand { get; }
        private void OnEditAward(Nomination nomination)
        {
            if (nomination == null)
            {
                Growl.WarningGlobal("请选择要修改的提名");
                return;
            }

            try
            {
                // 验证所选提名的奖项是否仍然存在
                using (var context = new DataBaseContext())
                {
                    // 确认奖项是否存在
                    bool awardExists = context.Awards.Any(a => a.AwardId == nomination.AwardId);
                    if (!awardExists)
                    {
                        Growl.WarningGlobal($"该提名关联的奖项已被删除，无法编辑！");
                        // 刷新列表以移除无效提名
                        OnSearchAward();
                        return;
                    }
                }
                
                var parameters = new NavigationParameters
                {
                    { "Nomination", nomination }
                };
                regionManager.RequestNavigate("AwardNominateEditRegion", "EditAwardNominate", parameters);
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"打开编辑界面失败：{ex.Message}");
            }
        }
        #endregion

        #region 查询奖项命令
        public DelegateCommand SearchAwardCommand { get; }

        private void OnSearchAward()
        {
            try
            {
                using (var context = new DataBaseContext())
                {
                    try
                    {
                        // 使用投影查询，只获取需要的字段，并添加null检查
                        var query = context.Nominations
                            .AsNoTracking()
                            .Select(n => new
                            {
                                n.NominationId,
                                n.AwardId,
                                n.Introduction,
                                n.NominateReason,
                                n.CoverImage,
                                n.NominationTime,
                                n.DepartmentId,
                                n.NominatedEmployeeId,
                                n.NominatedAdminId,
                                n.ProposerEmployeeId,
                                n.ProposerAdminId,
                                n.ProposerSupAdminId,
                                Award = n.Award != null ? new { n.Award.AwardId, n.Award.AwardName } : null,
                                Department = n.Department != null ? new { n.Department.DepartmentId, n.Department.DepartmentName } : null,
                                NominatedEmployee = n.NominatedEmployee != null ? new { n.NominatedEmployee.EmployeeId, n.NominatedEmployee.EmployeeName } : null,
                                NominatedAdmin = n.NominatedAdmin != null ? new { n.NominatedAdmin.AdminId, n.NominatedAdmin.AdminName } : null,
                                ProposerEmployee = n.ProposerEmployee != null ? new { n.ProposerEmployee.EmployeeId, n.ProposerEmployee.EmployeeName } : null,
                                ProposerAdmin = n.ProposerAdmin != null ? new { n.ProposerAdmin.AdminId, n.ProposerAdmin.AdminName } : null,
                                ProposerSupAdmin = n.ProposerSupAdmin != null ? new { n.ProposerSupAdmin.SupAdminId, n.ProposerSupAdmin.SupAdminName } : null
                            });

                        // 执行查询
                        var nominationsFromDb = query.ToList();
                        
                        // 转换为Nomination对象并设置UI属性，添加null检查
                        var allNominations = nominationsFromDb.Select(n => new Nomination
                        {
                            NominationId = n.NominationId,
                            AwardId = n.AwardId,
                            Introduction = n.Introduction ?? string.Empty,
                            NominateReason = n.NominateReason ?? string.Empty,
                            CoverImage = n.CoverImage,
                            NominationTime = n.NominationTime,
                            DepartmentId = n.DepartmentId,
                            NominatedEmployeeId = n.NominatedEmployeeId,
                            NominatedAdminId = n.NominatedAdminId,
                            ProposerEmployeeId = n.ProposerEmployeeId,
                            ProposerAdminId = n.ProposerAdminId,
                            ProposerSupAdminId = n.ProposerSupAdminId,
                            Award = n.Award != null ? new Award { AwardId = n.Award.AwardId, AwardName = n.Award.AwardName } : null,
                            Department = n.Department != null ? new Department { DepartmentId = n.Department.DepartmentId, DepartmentName = n.Department.DepartmentName } : null,
                            NominatedEmployee = n.NominatedEmployee != null ? new Employee 
                            { 
                                EmployeeId = n.NominatedEmployee.EmployeeId, 
                                EmployeeName = n.NominatedEmployee.EmployeeName,
                                EmployeePassword = string.Empty // 添加必需的密码字段
                            } : null,
                            NominatedAdmin = n.NominatedAdmin != null ? new Admin 
                            { 
                                AdminId = n.NominatedAdmin.AdminId, 
                                AdminName = n.NominatedAdmin.AdminName,
                                AdminPassword = string.Empty // 添加必需的密码字段
                            } : null,
                            ProposerEmployee = n.ProposerEmployee != null ? new Employee 
                            { 
                                EmployeeId = n.ProposerEmployee.EmployeeId, 
                                EmployeeName = n.ProposerEmployee.EmployeeName,
                                EmployeePassword = string.Empty // 添加必需的密码字段
                            } : null,
                            ProposerAdmin = n.ProposerAdmin != null ? new Admin 
                            { 
                                AdminId = n.ProposerAdmin.AdminId, 
                                AdminName = n.ProposerAdmin.AdminName,
                                AdminPassword = string.Empty // 添加必需的密码字段
                            } : null,
                            ProposerSupAdmin = n.ProposerSupAdmin != null ? new SupAdmin 
                            { 
                                SupAdminId = n.ProposerSupAdmin.SupAdminId, 
                                SupAdminName = n.ProposerSupAdmin.SupAdminName,
                                SupAdminPassword = string.Empty // 添加必需的密码字段
                            } : null,
                            // 设置UI相关属性
                            IsActive = true,
                            IsCommentSectionVisible = false,
                            UIComments = new ObservableCollection<CommentRecord>(),
                            NewCommentText = string.Empty,
                            CommentCount = 0,
                            IsUserVoted = false
                        }).ToList();

                        if (string.IsNullOrWhiteSpace(SearchKeyword))
                        {
                            TempViewNominates = new ObservableCollection<Nomination>(allNominations);
                        }
                        else
                        {
                            var searchResults = allNominations.Where(n =>
                                (n.Award?.AwardName?.Contains(SearchKeyword) ?? false) ||
                                (n.NominatedEmployee?.EmployeeName?.Contains(SearchKeyword) ?? false) ||
                                (n.NominatedAdmin?.AdminName?.Contains(SearchKeyword) ?? false) ||
                                (n.Department?.DepartmentName?.Contains(SearchKeyword) ?? false) ||
                                (n.Introduction?.Contains(SearchKeyword) ?? false) ||
                                (n.NominateReason?.Contains(SearchKeyword) ?? false) ||
                                (n.ProposerEmployee?.EmployeeName?.Contains(SearchKeyword) ?? false) ||
                                (n.ProposerAdmin?.AdminName?.Contains(SearchKeyword) ?? false) ||
                                (n.ProposerSupAdmin?.SupAdminName?.Contains(SearchKeyword) ?? false)
                            ).ToList();

                            TempViewNominates = new ObservableCollection<Nomination>(searchResults);
                        }

                        UpdateListViewData();
                    }
                    catch (Exception ex)
                    {
                        Growl.ErrorGlobal($"加载提名数据失败: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"加载提名数据异常: {ex}");
                        
                        // 如果加载失败，尝试加载最基本的数据
                        var basicQuery = context.Nominations
                            .AsNoTracking()
                            .Select(n => new
                            {
                                n.NominationId,
                                n.AwardId,
                                n.Introduction,
                                n.NominateReason
                            });
                        var basicNominationsFromDb = basicQuery.ToList();
                        
                        var basicNominations = basicNominationsFromDb.Select(n => new Nomination
                        {
                            NominationId = n.NominationId,
                            AwardId = n.AwardId,
                            Introduction = n.Introduction ?? string.Empty,
                            NominateReason = n.NominateReason ?? string.Empty,
                            // 设置UI相关属性
                            IsActive = true,
                            IsCommentSectionVisible = false,
                            UIComments = new ObservableCollection<CommentRecord>(),
                            NewCommentText = string.Empty,
                            CommentCount = 0,
                            IsUserVoted = false
                        }).ToList();
                            
                        TempViewNominates = new ObservableCollection<Nomination>(basicNominations);
                        UpdateListViewData();
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"查询失败：{ex.Message}");
                System.Diagnostics.Debug.WriteLine($"查询异常: {ex}");
                
                // 初始化空集合防止界面异常
                TempViewNominates = new ObservableCollection<Nomination>();
                UpdateListViewData();
            }
        }

        #endregion

        #region 奖项(新增)更新时更新视图列表
        private void OnAwardAdded()
        {
            OnSearchAward();
        }
        private void OnAwardUpdated()
        {
            OnSearchAward();
        }
        #endregion

        #region 增删改查按钮是否启用

        #region 添加按钮是否启用
        private bool isAddEnabled;
        public bool IsAddEnabled { get => isAddEnabled; set => SetProperty(ref isAddEnabled, value); }
        #endregion

        #region 删除按钮是否启用
        private bool isDeleteEnabled;
        public bool IsDeleteEnabled { get => isDeleteEnabled; set => SetProperty(ref isDeleteEnabled, value); }
        #endregion

        #region 编辑按钮是否启用
        private bool isEditEnabled;
        public bool IsEditEnabled { get => isEditEnabled; set => SetProperty(ref isEditEnabled, value); }
        #endregion

        #region 查询按钮是否启用
        private bool isSearchEnabled;
        public bool IsSearchEnabled { get => isSearchEnabled; set => SetProperty(ref isSearchEnabled, value); }

        #endregion

        #region 启用增删改查按钮
        private void EnableButtons()
        {
            IsAddEnabled = true;
            IsDeleteEnabled = true;
            IsEditEnabled = true;
            IsSearchEnabled = true;
        }
        #endregion

        #endregion

        #endregion

        #region 导入导出

        #region 导出数据命令
        private DelegateCommand exportDataCommand;
        public ICommand ExportDataCommand => exportDataCommand ??= new DelegateCommand(ExportData);

        private void ExportData()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV文件|*.csv|Excel文件|*.xlsx|所有文件|*.*",
                Title = "导出提名数据",
                FileName = $"奖项提名列表_{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                OnExportData(saveFileDialog.FileName);
            }
        }
        private void OnExportData(string filePath)
        {
            try
            {
                // 使用UTF-8编码（带BOM）确保Excel可以正确识别中文
                using var writer = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true));
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                
                // 创建不包含图片字段的数据列表，但需要从数据库重新查询包含投票记录数量
                using (var context = new DataBaseContext())
                {
                    var nominationsWithVotes = context.Nominations
                        .AsNoTracking()
                        .Include(n => n.Award)
                        .Include(n => n.Department)
                        .Include(n => n.NominatedEmployee)
                        .Include(n => n.NominatedAdmin)
                        .Include(n => n.ProposerEmployee)
                        .Include(n => n.ProposerAdmin)
                        .Include(n => n.ProposerSupAdmin)
                        .Include(n => n.VoteRecords)
                        .ToList();
                        
                    // 找到对应的提名数据并获取实际票数
                    var exportData = TempViewNominates.Select(n => {
                        var fullData = nominationsWithVotes.FirstOrDefault(nv => nv.NominationId == n.NominationId);
                        int voteCount = fullData?.VoteRecords?.Count ?? 0;
                        
                        return new
                        {
                            提名ID = n.NominationId,
                            奖项名称 = n.Award?.AwardName ?? "未设置",
                            提名对象 = n.NominatedEmployee?.EmployeeName ?? n.NominatedAdmin?.AdminName ?? "未设置",
                            所属部门 = n.Department?.DepartmentName ?? "未设置",
                            一句话介绍 = n.Introduction,
                            提名理由 = n.NominateReason,
                            提名时间 = n.NominationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            提名人 = GetProposerName(n),
                            得票数 = voteCount
                        };
                    }).ToList();
                    
                    // 写入数据
                    csv.WriteRecords(exportData);
                    
                    Growl.SuccessGlobal($"成功导出{exportData.Count}条提名记录到: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"导出失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"导出提名数据出错: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // 辅助方法：获取提名人姓名
        private string GetProposerName(Nomination nomination)
        {
            if (nomination.ProposerEmployee != null)
                return nomination.ProposerEmployee.EmployeeName;
            else if (nomination.ProposerAdmin != null)
                return nomination.ProposerAdmin.AdminName;
            else if (nomination.ProposerSupAdmin != null)
                return nomination.ProposerSupAdmin.SupAdminName;
            else
                return "未设置";
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
        private int pageSize = 8;
        public int PageSize { get => pageSize; set => SetProperty(ref pageSize, value); }
        #endregion

        #region 每页显示条数集合
        private void PageSizeItems()
        {
            PageSizeOptions = new List<int> { 8, 5, 3, 2 };
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
                var isDigit = char.IsDigit(input[^1]);
                if (!isDigit)
                {
                    //如果输入的不是数字，则截取掉最后则截取掉最后一位字符
                    SearchText = searchText[..^1];
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

        #region 上一页 下一页


        private DelegateCommand previousPageCommand;
        public DelegateCommand PreviousPageCommand => previousPageCommand ??= new DelegateCommand(PreviousPage);

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                UpdateListViewData();
            }
        }

        private DelegateCommand nextPageCommand;
        public DelegateCommand NextPageCommand => nextPageCommand ??= new DelegateCommand(NextPage);

        private void NextPage()
        {
            if (CurrentPage < MaxPage)
            {
                CurrentPage++;
                UpdateListViewData();
            }
        }

        private DelegateCommand jumpPageCommand;
        //跳转页面
        public ICommand JumpPageCommand => jumpPageCommand ??= new DelegateCommand(JumpPage);

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
        #endregion

        #region 当每页个数变换时
        private DelegateCommand pageSizeChangedCommand;
        public ICommand PageSizeChangedCommand => pageSizeChangedCommand ??= new DelegateCommand(PageSizeChanged);

        private void PageSizeChanged()
        {
            CurrentPage = 1;
            UpdateListViewData();
        }

        private void UpdateListViewData()
        {
            if (TempViewNominates == null) return;

            TotalRecords = TempViewNominates.Count;
            MaxPage = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            var startIndex = (CurrentPage - 1) * PageSize;
            var endIndex = Math.Min(startIndex + PageSize, TotalRecords);
            ListViewNominates = new ObservableCollection<Nomination>(TempViewNominates.Skip(startIndex).Take(endIndex - startIndex));
        }

        #endregion

        #endregion


        #region 处理奖项删除事件
        private void OnAwardDeleted(int deletedAwardId)
        {
            try
            {
                // 清除编辑区域
                var region = regionManager.Regions["AwardNominateEditRegion"];
                if (region != null)
                {
                    region.RemoveAll();
                }
                
                // 刷新数据
                using (var context = new DataBaseContext())
                {
                    // 提示用户
                    var affectedCount = context.Nominations.Count(n => n.AwardId == deletedAwardId);
                    if (affectedCount > 0)
                    {
                        Growl.InfoGlobal($"已删除的奖项关联了{affectedCount}个提名，这些提名可能已失效");
                    }
                }
                
                // 刷新视图
                LoadNominates();
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"处理奖项删除事件失败：{ex.Message}");
            }
        }
        #endregion

        #region 改进搜索方法，确保每次搜索时都重新加载最新的奖项数据
        private void SearchNominate()
        {
            // 重新加载奖项列表，确保使用最新数据
            LoadNominates();
            
            using (var context = new DataBaseContext())
            {
                // ... 现有搜索逻辑 ...
            }
            
            // ... 其余代码 ...
        }
        #endregion

        private void AddNominate()
        {
            // 刷新奖项列表
            LoadNominates();
            
            // 验证是否有可用奖项
            if (!Nominates.Any())
            {
                Growl.WarningGlobal("没有可用的奖项，请先添加奖项！");
                return;
            }
            
            // ... 现有逻辑 ...
        }

        private void EditNominate()
        {
            if (!CanUpdateOrDelete()) { return; }
            
            // 验证所选提名的奖项是否仍然存在
            using (var context = new DataBaseContext())
            {
                bool awardExists = context.Awards.Any(a => a.AwardId == SelectedNominate.AwardId);
                if (!awardExists)
                {
                    Growl.WarningGlobal($"该提名关联的奖项已被删除，无法编辑！");
                    // 刷新列表以移除无效提名
                    SearchNominate();
                    return;
                }
            }
            
            // ... 现有逻辑 ...
        }

        #region 查看图片命令
        public DelegateCommand<Nomination> ViewImageCommand { get; }
        
        // 添加标志位防止重复执行
        private bool _isViewingImage = false;
        
        private void OnViewImage(Nomination nomination)
        {
            // 如果已经在显示图片，则不重复处理
            if (_isViewingImage) return;
            
            if (nomination == null || nomination.CoverImage == null || nomination.CoverImage.Length == 0)
            {
                Growl.WarningGlobal("没有可查看的图片");
                return;
            }

            try
            {
                // 设置标志位，防止重复打开窗口
                _isViewingImage = true;
                
                // 创建一个新窗口来展示图片
                var imageWindow = new WindowNS
                {
                    Title = $"提名图片 - {nomination.NominatedEmployee?.EmployeeName ?? nomination.NominatedAdmin?.AdminName ?? "未知"} - ID: {nomination.NominationId}",
                    Width = 700,  // 设置固定窗口宽度
                    Height = 600, // 设置固定窗口高度
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,  // 无法调整大小，且没有最大化最小化按钮
                    Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                    SizeToContent = SizeToContent.Manual  // 手动指定大小，不根据内容自动调整
                };

                // 创建主布局
                var mainGrid = new Grid();
                var rowDef1 = new RowDefinition(); 
                rowDef1.Height = System.Windows.GridLength.Auto;
                mainGrid.RowDefinitions.Add(rowDef1);
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.HorizontalAlignment = HorizontalAlignment.Center;
                mainGrid.VerticalAlignment = VerticalAlignment.Center;
                
                // 创建顶部信息面板
                var infoBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    Padding = new Thickness(10),
                    Margin = new Thickness(10, 10, 10, 5),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    BorderThickness = new Thickness(1),
                    Width = 650 // 与图片容器宽度一致
                };
                
                var infoPanel = new StackPanel { Margin = new Thickness(5) };
                
                // 添加提名信息文本
                infoPanel.Children.Add(new TextBlock 
                { 
                    Text = $"奖项: {nomination.Award?.AwardName ?? "未知"}",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5),
                    FontSize = 14
                });
                
                infoPanel.Children.Add(new TextBlock 
                { 
                    Text = $"提名对象: {nomination.NominatedEmployee?.EmployeeName ?? nomination.NominatedAdmin?.AdminName ?? "未知"}",
                    Margin = new Thickness(0, 0, 0, 5)
                });
                
                infoPanel.Children.Add(new TextBlock 
                { 
                    Text = $"部门: {nomination.Department?.DepartmentName ?? "未知"}",
                    Margin = new Thickness(0, 0, 0, 5)
                });
                
                if (!string.IsNullOrEmpty(nomination.Introduction))
                {
                    infoPanel.Children.Add(new TextBlock 
                    { 
                        Text = $"简介: {nomination.Introduction}",
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
                    MaxWidth = 640,  // 容器宽度减去内边距
                    MaxHeight = 390  // 容器高度减去内边距
                };

                // 将字节数组转换为图像源
                using (var ms = new System.IO.MemoryStream(nomination.CoverImage))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    
                    // 图片将根据MaxWidth和MaxHeight自动缩放，
                    // 不需要手动计算尺寸，让Uniform模式自动处理
                    image.Source = bitmap;
                }

                // 创建图片容器，使用DockPanel包装以保持居中
                var dockPanel = new DockPanel
                {
                    LastChildFill = true,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                var imageBorder = new Border
                {
                    Child = image,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
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
                // 发生异常时也要重置标志位
                _isViewingImage = false;
                Growl.ErrorGlobal($"查看图片失败：{ex.Message}");
            }
        }
        #endregion
    }
}
