using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SecurityProgram.App.Behaviors;

public static class PasswordBoxHelper
{
    public static readonly DependencyProperty BoundPasswordProperty = DependencyProperty.RegisterAttached(
        "BoundPassword",
        typeof(string),
        typeof(PasswordBoxHelper),
        new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

    private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
        "IsUpdating",
        typeof(bool),
        typeof(PasswordBoxHelper),
        new PropertyMetadata(false));

    public static string GetBoundPassword(DependencyObject obj) =>
        (string)obj.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject obj, string value) =>
        obj.SetValue(BoundPasswordProperty, value);

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
        {
            return;
        }

        passwordBox.PasswordChanged -= PasswordBoxPasswordChanged;

        if (!(bool)passwordBox.GetValue(IsUpdatingProperty))
        {
            passwordBox.Password = e.NewValue?.ToString() ?? string.Empty;
        }

        passwordBox.PasswordChanged += PasswordBoxPasswordChanged;
    }

    private static void PasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox passwordBox)
        {
            return;
        }

        passwordBox.SetValue(IsUpdatingProperty, true);
        SetBoundPassword(passwordBox, passwordBox.Password);
        BindingOperations.GetBindingExpression(passwordBox, BoundPasswordProperty)?.UpdateSource();
        passwordBox.SetValue(IsUpdatingProperty, false);
    }
}
