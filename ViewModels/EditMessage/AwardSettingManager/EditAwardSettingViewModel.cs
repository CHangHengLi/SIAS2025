using _2025毕业设计.Context;
using _2025毕业设计.Event;
using _2025毕业设计.Models;
using HandyControl.Controls;
using System.Windows.Input;

namespace _2025毕业设计.ViewModels.EditMessage.AwardSettingManager
{
    public class EditAwardSettingViewModel : BindableBase, INavigationAware
    {

        #region 区域导航
        private readonly IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 构造函数
        public EditAwardSettingViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
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

        #region 修改奖项
        private Award updateAward;
        public Award UpdateAward
        { get => updateAward; set => SetProperty(ref updateAward, value); }
        #endregion

        #region 奖项名称
        private string awardName;
        public string AwardName { get => awardName; set => SetProperty(ref awardName, value); }
        #endregion

        #region 奖项描述
        private string awardDescription;
        public string AwardDescription { get => awardDescription; set => SetProperty(ref awardDescription, value); }
        #endregion

        #region 最大投票次数
        private int maxVoteCount = 1;
        public int MaxVoteCount { get => maxVoteCount; set => SetProperty(ref maxVoteCount, value); }
        #endregion

        #endregion

        #region 命令

        #region 保存命令
        private DelegateCommand saveCommand;
        public ICommand SaveCommand => saveCommand ??= new DelegateCommand(Save);

        private async void Save()
        {
            using var context = new DataBaseContext();
            //账号密码不能为空
            if (string.IsNullOrEmpty(AwardName))
            {
                Growl.Warning("奖项名称不能为空");
                return;
            }
            if (AwardName == UpdateAward.AwardName)
            {
                UpdateAward.AwardName = AwardName;
                UpdateAward.AwardDescription = AwardDescription;
                UpdateAward.MaxVoteCount = MaxVoteCount;
                context.Awards.Update(UpdateAward);
                context.SaveChanges();
                eventAggregator.GetEvent<AwardUpdateEvent>().Publish();
                Growl.SuccessGlobal("奖项信息修改成功"); 
                Cancel();
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
            else
            {
                UpdateAward.AwardName = AwardName;
                UpdateAward.AwardDescription = AwardDescription;
                UpdateAward.MaxVoteCount = MaxVoteCount;
                context.Awards.Update(UpdateAward);
                context.SaveChanges();
            }
            // 发布事件通知左侧视图更新
            eventAggregator.GetEvent<AwardUpdateEvent>().Publish();
            Growl.SuccessGlobal("奖项信息修改成功");
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

        #region INavigationAware接口实现
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("Award"))
            {
                try
                {
                    UpdateAward = navigationContext.Parameters["Award"] as Award;
                    AwardName = UpdateAward?.AwardName ?? "";
                    AwardDescription = UpdateAward?.AwardDescription ?? "";
                    MaxVoteCount = UpdateAward?.MaxVoteCount ?? 1;
                }
                catch (Exception)
                {
                    Growl.Warning("获取奖项信息失败");
                }
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }
        #endregion
    }
}
