using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace SIASGraduate.Behaviors
{
    /// <summary>
    /// 滚动视图行为类，用于处理滚动相关的UI交互
    /// </summary>
    public class ScrollViewerBehavior : Behavior<ScrollViewer>
    {
        /// <summary>
        /// 滚动到顶部命令
        /// </summary>
        public static readonly DependencyProperty ScrollToTopCommandProperty =
            DependencyProperty.Register("ScrollToTopCommand", typeof(ICommand), typeof(ScrollViewerBehavior), new PropertyMetadata(null));
            
        /// <summary>
        /// 滚动到顶部命令
        /// </summary>
        public ICommand ScrollToTopCommand
        {
            get { return (ICommand)GetValue(ScrollToTopCommandProperty); }
            set { SetValue(ScrollToTopCommandProperty, value); }
        }
        
        /// <summary>
        /// 当行为附加到滚动视图上时调用
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            
            // 监听Loaded事件
            AssociatedObject.Loaded += ScrollViewer_Loaded;
        }
        
        /// <summary>
        /// 当行为从滚动视图上分离时调用
        /// </summary>
        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= ScrollViewer_Loaded;
            base.OnDetaching();
        }
        
        /// <summary>
        /// 处理滚动视图加载事件
        /// </summary>
        private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            // 可以在这里执行初始化操作
        }
        
        /// <summary>
        /// 执行滚动到顶部
        /// </summary>
        public void ScrollToTop()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.ScrollToTop();
            }
        }
    }
} 