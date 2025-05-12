using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using CsvHelper;
using CsvHelper.Configuration;
using HandyControl.Controls;
using Microsoft.Win32;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SIASGraduate.ViewModels.Pages
{
    public class AwardSettingViewModel : BindableBase
    {
        #region 时间属性
        private DispatcherTimer timer;
        #endregion

        #region 区域管理器
        private IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 构造函数
        public AwardSettingViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
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

            #region 区域管理器
            this.regionManager = regionManager;
            #endregion

            #region 事件聚合器
            this.eventAggregator = eventAggregator;
            //事件的订阅
            eventAggregator.GetEvent<AwardAddEvent>().Subscribe(OnAwardAdded);
            eventAggregator.GetEvent<AwardUpdateEvent>().Subscribe(OnAwardUpdated);
            eventAggregator.GetEvent<AwardSettingExportDataEvent>().Subscribe(OnExportData);
            #endregion

            #region 初始化奖项集合
            using (var context = new DataBaseContext())
            {
                // 确保所有奖项的MaxVoteCount至少为1
                var awardsToUpdate = context.Awards.Where(a => a.MaxVoteCount <= 0).ToList();
                foreach (var award in awardsToUpdate)
                {
                    award.MaxVoteCount = 1;
                }
                
                if (awardsToUpdate.Any())
                {
                    context.SaveChanges();
                }
                
                //获取所有奖项
                ListViewAwardSettings = TempAwardSettings = AwardSettings = new ObservableCollection<Award>(context.Awards);
                TotalRecords = TempAwardSettings.Count;
            }
            LoadAwards();
            #endregion

            #region 初始化命令
            AddAwardCommand = new DelegateCommand(OnAddAward);
            DeleteAwardCommand = new DelegateCommand<Award>(OnDeleteAward);
            EditAwardCommand = new DelegateCommand<Award>(OnEditAward);
            SearchAwardCommand = new DelegateCommand(OnSearchAward);
            PreviewTextInputCommand = new DelegateCommand<string>(OnPreviewTextInput);
            #endregion

            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
        }
        #endregion

        #region 加载所有奖项
        public void LoadAwards()
        {
            TotalRecords = TempAwardSettings.Count;
            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
            ListViewAwardSettings = new ObservableCollection<Award>(TempAwardSettings.Skip((CurrentPage - 1) * PageSize).Take(PageSize));
        }
        #endregion

        #region 属性

        #region 搜索关键词
        private string searchKeyword;
        public string SearchKeyword { get => searchKeyword; set => SetProperty(ref searchKeyword, value); }
        #endregion

        #region 选中的奖项
        private Award selectedAwardSetting;
        public Award SelectedAwardSetting { get => selectedAwardSetting; set => SetProperty(ref selectedAwardSetting, value); }
        #endregion

        #region 原始的奖项列表
        private ObservableCollection<Award> awardSettings;
        public ObservableCollection<Award> AwardSettings { get => awardSettings; set => SetProperty(ref awardSettings, value); }
        #endregion

        #region 临时的奖项列表
        private ObservableCollection<Award> tempAwardSettings;
        public ObservableCollection<Award> TempAwardSettings { get => tempAwardSettings; set => SetProperty(ref tempAwardSettings, value); }
        #endregion

        #region 视图展示的奖项列表
        private ObservableCollection<Award> listViewAwardSettings;
        public ObservableCollection<Award> ListViewAwardSettings { get => listViewAwardSettings; set => SetProperty(ref listViewAwardSettings, value); }
        #endregion

        #region

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
            regionManager.RequestNavigate("AwardEditRegion", "AddAwardSetting");
        }
        #endregion

        #region 判断能否删除奖项
        private bool CanUpdateOrDelete()
        {
            return SelectedAwardSetting != null;
        }
        #endregion

        #region 删除奖项命令
        public DelegateCommand<Award> DeleteAwardCommand { get; }
        private void OnDeleteAward(Award award)
        {
            if (award == null) return;
            
            // 使用完全限定名称确保不会有命名空间冲突
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                $"删除奖项「{award.AwardName}」将同时删除所有关联的提名记录！\n确定要继续吗？", 
                "删除确认", 
                System.Windows.MessageBoxButton.OKCancel);
                
            if (result != System.Windows.MessageBoxResult.OK) 
                return;
                
            try
            {
                using (var context = new DataBaseContext())
                {
                    // 先查找所有关联的提名记录
                    var relatedNominations = context.Nominations
                        .Where(n => n.AwardId == award.AwardId)
                        .ToList();
                        
                    if (relatedNominations.Any())
                    {
                        // 先查找并删除所有关联的评论记录
                        var nominationIds = relatedNominations.Select(n => n.NominationId).ToList();
                        var relatedComments = context.CommentRecords
                            .Where(c => nominationIds.Contains(c.NominationId))
                            .ToList();
                            
                        if (relatedComments.Any())
                        {
                            context.CommentRecords.RemoveRange(relatedComments);
                        }
                            
                        // 查找并删除所有关联的投票记录
                        var relatedVotes = context.VoteRecords
                            .Where(v => nominationIds.Contains(v.NominationId))
                            .ToList();
                            
                        if (relatedVotes.Any())
                        {
                            context.VoteRecords.RemoveRange(relatedVotes);
                        }
                            
                        // 删除所有关联的提名记录
                        context.Nominations.RemoveRange(relatedNominations);
                    }
                        
                    // 最后删除奖项本身
                    context.Awards.Remove(award);
                    context.SaveChanges();
                        
                    // 刷新列表
                    TempAwardSettings = AwardSettings = new ObservableCollection<Award>(context.Awards.ToList());
                }
                    
                Growl.SuccessGlobal("奖项删除成功");
                OnSearchAward();
            }
            catch (Exception ex)
            {
                Growl.Error($"删除奖项失败：{ex.Message}");
            }
        }
        #endregion

        #region 编辑奖项命令
        public DelegateCommand<Award> EditAwardCommand { get; }
        private void OnEditAward(Award award)
        {
            if (award == null) { return; }
            //传入参数
            var parameters = new NavigationParameters()
            {
                { "Award", award }
            };
            regionManager.RequestNavigate("AwardEditRegion", "EditAwardSetting", parameters);
        }
        #endregion

        #region 查询奖项命令
        public DelegateCommand SearchAwardCommand { get; }

        private void OnSearchAward()
        {
            using (var context = new DataBaseContext())
            {
                TempAwardSettings = AwardSettings = new ObservableCollection<Award>([.. context.Awards]);
            }

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                TempAwardSettings = new ObservableCollection<Award>(TempAwardSettings.Where(e =>
                        (e.AwardName != null && e.AwardName.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase)) ||
                        (e.AwardDescription != null && e.AwardDescription.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase))
                   ));
            }
            CurrentPage = 1;
            LoadAwards();
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
            // 自动生成文件名：奖项设置_导出_年月日_时分秒.csv
            string defaultFileName = $"奖项设置_导出_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV文件|*.csv|Excel文件|*.xlsx|所有文件|*.*",
                Title = "导出奖项设置数据",
                FileName = defaultFileName,
                DefaultExt = ".csv"
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                // 发布导出事件
                eventAggregator.GetEvent<AwardSettingExportDataEvent>().Publish(saveFileDialog.FileName);
            }
        }
        private void OnExportData(string filePath)
        {
            try
            {
                // 使用UTF-8编码（带BOM）确保Excel可以正确识别中文
                using var writer = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true));
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                // 注册自定义映射，将属性名映射为中文标题
                csv.Context.RegisterClassMap<AwardMap>();
                
                using var context = new DataBaseContext();
                var awards = context.Awards.ToList();
                csv.WriteRecords(awards);
                
                // 添加成功通知
                Growl.SuccessGlobal($"成功导出{awards.Count}条奖项数据到: {filePath}");
            }
            catch (Exception ex)
            {
                // 添加失败通知
                Growl.ErrorGlobal($"导出失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"导出奖项数据出错: {ex.Message}\n{ex.StackTrace}");
            }
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
                LoadAwards();
            }
        }

        private DelegateCommand nextPageCommand;
        public DelegateCommand NextPageCommand => nextPageCommand ??= new DelegateCommand(NextPage);

        private void NextPage()
        {

            if (CurrentPage < MaxPage)
            {
                CurrentPage++;
                LoadAwards();
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
                LoadAwards();
            }
        }
        #endregion

        #region 当每页个数变换时
        private DelegateCommand pageSizeChangedCommand;
        public ICommand PageSizeChangedCommand => pageSizeChangedCommand ??= new DelegateCommand(PageSizeChanged);

        private void PageSizeChanged()
        {
            CurrentPage = 1;
            MaxPage = TotalRecords % PageSize == 0 ? (TotalRecords / PageSize) : ((TotalRecords / PageSize) + 1);
            LoadAwards();
        }
        #endregion

        #endregion
    }

    // 定义奖项数据导出的映射类
    public sealed class AwardMap : ClassMap<Award>
    {
        public AwardMap()
        {
            Map(m => m.AwardId).Name("奖项ID");
            Map(m => m.AwardName).Name("奖项名称");
            Map(m => m.AwardDescription).Name("奖项描述");
            Map(m => m.MaxVoteCount).Name("最大投票次数");
        }
    }
}
