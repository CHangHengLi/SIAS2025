using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using _2025毕业设计.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using SIASGraduate.Common;
using SIASGraduate.Context;
using SIASGraduate.Models;

namespace SIASGraduate.ViewModels
{
    /// <summary>
    /// 被提名者得票数据类
    /// </summary>
    public class NomineeVoteCount
    {
        /// <summary>
        /// 被提名人姓名
        /// </summary>
        public string NomineeName { get; set; }
        
        /// <summary>
        /// 得票数
        /// </summary>
        public int VoteCount { get; set; }
    }
    
    /// <summary>
    /// 首页视图模型
    /// </summary>
    public class HomeViewModel : BindableBase
    {
        #region  属性
        private DispatcherTimer _timer;
        // 添加登录状态检查定时器
        private DispatcherTimer _loginCheckTimer;
        private IRegionManager regionManager;

        // 添加Random实例
        private Random _random = new Random();
        
        // 最小化窗口命令
        private DelegateCommand _minimizeWindowCommand;
        public DelegateCommand MinimizeWindowCommand =>
            _minimizeWindowCommand ??= new DelegateCommand(MinimizeWindow);
            
        // 处理最小化窗口事件
        private void MinimizeWindow()
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }
            
        // 最大化窗口命令
        private DelegateCommand _maximizeWindowCommand;
        public DelegateCommand MaximizeWindowCommand =>
            _maximizeWindowCommand ??= new DelegateCommand(MaximizeWindow);
            
        // 处理最大化窗口事件
        private void MaximizeWindow()
        {
            if (Application.Current.MainWindow.WindowState == WindowState.Maximized)
            {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
            else
            {
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            }
        }
        
        // 鼠标滚轮命令
        private DelegateCommand<MouseWheelEventArgs> _mouseWheelCommand;
        public DelegateCommand<MouseWheelEventArgs> MouseWheelCommand =>
            _mouseWheelCommand ??= new DelegateCommand<MouseWheelEventArgs>(OnMouseWheel);
            
        // 处理鼠标滚轮事件
        private void OnMouseWheel(MouseWheelEventArgs e)
        {
            // 在主窗口层面不拦截滚轮事件，让事件继续传递给子控件
            // 确保不设置e.Handled = true，以便事件能继续路由
            
            // 简单的调试输出
            Debug.WriteLine($"Home 鼠标滚轮事件: Delta={e.Delta}");
        }

        // 水平滚动命令
        private DelegateCommand<MouseWheelEventArgs> _horizontalScrollCommand;
        public DelegateCommand<MouseWheelEventArgs> HorizontalScrollCommand =>
            _horizontalScrollCommand ??= new DelegateCommand<MouseWheelEventArgs>(OnHorizontalScroll);
            
        // 处理水平滚动事件
        private void OnHorizontalScroll(MouseWheelEventArgs e)
        {
            if (e == null) return;
            
            // 获取事件源控件并找到ScrollViewer
            var source = e.Source as DependencyObject;
            if (source != null)
            {
                // 找到名为ShortcutScrollViewer的ScrollViewer
                var scrollViewer = FindVisualParent<System.Windows.Controls.ScrollViewer>(source);
                if (scrollViewer != null && scrollViewer.Name.Equals("ShortcutScrollViewer"))
                {
                    // 判断是否按下了Shift键，如果按下则反向滚动
                    bool isShiftKeyDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    
                    // 计算滚动量
                    double scrollAmount = e.Delta / 3; // 调整速度
                    if (isShiftKeyDown) scrollAmount = -scrollAmount;
                    
                    // 执行水平滚动
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - scrollAmount);
                    
                    // 标记事件为已处理，防止继续传递
                    e.Handled = true;
                }
            }
        }

        // 查找指定类型的视觉父元素
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            // 获取父元素
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            
            // 如果没有父元素，返回null
            if (parentObject == null) return null;
            
            // 如果父元素是我们要找的类型，直接返回
            if (parentObject is T parent) return parent;
            
            // 否则继续向上查找
            return FindVisualParent<T>(parentObject);
        }

        #region 内容区五行行高
        private GridLength row1Visible = GridLength.Auto;
        public GridLength Row1Visible
        {
            get { return row1Visible; }
            set { SetProperty(ref row1Visible, value); }
        }
        private GridLength row2Visible = GridLength.Auto;
        public GridLength Row2Visible
        {
            get { return row2Visible; }
            set { SetProperty(ref row2Visible, value); }
        }
        private GridLength row3Visible = GridLength.Auto;
        public GridLength Row3Visible
        {
            get { return row3Visible; }
            set { SetProperty(ref row3Visible, value); }
        }
        private GridLength row4Visible = GridLength.Auto;
        public GridLength Row4Visible
        {
            get { return row4Visible; }
            set { SetProperty(ref row4Visible, value); }
        }
        private GridLength row5Visible = GridLength.Auto;
        public GridLength Row5Visible
        {
            get { return row5Visible; }
            set { SetProperty(ref row5Visible, value); }
        }
        #endregion

        #region 管理员按钮是否可用
        private bool employeeManagerButtonIsEnableCopy;
        public bool EmployeeManagerButtonIsEnableCopy
        {
            get { return employeeManagerButtonIsEnableCopy; }
            set { SetProperty(ref employeeManagerButtonIsEnableCopy, value); }
        }
        #endregion

        #region 员工按钮是否可用
        private bool employeeManagerButtonIsEnable;
        public bool EmployeeManagerButtonIsEnable
        {
            get { return employeeManagerButtonIsEnable; }
            set { SetProperty(ref employeeManagerButtonIsEnable, value); }
        }
        #endregion

        #region 当前登录用户信息
        private string currentTime;
        public string CurrentTime
        {
            get { return currentTime; }
            set { SetProperty(ref currentTime, value); }
        }
        private string userName;
        public string UserName
        {
            get { return userName; }
            set { SetProperty(ref userName, value); }
        }
        
        // 添加显示姓名属性
        private string displayName;
        public string DisplayName
        {
            get { return displayName; }
            set { SetProperty(ref displayName, value); }
        }
        
        private string password;
        public string Password
        {
            get { return password; }
            set { SetProperty(ref password, value); }
        }
        private int roleId;
        public int RoleId
        {
            get { return roleId; }
            set { SetProperty(ref roleId, value); }
        }

        #endregion

        #endregion

        #region 构造函数
        public HomeViewModel(IRegionManager regionManager)
        {
       
            #region 区域管理器 regionManager
            this.regionManager = regionManager;
            #endregion

            #region 时间显示
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += UpdateTime;
            _timer.Tick += UpdateUserName;
            _timer.Start();
            #endregion
            
            #region 登录状态检查
            // 初始化登录状态检查定时器，但不立即启动
            // 将在 Loaded 方法中启动，确保用户已登录
            _loginCheckTimer = new DispatcherTimer();
            _loginCheckTimer.Interval = TimeSpan.FromSeconds(30);
            _loginCheckTimer.Tick += CheckLoginStatus;
            #endregion

            #region LiveCharts
            // 初始化图表集合
            seriesCollection = new SeriesCollection();
            rankingSeriesCollection = new SeriesCollection();
            nomineeRankingSeriesCollection = new SeriesCollection();
            labels = new List<string>();
            rankingLabels = new List<string>();
            nomineeRankingLabels = new List<string>();
            
            // 初始化坐标轴
            rankingAxisX = new AxesCollection { new Axis { Title = "奖项" } };
            rankingAxisY = new AxesCollection { new Axis { Title = "得票数" } };
            nomineeRankingAxisX = new AxesCollection { new Axis { Title = "候选人" } };
            nomineeRankingAxisY = new AxesCollection { new Axis { Title = "票数" } };
            
            // 初始化饼图标签格式化函数
            PieChartLabelFormat = chartPoint => 
                $"{chartPoint.Y:N0}个 ({chartPoint.Participation:P1})";
                
            PieChartTitleOnlyFormat = chartPoint => 
                $"{chartPoint.Y:N0}个";
            
            // 注意：数据加载移到了Loaded方法中
            #endregion
        }

        #region 退出登录事件
        public DelegateCommand ReStartCommand => new DelegateCommand(() =>
        {
            try
            {
                // 获取当前正在运行的进程
                var currentProcess = Process.GetCurrentProcess();
                // 获取当前执行文件的路径
                var fileName = currentProcess.MainModule?.FileName;
                if (fileName != null)
                {
                    // 启动一个新的进程实例，使用与当前进程相同的可执行文件
                    //Process.Start(new ProcessStartInfo
                    //{
                    //    FileName = fileName, // 指定要启动的可执行文件的路径
                    //    UseShellExecute = true, // 使用操作系统的外壳程序启动进程
                    //    Arguments = string.Join(" ", Environment.GetCommandLineArgs()) // 传递相同的命令行参数
                    //});
                    // 正常退出当前进程，0 表示正常退出代码
                    Environment.Exit(0);
                }
                else
                {
                    Debug.WriteLine("Failed to restart application: FileName is null");
                }
            }
            catch (Exception ex)
            {
                // 捕获并输出异常信息，以便调试
                Debug.WriteLine($"Failed to restart application: {ex.Message}");
            }
        });
        #endregion

        #region 返回主页面
        public DelegateCommand BackHomeCommand => new DelegateCommand(() =>
        {
            Row1Visible = new GridLength(0);
            Row2Visible = GridLength.Auto;
            Row3Visible = GridLength.Auto;
            Row4Visible = GridLength.Auto;
            Row5Visible = GridLength.Auto;
            
            // 重新加载图表数据，确保显示最新数据
            Application.Current.Dispatcher.InvokeAsync(() => {
                LoadChartData();
                LoadVoteDetails(); // 同时刷新投票详情数据
            }, System.Windows.Threading.DispatcherPriority.Background);
        });
        #endregion

        #region 加载静态类里面的属性
        private DelegateCommand loadedCommand;
        public ICommand LoadedCommand => loadedCommand ??= new DelegateCommand(Loaded);


        private void Loaded()
        {
            UserName = CurrentUser.UserName; //获取当前用户名
            UpdateDisplayName(); //获取并设置显示姓名
            Password = CurrentUser.Password; //获取当前用户密码
            RoleId = CurrentUser.RoleId; //1.超级管理员 2.管理员 3.雇员
            
            // 用户已成功登录，启动登录状态检查定时器
            if (!string.IsNullOrEmpty(CurrentUser.Account) && !string.IsNullOrEmpty(CurrentUser.UserName))
            {
                _loginCheckTimer.Start();
            }
            
            if (RoleId == 1)
            {
                IsButtonVisible = Visibility.Visible; EmployeeManagerButtonIsEnable = true;
                IsButtonVisibleCopy = Visibility.Visible; EmployeeManagerButtonIsEnableCopy = true;
                
                // 超级管理员可以看到所有按钮
                EmployeeManagerButtonVisible = Visibility.Visible;
                AwardSettingButtonVisible = Visibility.Visible;
                AwardNominateButtonVisible = Visibility.Visible;
                NominationDeclarationButtonVisible = Visibility.Visible;
            }
            else if (RoleId == 2)
            {
                IsButtonVisible = Visibility.Visible; EmployeeManagerButtonIsEnable = true;
                IsButtonVisibleCopy = Visibility.Collapsed; EmployeeManagerButtonIsEnableCopy = false;
                
                // 管理员可以看到所有按钮
                EmployeeManagerButtonVisible = Visibility.Visible;
                AwardSettingButtonVisible = Visibility.Visible;
                AwardNominateButtonVisible = Visibility.Visible;
                NominationDeclarationButtonVisible = Visibility.Visible;
            }
            else if (RoleId == 3)
            {
                // 修改为可见和可用，使员工能够访问提名申报功能
                IsButtonVisible = Visibility.Visible; 
                EmployeeManagerButtonIsEnable = true;
                
                // 其他管理功能按钮保持不可见
                IsButtonVisibleCopy = Visibility.Collapsed;
                EmployeeManagerButtonIsEnableCopy = false;
                
                // 员工不能看到这三个按钮
                EmployeeManagerButtonVisible = Visibility.Collapsed;
                AwardSettingButtonVisible = Visibility.Collapsed;
                AwardNominateButtonVisible = Visibility.Collapsed;
                
                // 但是员工可以看到提名申报按钮
                NominationDeclarationButtonVisible = Visibility.Visible;
            }
            else
            {
                IsButtonVisible = Visibility.Collapsed;
                
                // 未知角色隐藏所有特定按钮
                EmployeeManagerButtonVisible = Visibility.Collapsed;
                AwardSettingButtonVisible = Visibility.Collapsed;
                AwardNominateButtonVisible = Visibility.Collapsed;
                NominationDeclarationButtonVisible = Visibility.Collapsed;
            }
            
            // 在UI加载完成后加载图表数据
            Application.Current.Dispatcher.InvokeAsync(() => {
                LoadChartData();
                LoadVoteDetails(); // 加载投票详情数据
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
        #endregion

        #region 更新时间
        private void UpdateTime(object sender, EventArgs e)
        {
            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void UpdateUserName(object sender, EventArgs e)
        {
            UserName = CurrentUser.UserName;
            UpdateDisplayName();
        }
        
        // 根据当前用户角色更新显示姓名
        private void UpdateDisplayName()
        {
            try
            {
                using (var context = new DataBaseContext())
                {
                    switch (CurrentUser.RoleId)
                    {
                        case 1: // 超级管理员
                            var supAdmin = context.SupAdmins.FirstOrDefault(sa => sa.Account == CurrentUser.Account);
                            if (supAdmin != null)
                            {
                                DisplayName = supAdmin.SupAdminName;
                            }
                            break;
                        case 2: // 管理员
                            var admin = context.Admins.FirstOrDefault(a => a.Account == CurrentUser.Account);
                            if (admin != null)
                            {
                                DisplayName = admin.AdminName;
                            }
                            break;
                        case 3: // 员工
                            var employee = context.Employees.FirstOrDefault(e => e.Account == CurrentUser.Account);
                            if (employee != null)
                            {
                                DisplayName = employee.EmployeeName;
                            }
                            break;
                        default:
                            DisplayName = CurrentUser.UserName; // 默认使用账号
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新显示姓名时出错: {ex.Message}");
                DisplayName = CurrentUser.UserName; // 出错时使用账号
            }
        }
        #endregion

        #region 登录状态检查
        /// <summary>
        /// 检查用户登录状态是否合法
        /// </summary>
        private void CheckLoginStatus(object sender, EventArgs e)
        {
            try
            {
                // 首先检查当前是否有用户登录，如果没有则不执行后续检查
                if (string.IsNullOrEmpty(CurrentUser.Account) || string.IsNullOrEmpty(CurrentUser.UserName))
                {
                    return; // 没有用户登录，直接返回
                }

                // 验证当前用户信息是否有效
                if (!IsLoginValid())
                {
                    Debug.WriteLine("用户登录状态无效，强制退出登录");
                    // 强制退出登录
                    ForceLogout();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查登录状态时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证用户登录状态是否有效
        /// </summary>
        private bool IsLoginValid()
        {
            // 如果未登录，直接返回无效
            if (string.IsNullOrEmpty(CurrentUser.Account) || string.IsNullOrEmpty(CurrentUser.UserName))
            {
                return false;
            }
            
            using (var context = new DataBaseContext())
            {
                try
                {
                    // 根据角色检查用户在数据库中的状态
                    switch (CurrentUser.RoleId)
                    {
                        case 1: // 超级管理员
                            var supAdmin = context.SupAdmins.FirstOrDefault(sa => 
                                sa.Account == CurrentUser.Account && 
                                sa.SupAdminPassword == CurrentUser.Password);
                            return supAdmin != null && supAdmin.IsActive == true;
                            
                        case 2: // 管理员
                            var admin = context.Admins.FirstOrDefault(a => 
                                a.Account == CurrentUser.Account && 
                                a.AdminPassword == CurrentUser.Password);
                            return admin != null && admin.IsActive == true;
                            
                        case 3: // 员工
                            var employee = context.Employees.FirstOrDefault(e => 
                                e.Account == CurrentUser.Account && 
                                e.EmployeePassword == CurrentUser.Password);
                            return employee != null && employee.IsActive == true;
                            
                        default:
                            return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"验证登录状态时数据库异常: {ex.Message}");
                    return false; // 发生异常时保守处理，返回无效
                }
            }
        }

        /// <summary>
        /// 强制用户退出登录
        /// </summary>
        private void ForceLogout()
        {
            try
            {
                // 显示提示
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 显示错误提示对话框
                    HandyControl.Controls.MessageBox.Show(
                        "您的账号登录状态已失效，可能由于以下原因：\n\n" +
                        "1. 您的账号已被管理员禁用\n" + 
                        "2. 您的账号信息已被修改\n" + 
                        "3. 您的会话已过期\n\n" +
                        "系统将自动退出登录，请重新登录。",
                        "登录状态失效", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                        
                    // 同时在界面上显示提示消息
                    HandyControl.Controls.Growl.InfoGlobal("登录状态已失效，即将退出登录");
                });
                
                // 等待提示显示完毕
                Thread.Sleep(1500);
                
                // 清除当前用户信息
                CurrentUser.Clear();
                
                // 重启应用以返回登录界面
                var currentProcess = Process.GetCurrentProcess();
                var fileName = currentProcess.MainModule?.FileName;
                if (fileName != null)
                {
                    // 启动新进程
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = fileName,
                        UseShellExecute = true
                    });
                    
                    // 关闭当前程序
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                }
                else
                {
                    // 如果无法获取文件名，直接关闭应用
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"强制退出登录时出错: {ex.Message}");
                
                // 如果重启应用失败，至少尝试退出应用
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                }
                catch
                {
                    // 最后尝试强制退出
                    Environment.Exit(0);
                }
            }
        }
        #endregion

        #region 内容区五行高度设置

        public DelegateCommand ToggleRow1Command => new DelegateCommand(() => { Row1Visible = Row1Visible == GridLength.Auto ? new GridLength(0) : GridLength.Auto; });
        public DelegateCommand ToggleRow2Command => new DelegateCommand(() => { Row2Visible = Row2Visible == GridLength.Auto ? new GridLength(0) : GridLength.Auto; });
        public DelegateCommand ToggleRow3Command => new DelegateCommand(() => { Row3Visible = Row3Visible == GridLength.Auto ? new GridLength(0) : GridLength.Auto; });
        public DelegateCommand ToggleRow4Command => new DelegateCommand(() => { Row4Visible = Row4Visible == GridLength.Auto ? new GridLength(0) : GridLength.Auto; });
        public DelegateCommand ToggleRow5Command => new DelegateCommand(() => { Row5Visible = Row5Visible == GridLength.Auto ? new GridLength(0) : GridLength.Auto; });
        public DelegateCommand Page1Command => new DelegateCommand(() =>
        {
            regionManager.RequestNavigate("Row1Region", "Page1");
            Row1Visible = GridLength.Auto;
            Row2Visible = new GridLength(0);
            Row3Visible = new GridLength(0);
            Row4Visible = new GridLength(0);
            Row5Visible = new GridLength(0);
        });


        #endregion

        #region 个人信息管理页面
        public DelegateCommand PersonalInformationManagementCommand => new DelegateCommand(() =>
        {
            switch (CurrentUser.RoleId)
            {
                case 1:
                    { regionManager.RequestNavigate("Row1Region", "SupAdminPersonallyManager"); }
                    break;
                case 2:
                    { regionManager.RequestNavigate("Row1Region", "AdminPersonallyManager"); }
                    break;
                case 3:
                    { regionManager.RequestNavigate("Row1Region", "EmployeePersonallyManager"); }
                    break;
                default:
                    HandyControl.Controls.MessageBox.Show("未知登录角色");
                    break;
            }
            regionManager.RequestNavigate("Row1Region", "PersonalInformationManagement");
            Row1Visible = GridLength.Auto;
            Row2Visible = new GridLength(0);
            Row3Visible = new GridLength(0);
            Row4Visible = new GridLength(0);
            Row5Visible = new GridLength(0);
        });
        #endregion

        #region 管理员管理页面
        public DelegateCommand DepartmentManagerCommand => new DelegateCommand(() =>
        {
            regionManager.RequestNavigate("Row1Region", "DepartmentManager");
            Row1Visible = GridLength.Auto;
            Row2Visible = new GridLength(0);
            Row3Visible = new GridLength(0);
            Row4Visible = new GridLength(0);
            Row5Visible = new GridLength(0);
        });
        #endregion

        #region 管理员管理页面
        public DelegateCommand AdminManagerCommand => new DelegateCommand(() =>
        {
            regionManager.RequestNavigate("Row1Region", "AdminManager");
            Row1Visible = GridLength.Auto;
            Row2Visible = new GridLength(0);
            Row3Visible = new GridLength(0);
            Row4Visible = new GridLength(0);
            Row5Visible = new GridLength(0);
        });
        #endregion

        #region 员工管理页面
        public DelegateCommand EmployeeManagerCommand => new DelegateCommand(() =>
        {
            regionManager.RequestNavigate("Row1Region", "EmployeeManager");
            Row1Visible = GridLength.Auto;
            Row2Visible = new GridLength(0);
            Row3Visible = new GridLength(0);
            Row4Visible = new GridLength(0);
            Row5Visible = new GridLength(0);
        });
        #endregion

        #region 奖项提名界面
        public DelegateCommand AwardNominateCommand => new DelegateCommand(() =>
        {
            regionManager.RequestNavigate("Row1Region", "AwardNominate");

            // 设置行显示与隐藏
            Row1Visible = GridLength.Auto;
            Row2Visible = new GridLength(0);
            Row3Visible = new GridLength(0);
            Row4Visible = new GridLength(0);
            Row5Visible = new GridLength(0);

            #region 通知用户界面区域变换
            // Growl.InfoGlobal("已跳转到奖项提名");
            #endregion
        });
        #endregion

        #region 奖项设置界面
        public DelegateCommand AwardSettingCommand => new DelegateCommand(() =>
        {
            regionManager.RequestNavigate("Row1Region", "AwardSetting");
            Row1Visible = GridLength.Auto;
            Row2Visible = new GridLength(0);
            Row3Visible = new GridLength(0);
            Row4Visible = new GridLength(0);
            Row5Visible = new GridLength(0);
        });

        #endregion

        #region 投票入口界面
        public DelegateCommand VoteEntranceCommand => new DelegateCommand(() =>
        {
            regionManager.RequestNavigate("Row1Region", "VoteEntrance");
            Row1Visible = GridLength.Auto;
            Row2Visible = new GridLength(0);
            Row3Visible = new GridLength(0);
            Row4Visible = new GridLength(0);
            Row5Visible = new GridLength(0);
        });


        #endregion

        #region 投票结果界面
        public DelegateCommand VoteResultCommand => new DelegateCommand(() =>
        {
            regionManager.RequestNavigate("Row1Region", "VoteResult");
            Row1Visible = GridLength.Auto;
            Row2Visible = new GridLength(0);
            Row3Visible = new GridLength(0);
            Row4Visible = new GridLength(0);
            Row5Visible = new GridLength(0);
        });


        #endregion

        #region  设置管理员管理按钮的可见性
        private Visibility isButtonVisibleCopy;

        public Visibility IsButtonVisibleCopy { get => isButtonVisibleCopy; set => SetProperty(ref isButtonVisibleCopy, value); }
        #endregion

        #region  设置员工管理按钮的可见性
        private Visibility isButtonVisible;

        public Visibility IsButtonVisible { get => isButtonVisible; set => SetProperty(ref isButtonVisible, value); }
        #endregion

        #region 设置特定按钮的可见性
        private Visibility _employeeManagerButtonVisible = Visibility.Visible;
        public Visibility EmployeeManagerButtonVisible
        {
            get => _employeeManagerButtonVisible;
            set => SetProperty(ref _employeeManagerButtonVisible, value);
        }

        private Visibility _awardSettingButtonVisible = Visibility.Visible;
        public Visibility AwardSettingButtonVisible
        {
            get => _awardSettingButtonVisible;
            set => SetProperty(ref _awardSettingButtonVisible, value);
        }

        private Visibility _awardNominateButtonVisible = Visibility.Visible;
        public Visibility AwardNominateButtonVisible
        {
            get => _awardNominateButtonVisible;
            set => SetProperty(ref _awardNominateButtonVisible, value);
        }

        private Visibility _nominationDeclarationButtonVisible;
        public Visibility NominationDeclarationButtonVisible
        {
            get => _nominationDeclarationButtonVisible;
            set => SetProperty(ref _nominationDeclarationButtonVisible, value);
        }
        #endregion

        #endregion

        #region LiveCharts

        #region 属性
        private SeriesCollection seriesCollection;

        public SeriesCollection SeriesCollection
        {
            get { return seriesCollection; }
            set { SetProperty(ref seriesCollection, value); }
        }
        private List<string> labels;

        public List<string> Labels
        {
            get { return labels; }
            set { SetProperty(ref labels, value); }
        }
        
        // 添加饼图标签格式化函数
        private Func<ChartPoint, string> _pieChartLabelFormat;
        public Func<ChartPoint, string> PieChartLabelFormat
        {
            get { return _pieChartLabelFormat; }
            set { SetProperty(ref _pieChartLabelFormat, value); }
        }
        
        // 添加饼图标签格式化函数 - 仅显示标题
        private Func<ChartPoint, string> _pieChartTitleOnlyFormat;
        public Func<ChartPoint, string> PieChartTitleOnlyFormat
        {
            get { return _pieChartTitleOnlyFormat; }
            set { SetProperty(ref _pieChartTitleOnlyFormat, value); }
        }

        // 这个用于第三行的奖项得票总数条形图
        private SeriesCollection rankingSeriesCollection;
        public SeriesCollection RankingSeriesCollection
        {
            get { return rankingSeriesCollection; }
            set { SetProperty(ref rankingSeriesCollection, value); }
        }
        private List<string> rankingLabels;
        public List<string> RankingLabels
        {
            get { return rankingLabels; }
            set { SetProperty(ref rankingLabels, value); }
        }
        
        private AxesCollection rankingAxisX;
        public AxesCollection RankingAxisX
        {
            get { return rankingAxisX; }
            set { SetProperty(ref rankingAxisX, value); }
        }
        
        private AxesCollection rankingAxisY;
        public AxesCollection RankingAxisY
        {
            get { return rankingAxisY; }
            set { SetProperty(ref rankingAxisY, value); }
        }
        
        // 这个用于第四行的投票排名图表
        private SeriesCollection nomineeRankingSeriesCollection;
        public SeriesCollection NomineeRankingSeriesCollection
        {
            get { return nomineeRankingSeriesCollection; }
            set { SetProperty(ref nomineeRankingSeriesCollection, value); }
        }
        private List<string> nomineeRankingLabels;
        public List<string> NomineeRankingLabels
        {
            get { return nomineeRankingLabels; }
            set { SetProperty(ref nomineeRankingLabels, value); }
        }
        
        private AxesCollection nomineeRankingAxisX;
        public AxesCollection NomineeRankingAxisX
        {
            get { return nomineeRankingAxisX; }
            set { SetProperty(ref nomineeRankingAxisX, value); }
        }
        
        private AxesCollection nomineeRankingAxisY;
        public AxesCollection NomineeRankingAxisY
        {
            get { return nomineeRankingAxisY; }
            set { SetProperty(ref nomineeRankingAxisY, value); }
        }
        #endregion

        #endregion

        #region Model3D
        //private OpenTKRenderer _renderer;
        //public void StartRendering()
        //{
        //    _renderer.Run();
        //}

        //private AxisAngleRotation3D rotation;
        //private double angle;
        //private Model3D model3D;
        //public Model3D Model3D
        //{
        //    get { return model3D; }
        //    set { SetProperty(ref model3D, value); }
        //}
        //private Viewport3D myViewport3D;
        //private void LoadModel()
        //{
        //    var importer = new ModelImporter();
        //    string basePath = AppDomain.CurrentDomain.BaseDirectory;
        //    for (int i = 0; i <= 3; i++)
        //    {
        //        //获取父目录
        //        basePath = Directory.GetParent(basePath).FullName;
        //    }
        //    string modelPath = Path.Combine(basePath, "3DObject", "奖杯.obj");
        //    Model3D = importer.Load(modelPath);

        //    //Model3D = importer.Load("../../../3DObject/奖杯.obj");
        //    // 设置金色材质
        //    var material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(255, 215, 0))); // 金色
        //    var specularMaterial = new SpecularMaterial(new SolidColorBrush(Colors.White), 100); // 高光
        //    ApplyMaterial(Model3D, material);
        //    rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
        //    var transform = new RotateTransform3D(rotation);
        //    Model3D.Transform = transform;
        //}
        //private void ApplyMaterial(Model3D model, Material material)
        //{
        //    if (model is GeometryModel3D geometryModel)
        //    {
        //        geometryModel.Material = material;
        //        geometryModel.BackMaterial = material;
        //    }
        //    else if (model is Model3DGroup group)
        //    {
        //        foreach (var child in group.Children)
        //        {
        //            ApplyMaterial(child, material);
        //        }
        //    }
        //}
        //private void SetupRotation()
        //{
        //    CompositionTarget.Rendering += (sender, e) =>
        //    {
        //        angle += 1;
        //        if (angle >= 360)
        //        {
        //            angle = 0;
        //        }
        //        rotation.Angle = angle;
        //    };
        //}


        #endregion


        private DelegateCommand windowDragCommand;
        public DelegateCommand WindowDragCommand => windowDragCommand ??= new DelegateCommand(WindowDrag);

        private void WindowDrag()
        {
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(Application.Current.MainWindow).Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        /// <summary>
        /// 加载首页图表数据（各奖项提名占比和得票总数）
        /// </summary>
        private void LoadChartData()
        {
            try
            {
                using (var context = new DataBaseContext())
                {
                    // 获取所有奖项名称和对应的提名数量
                    var awardNominations = context.Nominations
                        .Include(n => n.Award)
                        .AsNoTracking()
                        .GroupBy(n => n.Award.AwardName)
                        .Select(g => new { AwardName = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .ToList();

                    if (awardNominations.Count > 0)
                    {
                        // 清空现有集合
                        SeriesCollection.Clear();
                        
                        // 生成饼图数据
                        foreach (var award in awardNominations)
                        {
                            var color = GetRandomColor();
                            SeriesCollection.Add(new PieSeries
                            {
                                Title = award.AwardName,
                                Values = new ChartValues<int> { award.Count },
                                DataLabels = false,
                                LabelPoint = PieChartTitleOnlyFormat,
                                Fill = new SolidColorBrush(color)
                            });
                        }

                        // 获取所有奖项对应的投票数量
                        var awardVotes = context.VoteRecords
                            .Include(v => v.Award)
                            .AsNoTracking()
                            .GroupBy(v => v.Award.AwardName)
                            .Select(g => new { AwardName = g.Key, Count = g.Count() })
                            .OrderByDescending(x => x.Count)
                            .ToList();

                        // 准备条形图数据
                        var rankingLabelsArray = awardVotes.Select(a => a.AwardName).ToArray();
                        var rankingValues = awardVotes.Select(a => a.Count).ToArray();

                        // 设置各奖项得票总数图表的轴
                        RankingAxisX = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "奖项",
                                Labels = rankingLabelsArray,
                                FontWeight = FontWeights.Bold,
                                Position = AxisPosition.LeftBottom,
                                Separator = new LiveCharts.Wpf.Separator { Step = 1 },
                                LabelsRotation = 0,
                                Margin = new Thickness(0, 20, 0, 0)  // 向下移动标签
                            }
                        };

                        RankingAxisY = new AxesCollection
                        {
                            new Axis
                            {
                                Title = "票数",
                                MinValue = 0,
                                Foreground = Brushes.Black
                            }
                        };
                        
                        // 清空现有集合
                        RankingSeriesCollection.Clear();
                        
                        // 添加条形图数据
                        RankingSeriesCollection.Add(new ColumnSeries
                        {
                            Title = "得票数",
                            Values = new ChartValues<int>(rankingValues),
                            Fill = Brushes.CornflowerBlue,
                            DataLabels = true
                        });

                        // 获取得票最多的前10名提名者
                        var topVoteRecords = context.VoteRecords
                            .AsNoTracking()
                            .GroupBy(v => v.NominationId)
                            .Select(g => new { NominationId = g.Key, VoteCount = g.Count() })
                            .OrderByDescending(x => x.VoteCount)
                            .Take(10)
                            .ToList();

                        // 分步加载被提名者信息，避免SQL查询中包含NotMapped属性
                        var topNominees = new List<NomineeVoteCount>();
                        
                        foreach (var record in topVoteRecords)
                        {
                            // 先获取基本提名信息
                            var nomination = context.Nominations
                                .AsNoTracking()
                                .Where(n => n.NominationId == record.NominationId)
                                .Select(n => new 
                                {
                                    n.NominationId,
                                    n.NominatedEmployeeId,
                                    n.NominatedAdminId
                                })
                                .FirstOrDefault();
                                
                            if (nomination != null)
                            {
                                string nomineeName = "未知";
                                
                                // 单独加载被提名员工
                                if (nomination.NominatedEmployeeId.HasValue)
                                {
                                    var employee = context.Employees
                                        .AsNoTracking()
                                        .Where(e => e.EmployeeId == nomination.NominatedEmployeeId.Value)
                                        .Select(e => e.EmployeeName)
                                        .FirstOrDefault();
                                    
                                    if (!string.IsNullOrEmpty(employee))
                                    {
                                        nomineeName = employee;
                                    }
                                }
                                // 单独加载被提名管理员
                                else if (nomination.NominatedAdminId.HasValue)
                                {
                                    var admin = context.Admins
                                        .AsNoTracking()
                                        .Where(a => a.AdminId == nomination.NominatedAdminId.Value)
                                        .Select(a => a.AdminName)
                                        .FirstOrDefault();
                                    
                                    if (!string.IsNullOrEmpty(admin))
                                    {
                                        nomineeName = admin;
                                    }
                                }
                                
                                // 添加到结果列表
                                topNominees.Add(new NomineeVoteCount 
                                { 
                                    NomineeName = nomineeName, 
                                    VoteCount = record.VoteCount 
                                });
                            }
                        }
                        
                        if (topNominees.Count > 0)
                        {
                            var nomineeLabels = topNominees.Select(n => n.NomineeName).ToArray();
                            var nomineeVotes = topNominees.Select(n => n.VoteCount).ToArray();

                            // 设置投票排名图表的轴
                            NomineeRankingAxisX = new AxesCollection
                            {
                                new Axis
                                {
                                    Title = "提名人",
                                    Labels = nomineeLabels,
                                    FontWeight = FontWeights.Bold,
                                    Position = AxisPosition.LeftBottom,
                                    Separator = new LiveCharts.Wpf.Separator { Step = 1 },
                                    LabelsRotation = 0,
                                    Margin = new Thickness(0, 20, 0, 0)  // 向下移动标签
                                }
                            };

                            NomineeRankingAxisY = new AxesCollection
                            {
                                new Axis
                                {
                                    Title = "票数",
                                    MinValue = 0,
                                    Foreground = Brushes.Black
                                }
                            };

                            // 清空现有集合
                            NomineeRankingSeriesCollection.Clear();
                            
                            // 添加条形图数据
                            NomineeRankingSeriesCollection.Add(new ColumnSeries
                            {
                                Title = "得票数",
                                Values = new ChartValues<int>(nomineeVotes),
                                Fill = Brushes.MediumSeaGreen,
                                DataLabels = true
                            });
                        }
                        else
                        {
                            SetDefaultCharts();
                        }
                    }
                    else
                    {
                        SetDefaultCharts();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载图表数据时出错: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                SetDefaultCharts();
            }
        }
        
        // 获取随机颜色的辅助方法
        private System.Windows.Media.Color GetRandomColor()
        {
            return System.Windows.Media.Color.FromRgb(
                (byte)_random.Next(100, 250),
                (byte)_random.Next(100, 250),
                (byte)_random.Next(100, 250));
        }
        
        private void SetDefaultCharts()
        {
            // 如果饼图集合为空，创建默认的空饼图
            if (SeriesCollection == null || SeriesCollection.Count == 0)
            {
                SeriesCollection = new SeriesCollection();
                SeriesCollection.Add(new PieSeries
                {
                    Title = "暂无数据",
                    Values = new ChartValues<int> { 1 },
                    Fill = Brushes.LightGray,
                    DataLabels = false,
                    LabelPoint = PieChartTitleOnlyFormat
                });
            }
            
            // 为条形图创建默认轴
            RankingAxisX.Clear();
            RankingAxisX.Add(new Axis
            {
                Title = "奖项",
                Labels = new[] { "无数据" },
                Foreground = Brushes.Black
            });
            
            RankingAxisY.Clear();
            RankingAxisY.Add(new Axis
            {
                Title = "得票数",
                MinValue = 0,
                Foreground = Brushes.Black
            });
            
            // 如果条形图集合为空，创建默认的空条形图
            if (RankingSeriesCollection == null || RankingSeriesCollection.Count == 0)
            {
                RankingSeriesCollection = new SeriesCollection();
                RankingSeriesCollection.Add(new ColumnSeries
                {
                    Title = "无数据",
                    Values = new ChartValues<int> { 0 },
                    Fill = Brushes.LightBlue,
                    DataLabels = true
                });
            }
            
            // 为提名排名图创建默认轴
            NomineeRankingAxisX.Clear();
            NomineeRankingAxisX.Add(new Axis
            {
                Title = "候选人",
                Labels = new[] { "无数据" },
                Foreground = Brushes.Black
            });
            
            NomineeRankingAxisY.Clear();
            NomineeRankingAxisY.Add(new Axis
            {
                Title = "票数",
                MinValue = 0,
                Foreground = Brushes.Black
            });
            
            // 如果提名排名图集合为空，创建默认的空条形图
            if (NomineeRankingSeriesCollection == null || NomineeRankingSeriesCollection.Count == 0)
            {
                NomineeRankingSeriesCollection = new SeriesCollection();
                NomineeRankingSeriesCollection.Add(new ColumnSeries
                {
                    Title = "无数据",
                    Values = new ChartValues<int> { 0 },
                    Fill = Brushes.LightGreen,
                    DataLabels = true
                });
            }
        }

        /// <summary>
        /// 窗口加载命令方法
        /// </summary>
        private void ExecuteLoadedCommand()
        {
            // GetAllPermission();
            UserName = CurrentUser.UserName;
            
            // 启动时钟线程
            Task.Run(() => {
                while (true)
                {
                    Application.Current.Dispatcher.Invoke(() => UpdateTime(null, EventArgs.Empty));
                    Thread.Sleep(1000);
                }
            });
            
            // 立即加载图表数据，确保初始显示
            LoadChartData();
            
            // 初始界面
            InitializeChartsIfNeeded();
            // LoadPageInfo();
        }
        
        /// <summary>
        /// 获取所有权限
        /// </summary>
        private void GetAllPermission()
        {
            // 这里实现获取权限的逻辑
            // 根据App.Current_LoginUser的角色和权限设置进行处理
        }
        
        /// <summary>
        /// 加载页面信息
        /// </summary>
        private void LoadPageInfo()
        {
            // 这里实现加载页面信息的逻辑
            // 可能包括加载用户基本信息、系统状态等
        }
        
        /// <summary>
        /// 初始化图表，确保显示不为空
        /// </summary>
        private void InitializeChartsIfNeeded()
        {
            // 如果饼图集合为空，创建默认的空饼图
            if (SeriesCollection == null || SeriesCollection.Count == 0)
            {
                SeriesCollection = new SeriesCollection();
                SeriesCollection.Add(new PieSeries
                {
                    Title = "暂无数据",
                    Values = new ChartValues<int> { 1 },
                    Fill = Brushes.LightGray,
                    DataLabels = false,
                    LabelPoint = PieChartTitleOnlyFormat
                });
            }
            
            // 如果条形图集合为空，创建默认的空条形图
            if (RankingSeriesCollection == null || RankingSeriesCollection.Count == 0)
            {
                RankingSeriesCollection = new SeriesCollection();
                RankingSeriesCollection.Add(new ColumnSeries
                {
                    Title = "奖项得票",
                    Values = new ChartValues<int> { 0 },
                    Fill = Brushes.LightBlue,
                    DataLabels = true
                });
                
                if (RankingLabels == null || RankingLabels.Count == 0)
                {
                    RankingLabels = new List<string> { "暂无数据" };
                }
                
                // 确保坐标轴已初始化
                if (RankingAxisX == null || RankingAxisX.Count == 0)
                {
                    if (RankingAxisX == null)
                        RankingAxisX = new AxesCollection();
                    
                    RankingAxisX.Clear();
                    RankingAxisX.Add(new Axis
                    {
                        Title = "奖项",
                        Labels = new[] { "暂无数据" },
                        Foreground = Brushes.Black
                    });
                }
                
                if (RankingAxisY == null || RankingAxisY.Count == 0)
                {
                    if (RankingAxisY == null)
                        RankingAxisY = new AxesCollection();
                    
                    RankingAxisY.Clear();
                    RankingAxisY.Add(new Axis
                    {
                        Title = "得票数",
                        MinValue = 0,
                        Foreground = Brushes.Black
                    });
                }
            }
            
            // 如果提名排名图集合为空，创建默认的空条形图
            if (NomineeRankingSeriesCollection == null || NomineeRankingSeriesCollection.Count == 0)
            {
                NomineeRankingSeriesCollection = new SeriesCollection();
                NomineeRankingSeriesCollection.Add(new ColumnSeries
                {
                    Title = "候选人得票",
                    Values = new ChartValues<int> { 0 },
                    Fill = Brushes.LightGreen,
                    DataLabels = true
                });
                
                if (NomineeRankingLabels == null || NomineeRankingLabels.Count == 0)
                {
                    NomineeRankingLabels = new List<string> { "暂无数据" };
                }
                
                // 确保坐标轴已初始化
                if (NomineeRankingAxisX == null || NomineeRankingAxisX.Count == 0)
                {
                    if (NomineeRankingAxisX == null)
                        NomineeRankingAxisX = new AxesCollection();
                    
                    NomineeRankingAxisX.Clear();
                    NomineeRankingAxisX.Add(new Axis
                    {
                        Title = "候选人",
                        Labels = new[] { "暂无数据" },
                        Foreground = Brushes.Black
                    });
                }
                
                if (NomineeRankingAxisY == null || NomineeRankingAxisY.Count == 0)
                {
                    if (NomineeRankingAxisY == null)
                        NomineeRankingAxisY = new AxesCollection();
                    
                    NomineeRankingAxisY.Clear();
                    NomineeRankingAxisY.Add(new Axis
                    {
                        Title = "票数",
                        MinValue = 0,
                        Foreground = Brushes.Black
                    });
                }
            }
        }

        #region 投票详情相关属性

        // 奖项列表
        private ObservableCollection<Award> awards;
        public ObservableCollection<Award> Awards
        {
            get { return awards; }
            set { SetProperty(ref awards, value); }
        }

        // 当前选中的奖项
        private Award selectedAward;
        public Award SelectedAward
        {
            get { return selectedAward; }
            set 
            { 
                SetProperty(ref selectedAward, value);
                // 当选中奖项改变时，可以自动触发筛选
                if (value != null)
                {
                    FilterVoteDetails();
                }
            }
        }

        // 投票详情数据
        private ObservableCollection<VoteDetailDto> voteDetails;
        public ObservableCollection<VoteDetailDto> VoteDetails
        {
            get { return voteDetails; }
            set { SetProperty(ref voteDetails, value); }
        }

        // 所有投票详情数据（未经筛选）
        private List<VoteDetailDto> allVoteDetails;

        // 筛选投票详情命令
        private DelegateCommand filterVoteDetailsCommand;
        public DelegateCommand FilterVoteDetailsCommand => 
            filterVoteDetailsCommand ??= new DelegateCommand(FilterVoteDetails);

        // 查看提名详情命令
        private DelegateCommand<VoteDetailDto> viewNominationDetailsCommand;
        public DelegateCommand<VoteDetailDto> ViewNominationDetailsCommand =>
            viewNominationDetailsCommand ??= new DelegateCommand<VoteDetailDto>(ViewNominationDetails);

        // 筛选投票详情方法
        private void FilterVoteDetails()
        {
            if (allVoteDetails == null || allVoteDetails.Count == 0)
            {
                LoadVoteDetails();
                return;
            }

            if (selectedAward == null)
            {
                // 如果未选择奖项，显示所有数据
                VoteDetails = new ObservableCollection<VoteDetailDto>(allVoteDetails);
            }
            else
            {
                // 根据选中的奖项筛选
                var filtered = allVoteDetails.Where(v => v.AwardId == selectedAward.AwardId).ToList();
                VoteDetails = new ObservableCollection<VoteDetailDto>(filtered);
            }
        }

        // 加载投票详情数据
        private void LoadVoteDetails()
        {
            try
            {
                using (var context = new DataBaseContext())
                {
                    // 加载奖项列表
                    if (Awards == null || Awards.Count == 0)
                    {
                        var awardsList = context.Awards.ToList();
                        Awards = new ObservableCollection<Award>(awardsList);
                    }

                    // 查询投票详情数据
                    var voteDetailsQuery = (from n in context.Nominations
                                          join a in context.Awards on n.AwardId equals a.AwardId
                                          join d in context.Departments on n.DepartmentId equals d.DepartmentId into depts
                                          from dept in depts.DefaultIfEmpty()
                                          select new VoteDetailDto
                                          {
                                              NominationId = n.NominationId,
                                              AwardId = a.AwardId,
                                              AwardName = a.AwardName,
                                              DepartmentId = n.DepartmentId,
                                              DepartmentName = dept != null ? dept.DepartmentName : "未指定",
                                              NomineeName = n.NominatedEmployeeId.HasValue ? 
                                                  context.Employees.Where(e => e.EmployeeId == n.NominatedEmployeeId.Value)
                                                      .Select(e => e.EmployeeName).FirstOrDefault() :
                                                  (n.NominatedAdminId.HasValue ? 
                                                      context.Admins.Where(ad => ad.AdminId == n.NominatedAdminId.Value)
                                                          .Select(ad => ad.AdminName).FirstOrDefault() : "未知"),
                                              Introduction = n.Introduction,
                                              NominateReason = n.NominateReason,
                                              VoteCount = context.VoteRecords.Count(vr => vr.NominationId == n.NominationId),
                                              EmployeeVoteCount = context.VoteRecords
                                                  .Join(context.Employees, 
                                                      vr => vr.VoterEmployeeId, 
                                                      e => e.EmployeeId, 
                                                      (vr, e) => new { VoteRecord = vr, Employee = e })
                                                  .Count(x => x.VoteRecord.NominationId == n.NominationId),
                                              AdminVoteCount = context.VoteRecords
                                                  .Join(context.Admins, 
                                                      vr => vr.VoterAdminId, 
                                                      a => a.AdminId, 
                                                      (vr, a) => new { VoteRecord = vr, Admin = a })
                                                  .Count(x => x.VoteRecord.NominationId == n.NominationId)
                                          }).ToList();

                    // 保存所有数据
                    allVoteDetails = voteDetailsQuery;

                    // 更新显示的数据
                    VoteDetails = new ObservableCollection<VoteDetailDto>(voteDetailsQuery);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载投票详情时出错: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                
                // 确保集合初始化
                if (VoteDetails == null)
                {
                    VoteDetails = new ObservableCollection<VoteDetailDto>();
                }
                
                if (Awards == null)
                {
                    Awards = new ObservableCollection<Award>();
                }
            }
        }
        
        /// <summary>
        /// 查看提名详情
        /// </summary>
        private void ViewNominationDetails(VoteDetailDto voteDetail)
        {
            if (voteDetail == null) return;
            
            try
            {
                // 创建并显示提名详情窗口，使用完整类型名以避免歧义
                var detailsWindow = new SIASGraduate.Views.EditMessage.NominationDetailsWindows.NominationDetailsWindow();
                
                // 加载提名详情前先确保显示窗口，以便UI上下文初始化
                detailsWindow.Show();
                
                try
                {
                    // 使用更安全的方式加载详情，避免立即使用EF查询
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                        new Action(() => detailsWindow.LoadNominationDetails(voteDetail)),
                        System.Windows.Threading.DispatcherPriority.Background);
                }
                catch (Exception ex)
                {
                    HandyControl.Controls.Growl.ErrorGlobal($"加载详情异常: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"加载详情异常: {ex}");
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.Growl.ErrorGlobal($"打开详情窗口异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"打开详情窗口异常: {ex}");
            }
        }

        #endregion

        // 添加饼图点击命令
        private DelegateCommand<ChartPoint> _pieChartDataClickCommand;
        public DelegateCommand<ChartPoint> PieChartDataClickCommand =>
            _pieChartDataClickCommand ??= new DelegateCommand<ChartPoint>(OnPieChartDataClick);
            
        private void OnPieChartDataClick(ChartPoint chartPoint)
        {
            // 获取饼图
            var chart = (PieChart)chartPoint.ChartView;
            
            // 清除所有扇区的突出显示
            foreach (PieSeries series in chart.Series)
            {
                series.PushOut = 0;
            }
            
            // 突出显示被点击的扇区
            var selectedSeries = (PieSeries)chartPoint.SeriesView;
            selectedSeries.PushOut = 8;
            
            // 可以在这里添加其他交互逻辑，如显示相关数据等
            Debug.WriteLine($"点击了饼图：{selectedSeries.Title}，值：{chartPoint.Y}，占比：{chartPoint.Participation:P2}");
        }

        public DelegateCommand NominationDeclarationCommand => new DelegateCommand(() =>
        {
            regionManager.RequestNavigate("Row1Region", "NominationDeclaration");

            // 设置行显示与隐藏
            Row1Visible = GridLength.Auto;
            Row2Visible = new GridLength(0);
            Row3Visible = new GridLength(0);
            Row4Visible = new GridLength(0);
            Row5Visible = new GridLength(0);
        });
    }
}
