using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using InternetCafe.Client.Core;

namespace InternetCafe.Client;

public partial class CompactWindow : Window
{
    private readonly MainWindow main;
    public bool AllowClose { get; set; }

    public CompactWindow(MainViewModel viewModel, MainWindow main)
    {
        InitializeComponent();
        this.main = main;
        DataContext = viewModel;
        Left = SystemParameters.WorkArea.Right - Width - 16;
        Top = SystemParameters.WorkArea.Top + 20;
    }

    private void OnDragMove(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Restore(object sender, RoutedEventArgs e) => main.RestoreMain();

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            main.RestoreMain();
        }
    }
}
