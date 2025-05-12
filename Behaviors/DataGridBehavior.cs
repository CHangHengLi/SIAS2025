using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace SIASGraduate.Behaviors
{
    /// <summary>
    /// DataGrid的行为类，用于处理DataGrid的各种事件
    /// </summary>
    public class DataGridBehavior : Behavior<DataGrid>
    {
        #region 依赖属性

        public static readonly DependencyProperty LoadingRowCommandProperty =
            DependencyProperty.Register(nameof(LoadingRowCommand), typeof(ICommand), typeof(DataGridBehavior), new PropertyMetadata(null));

        public static readonly DependencyProperty UnloadingRowCommandProperty =
            DependencyProperty.Register(nameof(UnloadingRowCommand), typeof(ICommand), typeof(DataGridBehavior), new PropertyMetadata(null));

        public static readonly DependencyProperty PreviewMouseWheelCommandProperty =
            DependencyProperty.Register(nameof(PreviewMouseWheelCommand), typeof(ICommand), typeof(DataGridBehavior), new PropertyMetadata(null));

        public static readonly DependencyProperty ScrollViewerProperty =
            DependencyProperty.Register(nameof(ScrollViewer), typeof(ScrollViewer), typeof(DataGridBehavior), new PropertyMetadata(null));

        /// <summary>
        /// 行加载命令
        /// </summary>
        public ICommand LoadingRowCommand
        {
            get { return (ICommand)GetValue(LoadingRowCommandProperty); }
            set { SetValue(LoadingRowCommandProperty, value); }
        }

        /// <summary>
        /// 行卸载命令
        /// </summary>
        public ICommand UnloadingRowCommand
        {
            get { return (ICommand)GetValue(UnloadingRowCommandProperty); }
            set { SetValue(UnloadingRowCommandProperty, value); }
        }

        /// <summary>
        /// 鼠标滚轮预览命令
        /// </summary>
        public ICommand PreviewMouseWheelCommand
        {
            get { return (ICommand)GetValue(PreviewMouseWheelCommandProperty); }
            set { SetValue(PreviewMouseWheelCommandProperty, value); }
        }

        /// <summary>
        /// 滚动视图
        /// </summary>
        public ScrollViewer ScrollViewer
        {
            get { return (ScrollViewer)GetValue(ScrollViewerProperty); }
            set { SetValue(ScrollViewerProperty, value); }
        }

        #endregion

        #region 事件处理

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.LoadingRow += OnLoadingRow;
            AssociatedObject.UnloadingRow += OnUnloadingRow;
            AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.LoadingRow -= OnLoadingRow;
            AssociatedObject.UnloadingRow -= OnUnloadingRow;
            AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;

            base.OnDetaching();
        }

        private void OnLoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (LoadingRowCommand?.CanExecute(e) == true)
            {
                LoadingRowCommand.Execute(e);
            }
        }

        private void OnUnloadingRow(object sender, DataGridRowEventArgs e)
        {
            if (UnloadingRowCommand?.CanExecute(e) == true)
            {
                UnloadingRowCommand.Execute(e);
            }
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (PreviewMouseWheelCommand?.CanExecute(e) == true)
            {
                PreviewMouseWheelCommand.Execute(e);
            }

            if (!e.Handled && ScrollViewer != null)
            {
                e.Handled = true;
                
                // 计算滚动偏移量
                double offset = ScrollViewer.VerticalOffset - (e.Delta / 3.0);
                
                // 确保偏移量在有效范围内
                offset = Math.Max(0, Math.Min(offset, ScrollViewer.ScrollableHeight));
                
                // 执行滚动
                ScrollViewer.ScrollToVerticalOffset(offset);
            }
        }

        #endregion
    }
} 