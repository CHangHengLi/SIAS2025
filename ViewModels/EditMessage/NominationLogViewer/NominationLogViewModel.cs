using _2025毕业设计.Context;
using _2025毕业设计.Models;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace _2025毕业设计.ViewModels.EditMessage.NominationLogViewer
{
    /// <summary>
    /// 申报日志查看器ViewModel
    /// 使用Prism MVVM模式实现
    /// </summary>
    public class NominationLogViewModel : BindableBase
    {
        #region 属性

        private ObservableCollection<NominationLog> _logs = new ObservableCollection<NominationLog>();
        /// <summary>
        /// 日志数据集合
        /// </summary>
        public ObservableCollection<NominationLog> Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        private int _currentPage = 1;
        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    LoadDataAsync().ConfigureAwait(false);
                }
            }
        }

        private int _pageSize = 20;
        /// <summary>
        /// 每页显示记录数
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    _currentPage = 1;
                    RaisePropertyChanged(nameof(CurrentPage));
                    LoadDataAsync().ConfigureAwait(false);
                }
            }
        }

        private List<int> _pageSizeOptions = new List<int> { 20, 50, 100, 200 };
        /// <summary>
        /// 页大小选项
        /// </summary>
        public List<int> PageSizeOptions
        {
            get => _pageSizeOptions;
            set => SetProperty(ref _pageSizeOptions, value);
        }

        private int _totalRecords;
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalRecords
        {
            get => _totalRecords;
            set => SetProperty(ref _totalRecords, value);
        }

        private int _maxPage = 1;
        /// <summary>
        /// 最大页数
        /// </summary>
        public int MaxPage
        {
            get => _maxPage;
            set => SetProperty(ref _maxPage, value);
        }

        private bool _isLoading;
        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 命令

        private DelegateCommand _initializeCommand;
        /// <summary>
        /// 初始化命令
        /// </summary>
        public DelegateCommand InitializeCommand =>
            _initializeCommand ?? (_initializeCommand = new DelegateCommand(async () => await InitializeAsync()));

        private DelegateCommand _refreshCommand;
        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand =>
            _refreshCommand ?? (_refreshCommand = new DelegateCommand(async () => await LoadDataAsync()));

        private DelegateCommand _firstPageCommand;
        /// <summary>
        /// 首页命令
        /// </summary>
        public DelegateCommand FirstPageCommand =>
            _firstPageCommand ?? (_firstPageCommand = new DelegateCommand(
                () => { CurrentPage = 1; },
                () => CurrentPage > 1
            ).ObservesProperty(() => CurrentPage));

        private DelegateCommand _previousPageCommand;
        /// <summary>
        /// 上一页命令
        /// </summary>
        public DelegateCommand PreviousPageCommand =>
            _previousPageCommand ?? (_previousPageCommand = new DelegateCommand(
                () => { CurrentPage--; },
                () => CurrentPage > 1
            ).ObservesProperty(() => CurrentPage));

        private DelegateCommand _nextPageCommand;
        /// <summary>
        /// 下一页命令
        /// </summary>
        public DelegateCommand NextPageCommand =>
            _nextPageCommand ?? (_nextPageCommand = new DelegateCommand(
                () => { CurrentPage++; },
                () => CurrentPage < MaxPage
            ).ObservesProperty(() => CurrentPage).ObservesProperty(() => MaxPage));

        private DelegateCommand _lastPageCommand;
        /// <summary>
        /// 末页命令
        /// </summary>
        public DelegateCommand LastPageCommand =>
            _lastPageCommand ?? (_lastPageCommand = new DelegateCommand(
                () => { CurrentPage = MaxPage; },
                () => CurrentPage < MaxPage
            ).ObservesProperty(() => CurrentPage).ObservesProperty(() => MaxPage));

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public NominationLogViewModel()
        {
            // 构造函数不需要执行LoadDataAsync，因为页面加载完成后会触发InitializeCommand
        }

        #endregion

        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private async Task InitializeAsync()
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// 加载指定页的数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                
                // 使用无需UI线程的方式清空现有数据
                Logs = new ObservableCollection<NominationLog>();
                
                await Task.Run(async () =>
                {
                    using (var context = new DataBaseContext())
                    {
                        // 分两步执行查询，先获取分页数据的ID，再获取详细信息
                        // 第一步：使用轻量级查询获取分页的日志ID列表
                        var logIds = await context.NominationLogs
                            .AsNoTracking()
                            .OrderByDescending(l => l.OperationTime)
                            .Skip((_currentPage - 1) * _pageSize)
                            .Take(_pageSize)
                            .Select(l => l.LogId)
                            .ToListAsync();
                        
                        // 第二步：只获取需要的日志记录的详细信息
                        var logs = await context.NominationLogs
                            .AsNoTracking()
                            .Where(l => logIds.Contains(l.LogId))
                            .Include(l => l.Declaration)
                                .ThenInclude(d => d.Award)
                            .Include(l => l.OperatorEmployee)
                                .ThenInclude(e => e.Department)
                            .Include(l => l.OperatorAdmin)
                            .Include(l => l.OperatorSupAdmin)
                            .OrderByDescending(l => l.OperationTime)
                            .ToListAsync();
                            
                        // 获取总记录数 - 使用简单计数，避免复杂查询
                        int totalCount = await context.NominationLogs.CountAsync();
                        
                        // 在主线程外准备要显示的数据
                        var newLogs = new ObservableCollection<NominationLog>(logs);
                        int maxPage = (int)Math.Ceiling((double)totalCount / _pageSize);
                        
                        // 批量更新UI属性，减少UI线程阻塞时间
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Logs = newLogs;
                            TotalRecords = totalCount;
                            MaxPage = maxPage;
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"加载日志数据出错: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
} 