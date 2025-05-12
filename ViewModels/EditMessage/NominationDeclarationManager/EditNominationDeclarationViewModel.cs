using _2025毕业设计.Common;
using _2025毕业设计.Converter;
using _2025毕业设计.Context;
using _2025毕业设计.Event;
using _2025毕业设计.Models;
using ConverterImage = _2025毕业设计.Converter.ConVerterImage;
using HandyControl.Controls;
using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace _2025毕业设计.ViewModels.EditMessage.NominationDeclarationManager
{
    public class EditNominationDeclarationViewModel : INotifyPropertyChanged, INavigationAware
    {
        #region 区域管理器
        private readonly IRegionManager regionManager;
        #endregion

        #region 事件聚合器
        private readonly IEventAggregator eventAggregator;
        #endregion

        #region 构造函数
        public EditNominationDeclarationViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            #region 初始化命令
            SaveCommand = new DelegateCommand(OnSave);
            CancelCommand = new DelegateCommand(OnCancel);
            SelectImageCommand = new DelegateCommand(OnSelectImage);
            #endregion
        }
        #endregion

        #region 属性
        private NominationDeclaration declaration;
        public NominationDeclaration Declaration
        {
            get => declaration;
            set
            {
                if (SetProperty(ref declaration, value))
                {
                    Introduction = declaration?.Introduction;
                    DeclarationReason = declaration?.DeclarationReason;
                    CoverImage = declaration?.CoverImage;
                }
            }
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
            set => SetProperty(ref coverImagePreview, value);
        }
        #endregion

        #region 命令
        public DelegateCommand SaveCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }
        public DelegateCommand SelectImageCommand { get; private set; }
        #endregion

        #region 命令处理
        private void OnSave()
        {
            try
            {
                // 验证数据
                if (string.IsNullOrWhiteSpace(Introduction))
                {
                    Growl.WarningGlobal("请填写一句话介绍");
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(DeclarationReason))
                {
                    Growl.WarningGlobal("请填写申报理由");
                    return;
                }
                
                // 更新申报记录
                using (var context = new DataBaseContext())
                {
                    var entity = context.NominationDeclarations.Find(Declaration.DeclarationId);
                    if (entity != null)
                    {
                        entity.Introduction = Introduction;
                        entity.DeclarationReason = DeclarationReason;
                        
                        if (CoverImage != null)
                        {
                            entity.CoverImage = CoverImage;
                        }
                        
                        context.SaveChanges();
                        
                        // 添加操作日志
                        var log = new NominationLog
                        {
                            DeclarationId = entity.DeclarationId,
                            OperationType = 5, // 编辑
                            OperationTime = DateTime.Now,
                            Content = "编辑申报"
                        };
                        
                        // 设置操作人
                        switch (CurrentUser.RoleId)
                        {
                            case 1: // 超级管理员
                                log.OperatorSupAdminId = CurrentUser.AdminId;
                                break;
                            case 2: // 管理员
                                log.OperatorAdminId = CurrentUser.AdminId;
                                break;
                            case 3: // 普通员工
                                log.OperatorEmployeeId = CurrentUser.EmployeeId;
                                break;
                        }
                        
                        context.NominationLogs.Add(log);
                        context.SaveChanges();
                        
                        // 发布更新事件
                        eventAggregator.GetEvent<NominationDeclarationUpdateEvent>().Publish();
                        
                        Growl.SuccessGlobal("申报修改成功");
                    }
                }
                
                // 清空编辑区域
                regionManager.Regions["NominationDeclarationEditRegion"].RemoveAll();
            }
            catch (Exception ex)
            {
                Growl.ErrorGlobal($"保存申报失败: {ex.Message}");
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
                CoverImage = ConverterImage.ConvertPngToByteArray(filePath);
            }
        }
        #endregion

        #region INavigationAware
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            if (Declaration == null)
                return true;
                
            var declaration = navigationContext.Parameters["Declaration"] as NominationDeclaration;
            return Declaration.DeclarationId == declaration?.DeclarationId;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 不需要处理
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("Declaration"))
            {
                Declaration = navigationContext.Parameters["Declaration"] as NominationDeclaration;
            }
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
        #endregion
    }
} 