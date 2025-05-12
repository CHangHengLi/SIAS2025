using System.Windows.Controls;
using System.Windows.Input;

namespace _2025毕业设计.Views.Pages
{
    /// <summary>
    /// NominationDeclaration.xaml 的交互逻辑
    /// </summary>
    public partial class NominationDeclaration : UserControl
    {
        public NominationDeclaration()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ListView项双击事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is Models.NominationDeclaration declaration)
            {
                var viewModel = DataContext as ViewModels.Pages.NominationDeclarationViewModel;
                viewModel?.ViewImageCommand.Execute(declaration);
            }
        }
    }
}
