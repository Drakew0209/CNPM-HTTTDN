using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InternetCafe.Client.Core;

namespace InternetCafe.Client;

/// <summary>
/// Implementation cho Desktop (WPF) - Custom Toast & Dialog
/// </summary>
public sealed class DesktopNotificationService : INotificationService
{
    private Window? _owner;
    private readonly List<ToastItemViewModel> _toasts = new();
    private ToastHostWindow? _toastHost;
    private ConfirmDialogWindow? _confirmDialog;
    private PromptDialogWindow? _promptDialog;
    private readonly object _lock = new();

    public DesktopNotificationService(Window? owner = null)
    {
        _owner = owner;
        if (_owner != null)
        {
            SetOwner(_owner);
        }
    }

    public void SetOwner(Window owner)
    {
        _owner = owner;
        if (owner.IsLoaded)
        {
            EnsureToastHost();
        }
        else
        {
            owner.Loaded += (s, e) => EnsureToastHost();
        }
    }

    private Window? ResolveOwner() => _owner ?? Application.Current?.MainWindow;

    private void EnsureToastHost()
    {
        if (_toastHost is not null && _toastHost.IsLoaded) return;
        var owner = ResolveOwner();
        if (owner == null) return;
        
        try
        {
            _toastHost = new ToastHostWindow(owner)
            {
                ShowInTaskbar = false,
                Topmost = true
            };
            if (owner.IsVisible)
            {
                try { _toastHost.Owner = owner; } catch { }
            }
            _toastHost.Show();
        }
        catch { }
    }

    public async Task ShowToastAsync(Notification notification)
    {
        var owner = ResolveOwner();
        if (owner == null) return;
        await owner.Dispatcher.InvokeAsync(() =>
        {
            EnsureToastHost();
            
            var toastVm = new ToastItemViewModel(notification);
            lock (_lock) _toasts.Add(toastVm);
            _toastHost!.AddToast(toastVm);

            var delay = notification.AutoCloseDelay ?? GetDefaultDelay(notification.Type);
            if (!notification.RequireInteraction)
            {
                StartToastAutoClose(toastVm, delay);
            }
        });
    }

    private void StartToastAutoClose(ToastItemViewModel toastVm, TimeSpan delay)
    {
        var totalMs = delay.TotalMilliseconds;
        var startTime = DateTime.Now;
        
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        
        timer.Tick += (s, e) =>
        {
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            toastVm.Progress = Math.Max(0, 1.0 - elapsed / totalMs);
            
            if (toastVm.Progress <= 0)
            {
                timer.Stop();
                toastVm.IsClosing = true;
                RemoveToast(toastVm.Id);
            }
        };
        timer.Start();
    }

    public Task<ConfirmResult> ShowConfirmAsync(string title, string message, string confirmText = "Xác nhận", string cancelText = "Hủy", NotificationType type = NotificationType.Confirm)
    {
        var tcs = new TaskCompletionSource<ConfirmResult>();
        var owner = ResolveOwner();
        if (owner == null) return Task.FromResult(new ConfirmResult(false));
        
        owner.Dispatcher.Invoke(() =>
        {
            try
            {
                var dialog = new ConfirmDialogWindow(owner, title, message, confirmText, cancelText, type);
                _confirmDialog = dialog;
                dialog.ShowDialog();
                var confirmed = dialog.Result ?? false;
                tcs.TrySetResult(new ConfirmResult(confirmed));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                _confirmDialog = null;
            }
        });
        
        return tcs.Task;
    }

    public Task<ConfirmResult> ShowPromptAsync(string title, string message, string placeholder = "", string confirmText = "OK", string cancelText = "Hủy")
    {
        var tcs = new TaskCompletionSource<ConfirmResult>();
        var owner = ResolveOwner();
        if (owner == null) return Task.FromResult(new ConfirmResult(false));
        
        owner.Dispatcher.Invoke(() =>
        {
            try
            {
                var dialog = new PromptDialogWindow(owner, title, message, placeholder, confirmText, cancelText);
                _promptDialog = dialog;
                dialog.ShowDialog();
                var confirmed = dialog.Result ?? false;
                tcs.TrySetResult(new ConfirmResult(confirmed, dialog.InputText));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                _promptDialog = null;
            }
        });
        
        return tcs.Task;
    }

    public void ClearAll()
    {
        var owner = ResolveOwner();
        if (owner == null) return;
        owner.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                foreach (var toast in _toasts.ToList())
                {
                    toast.IsClosing = true;
                }
                _toasts.Clear();
            }
            _toastHost?.ClearAll();
        });
    }

    private void RemoveToast(string id)
    {
        var owner = ResolveOwner();
        if (owner == null) return;
        owner.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                var toast = _toasts.FirstOrDefault(t => t.Id == id);
                if (toast is not null)
                {
                    toast.IsClosing = true;
                    _toasts.Remove(toast);
                }
            }
            _toastHost?.RemoveToast(id);
        });
    }

    private static TimeSpan GetDefaultDelay(NotificationType type) => type switch
    {
        NotificationType.Error => TimeSpan.FromSeconds(8),
        NotificationType.Warning => TimeSpan.FromSeconds(6),
        NotificationType.Success => TimeSpan.FromSeconds(4),
        NotificationType.Info => TimeSpan.FromSeconds(5),
        _ => TimeSpan.FromSeconds(5)
    };
}

/// <summary>
/// Host window cho các toast - nằm góc trên phải màn hình
/// </summary>
internal sealed class ToastHostWindow : Window
{
    private readonly Window _owner;
    private readonly ItemsControl _itemsControl;

    public ToastHostWindow(Window owner)
    {
        _owner = owner;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Focusable = false;
        IsHitTestVisible = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        Width = 380;
        SizeToContent = SizeToContent.Height;
        MaxHeight = SystemParameters.PrimaryScreenHeight * 0.8;

        _itemsControl = new ItemsControl
        {
            ItemsSource = new List<ToastItemViewModel>(),
            ItemTemplate = CreateToastTemplate(),
            IsHitTestVisible = true
        };

        var border = new Border
        {
            Child = _itemsControl,
            Background = Brushes.Transparent,
            IsHitTestVisible = true
        };

        Content = border;

        Loaded += OnLoaded;
        _owner.LocationChanged += OnOwnerPositionChanged;
        _owner.SizeChanged += OnOwnerPositionChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdatePosition();
    }

    private void OnOwnerPositionChanged(object? sender, EventArgs e)
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (!_owner.IsLoaded) return;
        
        var ownerRect = new Rect(_owner.PointToScreen(new Point(0, 0)), new Size(_owner.ActualWidth, _owner.ActualHeight));
        
        Left = ownerRect.Right - Width - 16;
        Top = ownerRect.Top + 16;
    }

    public void AddToast(ToastItemViewModel toast)
    {
        if (_itemsControl.ItemsSource is not System.Collections.IList list) return;
        list.Insert(0, toast);
    }

    public void RemoveToast(string id)
    {
        if (_itemsControl.ItemsSource is not System.Collections.IList list) return;
        var item = list.Cast<ToastItemViewModel>().FirstOrDefault(t => t.Id == id);
        if (item is not null) list.Remove(item);
    }

    public void ClearAll()
    {
        if (_itemsControl.ItemsSource is System.Collections.IList list) list.Clear();
    }

    private DataTemplate CreateToastTemplate()
    {
        var template = new DataTemplate(typeof(ToastItemViewModel));
        
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
        borderFactory.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 8));
        borderFactory.SetValue(Border.PaddingProperty, new Thickness(16, 14, 16, 14));
        borderFactory.SetValue(Border.EffectProperty, new System.Windows.Media.Effects.DropShadowEffect
        {
            BlurRadius = 20,
            ShadowDepth = 4,
            Opacity = 0.25,
            Color = Colors.Black
        });
        borderFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        
        borderFactory.SetValue(Border.BackgroundProperty, new Binding("TypeClass") 
        { 
            Converter = new ToastBackgroundConverter() 
        });

        // Use StackPanel instead of Grid to avoid ColumnDefinition issues
        var mainStack = new FrameworkElementFactory(typeof(StackPanel));
        mainStack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

        // Header row: Icon + Title + Close button
        var headerStack = new FrameworkElementFactory(typeof(StackPanel));
        headerStack.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

        var iconFactory = new FrameworkElementFactory(typeof(TextBlock));
        iconFactory.SetValue(TextBlock.TextProperty, new Binding("Icon"));
        iconFactory.SetValue(TextBlock.FontSizeProperty, 20.0);
        iconFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top);
        iconFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 12, 0));
        iconFactory.SetValue(TextBlock.ForegroundProperty, new Binding("TypeClass") { Converter = new ToastIconColorConverter() });
        headerStack.AppendChild(iconFactory);

        var contentStack = new FrameworkElementFactory(typeof(StackPanel));
        contentStack.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

        var titleFactory = new FrameworkElementFactory(typeof(TextBlock));
        titleFactory.SetValue(TextBlock.TextProperty, new Binding("Title"));
        titleFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        titleFactory.SetValue(TextBlock.FontSizeProperty, 14.0);
        titleFactory.SetValue(TextBlock.ForegroundProperty, Brushes.White);
        titleFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        contentStack.AppendChild(titleFactory);

        var msgFactory = new FrameworkElementFactory(typeof(TextBlock));
        msgFactory.SetValue(TextBlock.TextProperty, new Binding("Message"));
        msgFactory.SetValue(TextBlock.FontSizeProperty, 13.0);
        msgFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(220, 220, 220)));
        msgFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        msgFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 4, 0, 0));
        contentStack.AppendChild(msgFactory);

        headerStack.AppendChild(contentStack);

        // Close button
        var closeBtnFactory = new FrameworkElementFactory(typeof(Button));
        closeBtnFactory.SetValue(Button.ContentProperty, "✕");
        closeBtnFactory.SetValue(Button.StyleProperty, CreateCloseButtonStyle());
        closeBtnFactory.SetValue(Button.CommandProperty, new RelayCommand<ToastItemViewModel>(t => 
        {
            if (t is not null) 
            {
                var host = FindToastHost(t);
                host?.RemoveToast(t.Id);
            }
        }));
        closeBtnFactory.SetValue(Button.CommandParameterProperty, new Binding("."));
        closeBtnFactory.SetValue(Button.VisibilityProperty, new Binding("RequireInteraction") { Converter = new BoolToVisibilityConverter(true) });
        closeBtnFactory.SetValue(Button.VerticalAlignmentProperty, VerticalAlignment.Top);
        closeBtnFactory.SetValue(Button.MarginProperty, new Thickness(12, 0, 0, 0));
        headerStack.AppendChild(closeBtnFactory);

        mainStack.AppendChild(headerStack);

        // Progress bar
        var progressFactory = new FrameworkElementFactory(typeof(ProgressBar));
        progressFactory.SetValue(ProgressBar.ValueProperty, new Binding("Progress") { Converter = new DoubleToPercentConverter() });
        progressFactory.SetValue(ProgressBar.MaximumProperty, 100.0);
        progressFactory.SetValue(ProgressBar.HeightProperty, 3.0);
        progressFactory.SetValue(ProgressBar.MarginProperty, new Thickness(0, 8, 0, 0));
        progressFactory.SetValue(ProgressBar.ForegroundProperty, new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)));
        progressFactory.SetValue(ProgressBar.BackgroundProperty, new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)));
        progressFactory.SetValue(ProgressBar.BorderThicknessProperty, new Thickness(0));
        progressFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        mainStack.AppendChild(progressFactory);

        borderFactory.AppendChild(mainStack);

        // Animation trigger
        var storyboard = new Storyboard();
        var slideIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        var translateX = new TranslateTransform();
        borderFactory.SetValue(UIElement.RenderTransformProperty, translateX);
        borderFactory.SetValue(UIElement.RenderTransformOriginProperty, new Point(1, 0));
        Storyboard.SetTargetProperty(slideIn, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        storyboard.Children.Add(slideIn);
        
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
        storyboard.Children.Add(fadeIn);

        borderFactory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler((s, e) =>
        {
            if (s is FrameworkElement fe) fe.BeginStoryboard(storyboard);
        }));

        template.VisualTree = borderFactory;
        return template;
    }

    private Style CreateCloseButtonStyle()
    {
        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(180, 180, 180))));
        style.Setters.Add(new Setter(Button.FontSizeProperty, 14.0));
        style.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(8, 4, 8, 4)));
        style.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
        style.Setters.Add(new Setter(Button.VerticalAlignmentProperty, VerticalAlignment.Top));

        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
        
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(content);
        template.VisualTree = border;

        var triggerHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        triggerHover.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromArgb(60, 255, 255, 255))));
        triggerHover.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
        template.Triggers.Add(triggerHover);

        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }

    private ToastHostWindow? FindToastHost(ToastItemViewModel toast)
    {
        return Application.Current.Windows.OfType<ToastHostWindow>().FirstOrDefault();
    }
}

/// <summary>
/// Dialog xác nhận (Yes/No) custom
/// </summary>
internal sealed class ConfirmDialogWindow : Window
{
    public bool? Result { get; private set; }

    public ConfirmDialogWindow(Window owner, string title, string message, string confirmText, string cancelText, NotificationType type)
    {
        try { if (owner.IsVisible) Owner = owner; } catch { }
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        MaxWidth = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var mainBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(22, 28, 42)),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(24),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 30,
                ShadowDepth = 8,
                Opacity = 0.4,
                Color = Colors.Black
            },
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 70, 95)),
            BorderThickness = new Thickness(1)
        };

        var stack = new StackPanel();
        
        var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        
        var iconText = type switch
        {
            NotificationType.Warning => "⚠",
            NotificationType.Error => "✕",
            NotificationType.Success => "✓",
            _ => "?"
        };
        var iconColor = type switch
        {
            NotificationType.Warning => Color.FromRgb(250, 204, 21),
            NotificationType.Error => Color.FromRgb(239, 68, 68),
            NotificationType.Success => Color.FromRgb(34, 197, 94),
            _ => Color.FromRgb(59, 130, 246)
        };
        
        var icon = new TextBlock
        {
            Text = iconText,
            FontSize = 28,
            Foreground = new SolidColorBrush(iconColor),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(icon, 0);
        
        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(titleBlock, 1);
        
        headerGrid.Children.Add(icon);
        headerGrid.Children.Add(titleBlock);
        stack.Children.Add(headerGrid);

        var msgBlock = new TextBlock
        {
            Text = message,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 205, 215)),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 22,
            Margin = new Thickness(40, 0, 0, 24)
        };
        stack.Children.Add(msgBlock);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        
        var cancelBtn = new Button
        {
            Content = cancelText,
            Padding = new Thickness(20, 10, 20, 10),
            Margin = new Thickness(0, 0, 10, 0),
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 90, 110)),
            BorderThickness = new Thickness(1),
            Foreground = Brushes.White,
            Cursor = Cursors.Hand,
            MinWidth = 80
        };
        cancelBtn.Click += (s, e) => { Result = false; Close(); };
        
        var confirmBtn = new Button
        {
            Content = confirmText,
            Padding = new Thickness(20, 10, 20, 10),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Cursor = Cursors.Hand,
            MinWidth = 90
        };
        
        var confirmColor = type switch
        {
            NotificationType.Error => Color.FromRgb(239, 68, 68),
            NotificationType.Warning => Color.FromRgb(250, 204, 21),
            NotificationType.Success => Color.FromRgb(34, 197, 94),
            _ => Color.FromRgb(59, 130, 246)
        };
        
        confirmBtn.Background = new SolidColorBrush(confirmColor);
        confirmBtn.BorderThickness = new Thickness(0);
        confirmBtn.Foreground = Brushes.White;
        confirmBtn.Click += (s, e) => { Result = true; Close(); };

        btnPanel.Children.Add(cancelBtn);
        btnPanel.Children.Add(confirmBtn);
        stack.Children.Add(btnPanel);

        mainBorder.Child = stack;
        Content = mainBorder;

        Loaded += (s, e) =>
        {
            Opacity = 0;
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            BeginAnimation(OpacityProperty, anim);
        };

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) { Result = false; Close(); }
            if (e.Key == Key.Enter) { Result = true; Close(); }
        };
    }
}

/// <summary>
/// Dialog nhập liệu (Prompt) custom
/// </summary>
internal sealed class PromptDialogWindow : Window
{
    public bool? Result { get; private set; }
    public string? InputText { get; private set; }

    public PromptDialogWindow(Window owner, string title, string message, string placeholder, string confirmText, string cancelText)
    {
        try { if (owner.IsVisible) Owner = owner; } catch { }
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        MaxWidth = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var mainBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(22, 28, 42)),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(24),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 30,
                ShadowDepth = 8,
                Opacity = 0.4,
                Color = Colors.Black
            },
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 70, 95)),
            BorderThickness = new Thickness(1)
        };

        var stack = new StackPanel();
        
        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        stack.Children.Add(titleBlock);

        var msgBlock = new TextBlock
        {
            Text = message,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 205, 215)),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 22,
            Margin = new Thickness(0, 0, 0, 16)
        };
        stack.Children.Add(msgBlock);

        var inputBox = new TextBox
        {
            Padding = new Thickness(12, 10, 12, 10),
            FontSize = 14,
            Background = new SolidColorBrush(Color.FromRgb(15, 20, 32)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 70, 95)),
            BorderThickness = new Thickness(1),
            Foreground = Brushes.White,
            CaretBrush = Brushes.White,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        if (!string.IsNullOrEmpty(placeholder))
        {
            inputBox.Tag = placeholder;
            inputBox.GotFocus += (s, e) => { if (inputBox.Text == placeholder) inputBox.Text = ""; };
            inputBox.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(inputBox.Text)) inputBox.Text = placeholder; };
            inputBox.Text = placeholder;
            inputBox.Foreground = new SolidColorBrush(Color.FromRgb(120, 130, 150));
        }
        stack.Children.Add(inputBox);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        
        var cancelBtn = new Button
        {
            Content = cancelText,
            Padding = new Thickness(20, 10, 20, 10),
            Margin = new Thickness(0, 0, 10, 0),
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 90, 110)),
            BorderThickness = new Thickness(1),
            Foreground = Brushes.White,
            Cursor = Cursors.Hand,
            MinWidth = 80
        };
        cancelBtn.Click += (s, e) => { Result = false; Close(); };
        
        var confirmBtn = new Button
        {
            Content = confirmText,
            Padding = new Thickness(20, 10, 20, 10),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
            BorderThickness = new Thickness(0),
            Foreground = Brushes.White,
            Cursor = Cursors.Hand,
            MinWidth = 90
        };
        confirmBtn.Click += (s, e) => 
        { 
            Result = true; 
            InputText = inputBox.Text == placeholder ? "" : inputBox.Text; 
            Close(); 
        };

        btnPanel.Children.Add(cancelBtn);
        btnPanel.Children.Add(confirmBtn);
        stack.Children.Add(btnPanel);

        mainBorder.Child = stack;
        Content = mainBorder;

        Loaded += (s, e) =>
        {
            Opacity = 0;
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            BeginAnimation(OpacityProperty, anim);
            inputBox.Focus();
            inputBox.SelectAll();
        };

        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) { Result = false; Close(); }
            if (e.Key == Key.Enter) { Result = true; InputText = inputBox.Text == placeholder ? "" : inputBox.Text; Close(); }
        };
    }
}

// Converters
internal sealed class ToastBackgroundConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var type = value?.ToString()?.ToLowerInvariant();
        return type switch
        {
            "success" => new SolidColorBrush(Color.FromRgb(20, 80, 40)),
            "warning" => new SolidColorBrush(Color.FromRgb(85, 65, 10)),
            "error" => new SolidColorBrush(Color.FromRgb(85, 25, 25)),
            "info" => new SolidColorBrush(Color.FromRgb(15, 45, 75)),
            _ => new SolidColorBrush(Color.FromRgb(30, 35, 50))
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}

internal sealed class ToastIconColorConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var type = value?.ToString()?.ToLowerInvariant();
        return type switch
        {
            "success" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            "warning" => new SolidColorBrush(Color.FromRgb(250, 204, 21)),
            "error" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            "info" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
            _ => new SolidColorBrush(Color.FromRgb(150, 155, 170))
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}

internal sealed class BoolToVisibilityConverter : System.Windows.Data.IValueConverter
{
    private readonly bool _invert;
    public BoolToVisibilityConverter(bool invert = false) => _invert = invert;
    
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var b = value is bool vb && vb;
        if (_invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}

internal sealed class DoubleToPercentConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is double d) return d * 100;
        return 100;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
}