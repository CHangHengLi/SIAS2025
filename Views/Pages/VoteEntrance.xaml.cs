using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using _2025毕业设计.ViewModels.Pages;

namespace _2025毕业设计.Views.Pages
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
    }
}
