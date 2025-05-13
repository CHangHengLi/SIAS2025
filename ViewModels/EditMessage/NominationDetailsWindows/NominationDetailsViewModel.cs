using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using _2025毕业设计.Models;
using Microsoft.EntityFrameworkCore;
using SIASGraduate.Context;
using SIASGraduate.Event;
using SIASGraduate.Models;

namespace SIASGraduate.ViewModels.EditMessage.NominationDetailsWindows
{
    /// <summary>
    /// 提名详情窗口的视图模型
    /// </summary>
    public class NominationDetailsViewModel : BindableBase
    {
        private readonly DataBaseContext _dbContext;
        private readonly IEventAggregator _eventAggregator;
        private Nomination _nomination;
        private bool _isAdmin;
        private string _errorMessage;
        
        /// <summary>
        /// 当前提名对象
        /// </summary>
        public Nomination Nomination
        {
            get => _nomination;
            set
            {
                if (_nomination != value)
                {
                    _nomination = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private string _nomineeName;
        /// <summary>
        /// 被提名人姓名
        /// </summary>
        public string NomineeName
        {
            get { return _nomineeName; }
            set { SetProperty(ref _nomineeName, value); }
        }
        
        private string _departmentName;
        /// <summary>
        /// 部门名称
        /// </summary>
        public string DepartmentName
        {
            get { return _departmentName; }
            set { SetProperty(ref _departmentName, value); }
        }
        
        private string _awardName;
        /// <summary>
        /// 奖项名称
        /// </summary>
        public string AwardName
        {
            get { return _awardName; }
            set { SetProperty(ref _awardName, value); }
        }
        
        private string _introduction;
        /// <summary>
        /// 一句话介绍
        /// </summary>
        public string Introduction
        {
            get { return _introduction; }
            set { SetProperty(ref _introduction, value); }
        }
        
        private string _reason;
        /// <summary>
        /// 提名理由
        /// </summary>
        public string Reason
        {
            get { return _reason; }
            set { SetProperty(ref _reason, value); }
        }
        
        private int _voteCount;
        /// <summary>
        /// 总投票数
        /// </summary>
        public int VoteCount
        {
            get { return _voteCount; }
            set { SetProperty(ref _voteCount, value); }
        }
        
        private byte[] _nomineeImage;
        /// <summary>
        /// 被提名人图片
        /// </summary>
        public byte[] NomineeImage
        {
            get { return _nomineeImage; }
            set { SetProperty(ref _nomineeImage, value); }
        }
        
        /// <summary>
        /// 员工投票数量
        /// </summary>
        public int EmployeeVoteCount
        {
            get { return Nomination?.VoteRecords?.Count(v => v.VoterEmployeeId != null) ?? 0; }
        }
        
        /// <summary>
        /// 管理员投票数量
        /// </summary>
        public int AdminVoteCount
        {
            get { return Nomination?.VoteRecords?.Count(v => v.VoterAdminId != null) ?? 0; }
        }
        
        /// <summary>
        /// 总投票数（根据Nomination计算）
        /// </summary>
        public int TotalVoteCount
        {
            get { return Nomination?.VoteRecords?.Count ?? 0; }
        }
        
        // 用于控制删除投票按钮可见性的属性
        private bool _isSuperAdmin;
        /// <summary>
        /// 是否为超级管理员
        /// </summary>
        public bool IsSuperAdmin
        {
            get { return _isSuperAdmin; }
            set { SetProperty(ref _isSuperAdmin, value); }
        }
        
        private bool _canVote;
        /// <summary>
        /// 是否可以投票
        /// </summary>
        public bool CanVote
        {
            get { return _canVote; }
            set { SetProperty(ref _canVote, value); }
        }
        
        private bool _hasVoted;
        /// <summary>
        /// 是否已经投票
        /// </summary>
        public bool HasVoted
        {
            get { return _hasVoted; }
            set { SetProperty(ref _hasVoted, value); }
        }
        
        private bool _showVoteButtons = false;  // 默认不显示投票按钮
        /// <summary>
        /// 是否显示投票按钮
        /// </summary>
        public bool ShowVoteButtons
        {
            get { return _showVoteButtons; }
            set { SetProperty(ref _showVoteButtons, value); }
        }
        
        // 投票命令
        private NominationDelegateCommand _voteCommand;
        /// <summary>
        /// 投票命令
        /// </summary>
        public NominationDelegateCommand VoteCommand => 
            _voteCommand ?? (_voteCommand = new NominationDelegateCommand(ExecuteVoteCommand, CanExecuteVoteCommand));
            
        private bool CanExecuteVoteCommand()
        {
            // 超级管理员不能投票
            if (SIASGraduate.Common.CurrentUser.RoleId == 1)
                return false;
                
            return CanVote && !HasVoted && Nomination != null;
        }
        
        private void ExecuteVoteCommand(object parameter)
        {
            // 超级管理员不能投票
            if (SIASGraduate.Common.CurrentUser.RoleId == 1)
            {
                MessageBox.Show("超级管理员不能参与投票，仅可查看", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            try
            {
                using (var db = new DataBaseContext())
                {
                    // 获取奖项信息，包括投票限制
                    var award = db.Awards.FirstOrDefault(a => a.AwardId == Nomination.AwardId);
                    if (award == null)
                    {
                        MessageBox.Show("无法获取奖项信息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // 确定当前用户类型和ID
                    int? userId = null;
                    bool isEmployee = false;
                    
                    if (SIASGraduate.Common.CurrentUser.RoleId == 2) // 管理员
                    {
                        userId = SIASGraduate.Common.CurrentUser.AdminId;
                        isEmployee = false;
                    }
                    else if (SIASGraduate.Common.CurrentUser.RoleId == 3) // 普通员工
                    {
                        userId = SIASGraduate.Common.CurrentUser.EmployeeId;
                        isEmployee = true;
                    }
                    
                    if (userId == null)
                    {
                        MessageBox.Show("用户信息无效", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // 检查用户对该奖项已投票数量
                    int userVoteCount = 0;
                    if (isEmployee)
                    {
                        userVoteCount = db.VoteRecords.Count(v => 
                            v.AwardId == award.AwardId && 
                            v.VoterEmployeeId == userId);
                    }
                    else
                    {
                        userVoteCount = db.VoteRecords.Count(v => 
                            v.AwardId == award.AwardId && 
                            v.VoterAdminId == userId);
                    }
                    
                    // 使用MaxVoteCount代替MaxVotesPerUser
                    if (userVoteCount >= award.MaxVoteCount)
                    {
                        MessageBox.Show($"您已达到该奖项的最大投票次数({award.MaxVoteCount}票)", 
                            "投票限制", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    // 确认投票
                    string voteInfo = award.MaxVoteCount > 1 
                        ? $"您已对该奖项投了{userVoteCount}票，最多可投{award.MaxVoteCount}票" 
                        : "";
                        
                    MessageBoxResult result = MessageBox.Show(
                        $"确定要为 {NomineeName} 投票吗？投票后不能撤销。\n{voteInfo}", 
                        "投票确认", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);
                        
                    if (result == MessageBoxResult.Yes)
                    {
                        var newVote = new VoteRecord
                        {
                            NominationId = Nomination.NominationId,
                            AwardId = Nomination.AwardId,
                            VoteTime = DateTime.Now
                        };
                        
                        // 根据当前用户类型设置投票人ID
                        if (!isEmployee) // 管理员
                        {
                            newVote.VoterAdminId = userId;
                        }
                        else // 普通员工
                        {
                            newVote.VoterEmployeeId = userId;
                        }
                        
                        db.VoteRecords.Add(newVote);
                        db.SaveChanges();
                        
                        // 获取新添加的完整投票记录（包含关联对象）
                        var voteWithInfo = db.VoteRecords
                            .Include(v => v.VoterEmployee)
                            .ThenInclude(e => e.Department)
                            .Include(v => v.VoterAdmin)
                            .ThenInclude(a => a.Department)
                            .FirstOrDefault(v => v.VoteRecordId == newVote.VoteRecordId);

                        if (voteWithInfo != null)
                        {
                            // 将新的投票记录添加到提名的投票记录集合中
                            if (Nomination.VoteRecords == null)
                            {
                                Nomination.VoteRecords = new ObservableCollection<VoteRecord>();
                            }
                            Nomination.VoteRecords.Insert(0, voteWithInfo); // 插入到列表开头
                            
                            // 更新状态
                            VoteCount++;
                            
                            // 检查是否达到最大投票数
                            if (userVoteCount + 1 >= award.MaxVoteCount)
                            {
                                HasVoted = true;
                                CanVote = false;
                            }
                            
                            // 通知UI更新
                            RaisePropertyChanged(nameof(Nomination));
                            RaisePropertyChanged(nameof(EmployeeVoteCount));
                            RaisePropertyChanged(nameof(AdminVoteCount));
                            RaisePropertyChanged(nameof(TotalVoteCount));
                            RaisePropertyChanged(nameof(VoteCount));
                            
                            // 发送投票事件通知其他组件更新
                            _eventAggregator?.GetEvent<VoteRecordAddedEvent>().Publish(Nomination.NominationId);
                            
                            MessageBox.Show("投票成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"投票失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // 关闭窗口命令
        private NominationDelegateCommand _closeCommand;
        /// <summary>
        /// 关闭窗口命令
        /// </summary>
        public NominationDelegateCommand CloseCommand => 
            _closeCommand ?? (_closeCommand = new NominationDelegateCommand(ExecuteCloseCommand));
            
        private void ExecuteCloseCommand(object parameter)
        {
            // 查找包含该ViewModel的Window并关闭它
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }
        
        /// <summary>
        /// 使用Nomination对象构造
        /// </summary>
        /// <param name="nomination">提名对象</param>
        public NominationDetailsViewModel(Nomination nomination)
        {
            // 初始化事件聚合器
            _eventAggregator = new Prism.Events.EventAggregator();
            
            // 设置提名对象
            Nomination = nomination;
            
            if (nomination != null)
            {
                // 初始化视图所需的数据
                if (nomination.NominatedEmployee != null)
                {
                    NomineeName = nomination.NominatedEmployee.EmployeeName;
                    NomineeImage = nomination.NominatedEmployee.EmployeeImage;
                }
                else if (nomination.NominatedAdmin != null)
                {
                    NomineeName = nomination.NominatedAdmin.AdminName;
                    NomineeImage = nomination.NominatedAdmin.AdminImage;
                }
                
                if (nomination.Department != null)
                {
                    DepartmentName = nomination.Department.DepartmentName;
                }
                
                if (nomination.Award != null)
                {
                    AwardName = nomination.Award.AwardName;
                }
                
                // 读取相关描述信息
                Introduction = nomination.Introduction;
                Reason = nomination.NominateReason;
                
                // 设置投票数
                VoteCount = nomination.VoteRecords?.Count ?? 0;
                
                // 加载完整的投票记录（包括关联实体）
                EnsureVoteRecordsLoaded(nomination.NominationId);
                
                // 检查当前用户是否已投票
                CheckIfUserHasVoted();
                
                // 检查是否为超级管理员 - RoleId=1表示超级管理员
                IsSuperAdmin = SIASGraduate.Common.CurrentUser.RoleId == 1;
                
                // 超级管理员不能投票但可以管理投票记录
                if (IsSuperAdmin)
                {
                    CanVote = false;
                    
                    // 刷新DeleteVoteRecordCommand的可执行状态
                    if (_deleteVoteRecordCommand != null)
                        _deleteVoteRecordCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 使用VoteDetailDto对象构造
        /// </summary>
        /// <param name="voteDetail">投票详细数据传输对象</param>
        public NominationDetailsViewModel(VoteDetailDto voteDetail)
        {
            // 初始化事件聚合器
            _eventAggregator = new Prism.Events.EventAggregator();
            
            if (voteDetail != null)
            {
                // 创建一个新的Nomination对象
                Nomination = new Nomination
                {
                    NominationId = voteDetail.NominationId,
                    AwardId = voteDetail.AwardId,
                    DepartmentId = voteDetail.DepartmentId,
                    Introduction = voteDetail.Introduction,
                    NominateReason = voteDetail.NominateReason,
                    VoteRecords = new ObservableCollection<VoteRecord>()
                };
                
                // 设置其他属性
                NomineeName = voteDetail.NomineeName;
                DepartmentName = voteDetail.DepartmentName;
                AwardName = voteDetail.AwardName;
                Introduction = voteDetail.Introduction;
                Reason = voteDetail.NominateReason;
                VoteCount = voteDetail.VoteCount;
                
                // 检查当前用户是否为超级管理员
                IsSuperAdmin = SIASGraduate.Common.CurrentUser.RoleId == 1;
                
                // 超级管理员不能投票
                if (SIASGraduate.Common.CurrentUser.RoleId == 1)
                {
                    CanVote = false;
                }
                
                // 注意：此时VoteRecords为空，需要在外部加载完整的投票记录
            }
        }
        
        /// <summary>
        /// 确保投票记录关联数据被正确加载
        /// </summary>
        private void EnsureVoteRecordsLoaded(int nominationId)
        {
            // 如果投票记录为空或者没有包含关联数据，则重新加载
            if (Nomination?.VoteRecords == null || 
                Nomination.VoteRecords.Any(v => v.VoterEmployee == null && v.VoterEmployeeId != null))
            {
                try
                {
                    using (var db = new DataBaseContext())
                    {
                        // 使用分步方式加载数据，避免NotMapped属性问题
                        
                        // 1. 首先获取投票记录
                        var voteRecords = db.VoteRecords
                            .AsNoTracking()
                            .Where(v => v.NominationId == nominationId)
                            .OrderByDescending(v => v.VoteTime)
                            .ToList();
                        
                        // 2. 手动加载关联实体
                        foreach (var record in voteRecords)
                        {
                            // 加载投票者(员工)信息
                            if (record.VoterEmployeeId.HasValue)
                            {
                                record.VoterEmployee = db.Employees
                                    .AsNoTracking()
                                    .FirstOrDefault(e => e.EmployeeId == record.VoterEmployeeId);
                                    
                                // 如果员工存在且有部门ID，单独加载部门
                                if (record.VoterEmployee != null && record.VoterEmployee.DepartmentId.HasValue)
                                {
                                    record.VoterEmployee.Department = db.Departments
                                        .AsNoTracking()
                                        .FirstOrDefault(d => d.DepartmentId == record.VoterEmployee.DepartmentId);
                                }
                            }
                            
                            // 加载投票者(管理员)信息
                            if (record.VoterAdminId.HasValue)
                            {
                                record.VoterAdmin = db.Admins
                                    .AsNoTracking()
                                    .FirstOrDefault(a => a.AdminId == record.VoterAdminId);
                                    
                                // 如果管理员存在且有部门ID，单独加载部门
                                if (record.VoterAdmin != null && record.VoterAdmin.DepartmentId.HasValue)
                                {
                                    record.VoterAdmin.Department = db.Departments
                                        .AsNoTracking()
                                        .FirstOrDefault(d => d.DepartmentId == record.VoterAdmin.DepartmentId);
                                }
                            }
                            
                            // 加载奖项信息
                            record.Award = db.Awards
                                .AsNoTracking()
                                .FirstOrDefault(a => a.AwardId == record.AwardId);
                        }
                        
                        // 更新提名对象的投票记录集合
                        if (Nomination.VoteRecords == null)
                        {
                            Nomination.VoteRecords = new ObservableCollection<VoteRecord>();
                        }
                        else
                        {
                            Nomination.VoteRecords.Clear();
                        }
                        
                        foreach (var record in voteRecords)
                        {
                            Nomination.VoteRecords.Add(record);
                        }
                            
                        System.Diagnostics.Debug.WriteLine($"已加载 {Nomination.VoteRecords.Count} 条投票记录");
                        
                        // 通知UI更新
                        RaisePropertyChanged(nameof(Nomination));
                        RaisePropertyChanged(nameof(EmployeeVoteCount));
                        RaisePropertyChanged(nameof(AdminVoteCount));
                        RaisePropertyChanged(nameof(TotalVoteCount));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载投票记录失败: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 检查当前用户是否已对当前提名投票
        /// </summary>
        private void CheckIfUserHasVoted()
        {
            if (Nomination == null) return;
            
            using (var db = new DataBaseContext())
            {
                int userId = 0;
                bool isEmployee = false;
                
                if (SIASGraduate.Common.CurrentUser.EmployeeId.HasValue)
                {
                    userId = SIASGraduate.Common.CurrentUser.EmployeeId.Value;
                    isEmployee = true;
                }
                else if (SIASGraduate.Common.CurrentUser.AdminId.HasValue)
                {
                    userId = SIASGraduate.Common.CurrentUser.AdminId.Value;
                    isEmployee = false;
                }
                
                HasVoted = CheckIfUserHasVoted(db, Nomination.NominationId, userId, isEmployee);
                
                // 如果没有投票，则可以投票（此处可以添加其他条件）
                CanVote = !HasVoted;
            }
        }
        
        /// <summary>
        /// 检查用户是否已投票
        /// </summary>
        private bool CheckIfUserHasVoted(DataBaseContext db, int nominationId, int userId, bool isEmployee)
        {
            if (isEmployee)
            {
                return db.VoteRecords.Any(v => 
                    v.NominationId == nominationId && 
                    v.VoterEmployeeId == userId);
            }
            else
            {
                return db.VoteRecords.Any(v => 
                    v.NominationId == nominationId && 
                    v.VoterAdminId == userId);
            }
        }

        // 添加删除投票记录的命令
        private NominationDelegateCommand<VoteRecord> _deleteVoteRecordCommand;
        /// <summary>
        /// 删除投票记录的命令
        /// </summary>
        public NominationDelegateCommand<VoteRecord> DeleteVoteRecordCommand =>
            _deleteVoteRecordCommand ?? (_deleteVoteRecordCommand = new NominationDelegateCommand<VoteRecord>(ExecuteDeleteVoteRecordCommand, CanExecuteDeleteVoteRecordCommand));
            
        // 添加一键删除所有投票记录的命令
        private NominationDelegateCommand _deleteAllVoteRecordsCommand;
        /// <summary>
        /// 一键删除所有投票记录的命令
        /// </summary>
        public NominationDelegateCommand DeleteAllVoteRecordsCommand =>
            _deleteAllVoteRecordsCommand ?? (_deleteAllVoteRecordsCommand = new NominationDelegateCommand(ExecuteDeleteAllVoteRecordsCommand, CanExecuteDeleteAllVoteRecordsCommand));
        
        /// <summary>
        /// 检查是否可以执行一键删除所有投票记录的命令
        /// </summary>
        private bool CanExecuteDeleteAllVoteRecordsCommand()
        {
            // 只有超级管理员可以删除投票记录，且必须有投票记录
            return IsSuperAdmin && Nomination?.VoteRecords != null && Nomination.VoteRecords.Count > 0;
        }
        
        /// <summary>
        /// 执行一键删除所有投票记录的命令
        /// </summary>
        private void ExecuteDeleteAllVoteRecordsCommand(object parameter)
        {
            // 确认是否要删除所有投票记录
            if (Nomination?.VoteRecords == null || Nomination.VoteRecords.Count == 0)
            {
                MessageBox.Show("没有可删除的投票记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            int recordCount = Nomination.VoteRecords.Count;
            
            MessageBoxResult result = MessageBox.Show(
                $"确定要删除此提名下的所有 {recordCount} 条投票记录吗？\n\n删除后每个投票用户将收到票数返还，他们可以重新投票。", 
                "批量删除确认", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new DataBaseContext())
                    {
                        // 查找所有与当前提名相关的投票记录
                        var votesToDelete = db.VoteRecords
                            .Where(v => v.NominationId == Nomination.NominationId)
                            .ToList();
                        
                        if (votesToDelete.Count > 0)
                        {
                            // 记录日志信息
                            var operationDetail = $"超级管理员一键删除了提名 {Nomination.NominationId} 的所有投票记录，共 {votesToDelete.Count} 条";
                            System.Diagnostics.Debug.WriteLine(operationDetail);
                            
                            // 删除所有投票记录
                            db.VoteRecords.RemoveRange(votesToDelete);
                            db.SaveChanges();
                            
                            // 清空提名对象的投票记录集合
                            if (Nomination?.VoteRecords != null)
                            {
                                Nomination.VoteRecords.Clear();
                            }
                            
                            // 更新投票数量属性
                            VoteCount = 0;
                            
                            // 通知属性变更
                            RaisePropertyChanged(nameof(EmployeeVoteCount));
                            RaisePropertyChanged(nameof(AdminVoteCount));
                            RaisePropertyChanged(nameof(TotalVoteCount));
                            RaisePropertyChanged(nameof(VoteCount));
                            RaisePropertyChanged(nameof(Nomination));
                            
                            // 发送投票更新事件
                            _eventAggregator?.GetEvent<VoteRecordDeletedEvent>().Publish(Nomination.NominationId);
                            
                            // 显示成功消息
                            MessageBox.Show($"已成功删除所有 {votesToDelete.Count} 条投票记录，用户已收到票数返还。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("未找到与此提名相关的投票记录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"批量删除投票记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 检查是否可以执行删除投票记录的命令
        /// </summary>
        private bool CanExecuteDeleteVoteRecordCommand(VoteRecord record)
        {
            // 只有超级管理员可以删除投票记录
            return IsSuperAdmin && record != null;
        }
        
        /// <summary>
        /// 执行删除投票记录的命令
        /// </summary>
        private void ExecuteDeleteVoteRecordCommand(VoteRecord voteRecord)
        {
            // 确认是否要删除
            MessageBoxResult result = MessageBox.Show(
                "确定要删除此投票记录吗？删除后该用户可以重新投票。", 
                "删除确认", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new DataBaseContext())
                    {
                        // 查找匹配的投票记录
                        var voteToDelete = db.VoteRecords
                            .Include(v => v.Award)
                            .Include(v => v.VoterEmployee)
                            .Include(v => v.VoterAdmin)
                            .FirstOrDefault(v => v.VoteRecordId == voteRecord.VoteRecordId);
                        
                        if (voteToDelete != null)
                        {
                            // 需要获取提名ID和奖项ID
                            int nominationId = voteToDelete.NominationId;
                            int awardId = voteToDelete.AwardId;
                            
                            // 获取用户ID信息
                            int? employeeId = voteToDelete.VoterEmployeeId;
                            int? adminId = voteToDelete.VoterAdminId;
                            
                            // 获取奖项投票设置（获取最大投票数等信息）
                            var award = db.Awards.FirstOrDefault(a => a.AwardId == awardId);
                            
                            // 删除投票记录
                            db.VoteRecords.Remove(voteToDelete);
                            db.SaveChanges();
                            
                            // 日志记录投票删除操作
                            var userType = employeeId.HasValue ? "员工" : "管理员";
                            var userId = employeeId.HasValue ? employeeId.Value : adminId.Value;
                            var userName = employeeId.HasValue ? 
                                voteToDelete.VoterEmployee?.EmployeeName : 
                                voteToDelete.VoterAdmin?.AdminName;
                            var operationDetail = $"超级管理员删除了{userType} {userName}(ID:{userId}) 对提名 {Nomination.NominationId} 的投票记录";
                            System.Diagnostics.Debug.WriteLine(operationDetail);
                            
                            // 从提名对象的投票记录集合中移除
                            if (Nomination?.VoteRecords != null)
                            {
                                var recordToRemove = Nomination.VoteRecords.FirstOrDefault(vr => 
                                    vr.VoteRecordId == voteRecord.VoteRecordId);
                                
                                if (recordToRemove != null)
                                {
                                    Nomination.VoteRecords.Remove(recordToRemove);
                                }
                            }
                            
                            // 更新投票数量属性
                            VoteCount = Nomination?.VoteRecords?.Count ?? 0;
                            
                            // 通知属性变更
                            RaisePropertyChanged(nameof(EmployeeVoteCount));
                            RaisePropertyChanged(nameof(AdminVoteCount));
                            RaisePropertyChanged(nameof(TotalVoteCount));
                            RaisePropertyChanged(nameof(VoteCount));
                            RaisePropertyChanged(nameof(Nomination));
                            
                            // 发送投票更新事件
                            _eventAggregator?.GetEvent<VoteRecordDeletedEvent>().Publish(Nomination.NominationId);
                            
                            // 显示适当的成功消息
                            var voteLimit = award?.MaxVoteCount ?? 1;
                            string message = voteLimit > 1 
                                ? $"投票记录已删除，用户已返还对该奖项的一票（最多可投{voteLimit}票）"
                                : "投票记录已删除，用户已返还一票";
                            
                            MessageBox.Show(message, "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("未找到对应的投票记录", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除投票记录时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 自定义命令类
    /// </summary>
    public class NominationDelegateCommand : ICommand
    {
        private readonly Action<object> _executeMethod;
        private readonly Func<bool> _canExecuteMethod;

        public event EventHandler CanExecuteChanged;

        public NominationDelegateCommand(Action<object> executeMethod, Func<bool> canExecuteMethod = null)
        {
            _executeMethod = executeMethod ?? throw new ArgumentNullException(nameof(executeMethod));
            _canExecuteMethod = canExecuteMethod;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteMethod?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            _executeMethod(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 自定义命令类
    /// </summary>
    public class NominationDelegateCommand<T> : ICommand
    {
        private readonly Action<T> _executeMethod;
        private readonly Func<T, bool> _canExecuteMethod;

        public event EventHandler CanExecuteChanged;

        public NominationDelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod = null)
        {
            _executeMethod = executeMethod ?? throw new ArgumentNullException(nameof(executeMethod));
            _canExecuteMethod = canExecuteMethod;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteMethod?.Invoke((T)parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _executeMethod((T)parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
} 
