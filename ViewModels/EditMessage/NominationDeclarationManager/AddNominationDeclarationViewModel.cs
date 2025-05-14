using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HandyControl.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SIASGraduate.Common;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;
using ConverterImage = SIASGraduate.Converter.ConVerterImage;

namespace SIASGraduate.ViewModels.EditMessage.NominationDeclarationManager
{
    public class AddNominationDeclarationViewModel : INotifyPropertyChanged, INavigationAware
    {
        #region 区域管理器
        private readonly IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 构造函数
        public AddNominationDeclarationViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            #region 初始化命令
            SubmitCommand = new DelegateCommand(OnSubmit);
            CancelCommand = new DelegateCommand(OnCancel);
            SelectImageCommand = new DelegateCommand(OnSelectImage);
            NavigateBackCommand = new DelegateCommand(OnNavigateBack);
            #endregion

            // 初始化集合，避免空引用异常
            Nominees = new ObservableCollection<object>();
            
            // 初始化提名类型列表
            InitializeNomineeTypes();
            
            // 设置默认提名类型
            SelectedNomineeType = NomineeTypes.FirstOrDefault(t => t.Value == "员工");
            
            #region 初始化数据
            LoadData();
            #endregion
        }
        #endregion

        #region 属性
        private ObservableCollection<Award> awards;
        public ObservableCollection<Award> Awards
        {
            get => awards;
            set => SetProperty(ref awards, value);
        }

        private ObservableCollection<Award> filteredAwards;
        public ObservableCollection<Award> FilteredAwards
        {
            get => filteredAwards;
            set => SetProperty(ref filteredAwards, value);
        }

        /// <summary>
        /// 奖项搜索文本
        /// </summary>
        private string _awardSearchText;
        public string AwardSearchText
        {
            get => _awardSearchText;
            set
            {
                // 如果值相同则不更新，避免不必要的刷新
                if (value == _awardSearchText)
                    return;
                
                if (SetProperty(ref _awardSearchText, value))
                {
                    // 当文本变化时更新过滤列表
                    UpdateFilteredAwards();
                    
                    // 只有在有文本时才自动打开下拉列表
                    IsAwardDropDownOpen = !string.IsNullOrEmpty(value);
                    
                    // 处理用户直接输入完整奖项名称的情况
                    // 仅当TextBox中输入了有效文本时才执行
                    if (!string.IsNullOrWhiteSpace(value) && Awards != null)
                    {
                        var exactMatch = Awards.FirstOrDefault(a => 
                            a.AwardName != null && 
                            a.AwardName.Equals(value, StringComparison.OrdinalIgnoreCase));
                            
                        // 只有当前没有选中项或选中项不匹配时才设置新的选中项
                        if (exactMatch != null && (SelectedAward == null || !SelectedAward.AwardName.Equals(value, StringComparison.OrdinalIgnoreCase)))
                        {
                            SelectedAward = exactMatch;
                        }
                    }
                }
            }
        }

        private bool isAwardDropDownOpen;
        public bool IsAwardDropDownOpen
        {
            get => isAwardDropDownOpen;
            set => SetProperty(ref isAwardDropDownOpen, value);
        }

        private Award _selectedAward;
        public Award SelectedAward
        {
            get => _selectedAward;
            set
            {
                if (SetProperty(ref _selectedAward, value))
                {
                    // 加载奖项封面图片
                    if (value != null)
                    {
                        LoadAwardImage();
                        
                        // 只有当搜索文本与当前奖项名称不匹配时才更新搜索文本
                        // 这样可以避免因属性设置导致的循环刷新
                        if (AwardSearchText != value.AwardName)
                        {
                            // 使用直接赋值给字段的方式，避免再次触发UpdateFilteredAwards
                            _awardSearchText = value.AwardName;
                            OnPropertyChanged(nameof(AwardSearchText));
                        }
                        
                        // 检查是否已经提名过
                        CheckIfAlreadyNominated();
                    }
                    else
                    {
                        CoverImagePreview = null;
                    }
                }
            }
        }

        private List<KeyValuePair<int, string>> nomineeTypes;
        public List<KeyValuePair<int, string>> NomineeTypes
        {
            get => nomineeTypes;
            set => SetProperty(ref nomineeTypes, value);
        }

        private KeyValuePair<int, string> selectedNomineeType;
        public KeyValuePair<int, string> SelectedNomineeType
        {
            get => selectedNomineeType;
            set
            {
                if (SetProperty(ref selectedNomineeType, value))
                {
                    try
                    {
                        LoadNominees();
                        OnPropertyChanged(nameof(IsNomineeVisible));
                    }
                    catch (Exception ex)
                    {
                        Growl.ErrorGlobal($"切换申报对象类型时出错: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"切换申报对象类型异常详情: {ex}");
                    }
                }
            }
        }

        // 是否可以编辑申报对象类型（只有超级管理员可以）
        public bool CanEditNomineeType => CurrentUser.RoleId == 1;

        private ObservableCollection<object> nominees;
        public ObservableCollection<object> Nominees
        {
            get => nominees;
            set => SetProperty(ref nominees, value);
        }

        private object selectedNominee;
        public object SelectedNominee
        {
            get => selectedNominee;
            set 
            {
                if (SetProperty(ref selectedNominee, value))
                {
                    // 当选择申报对象后，自动设置对应部门
                    if (value is Employee employee)
                    {
                        AutoSelectDepartment(employee.DepartmentId);
                    }
                    else if (value is Admin admin)
                    {
                        AutoSelectDepartment(admin.DepartmentId);
                    }
                }
            }
        }

        // 是否可以选择申报对象（只有管理员和超级管理员可以）
        public bool CanSelectNominee => CurrentUser.RoleId <= 2;

        public bool IsNomineeVisible => nominees != null && nominees.Count > 0;

        private ObservableCollection<Department> departments;
        public ObservableCollection<Department> Departments
        {
            get => departments;
            set => SetProperty(ref departments, value);
        }

        private Department selectedDepartment;
        public Department SelectedDepartment
        {
            get => selectedDepartment;
            set => SetProperty(ref selectedDepartment, value);
        }

        private string introduction;
        public string Introduction
        {
            get => introduction;
            set => SetProperty(ref introduction, value);
        }

        private string declarationReason;
        public string DeclarationReason
        {
            get => declarationReason;
            set => SetProperty(ref declarationReason, value);
        }

        private byte[] coverImage;
        public byte[] CoverImage
        {
            get => coverImage;
            set
            {
                if (SetProperty(ref coverImage, value))
                {
                    CoverImagePreview = ConverterImage.ConvertByteArrayToBitmapImage(value);
                }
            }
        }

        private BitmapImage coverImagePreview;
        public BitmapImage CoverImagePreview
        {
            get => coverImagePreview;
            set 
            { 
                if (SetProperty(ref coverImagePreview, value))
                {
                    OnPropertyChanged(nameof(HasCoverImage));
                }
            }
        }

        /// <summary>
        /// 是否有封面图片
        /// </summary>
        public bool HasCoverImage => CoverImagePreview != null;

        // 是否可以修改所属部门
        public bool CanEditDepartment => false; // 禁止所有用户修改部门

        // 奖项封面图片预览
        private ImageSource _awardImagePreview;
        public ImageSource AwardImagePreview
        {
            get => _awardImagePreview;
            set 
            { 
                if (SetProperty(ref _awardImagePreview, value))
                {
                    OnPropertyChanged(nameof(HasAwardImagePreview));
                }
            }
        }

        /// <summary>
        /// 是否有奖项图片预览
        /// </summary>
        public bool HasAwardImagePreview => AwardImagePreview != null;
        #endregion

        #region 命令
        public DelegateCommand SubmitCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }
        public DelegateCommand SelectImageCommand { get; private set; }
        public DelegateCommand NavigateBackCommand { get; private set; }
        #endregion

        #region 加载数据
        public async void LoadData()
        {
            try
            {
                // 使用上下文加载数据
                using (var context = new DataBaseContext())
                {
                    // 加载全部奖项（包括封面图片）
                    Awards = new ObservableCollection<Award>(
                        await context.Awards.AsNoTracking().ToListAsync());
                        
                    // 初始化筛选后的奖项列表
                    FilteredAwards = new ObservableCollection<Award>(Awards);
                        
                    // 加载学院信息，显式转换为ObservableCollection
                    var deptList = await context.Departments.AsNoTracking().ToListAsync();
                    Departments = new ObservableCollection<Department>(deptList);
                    
                    // 提名类型在构造函数中已初始化，此处不需要重复调用
                    // 根据用户角色设置默认提名类型
                    switch (CurrentUser.RoleId)
                    {
                        case 1: // 超级管理员
                            SelectedNomineeType = NomineeTypes.FirstOrDefault(t => t.Value == "管理员");
                            break;
                        case 2: // 管理员
                        case 3: // 员工
                            SelectedNomineeType = NomineeTypes.FirstOrDefault(t => t.Value == "员工");
                            break;
                        default:
                            // 默认选择员工类型
                            SelectedNomineeType = NomineeTypes.FirstOrDefault(t => t.Value == "员工");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.Error($"加载数据时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据搜索文本更新筛选后的奖项列表
        /// </summary>
        private void UpdateFilteredAwards()
        {
            try 
            {
                if (FilteredAwards == null)
                    FilteredAwards = new ObservableCollection<Award>();
                
                FilteredAwards.Clear();
                
                if (Awards == null || Awards.Count == 0)
                    return;
                
                if (string.IsNullOrWhiteSpace(AwardSearchText))
                {
                    // 如果搜索文本为空，显示所有奖项，但不触发选择
                    foreach (var award in Awards)
                    {
                        FilteredAwards.Add(award);
                    }
                }
                else
                {
                    // 根据搜索文本筛选奖项
                    string searchText = AwardSearchText.ToLower();
                    var matchingAward = Awards.FirstOrDefault(a => 
                        a.AwardName != null && a.AwardName.Equals(AwardSearchText, StringComparison.OrdinalIgnoreCase));
                    
                    // 只有当搜索文本不为空且当前没有选中项或选中项不匹配时，才设置新的选中项
                    if (matchingAward != null && (SelectedAward == null || !matchingAward.AwardName.Equals(SelectedAward.AwardName)))
                    {
                        // 暂时禁用属性更新通知，防止循环
                        var oldAwardSearchText = _awardSearchText; 
                        SelectedAward = matchingAward;
                        _awardSearchText = oldAwardSearchText; // 恢复搜索文本，避免再次触发
                    }
                    
                    // 显示所有包含搜索文本的奖项
                    foreach (var award in Awards)
                    {
                        if (award.AwardName?.ToLower().Contains(searchText) == true)
                        {
                            FilteredAwards.Add(award);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"筛选奖项时出错: {ex.Message}");
            }
        }

        private void LoadNominees()
        {
            // 检查SelectedNomineeType是否为空
            if (SelectedNomineeType.Key == 0 && string.IsNullOrEmpty(SelectedNomineeType.Value))
            {
                return;
            }
            
            try
            {
                // 清空当前选择的提名对象
                SelectedNominee = null;

                // 确保Nominees已初始化
                if (Nominees == null)
                {
                    Nominees = new ObservableCollection<object>();
                }
                else
                {
                    Nominees.Clear();
                }
                
                using (var context = new DataBaseContext())
                {
                    // 根据当前用户角色来决定可选的申报对象
                    switch (CurrentUser.RoleId)
                    {
                        case 1: // 超级管理员
                            // 超级管理员可以提名所有员工或管理员
                            switch (SelectedNomineeType.Key)
                            {
                                case 1: // 员工类型
                                    var employees = context.Employees
                                        .Include(e => e.Department)
                                        .Where(e => e.IsActive == true)
                                        .ToList();
                                    
                                    if (employees.Any())
                                    {
                                        foreach (var employee in employees)
                                        {
                                            Nominees.Add(employee);
                                        }
                                    }
                                    else
                                    {
                                        Growl.WarningGlobal("系统中没有可提名的员工");
                                        // 如果没有员工，自动切换回管理员类型
                                        SelectedNomineeType = NomineeTypes.FirstOrDefault(t => t.Value == "管理员");
                                        return;
                                    }
                                    break;
                                    
                                case 2: // 管理员类型
                                    var admins = context.Admins
                                        .Where(a => a.IsActive == true)
                                        .ToList();
                                    
                                    if (admins.Any())
                                    {
                                        foreach (var admin in admins)
                                        {
                                            Nominees.Add(admin);
                                        }
                                    }
                                    else
                                    {
                                        Growl.WarningGlobal("系统中没有可提名的管理员");
                                        // 如果没有管理员，自动切换回员工类型
                                        SelectedNomineeType = NomineeTypes.FirstOrDefault(t => t.Value == "员工");
                                        return;
                                    }
                                    break;
                            }
                            break;
                            
                        case 2: // 管理员
                            switch (SelectedNomineeType.Key)
                            {
                                case 1: // 员工类型 - 尝试查找关联员工身份
                                    if (CurrentUser.EmployeeId.HasValue)
                                    {
                                        var adminAsEmployee = context.Employees
                                            .Include(e => e.Department)
                                            .FirstOrDefault(e => e.EmployeeId == CurrentUser.EmployeeId);
                                        
                                        if (adminAsEmployee != null)
                                        {
                                            Nominees.Add(adminAsEmployee);
                                            
                                            // 自动设置部门
                                            AutoSelectDepartment(adminAsEmployee.DepartmentId);
                                        }
                                        else
                                        {
                                            // 管理员没有有效的员工身份，静默切换到管理员类型
                                            SelectedNomineeType = NomineeTypes.FirstOrDefault(t => t.Value == "管理员");
                                            return; // 退出当前方法，让切换事件重新触发LoadNominees
                                        }
                                    }
                                    else
                                    {
                                        // 管理员没有关联员工身份，静默切换到管理员类型
                                        SelectedNomineeType = NomineeTypes.FirstOrDefault(t => t.Value == "管理员");
                                        return; // 退出当前方法，让切换事件重新触发LoadNominees
                                    }
                                    break;
                                    
                                case 2: // 管理员类型 - 直接以管理员身份提名自己
                                    if (CurrentUser.AdminId.HasValue)
                                    {
                                        var admin = context.Admins
                                            .Include(a => a.Department)
                                            .FirstOrDefault(a => a.AdminId == CurrentUser.AdminId);
                                        
                                        if (admin != null)
                                        {
                                            Nominees.Add(admin);
                                            
                                            // 自动设置部门
                                            AutoSelectDepartment(admin.DepartmentId);
                                        }
                                        else
                                        {
                                            // 管理员记录不存在时，提供更友好的提示
                                            Growl.WarningGlobal("管理员信息不完整，请联系系统管理员");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        // 这种情况几乎不应该发生，因为管理员应该有AdminId
                                        // 提供一个更简洁的消息
                                        Growl.WarningGlobal("管理员身份验证失败");
                                        return;
                                    }
                                    break;
                            }
                            break;
                            
                        case 3: // 普通员工
                            // 员工只能提名自己
                            if (SelectedNomineeType.Key == 1) // 员工类型
                            {
                                // 检查当前员工ID是否有效
                                if (CurrentUser.EmployeeId.HasValue)
                                {
                                    var currentEmployee = context.Employees
                                        .Include(e => e.Department)
                                        .FirstOrDefault(e => e.EmployeeId == CurrentUser.EmployeeId);
                                    
                                    if (currentEmployee != null)
                                    {
                                        Nominees.Add(currentEmployee);
                                        
                                        // 自动设置部门
                                        AutoSelectDepartment(currentEmployee.DepartmentId);
                                    }
                                    else
                                    {
                                        Growl.WarningGlobal($"未找到ID为{CurrentUser.EmployeeId}的员工记录");
                                        return;
                                    }
                                }
                                else
                                {
                                    Growl.ErrorGlobal("当前登录用户没有关联的员工ID，无法进行申报");
                                    return;
                                }
                            }
                            else
                            {
                                Growl.WarningGlobal("员工只能以员工身份进行提名");
                                // 自动切换回员工类型
                                SelectedNomineeType = NomineeTypes.FirstOrDefault(t => t.Value == "员工");
                                return;
                            }
                            break;
                            
                        default:
                            Growl.ErrorGlobal("未知的用户角色，无法加载申报对象");
                            return;
                    }
                    
                    // 只有当Nominees集合不为空时才设置SelectedNominee
                    if (Nominees.Count > 0)
                    {
                        SelectedNominee = Nominees.FirstOrDefault();
                    }
                    else
                    {
                        // 没有可用的提名对象时，显示警告
                        Growl.WarningGlobal("没有找到符合条件的提名对象");
                    }

                        OnPropertyChanged(nameof(IsNomineeVisible));
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"加载申报对象失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"加载申报对象异常详情: {ex}");
                
                // 确保Nominees不为空，避免后续使用时出错
                if (Nominees == null)
                {
                    Nominees = new ObservableCollection<object>();
                }
            }
        }

        private void InitializeNomineeTypes()
        {
            // 初始化提名类型选项
            NomineeTypes = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(1, "员工"),
                new KeyValuePair<int, string>(2, "管理员")
            };
        }
        
        /// <summary>
        /// 加载奖项封面图片
        /// </summary>
        private void LoadAwardImage()
        {
            if (SelectedAward != null && SelectedAward.CoverImage != null)
            {
                try
                {
                    // 如果CoverImage是字符串路径
                    if (!string.IsNullOrEmpty(SelectedAward.CoverImage))
                    {
                        // 创建ConVerterImage实例并调用Convert方法
                        var converter = new ConverterImage();
                        AwardImagePreview = converter.Convert(SelectedAward.CoverImage, typeof(ImageSource), null, null) as ImageSource;
                    }
                    else
                    {
                        AwardImagePreview = null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载奖项图片失败: {ex.Message}");
                    AwardImagePreview = null;
                }
            }
            else
            {
                AwardImagePreview = null;
            }
        }
        #endregion
        
        #region 命令处理
        private void OnSubmit()
        {
            try
            {
                // 基本数据验证
                if (!CanSubmit())
                {
                    return;
                }

                // 严格验证提名对象
                if (SelectedNominee == null)
                {
                    Growl.WarningGlobal("请选择申报对象");
                    return;
                }

                // 严格验证提名对象类型与选择匹配
                bool isValidNominee = false;
                if (SelectedNomineeType.Key == 1 && SelectedNominee is Employee)
                {
                    isValidNominee = true;
                }
                else if (SelectedNomineeType.Key == 2 && SelectedNominee is Admin)
                {
                    isValidNominee = true;
                }

                if (!isValidNominee)
                {
                    Growl.WarningGlobal("提名对象类型与选择不匹配，请重新选择");
                    // 尝试重新加载提名对象
                    LoadNominees();
                    return;
                }

                // 创建申报记录
                using (var context = new DataBaseContext())
                {
                    // 创建新的申报记录对象
                    var newDeclaration = new NominationDeclaration
                    {
                        AwardId = SelectedAward.AwardId,
                        DepartmentId = SelectedDepartment?.DepartmentId, // 允许为null
                        Introduction = Introduction ?? "", // 使用空字符串代替null
                        DeclarationReason = DeclarationReason,
                        DeclarationTime = DateTime.Now,
                        Status = 0, // 0表示待审核
                        IsActive = true
                    };

                    // 根据当前用户角色设置申报者身份
                    switch (CurrentUser.RoleId)
                    {
                        case 1: // 超级管理员
                            // 设置申报人为超级管理员
                            newDeclaration.DeclarerSupAdminId = CurrentUser.AdminId;
                            newDeclaration.DeclarerAdminId = null;
                            newDeclaration.DeclarerEmployeeId = null;
                            break;
                            
                        case 2: // 管理员
                            // 设置申报人为管理员
                            newDeclaration.DeclarerAdminId = CurrentUser.AdminId;
                            newDeclaration.DeclarerSupAdminId = null;
                            newDeclaration.DeclarerEmployeeId = null;
                            break;
                            
                        case 3: // 普通员工
                            // 设置申报人为员工
                            newDeclaration.DeclarerEmployeeId = CurrentUser.EmployeeId;
                            newDeclaration.DeclarerAdminId = null;
                            newDeclaration.DeclarerSupAdminId = null;
                            break;
                            
                        default:
                            Growl.ErrorGlobal("未知的用户角色，无法提交申报");
                            return;
                    }

                    // 设置被提名者ID
                    if (SelectedNominee is Employee employee)
                    {
                        newDeclaration.NominatedEmployeeId = employee.EmployeeId;
                        newDeclaration.NominatedAdminId = null;
                    }
                    else if (SelectedNominee is Admin admin)
                    {
                        newDeclaration.NominatedAdminId = admin.AdminId;
                        newDeclaration.NominatedEmployeeId = null;
                    }
                    else
                    {
                        Growl.ErrorGlobal("未知的申报对象类型");
                        return;
                    }

                    // 添加图片数据
                    if (CoverImage != null)
                    {
                        newDeclaration.CoverImage = CoverImage;
                    }

                    // 保存申报记录
                    context.NominationDeclarations.Add(newDeclaration);
                    context.SaveChanges();

                    // 记录操作日志
                    var log = new NominationLog
                    {
                        DeclarationId = newDeclaration.DeclarationId,
                        OperationType = 1, // 提交申报
                        OperationTime = DateTime.Now
                    };

                    // 根据当前用户角色设置操作者ID
                    switch (CurrentUser.RoleId)
                    {
                        case 1: // 超级管理员
                            log.OperatorSupAdminId = CurrentUser.AdminId;
                            log.OperatorAdminId = null;
                            log.OperatorEmployeeId = null;
                            break;
                            
                        case 2: // 管理员
                            log.OperatorAdminId = CurrentUser.AdminId;
                            log.OperatorSupAdminId = null;
                            log.OperatorEmployeeId = null;
                            break;
                            
                        case 3: // 普通员工
                            log.OperatorEmployeeId = CurrentUser.EmployeeId;
                            log.OperatorAdminId = null;
                            log.OperatorSupAdminId = null;
                            break;
                    }

                    context.NominationLogs.Add(log);
                    context.SaveChanges();

                    // 显示成功消息
                    Growl.SuccessGlobal("申报提交成功");
                    
                    // 发布申报添加事件
                    eventAggregator.GetEvent<NominationDeclarationAddEvent>().Publish();
                    
                    // 通知申报列表页面刷新数据
                    eventAggregator.GetEvent<NominationDataChangedEvent>().Publish();

                    // 清空编辑区
                    regionManager.Regions["NominationDeclarationEditRegion"].RemoveAll();
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"提交申报失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"提交申报异常详情: {ex}");
            }
        }

        private void OnCancel()
        {
            // 清空编辑区域
            regionManager.Regions["NominationDeclarationEditRegion"].RemoveAll();
        }

        private void OnSelectImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "选择图片";
            openFileDialog.Filter = "图片文件|*.jpg;*.jpeg;*.png;*.gif;*.bmp|所有文件|*.*";
            
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                CoverImage = ConverterImage.ConvertToByteArray(filePath);
            }
        }
        
        private void OnNavigateBack()
        {
            NavigateBack();
        }
        
        private void NavigateBack()
        {
            // 通知申报列表页面刷新数据
            eventAggregator.GetEvent<NominationDataChangedEvent>().Publish();
            
            // 清空编辑区域
            regionManager.Regions["NominationDeclarationEditRegion"].RemoveAll();
        }
        #endregion

        #region INavigationAware
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 不需要处理
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 重新加载数据
            LoadData();
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }
        #endregion

        private void AutoSelectDepartment(int? departmentId)
        {
            if (departmentId.HasValue && Departments != null)
            {
                var matchingDept = Departments.FirstOrDefault(d => d.DepartmentId == departmentId.Value);
                if (matchingDept != null)
                {
                    SelectedDepartment = matchingDept;
                }
                else
                {
                    SelectedDepartment = null; // 如果找不到匹配的部门，设置为null
                }
            }
            else
            {
                SelectedDepartment = null; // 如果部门ID为null，设置为null
            }
        }

        /// <summary>
        /// 检查当前用户是否已经提名过选定的奖项
        /// </summary>
        private void CheckIfAlreadyNominated()
        {
            if (SelectedAward == null)
                return;

            try
            {
                using (var context = new DataBaseContext())
                {
                    bool alreadyNominated = false;
                    string message = string.Empty;

                    // 根据用户角色检查是否已经提名申报过该奖项
                    switch (CurrentUser.RoleId)
                    {
                        case 1: // 超级管理员
                            // 管理员使用DeclarerAdminId字段
                            alreadyNominated = context.NominationDeclarations.Any(n => 
                                n.AwardId == SelectedAward.AwardId && 
                                n.DeclarerAdminId == CurrentUser.AdminId &&
                                n.Status == 1); // 1-已提交状态
                            
                            if (alreadyNominated)
                            {
                                message = $"您已经为奖项【{SelectedAward.AwardName}】提名申报过，不能重复申报";
                            }
                            break;

                        case 2: // 管理员
                            if (CurrentUser.AdminId.HasValue)
                            {
                                alreadyNominated = context.NominationDeclarations.Any(n => 
                                    n.AwardId == SelectedAward.AwardId && 
                                    n.DeclarerAdminId == CurrentUser.AdminId &&
                                    n.Status == 1); // 1-已提交状态
                                
                                if (alreadyNominated)
                                {
                                    message = $"您已经为奖项【{SelectedAward.AwardName}】提名申报过，不能重复申报";
                                }
                            }
                            break;

                        case 3: // 普通员工
                            if (CurrentUser.EmployeeId.HasValue)
                            {
                                alreadyNominated = context.NominationDeclarations.Any(n => 
                                    n.AwardId == SelectedAward.AwardId && 
                                    n.DeclarerEmployeeId == CurrentUser.EmployeeId &&
                                    n.Status == 1); // 1-已提交状态
                                
                                if (alreadyNominated)
                                {
                                    message = $"您已经为奖项【{SelectedAward.AwardName}】提名申报过，不能重复申报";
                                }
                            }
                            break;
                    }

                    // 如果已经提名过，显示提示
                    if (alreadyNominated)
                    {
                        Growl.WarningGlobal(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"检查提名状态失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"检查提名状态异常: {ex}");
            }
        }

        private bool CanSubmit()
        {
            // 数据验证：确保已选择奖项
            if (SelectedAward == null)
            {
                Growl.WarningGlobal("请选择申报奖项");
                return false;
            }

            // 数据验证：确保已选择提名对象类型
            if (SelectedNomineeType.Key == 0 && string.IsNullOrEmpty(SelectedNomineeType.Value))
            {
                Growl.WarningGlobal("请选择申报对象类型");
                return false;
            }

            // 数据验证：确保已填写申报理由
            if (string.IsNullOrWhiteSpace(DeclarationReason))
            {
                Growl.WarningGlobal("请填写申报理由");
                return false;
            }

            // 验证一句话介绍长度
            if (!string.IsNullOrEmpty(Introduction) && Introduction.Length > 100)
            {
                Growl.WarningGlobal("一句话介绍请控制在100字以内");
                return false;
            }

            // 验证申报理由长度
            if (DeclarationReason.Length > 500)
            {
                Growl.WarningGlobal("申报理由请控制在500字以内");
                return false;
            }

            return true;
        }
    }
} 