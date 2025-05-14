using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace SIASGraduate.Behaviors
{
    public class PasswordBoxBindingBehavior : Behavior<PasswordBox>
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(nameof(Password), typeof(string), typeof(PasswordBoxBindingBehavior), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordPropertyChanged));
        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PasswordChanged += OnPasswordChanged;
        }
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PasswordChanged -= OnPasswordChanged;
        }
        private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBoxBindingBehavior behavior && behavior.AssociatedObject != null)
            {
                // 如果新密码与当前密码不同，则更新 PasswordBox 的 Password 属性
                if (behavior.AssociatedObject.Password != (string)e.NewValue)
                {
                    behavior.AssociatedObject.Password = (string)e.NewValue;
                }
            }
        }
        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject != null)
            {
                //  如果 PasswordBox 的 Password 属性与 Password 属性不同，则更新 Password 属性
                if (Password != AssociatedObject.Password)
                {
                    Password = AssociatedObject.Password;
                }
            }

        }
    }
}