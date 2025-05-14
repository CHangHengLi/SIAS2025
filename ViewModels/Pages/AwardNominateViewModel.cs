using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CsvHelper;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using WindowNS = System.Windows.Window;

namespace SIASGraduate.ViewModels.Pages
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
            // 设置加载状态
            IsLoading = true;

            try
            {
                using (var context = new DataBaseContext())
                {
                    try
                    {
                        // 使用分步加载方式避免NotMapped属性问题

                        // 先获取提名ID列表
                        var nominationIds = await context.Nominations
                            .AsNoTracking()
                            .Select(n => n.NominationId)
                            .ToListAsync();

                        // 然后分别加载每个提名的完整数据
                        var nominations = new List<Nomination>();

                        foreach (var nominationId in nominationIds)
                        {
                            // 使用Select投影获取基本提名信息，避免导航属性
                            var nomination = await context.Nominations
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
                                    ProposerSupAdminId = n.ProposerSupAdminId,
                                    Introduction = n.Introduction,
                                    NominateReason = n.NominateReason,
                                    CoverImage = n.CoverImage,
                                    NominationTime = n.NominationTime
                                })
                                .FirstOrDefaultAsync();

                            if (nomination != null)
                            {
                                // 单独加载各个关联实体
                                if (nomination.AwardId > 0)
                                {
                                    nomination.Award = await context.Awards
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(a => a.AwardId == nomination.AwardId);
                                }

                                if (nomination.DepartmentId.HasValue && nomination.DepartmentId.Value > 0)
                                {
                                    nomination.Department = await context.Departments
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(d => d.DepartmentId == nomination.DepartmentId.Value);
                                }

                                if (nomination.NominatedEmployeeId.HasValue && nomination.NominatedEmployeeId.Value > 0)
                                {
                                    nomination.NominatedEmployee = await context.Employees
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(e => e.EmployeeId == nomination.NominatedEmployeeId.Value);
                                }

                                if (nomination.NominatedAdminId.HasValue && nomination.NominatedAdminId.Value > 0)
                                {
                                    nomination.NominatedAdmin = await context.Admins
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(a => a.AdminId == nomination.NominatedAdminId.Value);
                                }

                                if (nomination.ProposerEmployeeId.HasValue && nomination.ProposerEmployeeId.Value > 0)
                                {
                                    nomination.ProposerEmployee = await context.Employees
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(e => e.EmployeeId == nomination.ProposerEmployeeId.Value);
                                }

                                if (nomination.ProposerAdminId.HasValue && nomination.ProposerAdminId.Value > 0)
                                {
                                    nomination.ProposerAdmin = await context.Admins
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(a => a.AdminId == nomination.ProposerAdminId.Value);
                                }

                                if (nomination.ProposerSupAdminId.HasValue && nomination.ProposerSupAdminId.Value > 0)
                                {
                                    nomination.ProposerSupAdmin = await context.SupAdmins
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(s => s.SupAdminId == nomination.ProposerSupAdminId.Value);
                                }

                                // 单独加载投票记录
                                var voteRecords = await context.VoteRecords
                                    .AsNoTracking()
                                    .Where(v => v.NominationId == nominationId)
                                    .ToListAsync();

                                nomination.VoteRecords = new ObservableCollection<VoteRecord>(voteRecords);
                                nominations.Add(nomination);
                            }
                        }

                        // 转为观察集合
                        TempViewNominates = new ObservableCollection<Nomination>(nominations);
                        Nominates = new ObservableCollection<Nomination>(nominations);
                        TotalRecords = TempViewNominates.Count;
                        MaxPage = (int)Math.Ceiling(TotalRecords / (double)PageSize);

                        // 使用 Skip 和 Take 进行分页
                        var pagedNominations = TempViewNominates.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
                        ListViewNominates = new ObservableCollection<Nomination>(pagedNominations);
                        System.Diagnostics.Debug.WriteLine($"成功加载{nominations.Count}条提名数据");
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
                            VoteRecords = new ObservableCollection<VoteRecord>()
                        }).ToList();

                        TempViewNominates = new ObservableCollection<Nomination>(basicProcessedNominations);
                        Nominates = new ObservableCollection<Nomination>(basicProcessedNominations);
                        TotalRecords = TempViewNominates.Count;
                        MaxPage = (int)Math.Ceiling(TotalRecords / (double)PageSize);

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
            finally
            {
                // 无论成功失败，都重置加载状态
                IsLoading = false;
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

            try
            {
                using (var context = new DataBaseContext())
                {
                    // 检查该提名是否有投票记录
                    int voteCount = context.VoteRecords.Count(v => v.NominationId == nomination.NominationId);
                    if (voteCount > 0)
                    {
                        // 如果有投票，不允许删除
                        Growl.WarningGlobal($"提名「{nomination.NominationId}」已有{voteCount}个投票记录，不能删除！");
                        return;
                    }

                    // 使用完全限定名称确保不会有命名空间冲突
                    System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                        $"删除提名「{nomination.NominationId}」将同时删除所有关联的评论和投票记录！\n确定要继续吗？",
                        "删除确认",
                        System.Windows.MessageBoxButton.OKCancel);

                    if (result != System.Windows.MessageBoxResult.OK)
                        return;

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
                        // 修改查询，先创建基本查询并获取ID列表
                        var baseQuery = context.Nominations.AsNoTracking();

                        // 使用基本过滤条件
                        if (!string.IsNullOrWhiteSpace(SearchKeyword))
                        {
                            baseQuery = baseQuery.Where(n =>
                                (n.Award != null && n.Award.AwardName.Contains(SearchKeyword)) ||
                                (n.NominatedEmployee != null && n.NominatedEmployee.EmployeeName.Contains(SearchKeyword)) ||
                                (n.NominatedAdmin != null && n.NominatedAdmin.AdminName.Contains(SearchKeyword)) ||
                                (n.Department != null && n.Department.DepartmentName.Contains(SearchKeyword)) ||
                                (n.Introduction != null && n.Introduction.Contains(SearchKeyword)) ||
                                (n.NominateReason != null && n.NominateReason.Contains(SearchKeyword)) ||
                                (n.ProposerEmployee != null && n.ProposerEmployee.EmployeeName.Contains(SearchKeyword)) ||
                                (n.ProposerAdmin != null && n.ProposerAdmin.AdminName.Contains(SearchKeyword)) ||
                                (n.ProposerSupAdmin != null && n.ProposerSupAdmin.SupAdminName.Contains(SearchKeyword))
                            );
                        }

                        // 首先获取匹配条件的提名ID列表
                        var nominationIds = baseQuery.Select(n => n.NominationId).ToList();

                        // 然后分别加载每个提名的完整数据
                        var nominations = new List<Nomination>();

                        foreach (var nominationId in nominationIds)
                        {
                            // 首先获取基本提名信息，不包含导航属性
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
                                    ProposerSupAdminId = n.ProposerSupAdminId,
                                    Introduction = n.Introduction,
                                    NominateReason = n.NominateReason,
                                    CoverImage = n.CoverImage,
                                    NominationTime = n.NominationTime
                                })
                                .FirstOrDefault();

                            if (nomination != null)
                            {
                                // 单独加载各个关联实体
                                if (nomination.AwardId > 0)
                                {
                                    nomination.Award = context.Awards
                                        .AsNoTracking()
                                        .FirstOrDefault(a => a.AwardId == nomination.AwardId);
                                }

                                if (nomination.DepartmentId.HasValue && nomination.DepartmentId.Value > 0)
                                {
                                    nomination.Department = context.Departments
                                        .AsNoTracking()
                                        .FirstOrDefault(d => d.DepartmentId == nomination.DepartmentId.Value);
                                }

                                if (nomination.NominatedEmployeeId.HasValue && nomination.NominatedEmployeeId.Value > 0)
                                {
                                    nomination.NominatedEmployee = context.Employees
                                        .AsNoTracking()
                                        .FirstOrDefault(e => e.EmployeeId == nomination.NominatedEmployeeId.Value);
                                }

                                if (nomination.NominatedAdminId.HasValue && nomination.NominatedAdminId.Value > 0)
                                {
                                    nomination.NominatedAdmin = context.Admins
                                        .AsNoTracking()
                                        .FirstOrDefault(a => a.AdminId == nomination.NominatedAdminId.Value);
                                }

                                if (nomination.ProposerEmployeeId.HasValue && nomination.ProposerEmployeeId.Value > 0)
                                {
                                    nomination.ProposerEmployee = context.Employees
                                        .AsNoTracking()
                                        .FirstOrDefault(e => e.EmployeeId == nomination.ProposerEmployeeId.Value);
                                }

                                if (nomination.ProposerAdminId.HasValue && nomination.ProposerAdminId.Value > 0)
                                {
                                    nomination.ProposerAdmin = context.Admins
                                        .AsNoTracking()
                                        .FirstOrDefault(a => a.AdminId == nomination.ProposerAdminId.Value);
                                }

                                if (nomination.ProposerSupAdminId.HasValue && nomination.ProposerSupAdminId.Value > 0)
                                {
                                    nomination.ProposerSupAdmin = context.SupAdmins
                                        .AsNoTracking()
                                        .FirstOrDefault(a => a.SupAdminId == nomination.ProposerSupAdminId.Value);
                                }

                                // 单独加载投票记录
                                var voteRecords = context.VoteRecords
                                    .AsNoTracking()
                                    .Where(v => v.NominationId == nominationId)
                                    .ToList();

                                nomination.VoteRecords = new ObservableCollection<VoteRecord>(voteRecords);
                                nominations.Add(nomination);
                            }
                        }

                        // 转为观察集合
                        TempViewNominates = new ObservableCollection<Nomination>(nominations);

                        // 更新UI
                        UpdateListViewData();
                    }
                    catch (Exception ex)
                    {
                        Growl.ErrorGlobal($"加载提名数据失败: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"加载提名数据异常: {ex}");

                        // 如果加载失败，尝试加载最基本的数据
                        var basicQuery = context.Nominations
                            .AsNoTracking();

                        if (!string.IsNullOrWhiteSpace(SearchKeyword))
                        {
                            basicQuery = basicQuery.Where(n =>
                                (n.Introduction != null && n.Introduction.Contains(SearchKeyword)) ||
                                (n.NominateReason != null && n.NominateReason.Contains(SearchKeyword))
                            );
                        }

                        var basicNominations = basicQuery.ToList();

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
                    // 首先获取需要导出的提名ID列表
                    var nominationIds = TempViewNominates.Select(n => n.NominationId).ToList();

                    // 然后分步加载数据
                    var exportData = new List<object>();

                    foreach (var nominationId in nominationIds)
                    {
                        // 先获取基础提名信息，不包含导航属性
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
                                ProposerSupAdminId = n.ProposerSupAdminId,
                                Introduction = n.Introduction,
                                NominateReason = n.NominateReason,
                                NominationTime = n.NominationTime
                            })
                            .FirstOrDefault();

                        if (nomination != null)
                        {
                            // 单独加载各个关联实体
                            if (nomination.AwardId > 0)
                            {
                                nomination.Award = context.Awards
                                    .AsNoTracking()
                                    .FirstOrDefault(a => a.AwardId == nomination.AwardId);
                            }

                            if (nomination.DepartmentId.HasValue && nomination.DepartmentId.Value > 0)
                            {
                                nomination.Department = context.Departments
                                    .AsNoTracking()
                                    .FirstOrDefault(d => d.DepartmentId == nomination.DepartmentId.Value);
                            }

                            if (nomination.NominatedEmployeeId.HasValue && nomination.NominatedEmployeeId.Value > 0)
                            {
                                nomination.NominatedEmployee = context.Employees
                                    .AsNoTracking()
                                    .FirstOrDefault(e => e.EmployeeId == nomination.NominatedEmployeeId.Value);
                            }

                            if (nomination.NominatedAdminId.HasValue && nomination.NominatedAdminId.Value > 0)
                            {
                                nomination.NominatedAdmin = context.Admins
                                    .AsNoTracking()
                                    .FirstOrDefault(a => a.AdminId == nomination.NominatedAdminId.Value);
                            }

                            if (nomination.ProposerEmployeeId.HasValue && nomination.ProposerEmployeeId.Value > 0)
                            {
                                nomination.ProposerEmployee = context.Employees
                                    .AsNoTracking()
                                    .FirstOrDefault(e => e.EmployeeId == nomination.ProposerEmployeeId.Value);
                            }

                            if (nomination.ProposerAdminId.HasValue && nomination.ProposerAdminId.Value > 0)
                            {
                                nomination.ProposerAdmin = context.Admins
                                    .AsNoTracking()
                                    .FirstOrDefault(a => a.AdminId == nomination.ProposerAdminId.Value);
                            }

                            if (nomination.ProposerSupAdminId.HasValue && nomination.ProposerSupAdminId.Value > 0)
                            {
                                nomination.ProposerSupAdmin = context.SupAdmins
                                    .AsNoTracking()
                                    .FirstOrDefault(a => a.SupAdminId == nomination.ProposerSupAdminId.Value);
                            }

                            // 单独查询投票记录数量
                            int voteCount = context.VoteRecords
                                .Count(v => v.NominationId == nominationId);

                            // 获取提名人和被提名人姓名
                            string nominatedName = GetNominatedName(nomination);
                            string proposerName = GetProposerName(nomination);

                            // 添加到导出数据
                            exportData.Add(new
                            {
                                提名ID = nomination.NominationId,
                                奖项名称 = nomination.Award?.AwardName ?? "未设置",
                                提名对象 = nominatedName,
                                所属部门 = nomination.Department?.DepartmentName ?? "未设置",
                                提名人 = proposerName,
                                一句话介绍 = nomination.Introduction ?? string.Empty,
                                提名理由 = nomination.NominateReason ?? string.Empty,
                                提名时间 = nomination.NominationTime,
                                得票数 = voteCount
                            });
                        }
                    }

                    csv.WriteRecords(exportData);
                }

                Growl.SuccessGlobal($"已成功导出提名数据到: {filePath}");
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"导出数据失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"导出数据异常: {ex}");
            }
        }

        // 辅助方法：获取提名人姓名
        private string GetProposerName(Nomination nomination)
        {
            if (nomination.ProposerEmployee != null)
                return nomination.ProposerEmployee.EmployeeName ?? "未设置";
            else if (nomination.ProposerAdmin != null)
                return nomination.ProposerAdmin.AdminName ?? "未设置";
            else if (nomination.ProposerSupAdmin != null)
                return nomination.ProposerSupAdmin.SupAdminName ?? "未设置";
            else
                return "未设置";
        }

        // 辅助方法：获取提名对象姓名
        private string GetNominatedName(Nomination nomination)
        {
            if (nomination.NominatedEmployee != null)
                return nomination.NominatedEmployee.EmployeeName ?? "未设置";
            else if (nomination.NominatedAdmin != null)
                return nomination.NominatedAdmin.AdminName ?? "未设置";
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

        #region UI状态属性
        private bool isLoading;
        /// <summary>
        /// 指示当前是否正在加载数据
        /// </summary>
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }
        #endregion
    }
}
