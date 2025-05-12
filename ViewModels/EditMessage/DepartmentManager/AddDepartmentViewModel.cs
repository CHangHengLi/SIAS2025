using _2025毕业设计.Event;
using _2025毕业设计.Models;
using _2025毕业设计.Services;
using HandyControl.Controls;
using Microsoft.IdentityModel.Tokens;

namespace _2025毕业设计.ViewModels.EditMessage.DepartmentManager
{
    public class AddDepartmentViewModel : BindableBase
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

        #region 添加部门
        private Department addDepartment;
        public Department AddDepartment
        {
            get => addDepartment;
            set => SetProperty(ref addDepartment, value);
        }
        #endregion

        #region 部门名称
        private string departmentName;
        public string DepartmentName
        {
            get { return departmentName; }
            set { SetProperty(ref departmentName, value); }
        }


        #endregion

        #region 构造函数
        public AddDepartmentViewModel(IDepartmentService departmentService, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            #region 服务
            this.departmentService = departmentService;
            #endregion

            SubmitCommand = new DelegateCommand(OnSubmit);

            this.eventAggregator = eventAggregator;
            this.regionManager = regionManager;
            SaveCommand = new DelegateCommand(OnSave);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        #endregion

        #region 方法
        public DelegateCommand SaveCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        #endregion

        #region OnSave方法: 保存按钮点击事件
        private async void OnSave()
        {
            //账号密码不能为空
            if (DepartmentName.IsNullOrEmpty())
            {
                Growl.Warning("部门名称不能为空");
                return;
            }

            bool nameExists = await Task.Run(() =>
            {
                return departmentService.DepartmentNameExists(DepartmentName); ;
            });
            if (nameExists)
            {
                Growl.Warning("账号名已存在");
                return;
            }



            var AddDepartment = new Department()
            {
                DepartmentName = DepartmentName

            };
            departmentService.AddDepartment(AddDepartment);
            // 发布事件通知左侧视图更新
            eventAggregator.GetEvent<DepartmentAddEvent>().Publish();
            Growl.SuccessGlobal("部门添加成功");
            OnCancel();
        }
        private void OnCancel()
        {
            var region = regionManager.Regions["DepartmentEditRegion"];
            region.RemoveAll();
            // 导航回到员工管理视图
        }
        #endregion

        #region Enter
        public DelegateCommand SubmitCommand { get; private set; }
        private void OnSubmit()
        {
            // 执行在按下回车键时触发的逻辑
            SaveCommand.Execute();
        }
        #endregion
        #endregion
    }
}
