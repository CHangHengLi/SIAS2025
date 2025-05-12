using _2025毕业设计.Context;
using _2025毕业设计.Event;
using _2025毕业设计.Models;
using _2025毕业设计.Services;
using HandyControl.Controls;

namespace _2025毕业设计.ViewModels.EditMessage.DepartmentManager
{
    public class EditDepartmentViewModel : BindableBase, INavigationAware
    {
        #region 服务
        private readonly IDepartmentService departmentService;
        #endregion

        #region 区域导航
        private readonly IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region  属性

        #region 原始部门储存
        private Department baseDepartment;
        public Department BaseDepartment
        {
            get { return baseDepartment; }
            set { SetProperty(ref baseDepartment, value); }
        }
        #endregion

        #region 部门名称
        private string? departmentName;
        public string? DepartmentName
        {
            get { return departmentName; }
            set { SetProperty(ref departmentName, value); }
        }
        #endregion

        #endregion

        #region 构造函数
        public EditDepartmentViewModel(IDepartmentService departmentService, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            #region 服务
            this.departmentService = departmentService;

            #endregion
            this.eventAggregator = eventAggregator;
            this.regionManager = regionManager;
            SubmitCommand = new DelegateCommand(OnSubmit);
            SaveCommand = new DelegateCommand(OnSave);
            CancelCommand = new DelegateCommand(OnCancel);

        }

        #endregion

        #region 方法
        public DelegateCommand SaveCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }


        #endregion

        #region INavigationAware接口的实现

        #region  OnNavigatedTo方法: 当导航到该视图模型时调用
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("Department"))
            {
                try
                {
                    BaseDepartment = navigationContext.Parameters["Department"] as Department;
                    DepartmentName = BaseDepartment.DepartmentName ?? "";
                }
                catch (Exception)
                {
                    Growl.Error("获取部门信息失败");
                }
            }
        }
        #endregion

        #region IsNavigationTarget方法: 判断是否可以导航到该视图模型
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }
        #endregion

        #region OnNavigatedFrom方法: 当导航离开该视图模型时调用

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }
        #endregion

        #endregion

        #region OnSave方法: 保存按钮点击事件
        private async void OnSave()
        {
            //账号密码不能为空
            if (string.IsNullOrEmpty(DepartmentName))
            {
                Growl.Warning("账号或密码不能为空");
                return;
            }

            if (DepartmentName == BaseDepartment.DepartmentName) { Growl.Success("部门名称修改成功"); OnCancel(); return; }
            bool nameExists = await Task.Run(() =>
            {
                return departmentService.DepartmentNameExists(DepartmentName);
            });
            if (nameExists)
            {
                Growl.Warning("部门名称已存在");
                return;
            }
            else
            {
                using (var context = new DataBaseContext())
                {
                    BaseDepartment.DepartmentName = DepartmentName;
                    context.Departments.Update(BaseDepartment);
                    context.SaveChanges();
                }
            }
            // 发布事件通知左侧视图更新
            eventAggregator.GetEvent<DepartmentUpdatedEvent>().Publish();
            Growl.SuccessGlobal("部门名称修改成功");
            OnCancel();

        }
        private void OnCancel()
        {
            var region = regionManager.Regions["DepartmentEditRegion"];
            region.RemoveAll();
            // 导航回到员工管理视图
        }

        #endregion

        #region Enter键触发保存按钮点击事件
        public DelegateCommand SubmitCommand { get; private set; }
        private void OnSubmit()
        {
            // 执行在按下回车键时触发的逻辑
            SaveCommand.Execute();
        }
        #endregion

    }
}
