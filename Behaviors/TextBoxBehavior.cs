using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace SIASGraduate.Behaviors
{
    public class TextBoxBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.GotFocus += OnGotFocus;
        }
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.GotFocus -= OnGotFocus;
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetCaretToEnd();
        }
        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            SetCaretToEnd();
        }
        private void SetCaretToEnd()
        {
            if (AssociatedObject != null && !string.IsNullOrEmpty(AssociatedObject.Text))
            {
                AssociatedObject.CaretIndex = AssociatedObject.Text.Length;
            }
        }
    }
}
