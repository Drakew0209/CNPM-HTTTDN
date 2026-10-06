using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace InternetCafe.Client
{
    public partial class MainWindow : Window
    {
        private KeyboardHook _keyboardHook;
        private bool _isLocked = true;
        private HubConnection _hubConnection;
        private HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5200") };
        private string _token;

        public MainWindow()
        {
            InitializeComponent();
            LockStation();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = "Đang đăng nhập...";
            btnLogin.IsEnabled = false;

            try
            {
                var loginData = new { Identifier = txtUsername.Text, Password = txtPassword.Password };
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    _token = result.GetProperty("accessToken").GetString();
                    
                    await SetupSignalR();
                    UnlockStation();
                }
                else
                {
                    txtError.Text = "Sai tên đăng nhập hoặc mật khẩu!";
                }
            }
            catch (Exception ex)
            {
                txtError.Text = "Lỗi kết nối máy chủ!";
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }

        private async Task SetupSignalR()
        {
            if (_hubConnection != null)
                await _hubConnection.DisposeAsync();

            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5200/cafeHub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_token);
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<int>("LockComputer", (computerId) =>
            {
                Dispatcher.Invoke(() => LockStation());
            });

            _hubConnection.On<string>("ForceLockClient", (message) =>
            {
                Dispatcher.Invoke(() => LockStation());
            });

            await _hubConnection.StartAsync();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Optionally tell the server to end session here
            LockStation();
        }

        private void UnlockStation()
        {
            _isLocked = false;
            _keyboardHook?.Dispose();
            this.Topmost = false;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.WindowState = WindowState.Normal;
            
            WorkspaceGrid.Visibility = Visibility.Visible;
            txtSessionInfo.Text = $"Xin chào {txtUsername.Text}! Phiên chơi đã bắt đầu.";
        }

        private void LockStation()
        {
            _isLocked = true;
            if (_keyboardHook == null)
                _keyboardHook = new KeyboardHook();
            
            this.WindowStyle = WindowStyle.None;
            this.WindowState = WindowState.Maximized;
            this.Topmost = true;
            
            txtUsername.Text = "";
            txtPassword.Password = "";
            txtError.Text = "";
            
            WorkspaceGrid.Visibility = Visibility.Collapsed;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isLocked)
            {
                e.Cancel = true;
            }
            else
            {
                _keyboardHook?.Dispose();
            }
        }
    }
}