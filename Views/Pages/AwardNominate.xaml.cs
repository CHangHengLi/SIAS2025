using _2025毕业设计.Models;
using _2025毕业设计.ViewModels.Pages;
using System.Windows.Controls;

namespace _2025毕业设计.Views.Pages
{
    /// <summary>
    /// AwardNominate.xaml 的交互逻辑
    /// </summary>
    public partial class AwardNominate : UserControl
    {
        public AwardNominate()
        {
            InitializeComponent();
        }

        private void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is Nomination nomination)
            {
                var viewModel = DataContext as AwardNominateViewModel;
                if (viewModel != null && viewModel.ViewImageCommand.CanExecute(nomination))
                {
                    // 标记事件已处理，防止冒泡导致重复触发
                    e.Handled = true;
                    viewModel.ViewImageCommand.Execute(nomination);
                }
            }
        }
    }
}
