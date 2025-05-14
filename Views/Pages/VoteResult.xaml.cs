using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SIASGraduate.Models;
using SIASGraduate.ViewModels.Pages;

namespace SIASGraduate.Views.Pages
{
    /// <summary>
    /// VoteResult.xaml 的交互逻辑
    /// </summary>
    public partial class VoteResult : UserControl
    {
        /// <summary>
        /// 初始化组件
        /// </summary>
        /// <remarks>
        /// 根据MVVM设计模式，业务逻辑已移至VoteResultViewModel
        /// </remarks>
        public VoteResult()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 处理DataGrid的鼠标滚轮事件，使滚轮可以控制父容器的滚动
        /// </summary>
        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }

        /// <summary>
        /// 数据网格行加载事件，委托给ViewModel处理
        /// </summary>
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (DataContext is VoteResultViewModel viewModel && e.Row.DataContext is Nomination nomination)
            {
                viewModel.HandleRowLoading(nomination);
                e.Row.Tag = nomination.NominationId; // 保留行标识
            }
        }

        /// <summary>
        /// 数据网格行卸载事件，委托给ViewModel处理
        /// </summary>
        private void DataGrid_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            if (DataContext is VoteResultViewModel viewModel && e.Row.DataContext is Nomination nomination)
            {
                viewModel.HandleRowUnloading(nomination);
                e.Row.Tag = null; // 清理标识
            }
        }
    }
}
