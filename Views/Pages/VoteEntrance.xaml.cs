using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SIASGraduate.ViewModels.Pages;

namespace SIASGraduate.Views.Pages
{
    /// <summary>
    /// VoteEntrance.xaml 的交互逻辑
    /// </summary>
    public partial class VoteEntrance : UserControl
    {
        /// <summary>
        /// 初始化组件
        /// </summary>
        /// <remarks>
        /// 根据MVVM设计模式，业务逻辑已移至VoteEntranceViewModel
        /// </remarks>
        public VoteEntrance()
        {
            InitializeComponent();
            Loaded += VoteEntrance_Loaded;
        }

        /// <summary>
        /// 页面加载事件
        /// </summary>
        private void VoteEntrance_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保控件和ScrollViewer已经初始化
            if (DataContext is VoteEntranceViewModel viewModel)
            {
                // 将ScrollToTop命令绑定到实际的方法
                viewModel.ScrollToTopRequested += ScrollToTop;
            }
        }

        /// <summary>
        /// 处理内部控件的鼠标滚轮事件，使其传递到主ScrollViewer
        /// </summary>
        private void Control_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled && MainScrollViewer != null)
            {
                e.Handled = true;
                
                // 计算滚动偏移量
                double offset = MainScrollViewer.VerticalOffset - (e.Delta / 3.0);
                
                // 确保偏移量在有效范围内
                offset = Math.Max(0, Math.Min(offset, MainScrollViewer.ScrollableHeight));
                
                // 执行滚动
                MainScrollViewer.ScrollToVerticalOffset(offset);
            }
        }

        /// <summary>
        /// 滚动到顶部方法
        /// </summary>
        public void ScrollToTop()
        {
            if (MainScrollViewer != null)
            {
                MainScrollViewer.ScrollToTop();
            }
        }

        /// <summary>
        /// 处理投票按钮的鼠标左键按下事件
        /// 注意：此方法目前未被使用，因为按钮上没有绑定MouseLeftButtonDown事件
        /// 保留此方法是为了向后兼容
        /// </summary>
        private void VoteButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 方法保留但不执行任何操作，避免重复投票
            // 实际的投票逻辑已通过Command属性绑定到ViewModel中的VoteButtonClickCommand
        }

        /// <summary>
        /// 处理投票按钮的预览鼠标左键按下事件
        /// 注意：此方法目前未被使用，因为按钮上没有绑定PreviewMouseLeftButtonDown事件
        /// 保留此方法是为了向后兼容
        /// </summary>
        private void VoteButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 方法保留但不执行任何操作，避免重复投票
            // 实际的投票逻辑已通过Command属性绑定到ViewModel中的VoteButtonClickCommand
        }
    }
}
