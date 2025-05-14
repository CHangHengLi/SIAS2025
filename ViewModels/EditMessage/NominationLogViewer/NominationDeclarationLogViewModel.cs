using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CsvHelper;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SIASGraduate.Context;
using SIASGraduate.Models;
// 添加别名以解决Window类型不明确的问题
using SysWindow = System.Windows.Window;

namespace SIASGraduate.ViewModels.EditMessage.NominationLogViewer
{
    /// <summary>
    /// 申报日志查看器ViewModel
    /// </summary>
    public class NominationDeclarationLogViewModel : BindableBase
    {
        #region 属性

        private int _declarationId;
        private Award _award;
        private string _nominatedName;
        private Department _department;
        private string _statusText;
        private ObservableCollection<NominationLog> _logs;
        private bool _isLoading;
        private bool _isAllLogs;
        private DateTime _lastLoadTime = DateTime.MinValue;
        private DelegateCommand _refreshCommand;
        private DelegateCommand _exportCommand;
        private DelegateCommand _loadMoreCommand;

        // 缓存过期时间（分钟）
        private const int CACHE_EXPIRY_MINUTES = 2;

        // 初始加载数量和增量加载数量
        private const int INITIAL_LOAD_LIMIT = 100;
        private const int LOAD_MORE_INCREMENT = 100;
        private int _currentLoadedCount = 0;
        private bool _hasMoreToLoad = false;

        /// <summary>
        /// 奖项
        /// </summary>
        public Award Award
        {
            get => _award;
            set => SetProperty(ref _award, value);
        }

        /// <summary>
        /// 被提名人姓名
        /// </summary>
        public string NominatedName
        {
            get => _nominatedName;
            set => SetProperty(ref _nominatedName, value);
        }

        /// <summary>
        /// 部门
        /// </summary>
        public Department Department
        {
            get => _department;
            set => SetProperty(ref _department, value);
        }

        /// <summary>
        /// 状态文本
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>
        /// 申报ID, 如果为-1则表示查看所有日志
        /// </summary>
        public int NominationDeclarationId
        {
            get => _declarationId;
            set => SetProperty(ref _declarationId, value);
        }

        /// <summary>
        /// 日志数据集合
        /// </summary>
        public ObservableCollection<NominationLog> Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 是否在查看所有日志
        /// </summary>
        public bool IsAllLogs
        {
            get => _isAllLogs;
            set => SetProperty(ref _isAllLogs, value);
        }

        /// <summary>
        /// 缓存是否有效
        /// </summary>
        private bool IsCacheValid => (DateTime.Now - _lastLoadTime).TotalMinutes < CACHE_EXPIRY_MINUTES;

        /// <summary>
        /// 是否有更多数据可以加载
        /// </summary>
        public bool HasMoreToLoad
        {
            get => _hasMoreToLoad;
            set => SetProperty(ref _hasMoreToLoad, value);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand
        {
            get
            {
                return _refreshCommand ??= new DelegateCommand(async () =>
                {
                    _currentLoadedCount = 0;
                    await LoadLogsAsync(true);
                });
            }
        }

        /// <summary>
        /// 导出命令
        /// </summary>
        public DelegateCommand ExportCommand
        {
            get
            {
                return _exportCommand ??= new DelegateCommand(() =>
                {
                    ExportLogs();
                }, () => Logs != null && Logs.Count > 0);
            }
        }

        /// <summary>
        /// 加载更多命令
        /// </summary>
        public DelegateCommand LoadMoreCommand
        {
            get
            {
                return _loadMoreCommand ??= new DelegateCommand(async () =>
                {
                    await LoadMoreLogsAsync();
                }, () => HasMoreToLoad && !IsLoading);
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public NominationDeclarationLogViewModel()
        {
            // 构造函数中不需要再初始化命令，因为已经在属性中初始化
        }

        #endregion

        #region 方法

        /// <summary>
        /// 加载日志数据
        /// </summary>
        /// <param name="forceReload">是否强制重新加载，忽略缓存</param>
        public async Task LoadLogsAsync(bool forceReload = false)
        {
            // 如果已经在加载中，则返回
            if (IsLoading)
                return;

            // 如果缓存有效且已有数据，且非强制刷新，则直接返回
            if (!forceReload && IsCacheValid && Logs != null && Logs.Count > 0)
                return;

            try
            {
                IsLoading = true;

                // 首次加载或刷新时重置计数
                if (forceReload || Logs == null || Logs.Count == 0)
                {
                    _currentLoadedCount = 0;
                }

                await Task.Run(async () =>
                {
                    using (var context = new DataBaseContext())
                    {
                        // 1. 优化查询性能：明确选择需要的字段
                        var baseQuery = context.NominationLogs
                            .AsNoTracking()
                            .AsSplitQuery();

                        // 分别加载三个导航属性
                        baseQuery = baseQuery.Include(l => l.OperatorEmployee);
                        baseQuery = baseQuery.Include(l => l.OperatorAdmin);
                        baseQuery = baseQuery.Include(l => l.OperatorSupAdmin);

                        // 加载Declaration及其相关数据
                        baseQuery = baseQuery.Include(l => l.Declaration)
                            .ThenInclude(d => d.Award);

                        // 加载被提名者信息
                        baseQuery = baseQuery.Include(l => l.Declaration)
                            .ThenInclude(d => d.NominatedEmployee);

                        baseQuery = baseQuery.Include(l => l.Declaration)
                            .ThenInclude(d => d.NominatedAdmin);

                        // 加载部门信息
                        baseQuery = baseQuery.Include(l => l.Declaration)
                            .ThenInclude(d => d.Department);

                        // 筛选条件
                        if (NominationDeclarationId > 0)
                        {
                            baseQuery = baseQuery.Where(l => l.DeclarationId == NominationDeclarationId);
                            IsAllLogs = false;
                        }
                        else
                        {
                            IsAllLogs = true;

                            // 更新标题
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (Application.Current.Windows.OfType<SysWindow>().SingleOrDefault(w => w.IsActive) is SysWindow window)
                                {
                                    window.Title = "所有申报日志";
                                }
                            });
                        }

                        // 2. 检查总记录数 - 这个查询更快，因为不需要加载所有数据
                        var totalCount = await baseQuery.CountAsync();

                        // 3. 限制加载数量，使用分页加载策略
                        var logs = await baseQuery
                            .OrderByDescending(l => l.OperationTime)
                            .Skip(_currentLoadedCount)
                            .Take(INITIAL_LOAD_LIMIT)
                            .ToListAsync(); // 直接获取完整的日志对象

                        // 4. 更新UI
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 首次加载或刷新时创建新集合
                            if (forceReload || Logs == null)
                            {
                                Logs = new ObservableCollection<NominationLog>();
                            }

                            // 添加到集合
                            foreach (var log in logs)
                            {
                                Logs.Add(log);
                            }

                            // 更新已加载的数量和是否有更多数据可以加载
                            _currentLoadedCount += logs.Count;
                            HasMoreToLoad = _currentLoadedCount < totalCount;

                            // 确保命令状态更新
                            CommandManager.InvalidateRequerySuggested();
                            ExportCommand.RaiseCanExecuteChanged();
                        });
                    }
                });

                // 更新加载时间
                _lastLoadTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"加载日志数据出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                // 确保命令状态最终更新
                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>
        /// 加载更多日志数据
        /// </summary>
        private async Task LoadMoreLogsAsync()
        {
            if (IsLoading || !HasMoreToLoad)
                return;

            await LoadLogsAsync(false);
        }

        /// <summary>
        /// 导出日志
        /// </summary>
        private void ExportLogs()
        {
            if (Logs == null || Logs.Count == 0)
            {
                Growl.WarningGlobal("没有可导出的日志数据");
                return;
            }

            try
            {
                // 显示保存对话框
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV文件|*.csv",
                    Title = "导出日志数据",
                    FileName = IsAllLogs ? "所有申报日志.csv" : $"申报ID{NominationDeclarationId}的日志.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                IsLoading = true;
                                Growl.InfoGlobal("正在导出日志，请稍候...");
                            });

                            // 创建简单的导出视图模型，避免在CSV映射中使用复杂表达式
                            var exportLogs = Logs.Select(log => new NominationLogExport
                            {
                                LogId = log.LogId,
                                DeclarationId = log.DeclarationId,
                                OperationTypeText = log.OperationTypeText,
                                OperationTime = log.OperationTime,
                                OperatorName = log.OperatorName,
                                AwardName = GetAwardName(log),
                                NomineeName = GetNomineeName(log),
                                Content = log.Content
                            }).ToList();

                            // 使用UTF-8编码加BOM，便于Excel正确识别中文
                            using (var writer = new StreamWriter(saveFileDialog.FileName, false, new UTF8Encoding(true)))
                            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                            {
                                // 配置标题
                                csv.Context.Configuration.HasHeaderRecord = true;

                                // 列名映射
                                csv.WriteHeader<NominationLogExport>();
                                csv.NextRecord();

                                // 写入数据
                                csv.WriteRecords(exportLogs);
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Growl.SuccessGlobal($"成功导出 {Logs.Count} 条日志记录到 {saveFileDialog.FileName}");
                                IsLoading = false;
                            });
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Growl.ErrorGlobal($"导出日志失败: {ex.Message}");
                                IsLoading = false;
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"导出日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取奖项名称
        /// </summary>
        private string GetAwardName(NominationLog log)
        {
            try
            {
                if (log.Declaration == null) return "";
                if (log.Declaration.Award == null) return "";
                return log.Declaration.Award.AwardName ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 获取被提名人姓名
        /// </summary>
        private string GetNomineeName(NominationLog log)
        {
            try
            {
                if (log.Declaration == null) return "";
                if (log.Declaration.NominatedEmployee != null) return log.Declaration.NominatedEmployee.EmployeeName ?? "";
                if (log.Declaration.NominatedAdmin != null) return log.Declaration.NominatedAdmin.AdminName ?? "";
                return "";
            }
            catch
            {
                return "";
            }
        }

        #endregion
    }

    /// <summary>
    /// 用于导出的日志视图模型
    /// </summary>
    public class NominationLogExport
    {
        [CsvHelper.Configuration.Attributes.Name("日志ID")]
        public int LogId { get; set; }

        [CsvHelper.Configuration.Attributes.Name("申报ID")]
        public int DeclarationId { get; set; }

        [CsvHelper.Configuration.Attributes.Name("操作类型")]
        public string OperationTypeText { get; set; }

        [CsvHelper.Configuration.Attributes.Name("操作时间")]
        [CsvHelper.Configuration.Attributes.Format("yyyy-MM-dd HH:mm:ss")]
        public DateTime OperationTime { get; set; }

        [CsvHelper.Configuration.Attributes.Name("操作人")]
        public string OperatorName { get; set; }

        [CsvHelper.Configuration.Attributes.Name("关联奖项")]
        public string AwardName { get; set; }

        [CsvHelper.Configuration.Attributes.Name("被提名人")]
        public string NomineeName { get; set; }

        [CsvHelper.Configuration.Attributes.Name("操作内容")]
        public string Content { get; set; }
    }
}
