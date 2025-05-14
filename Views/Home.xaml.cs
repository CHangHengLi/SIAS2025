using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIASGraduate.Views
{
    /// <summary>
    /// Home.xaml 的交互逻辑
    /// </summary>
    public partial class Home : Window
    {
        private const double MinWindowWidth = 800;
        private const double MinWindowHeight = 500;
        private double baseWidth;
        private double baseHeight;

        public Home()
        {
            InitializeComponent();

            // 记录初始窗口尺寸作为基础值
            baseWidth = MinWindowWidth;
            baseHeight = MinWindowHeight;

            // 添加窗口大小变化事件处理器
            SizeChanged += Home_SizeChanged;

            // 添加窗口状态变化事件处理器
            StateChanged += Home_StateChanged;

            // 设置内容最小尺寸
            MinWidth = MinWindowWidth;
            MinHeight = MinWindowHeight;
        }

        private void Home_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayout();

            // 当窗口大小改变时，计算缩放比例
            if (ApplicationScaleTransform != null && ActualWidth > 0 && ActualHeight > 0)
            {
                // 只有当窗口宽度大于基础宽度时才进行缩放
                if (ActualWidth > baseWidth || ActualHeight > baseHeight)
                {
                    // 强制更大的缩放效果
                    double scaleX = Math.Max(ActualWidth / baseWidth, 2.2); // 至少2.5倍
                    double scaleY = Math.Max(ActualHeight / baseHeight, 2.2); // 至少2.5倍
                    double scale = Math.Min(scaleX, scaleY); // 保持比例

                    // 应用缩放变换，实现等比缩放
                    ApplicationScaleTransform.ScaleX = scale;
                    ApplicationScaleTransform.ScaleY = scale;

                    // 通知视图刷新布局
                    InvalidateVisual();
                    UpdateLayout();
                }
                else
                {
                    // 如果窗口小于或等于基础尺寸，则不进行缩放
                    ApplicationScaleTransform.ScaleX = 1.0;
                    ApplicationScaleTransform.ScaleY = 1.0;

                    // 通知视图刷新布局
                    InvalidateVisual();
                    UpdateLayout();
                }
            }
        }

        private void Home_StateChanged(object sender, EventArgs e)
        {
            // 当窗口状态改变时（最大化/还原），确保重新计算缩放比例
            if (WindowState == WindowState.Normal)
            {
                // 还原后重置缩放
                ApplicationScaleTransform.ScaleX = 1.0;
                ApplicationScaleTransform.ScaleY = 1.0;

                // 通知视图刷新布局
                InvalidateVisual();
                UpdateLayout();
            }
            else if (WindowState == WindowState.Maximized)
            {
                // 最大化时直接使用2.5倍缩放
                ApplicationScaleTransform.ScaleX = 2.2;
                ApplicationScaleTransform.ScaleY = 2.2;
            }
        }

        // 处理快捷入口区域的鼠标滚轮事件
        private void ShortcutScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            if (e.Delta > 0)
            {
                scrollViewer.LineLeft();
                scrollViewer.LineLeft();
            }
            else
            {
                scrollViewer.LineRight();
                scrollViewer.LineRight();
            }
            e.Handled = true;
        }
    }
}
