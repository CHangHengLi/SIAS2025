using _2025毕业设计.Context;
using _2025毕业设计.Event;
using _2025毕业设计.Models;
using HandyControl.Controls;
using Microsoft.IdentityModel.Tokens;
using System.Windows.Input;

namespace _2025毕业设计.ViewModels.EditMessage.AwardSettingManager
{
    public class AddAwardSettingViewModel : BindableBase
    {
        #region 区域导航
        private readonly IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 构造函数
        public AddAwardSettingViewModel(IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            #region 区域导航
            this.regionManager = regionManager;
            #endregion

            #region 事件聚合器
            this.eventAggregator = eventAggregator;
            #endregion
        }
        #endregion

        #region 属性

        #region 添加奖项
        private Award addAward;
        public Award AddAward
        {
            get { return addAward; }
            set { SetProperty(ref addAward, value); }
        }
        #endregion

        #region 奖项名称
        private string awardName;
        public string AwardName
        {
            get { return awardName; }
            set { SetProperty(ref awardName, value); }
        }
        #endregion

        #region 奖项描述

        private string? awardDescription;

        public string? AwardDescription { get => awardDescription; set => SetProperty(ref awardDescription, value); }
        //private string? awardDescription;
        //public string? AwardDescription
        //{
        //    get { return awardDescription; }
        //    set { SetProperty(ref awardDescription, value); }
        //}
        #endregion

        #region 最大投票次数
        private int maxVoteCount = 1;
        public int MaxVoteCount
        {
            get { return maxVoteCount; }
            set { SetProperty(ref maxVoteCount, value); }
        }
        #endregion

        #endregion

        #region 命令

        #region 保存命令

        private DelegateCommand saveCommand;
        public ICommand SaveCommand => saveCommand ??= new DelegateCommand(Save);

        private async void Save()
        {
            using (var context = new DataBaseContext())
            {
                //账号密码不能为空
                if (AwardName.IsNullOrEmpty())
                {
                    Growl.Warning("新增奖项名称不能为空");
                    return;
                }

                bool nameExists = await Task.Run(() =>
            {
                return context.Awards.Any(a => a.AwardName == AwardName);
            });
                if (nameExists)
                {
                    Growl.Warning("奖项名称已存在");
                    return;
                }
                var AddDepartment = new Award()
                {
                    AwardName = AwardName,
                    AwardDescription = AwardDescription,
                    MaxVoteCount = MaxVoteCount
                };

                context.Awards.Add(AddDepartment);
                await context.SaveChangesAsync();
            }
            // 发布事件通知左侧视图更新
            eventAggregator.GetEvent<AwardAddEvent>().Publish();
            Cancel();
        }
        #endregion

        #region 取消命令
        private DelegateCommand cancelCommand;
        public ICommand CancelCommand => cancelCommand ??= new DelegateCommand(Cancel);

        private void Cancel()
        {
            var region = regionManager.Regions["AwardEditRegion"];
            region.RemoveAll();
            // 导航回到奖项设置视图
        }
        #endregion

        #endregion

    }
}
