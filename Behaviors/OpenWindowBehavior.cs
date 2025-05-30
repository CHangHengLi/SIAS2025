using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace SIASGraduate.Behaviors
{
    public class OpenWindowBehavior : Behavior<Button>
    {
        public Type TargetType { get; set; }
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Click += OnButtonClick;
        }
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Click -= OnButtonClick;
        }
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (TargetType != null)
            {
                Window window = (Window)Activator.CreateInstance(TargetType);
                window.Show();
                // 关闭当前窗口
                Window.GetWindow(AssociatedObject)?.Close();
            }
        }
    }
}
