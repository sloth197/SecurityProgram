using System.Windows;
using System.Windows.Controls;

namespace SecurityProgram.App.Behaviors
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty BoundPasswordProperty = DependencyProperty.RegisterAttached("BoundPassword",
                          typeof(string),
                          typeof(PasswordBoxHelper),
                          new PropertyMetadata(string.Empty, OnBoundPasswordChanged));
        public static string GetBoundPassword(DependencyObject obj)
            => (string)obj.GetValue(BoundPasswordProperty);
        public static void SetBoundPassword(DependencyObject obj, string value)
            => obj.SetValue(BoundPasswordProperty, value);
        private static void OnBoundPasswordChanged(
            DependencyObject d,
            DependencyPropertyChangedArgs e)
        {
            if(d is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                if(!passwordBox.Password.Equals(e.NewVaule))
                    passwordBox.Password = e.NewVAule?.ToString();
                    passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
            }
        }
        private static void PasswordBox _PasswordChagned(object sender, RoutedEventArgs e)
        {
            if(sender is PasswordBox passwordBox)
            {
                SetBoundPAssword(passwrodBox, passwordBox.Password);
            }
        }
    }
}