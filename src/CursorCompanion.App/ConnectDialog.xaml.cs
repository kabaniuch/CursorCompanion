using System.Windows;

namespace CursorCompanion.App;

public partial class ConnectDialog : Window
{
    public string IpAddress => IpTextBox.Text.Trim();

    public ConnectDialog()
    {
        InitializeComponent();
        IpTextBox.Focus();
        IpTextBox.SelectAll();
    }

    private void OnConnect(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(IpAddress))
            return;

        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
