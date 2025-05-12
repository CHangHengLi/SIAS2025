using SIASGraduate.Context;
using SIASGraduate.Models;
using SIASGraduate.ViewModels.EditMessage.NominationDetailsWindows;
using SIASGraduate.Views.EditMessage.NominationDetailsWindows;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIASGraduate.ViewModels.Pages
{
    public class VoteResultViewModel : BindableBase
    {
        #region 属性
        private ObservableCollection<Nomination> _nominations;
        /// <summary>
        /// 提名列表
        /// </summary>
        public ObservableCollection<Nomination> Nominations
        {
            get { return _nominations; }
            set { SetProperty(ref _nominations, value); }
        }

        private int _nominationsCount;
        /// <summary>
        /// 提名数量
        /// </summary>
        public int NominationsCount
        {
            get { return _nominationsCount; }
            set { SetProperty(ref _nominationsCount, value); }
        }

        private string _searchText;
        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get { return _searchText; }
            set { SetProperty(ref _searchText, value); }
        }

        private bool _isLoading;
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set { SetProperty(ref _isLoading, value); }
        }
        
        /// <summary>
        /// 已加载的提名ID集合
        /// </summary>
        private HashSet<int> LoadedNominationIds { get; } = new HashSet<int>();
        
        /// <summary>
        /// 记录页面加载日志
        /// </summary>
        public void LogPageLoaded()
        {
            Debug.WriteLine("投票结果界面已加载");
        }
        
        /// <summary>
        /// 处理行加载事件
        /// </summary>
        public void HandleRowLoading(Nomination nomination)
        {
            if (nomination != null && !LoadedNominationIds.Contains(nomination.NominationId))
            {
                LoadedNominationIds.Add(nomination.NominationId);
                Debug.WriteLine($"行加载: 提名ID={nomination.NominationId}");
            }
        }
        
        /// <summary>
        /// 处理行卸载事件
        /// </summary>
        public void HandleRowUnloading(Nomination nomination)
        {
            if (nomination != null && LoadedNominationIds.Contains(nomination.NominationId))
            {
                LoadedNominationIds.Remove(nomination.NominationId);
                Debug.WriteLine($"行卸载: 提名ID={nomination.NominationId}");
            }
        }
        #endregion

        #region 命令
        private DelegateCommand _searchCommand;
        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand =>
            _searchCommand ?? (_searchCommand = new DelegateCommand(ExecuteSearchCommand));

        private DelegateCommand _exportResultCommand;
        /// <summary>
        /// 导出报表命令
        /// </summary>
        public DelegateCommand ExportResultCommand =>
            _exportResultCommand ?? (_exportResultCommand = new DelegateCommand(ExecuteExportResultCommand));
            
        private DelegateCommand<Nomination> _displayNominationDetailsCommand;
        /// <summary>
        /// 显示提名详情命令
        /// </summary>
        public DelegateCommand<Nomination> DisplayNominationDetailsCommand =>
            _displayNominationDetailsCommand ?? (_displayNominationDetailsCommand = new DelegateCommand<Nomination>(ExecuteDisplayNominationDetailsCommand));

        private DelegateCommand<DataGridRowEventArgs> _loadingRowCommand;
        /// <summary>
        /// 行加载命令
        /// </summary>
        public DelegateCommand<DataGridRowEventArgs> LoadingRowCommand =>
            _loadingRowCommand ?? (_loadingRowCommand = new DelegateCommand<DataGridRowEventArgs>(ExecuteLoadingRowCommand));

        private DelegateCommand<DataGridRowEventArgs> _unloadingRowCommand;
        /// <summary>
        /// 行卸载命令
        /// </summary>
        public DelegateCommand<DataGridRowEventArgs> UnloadingRowCommand =>
            _unloadingRowCommand ?? (_unloadingRowCommand = new DelegateCommand<DataGridRowEventArgs>(ExecuteUnloadingRowCommand));

        private DelegateCommand<MouseWheelEventArgs> _previewMouseWheelCommand;
        /// <summary>
        /// 鼠标滚轮预览命令
        /// </summary>
        public DelegateCommand<MouseWheelEventArgs> PreviewMouseWheelCommand =>
            _previewMouseWheelCommand ?? (_previewMouseWheelCommand = new DelegateCommand<MouseWheelEventArgs>(ExecutePreviewMouseWheelCommand));

        private DelegateCommand _pageLoadedCommand;
        /// <summary>
        /// 页面加载命令
        /// </summary>
        public DelegateCommand PageLoadedCommand =>
            _pageLoadedCommand ?? (_pageLoadedCommand = new DelegateCommand(ExecutePageLoadedCommand));

        private DelegateCommand<Nomination> _viewNominationDetailsCommand;
        /// <summary>
        /// 查看提名详情命令（与首页详情展示一致）
        /// </summary>
        public DelegateCommand<Nomination> ViewNominationDetailsCommand =>
            _viewNominationDetailsCommand ?? (_viewNominationDetailsCommand = new DelegateCommand<Nomination>(ExecuteViewNominationDetailsCommand));
        #endregion

        #region 构造函数
        public VoteResultViewModel()
        {
            // 初始化集合
            Nominations = new ObservableCollection<Nomination>();
            SearchText = string.Empty;
            
            // 加载数据
            LoadData();
        }
        #endregion

        #region 方法
        /// <summary>
        /// 执行显示提名详情命令
        /// </summary>
        private void ExecuteDisplayNominationDetailsCommand(Nomination nomination)
        {
            if (nomination == null)
                return;
                
            // 显示详细信息
            HandyControl.Controls.MessageBox.Show(
                $"提名ID: {nomination.NominationId}\n" +
                $"提名对象: {(nomination.NominatedEmployee != null ? nomination.NominatedEmployee.EmployeeName : nomination.NominatedAdmin?.AdminName)}\n" +
                $"提名奖项: {nomination.Award?.AwardName}\n" +
                $"所属部门: {nomination.Department?.DepartmentName}\n" +
                $"得票数: {nomination.VoteRecords?.Count ?? 0}\n" +
                $"提名时间: {nomination.NominationTime}",
                "提名详情", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        /// <summary>
        /// 查看提名详情
        /// </summary>
        private async void ExecuteViewNominationDetailsCommand(Nomination nomination)
        {
            if (nomination == null)
                return;
            
            // 显示加载状态
            IsLoading = true;
            
            try
            {
                // 从数据库加载完整的提名数据
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
                        // 创建并显示详情窗口
                        var detailsWindow = new NominationDetailsWindow(fullNomination);
                        
                        // 隐藏加载状态
                        IsLoading = false;
                        
                        // 显示窗口
                        detailsWindow.ShowDialog();
                    }
                    else
                    {
                        // 数据不存在时显示错误消息
                        Growl.ErrorGlobal("找不到该提名的详细信息");
                        IsLoading = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"加载详情时出错: {ex.Message}");
                Debug.WriteLine($"加载详情异常: {ex}");
                IsLoading = false;
            }
        }

        /// <summary>
        /// 添加详情行
        /// </summary>
        private void AddDetailRow(Grid grid, int rowIndex, string label, string value, bool isMultiLine = false)
        {
            // 标签
            var labelBlock = new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 10, 5)
            };
            Grid.SetRow(labelBlock, rowIndex);
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);
            
            // 值
            if (isMultiLine)
            {
                // 多行文本 - 使用完全限定名称解决命名空间冲突
                var textBox = new System.Windows.Controls.TextBox
                {
                    Text = value,
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(5),
                    Height = 100,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                Grid.SetRow(textBox, rowIndex);
                Grid.SetColumn(textBox, 1);
                grid.Children.Add(textBox);
            }
            else
            {
                // 单行文本
                var valueBlock = new TextBlock
                {
                    Text = value,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                Grid.SetRow(valueBlock, rowIndex);
                Grid.SetColumn(valueBlock, 1);
                grid.Children.Add(valueBlock);
            }
        }
        
        /// <summary>
        /// 加载数据
        /// </summary>
        private async void LoadData()
        {
            try
            {
                IsLoading = true;
                
                // 清空现有数据
                Nominations.Clear();
                
                // 从数据库加载提名记录
                using (var context = new DataBaseContext())
                {
                    // 配置数据库命令超时时间
                    context.Database.SetCommandTimeout(60); // 设置为60秒
                    
                    // 先计算总记录数
                    NominationsCount = await context.Nominations.CountAsync();
                    
                    // 限制查询结果数量
                    const int maxResults = 200;
                    
                    // 使用优化的查询
                    var query = context.Nominations
                        .AsNoTracking() // 不跟踪实体变化
                        .AsSplitQuery() // 拆分为多个查询以提高性能
                        .Select(n => new 
                        {
                            NominationId = n.NominationId,
                            NominatedEmployeeName = n.NominatedEmployee != null ? n.NominatedEmployee.EmployeeName : null,
                            NominatedAdminName = n.NominatedAdmin != null ? n.NominatedAdmin.AdminName : null,
                            AwardName = n.Award != null ? n.Award.AwardName : null,
                            DepartmentName = n.Department != null ? n.Department.DepartmentName : null,
                            Introduction = n.Introduction,
                            NominateReason = n.NominateReason,
                            VotesCount = n.VoteRecords.Count,
                            NominationTime = n.NominationTime,
                            // 只选取需要的关联实体ID
                            NominatedEmployeeId = n.NominatedEmployeeId,
                            NominatedAdminId = n.NominatedAdminId,
                            AwardId = n.AwardId,
                            DepartmentId = n.DepartmentId
                        })
                        .OrderByDescending(n => n.VotesCount) // 按票数排序
                        .ThenByDescending(n => n.NominationTime) // 票数相同时按时间排序
                        .Take(maxResults);
                    
                    // 执行查询
                    var nominationsData = await query.ToListAsync();
                    
                    // 在内存中构建显示对象
                    foreach (var item in nominationsData)
                    {
                        var nomination = new Nomination
                        {
                            NominationId = item.NominationId,
                            NominationTime = item.NominationTime,
                            Introduction = item.Introduction,
                            NominateReason = item.NominateReason,
                            AwardId = item.AwardId,
                            DepartmentId = item.DepartmentId,
                            NominatedEmployeeId = item.NominatedEmployeeId,
                            NominatedAdminId = item.NominatedAdminId,
                            
                            // 创建最小化的关联对象，只包含显示所需的字段
                            Award = item.AwardName == null ? null : new Award
                            {
                                AwardId = item.AwardId,
                                AwardName = item.AwardName
                            },
                            
                            Department = item.DepartmentName == null ? null : new Department
                            {
                                DepartmentId = item.DepartmentId ?? 0,
                                DepartmentName = item.DepartmentName
                            },
                            
                            NominatedEmployee = item.NominatedEmployeeName == null ? null : new Employee
                            {
                                EmployeeId = item.NominatedEmployeeId ?? 0,
                                EmployeeName = item.NominatedEmployeeName,
                                EmployeePassword = "placeholder" // 填充required字段
                            },
                            
                            NominatedAdmin = item.NominatedAdminName == null ? null : new Admin
                            {
                                AdminId = item.NominatedAdminId ?? 0,
                                AdminName = item.NominatedAdminName,
                                AdminPassword = "placeholder" // 填充required字段
                            }
                        };
                        
                        // 创建表示投票数量的集合，但不包含实际投票数据
                        var voteRecords = new List<VoteRecord>();
                        for (int i = 0; i < item.VotesCount; i++)
                        {
                            voteRecords.Add(new VoteRecord { NominationId = nomination.NominationId });
                        }
                        nomination.VoteRecords = new ObservableCollection<VoteRecord>(voteRecords);
                        
                        Nominations.Add(nomination);
                    }
                    
                    // 如果结果数量少于总数，显示提示
                    if (NominationsCount > maxResults)
                    {
                        Growl.InfoGlobal($"显示 {maxResults} 条记录，共 {NominationsCount} 条。请使用搜索功能查看更多。");
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"加载数据时发生错误: {ex.Message}");
                Debug.WriteLine($"加载数据异常详情: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行搜索命令
        /// </summary>
        private async void ExecuteSearchCommand()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    // 如果搜索框为空，则加载所有数据
                    LoadData();
                    return;
                }
                
                IsLoading = true;
                
                // 清空现有数据
                Nominations.Clear();
                
                // 搜索关键词（去除前后空格并转为小写）
                string keyword = SearchText.Trim().ToLower();
                
                // 从数据库搜索匹配的提名记录
                using (var context = new DataBaseContext())
                {
                    // 配置数据库命令超时时间
                    context.Database.SetCommandTimeout(60); // 设置为60秒
                    
                    // 限制查询结果数量
                    const int maxResults = 200;
                    
                    // 使用优化的查询
                    var query = context.Nominations
                        .AsNoTracking() // 不跟踪实体变化
                        .AsSplitQuery() // 拆分为多个查询
                        .Where(n => 
                            (n.NominatedEmployee != null && EF.Functions.Like(n.NominatedEmployee.EmployeeName.ToLower(), $"%{keyword}%")) ||
                            (n.NominatedAdmin != null && EF.Functions.Like(n.NominatedAdmin.AdminName.ToLower(), $"%{keyword}%")) ||
                            (n.Award != null && EF.Functions.Like(n.Award.AwardName.ToLower(), $"%{keyword}%")) ||
                            (n.Department != null && EF.Functions.Like(n.Department.DepartmentName.ToLower(), $"%{keyword}%")) ||
                            (n.Introduction != null && EF.Functions.Like(n.Introduction.ToLower(), $"%{keyword}%")) ||
                            (n.NominateReason != null && EF.Functions.Like(n.NominateReason.ToLower(), $"%{keyword}%"))
                        )
                        .Select(n => new 
                        {
                            NominationId = n.NominationId,
                            NominatedEmployeeName = n.NominatedEmployee != null ? n.NominatedEmployee.EmployeeName : null,
                            NominatedAdminName = n.NominatedAdmin != null ? n.NominatedAdmin.AdminName : null,
                            AwardName = n.Award != null ? n.Award.AwardName : null,
                            DepartmentName = n.Department != null ? n.Department.DepartmentName : null,
                            Introduction = n.Introduction,
                            NominateReason = n.NominateReason,
                            VotesCount = n.VoteRecords.Count,
                            NominationTime = n.NominationTime,
                            // 只选取需要的关联实体ID
                            NominatedEmployeeId = n.NominatedEmployeeId,
                            NominatedAdminId = n.NominatedAdminId,
                            AwardId = n.AwardId,
                            DepartmentId = n.DepartmentId
                        })
                        .OrderByDescending(n => n.VotesCount)
                        .ThenByDescending(n => n.NominationTime)
                        .Take(maxResults);
                    
                    // 执行查询
                    var nominationsData = await query.ToListAsync();
                    
                    // 在内存中构建显示对象
                    foreach (var item in nominationsData)
                    {
                        var nomination = new Nomination
                        {
                            NominationId = item.NominationId,
                            NominationTime = item.NominationTime,
                            Introduction = item.Introduction,
                            NominateReason = item.NominateReason,
                            AwardId = item.AwardId,
                            DepartmentId = item.DepartmentId,
                            NominatedEmployeeId = item.NominatedEmployeeId,
                            NominatedAdminId = item.NominatedAdminId,
                            
                            // 创建最小化的关联对象，只包含显示所需的字段
                            Award = item.AwardName == null ? null : new Award
                            {
                                AwardId = item.AwardId,
                                AwardName = item.AwardName
                            },
                            
                            Department = item.DepartmentName == null ? null : new Department
                            {
                                DepartmentId = item.DepartmentId ?? 0,
                                DepartmentName = item.DepartmentName
                            },
                            
                            NominatedEmployee = item.NominatedEmployeeName == null ? null : new Employee
                            {
                                EmployeeId = item.NominatedEmployeeId ?? 0,
                                EmployeeName = item.NominatedEmployeeName,
                                EmployeePassword = "placeholder" // 填充required字段
                            },
                            
                            NominatedAdmin = item.NominatedAdminName == null ? null : new Admin
                            {
                                AdminId = item.NominatedAdminId ?? 0,
                                AdminName = item.NominatedAdminName,
                                AdminPassword = "placeholder" // 填充required字段
                            }
                        };
                        
                        // 创建表示投票数量的集合，但不包含实际投票数据
                        var voteRecords = new List<VoteRecord>();
                        for (int i = 0; i < item.VotesCount; i++)
                        {
                            voteRecords.Add(new VoteRecord { NominationId = nomination.NominationId });
                        }
                        nomination.VoteRecords = new ObservableCollection<VoteRecord>(voteRecords);
                        
                        Nominations.Add(nomination);
                    }
                    
                    // 获取满足搜索条件的总记录数
                    NominationsCount = Nominations.Count;
                    
                    // 显示搜索结果提示
                    Growl.InfoGlobal($"搜索结果: 找到 {NominationsCount} 条记录");
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"搜索数据时发生错误: {ex.Message}");
                Debug.WriteLine($"搜索数据异常详情: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行导出报表命令
        /// </summary>
        private async void ExecuteExportResultCommand()
        {
            try
            {
                // 创建保存文件对话框
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV文件 (*.csv)|*.csv",
                    FileName = $"投票结果列表_{DateTime.Now:yyyyMMdd}",
                    Title = "导出投票结果列表"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    IsLoading = true;

                    await Task.Run(() =>
                    {
                        using (var writer = new StreamWriter(saveDialog.FileName, false, System.Text.Encoding.UTF8))
                        {
                            // 写入CSV头
                            writer.WriteLine("提报对象,奖项编号,提报奖项,所属部门,一句话介绍,提名理由,得票数");

                            // 写入数据行
                            foreach (var nomination in Nominations)
                            {
                                string nominatedName = nomination.NominatedEmployee?.EmployeeName ?? nomination.NominatedAdmin?.AdminName ?? "";
                                string awardName = nomination.Award?.AwardName ?? "";
                                string departmentName = nomination.Department?.DepartmentName ?? "";
                                string introduction = nomination.Introduction ?? "";
                                string reason = nomination.NominateReason ?? "";
                                int voteCount = nomination.VoteRecords?.Count ?? 0;
                                
                                // 转义包含逗号的文本
                                if (nominatedName.Contains(",")) nominatedName = $"\"{nominatedName}\"";
                                if (awardName.Contains(",")) awardName = $"\"{awardName}\"";
                                if (departmentName.Contains(",")) departmentName = $"\"{departmentName}\"";
                                if (introduction.Contains(",")) introduction = $"\"{introduction}\"";
                                if (reason.Contains(",")) reason = $"\"{reason}\"";
                                
                                writer.WriteLine($"{nominatedName},{nomination.NominationId},{awardName},{departmentName},{introduction},{reason},{voteCount}");
                            }
                        }
                    });

                    Growl.SuccessGlobal($"已成功导出投票结果列表到: {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"导出数据时发生错误: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 执行页面加载命令
        /// </summary>
        private void ExecutePageLoadedCommand()
        {
            LogPageLoaded();
        }

        /// <summary>
        /// 执行行加载命令
        /// </summary>
        private void ExecuteLoadingRowCommand(DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is Nomination nomination)
            {
                HandleRowLoading(nomination);
                e.Row.Tag = nomination.NominationId; // 保留行标识
            }
        }

        /// <summary>
        /// 执行行卸载命令
        /// </summary>
        private void ExecuteUnloadingRowCommand(DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is Nomination nomination)
            {
                HandleRowUnloading(nomination);
                e.Row.Tag = null; // 清理标识
            }
        }

        /// <summary>
        /// 执行鼠标滚轮预览命令
        /// </summary>
        private void ExecutePreviewMouseWheelCommand(MouseWheelEventArgs e)
        {
            // 标记事件已处理，实际的滚动操作由Behavior处理
            e.Handled = true;
        }
        #endregion
    }
}
