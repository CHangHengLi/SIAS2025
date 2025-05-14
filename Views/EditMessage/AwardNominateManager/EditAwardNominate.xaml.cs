using System.Windows.Controls;

namespace SIASGraduate.Views.EditMessage.AwardNominateManager
{
    /// <summary>
    /// EditAwardNominate.xaml 的交互逻辑
    /// </summary>
    public partial class EditAwardNominate : UserControl
    {
        public EditAwardNominate()
        {
            InitializeComponent();
        }

        private void ComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.IsDropDownOpen = true;
            }
        }
    }
}
