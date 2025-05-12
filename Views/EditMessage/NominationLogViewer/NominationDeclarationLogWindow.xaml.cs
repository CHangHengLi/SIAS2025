using System.Windows;
using _2025毕业设计.ViewModels.EditMessage.NominationLogViewer;

namespace _2025毕业设计.Views.EditMessage.NominationLogViewer
{
    /// <summary>
    /// NominationDeclarationLogWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NominationDeclarationLogWindow : Window
    {
        public NominationDeclarationLogWindow()
        {
            InitializeComponent();
            // 在代码后台设置DataContext
            this.DataContext = new NominationDeclarationLogViewModel();
        }

        /// <summary>
        /// 设置申报ID并加载日志
        /// </summary>
        /// <param name="declarationId">申报ID，如果为-1或null则加载所有日志</param>
        public async void SetDeclarationId(int? declarationId = null)
        {
            if (this.DataContext is NominationDeclarationLogViewModel viewModel)
            {
                viewModel.NominationDeclarationId = declarationId ?? -1;
                await viewModel.LoadLogsAsync();
            }
        }
        
        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 