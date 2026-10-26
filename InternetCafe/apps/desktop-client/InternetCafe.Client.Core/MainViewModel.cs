using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InternetCafe.Client.Core;

public sealed partial class CartItem(Product product) : ObservableObject
{
    public string ProductId { get; } = product.Id;
    public string Name { get; } = product.Name;
    public decimal UnitPrice { get; } = product.Price;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TotalLabel))] private int quantity = 1;
    public string TotalLabel => Format.Money(UnitPrice * Quantity);
    public string UnitPriceLabel => Format.Money(UnitPrice);
}

public sealed partial class AnswerViewModel(SurveyQuestion question) : ObservableObject
{
    public string Id { get; } = question.Id;
    public string Text { get; } = question.Text;
    public string[] Options { get; } = question.Options;
    [ObservableProperty] private string? selectedAnswer;
}

/// <summary>Display-only estimate: no balance mutation or local billing.</summary>
public sealed record SessionEstimate(TimeSpan Elapsed, TimeSpan? Remaining, decimal EstimatedRental)
{
    public static SessionEstimate Calculate(decimal balance, DateTimeOffset? start, decimal hourlyRate, DateTimeOffset now)
    {
        var elapsed = start is { } value ? TimeSpan.FromSeconds(Math.Max(0, (now - value).TotalSeconds)) : TimeSpan.Zero;
        var rental = decimal.Ceiling((decimal)elapsed.TotalSeconds * Math.Max(0, hourlyRate) / 3600m);
        var remaining = hourlyRate > 0 ? TimeSpan.FromSeconds((double)(Math.Max(0, balance - rental) / hourlyRate * 3600m)) : (TimeSpan?)null;
        return new(elapsed, remaining, rental);
    }
    public static string Duration(TimeSpan value) => $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}";
}

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ICafeApi api;
    private readonly IRealtimeUpdates realtime;
    private readonly INotificationService notifications;
    private readonly TimeProvider clock;
    private Workspace snapshot = new();
    private DateTimeOffset? receivedServerTime;
    private DateTimeOffset receivedLocalTime;
    private bool syncing;
    private bool isAutoEnding;
    private string? userId;
    private bool profileLoaded;
    private readonly CancellationTokenSource lifetime = new();
    public async Task ShutdownAsync() { lifetime.Cancel(); await realtime.StopAsync(); api.AccessToken = null; notifications.ClearAll(); }
    public MainViewModel(ICafeApi api, IRealtimeUpdates realtime, INotificationService notifications, string machineId = "pc-001", TimeProvider? clock = null)
    {
        this.api = api; this.realtime = realtime; this.notifications = notifications; this.clock = clock ?? TimeProvider.System; this.machineId = machineId;
        var display = machineId.Trim().ToUpperInvariant();
        if (!display.StartsWith("PC")) display = "PC" + display;
        MachineName = display;
        HourlyRateLabel = display is "PC02" or "PC05" or "PC06" ? "15.000 ₫/giờ" : "8.000 ₫/giờ";
        Games.Clear();
        foreach (var g in GetDefaultGames()) Games.Add(g);
        UpdateGameCategories();
        FilterGames();
        _ = CheckConnectivityAsync();
    }
    public Func<string, Task<bool>> ConfirmAsync { get; set; } = _ => Task.FromResult(false);
    public Action? OnGameLaunched { get; set; }
    public Action<string>? OnPlaySound { get; set; }
    public Action<bool>? OnSoundMuteChanged { get; set; }
    public Action<string>? OnCopyToClipboard { get; set; }
    public Action<string>? OnRemoteCommand { get; set; }
    [ObservableProperty] private string machineId = "pc-001";
    public ICafeApi Api => api;
    public IRealtimeUpdates Realtime => realtime;
    partial void OnMachineIdChanged(string value)
    {
        var display = value.Trim().ToUpperInvariant();
        if (!display.StartsWith("PC")) display = "PC" + display;
        MachineName = display;
        if (Machine is { } machine)
        {
            HourlyRateLabel = Format.Money(machine.HourlyRate) + "/giờ";
        }
        else
        {
            HourlyRateLabel = display is "PC02" or "PC05" or "PC06" ? "15.000 ₫/giờ" : "8.000 ₫/giờ";
        }
        OnPropertyChanged(nameof(CanOrder));
        if (IsConnected)
        {
            _ = PollAsync();
        }
    }
    public async Task UpdateServerEndpointAsync(string? newBaseUrl)
    {
        if (api is SmartCafeApi smartApi)
        {
            smartApi.CustomBaseUrl = newBaseUrl;
            smartApi.ResetMode();
        }
        else if (api is CafeApi cafeApi)
        {
            cafeApi.CustomBaseUrl = newBaseUrl;
        }

        if (realtime is SignalRUpdates signalR && !string.IsNullOrWhiteSpace(newBaseUrl))
        {
            var clean = newBaseUrl.TrimEnd('/');
            if (clean.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase)) clean = clean[..^7];
            signalR.Url = $"{clean}/hubs/internetcafe";
            if (IsAuthenticated)
            {
                try { await realtime.StartAsync(() => Task.FromResult<string?>(api.AccessToken)); } catch { }
            }
        }
        await CheckConnectivityAsync();
    }
    private decimal previousBalance;
    [ObservableProperty] private string vipBadge = "💎 VIP BẠCH KIM";
    [ObservableProperty] private int currentExp = 3850;
    [ObservableProperty] private int targetExp = 5000;
    [ObservableProperty] private double expPercentage = 77.0;
    [ObservableProperty] private int dailyStreak = 3;
    [ObservableProperty] private int luckySpins = 1;
    [ObservableProperty] private string luckyRewardMessage = "";
    [ObservableProperty] private bool isLuckyWheelOpen;
    [ObservableProperty] private double wheelAngle;
    [ObservableProperty] private bool isSpinning;
    [ObservableProperty] private bool hasWonPrize;
    [ObservableProperty] private string wonPrizeName = "";
    [ObservableProperty] private string wonPrizeIcon = "Gift";
    public Func<double, Task>? OnSpinWheelAnimation { get; set; }
    [ObservableProperty] private bool isSoundMuted;
    [ObservableProperty] private bool isAuthenticated;
    [ObservableProperty] private bool isRegistering;
    [ObservableProperty] private bool isLoginModalOpen;
    [ObservableProperty] private int currentBannerIndex;
    [ObservableProperty] private string currentBannerImage = "banner_cybercafe.jpg";
    [ObservableProperty] private string digitalClock = "12:00:00";
    [ObservableProperty] private string digitalDate = "Thứ Hai, 20/10/2026";
    private readonly string[] bannerImages = ["banner_cybercafe.jpg", "banner_lol.jpg", "banner_valorant.jpg"];
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isConnected;
    [ObservableProperty] private bool isAppLocked;
    [ObservableProperty] private bool isTemporaryLocked;
    [ObservableProperty] private bool lowBalance;
    [ObservableProperty] private string username = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string passwordConfirmation = "";
    [ObservableProperty] private string fullName = "";
    [ObservableProperty] private string phone = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string dateOfBirth = "2000-01-01";
    [ObservableProperty] private string hobbies = "";
    [ObservableProperty] private string currentPassword = "";
    [ObservableProperty] private string newPassword = "";
    [ObservableProperty] private string newPasswordConfirmation = "";
    [ObservableProperty] private string connectionStatus = "Đang kiểm tra kết nối máy chủ...";
    [ObservableProperty] private string accountName = "Khách hàng";
    [ObservableProperty] private string tier = "";
    [ObservableProperty] private decimal balance;
    [ObservableProperty] private decimal realtimeBalance;
    [ObservableProperty] private string machineName = "PC-01";
    [ObservableProperty] private string hourlyRateLabel = "10.000 ₫/giờ";
    [ObservableProperty] private string elapsedLabel = "00:00:00";
    [ObservableProperty] private string remainingLabel = "—";
    [ObservableProperty] private string sessionStatus = "Chưa bắt đầu phiên";
    [ObservableProperty] private string sessionSummary = "";
    [ObservableProperty] private string lastSyncLabel = "Khởi tạo...";
    [ObservableProperty] private string menuSearch = "";
    [ObservableProperty] private Category? selectedCategory;
    [ObservableProperty] private Order? selectedOrder;
    [ObservableProperty] private Survey? selectedSurvey;
    [ObservableProperty] private string orderNote = "";
    [ObservableProperty] private string topupAmount = "50000";
    [ObservableProperty] private string topupNote = "";
    [ObservableProperty] private string paymentMethod = "Cash";
    public bool IsCashMethod => PaymentMethod == "Cash";
    public bool IsTransferMethod => PaymentMethod == "Transfer";
    public string BankName => "MB Bank (Ngân hàng Quân Đội)";
    public string BankAccount => "0987654321";
    public string BankAccountName => "CYBERCAFE GAMING";
    public string TransferSyntax => $"NAP {Username}";
    public string VietQrUrl
    {
        get
        {
            if (!int.TryParse(TopupAmount, out var amt) || amt <= 0) amt = 50000;
            var syntax = Uri.EscapeDataString($"NAP {Username}");
            return $"https://api.vietqr.io/image/970422-0987654321-compact2.jpg?amount={amt}&addInfo={syntax}&accountName=CYBERCAFE%20GAMING";
        }
    }

    partial void OnPaymentMethodChanged(string value)
    {
        OnPropertyChanged(nameof(IsCashMethod));
        OnPropertyChanged(nameof(IsTransferMethod));
    }
    partial void OnTopupAmountChanged(string value)
    {
        OnPropertyChanged(nameof(VietQrUrl));
    }
    partial void OnUsernameChanged(string value)
    {
        OnPropertyChanged(nameof(TransferSyntax));
        OnPropertyChanged(nameof(VietQrUrl));
    }

    [RelayCommand] private void SetCashPayment() => PaymentMethod = "Cash";
    [RelayCommand] private void SetTransferPayment() => PaymentMethod = "Transfer";
    [ObservableProperty] private string temporaryLockPassword = "";
    [ObservableProperty] private string feedbackSubject = "";
    [ObservableProperty] private string feedbackContent = "";
    [ObservableProperty] private int selectedTab;
    [ObservableProperty] private string gameSearch = "";
    [ObservableProperty] private string selectedGameCategory = "Tất cả";
    public ObservableCollection<string> GameCategories { get; } = [];
    public ObservableCollection<GameItem> FilteredGames { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<GameItem> Games { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<CartItem> Cart { get; } = [];
    public ObservableCollection<Order> Orders { get; } = [];
    public ObservableCollection<Topup> Topups { get; } = [];
    public ObservableCollection<Combo> Combos { get; } = [];
    public ObservableCollection<Transaction> Transactions { get; } = [];
    public ObservableCollection<Session> Sessions { get; } = [];
    public ObservableCollection<Feedback> FeedbackItems { get; } = [];
    public ObservableCollection<Survey> Surveys { get; } = [];
    public ObservableCollection<AnswerViewModel> Answers { get; } = [];
    public string BalanceLabel => Format.Money(ActiveSession is null ? Balance : RealtimeBalance);
    public string CartTotalLabel => Format.Money(Cart.Sum(x => x.Quantity * x.UnitPrice));
    public int CartCount => Cart.Sum(x => x.Quantity);
    public string CartCountLabel => $"{CartCount} món";
    public bool HasActiveSession => ActiveSession is not null;
    public bool CanStartSession => IsAuthenticated && IsConnected && !IsBusy && !HasActiveSession && Balance > 0 && Machine is { Online: true, Status: "Available" };
    public bool CanEndSession => IsAuthenticated && IsConnected && !IsBusy && HasActiveSession;
    public bool CanMutate => IsAuthenticated && IsConnected && !IsBusy;
    public bool CanOrder => CanMutate && ActiveSession is not null && IsMatchingMachine(ActiveSession.ComputerId) && Cart.Count > 0;
    public bool CanCancelOrder => CanMutate && SelectedOrder?.Status == "Pending";
    public bool CanAnswerSurvey => CanMutate && SelectedSurvey is { Status: "Published" } survey && survey.Responses.All(x => x.CustomerId != userId);
    public string SurveyState => SelectedSurvey is null ? "Chọn khảo sát dành cho bạn." : SelectedSurvey.Responses.Any(x => x.CustomerId == userId) ? "Bạn đã gửi câu trả lời. Cảm ơn bạn!" : SelectedSurvey.Status == "Closed" ? "Khảo sát đã đóng." : "Chọn một câu trả lời cho mỗi câu hỏi.";
    private Session? ActiveSession => snapshot.Sessions.FirstOrDefault(x => x.Status == "Active" && x.CustomerId == userId);
    private Computer? Machine => snapshot.Computers.FirstOrDefault(x => IsMatchingMachine(x.Id));
    partial void OnIsBusyChanged(bool value) => NotifyState();
    partial void OnIsConnectedChanged(bool value) => NotifyState();
    partial void OnIsAuthenticatedChanged(bool value) => NotifyState();
    partial void OnBalanceChanged(decimal value) { OnPropertyChanged(nameof(BalanceLabel)); NotifyState(); }
    partial void OnRealtimeBalanceChanged(decimal value) => OnPropertyChanged(nameof(BalanceLabel));
    partial void OnMenuSearchChanged(string value) => FilterProducts();
    partial void OnSelectedCategoryChanged(Category? value) => FilterProducts();
    partial void OnGameSearchChanged(string value) => FilterGames();
    partial void OnSelectedGameCategoryChanged(string value) => FilterGames();
    [RelayCommand] private void SelectGameCategory(string category) => SelectedGameCategory = category;
    [RelayCommand] private void SelectCategory(Category? category) => SelectedCategory = category;
    [RelayCommand] private void RemoveCartItem(CartItem? item) { if (item is not null) { Cart.Remove(item); NotifyCart(); } }
    partial void OnSelectedOrderChanged(Order? value) => OnPropertyChanged(nameof(CanCancelOrder));
    partial void OnSelectedSurveyChanged(Survey? value)
    {
        Answers.Clear();
        if (value is not null) foreach (var question in value.Questions)
        {
            var answer = new AnswerViewModel(question);
            if (value.Responses.FirstOrDefault(x => x.CustomerId == userId)?.Answers.TryGetValue(question.Id, out var saved) == true) answer.SelectedAnswer = saved;
            Answers.Add(answer);
        }
        OnPropertyChanged(nameof(CanAnswerSurvey)); OnPropertyChanged(nameof(SurveyState));
    }
    private void NotifyState()
    {
        foreach (var name in new[] { nameof(HasActiveSession), nameof(CanStartSession), nameof(CanEndSession), nameof(CanMutate), nameof(CanOrder), nameof(CanCancelOrder), nameof(CanAnswerSurvey) }) OnPropertyChanged(name);
    }
    [RelayCommand] private void ToggleRegistration() { IsRegistering = !IsRegistering; Password = ""; PasswordConfirmation = ""; }
    [RelayCommand] private async Task LoginAsync()
    {
        if (Validation.Credentials(Username, Password) is { } validation) 
        { 
            await notifications.ShowError("Đăng nhập thất bại", validation); 
            return; 
        }
        await ExecuteAsync(async () =>
        {
            var login = await api.SendAsync<LoginResult>(HttpMethod.Post, "auth/login", new { username = Username.Trim(), password = Password });
            if (login.User.Role != "Customer") throw new ApiException("FORBIDDEN", "Ứng dụng máy trạm chỉ dành cho khách hàng. Nhân viên dùng cổng web.", HttpStatusCode.Forbidden);
            api.AccessToken = login.AccessToken; userId = login.User.Id; IsAuthenticated = true; IsRegistering = false; profileLoaded = false; Password = "";
            await LoadSnapshotAsync();
            if (Balance <= 0)
            {
                await notifications.ShowError("Hết số dư", "Tài khoản của bạn đã hết số dư (0 ₫). Vui lòng nạp tiền tại quầy để mở máy!");
                await ClearIdentityAsync();
                return;
            }
            if (Balance > 0 && ActiveSession is null)
            {
                await api.SendAsync<Session>(HttpMethod.Post, "sessions/start", new { computerId = MachineId });
                await LoadSnapshotAsync();
                await notifications.ShowSuccess("Đăng nhập thành công", "Đã tự động bắt đầu tính giờ chơi!");
            }
            OnPlaySound?.Invoke("login");
            try { await realtime.StartAsync(() => Task.FromResult<string?>(api.AccessToken)); }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or OperationCanceledException) 
            { 
                await notifications.ShowInfo("Kết nối", "Realtime chưa sẵn sàng; ứng dụng vẫn đồng bộ qua HTTP."); 
            }
        });
    }
    [RelayCommand] private async Task RegisterAsync()
    {
        if (Validation.Register(Username, Password, PasswordConfirmation, FullName, Phone, Email, DateOfBirth) is { } validation) { await notifications.ShowError("Đăng ký thất bại", validation); return; }
        if (Hobbies.Length > 500) { await notifications.ShowError("Đăng ký thất bại", "Sở thích tối đa 500 ký tự."); return; }
        await ExecuteAsync(async () =>
        {
            await api.SendAsync<Customer>(HttpMethod.Post, "auth/register", new { username = Username.Trim(), password = Password, fullName = FullName.Trim(), phone = Phone, email = Email, dateOfBirth = DateOfBirth, hobbies = Hobbies });
            IsRegistering = false; Password = ""; PasswordConfirmation = "";
            await notifications.ShowSuccess("Đã tạo tài khoản", "Đăng nhập và nạp tiền tại quầy để bắt đầu.");
        });
    }
    [RelayCommand] private async Task LogoutAsync()
    {
        await ExecuteAsync(async () =>
        {
            if (ActiveSession is { } active)
                await api.SendAsync<Session>(HttpMethod.Post, $"sessions/{Uri.EscapeDataString(active.Id)}/end", new { });
            await api.SendAsync<JsonElement>(HttpMethod.Post, "auth/logout", new { });
            await ClearIdentityAsync();
            await notifications.ShowInfo("Đã đăng xuất", "Phiên đăng nhập đã kết thúc.");
        });
    }
    private async Task ClearIdentityAsync()
    {
        api.AccessToken = null; userId = null; IsAuthenticated = false; IsLoginModalOpen = false; IsAppLocked = false; IsTemporaryLocked = false; TemporaryLockPassword = ""; profileLoaded = false; snapshot = new(); Balance = 0;
        Cart.Clear(); Products.Clear(); Categories.Clear(); Orders.Clear(); Topups.Clear(); Combos.Clear(); Transactions.Clear(); Sessions.Clear(); FeedbackItems.Clear(); Surveys.Clear(); Answers.Clear(); SelectedSurvey = null; SelectedOrder = null;
        Password = ""; CurrentPassword = ""; NewPassword = ""; NewPasswordConfirmation = "";
        await realtime.StopAsync(); NotifyState(); NotifyCart();
        _ = CheckConnectivityAsync();
    }
    private string GetServerDisplayUrl()
    {
        if (api is SmartCafeApi smartApi && !string.IsNullOrWhiteSpace(smartApi.CustomBaseUrl))
            return smartApi.CustomBaseUrl.TrimEnd('/');
        if (api is CafeApi cafeApi && !string.IsNullOrWhiteSpace(cafeApi.CustomBaseUrl))
            return cafeApi.CustomBaseUrl.TrimEnd('/');
        return "http://127.0.0.1:5055";
    }
    public async Task CheckConnectivityAsync()
    {
        try
        {
            var health = await api.GetAsync<JsonElement>("health");
            IsConnected = true;
            if (api is SmartCafeApi { IsStandalone: true })
            {
                ConnectionStatus = "🟢 Máy chủ Ngoại tuyến Standalone Demo · Sẵn sàng mở máy";
                LastSyncLabel = "Hệ thống sẵn sàng";
            }
            else
            {
                var url = GetServerDisplayUrl();
                ConnectionStatus = $"🟢 Đã kết nối máy chủ ({url}) · Sẵn sàng mở máy";
                LastSyncLabel = "Đồng bộ: Sẵn sàng";
            }
        }
        catch (Exception)
        {
            IsConnected = false;
            var url = GetServerDisplayUrl();
            ConnectionStatus = $"🔴 Không thể kết nối máy chủ ({url})";
            LastSyncLabel = "Ngoại tuyến";
        }
    }
    [RelayCommand] private async Task RefreshAsync()
    {
        if (api is SmartCafeApi smartApi) smartApi.ResetMode();
        if (IsAuthenticated) await ExecuteAsync(LoadSnapshotAsync);
        else await CheckConnectivityAsync();
    }
    public async Task PollAsync()
    {
        if (IsBusy || syncing) return;
        if (!IsAuthenticated)
        {
            // BUG-01 fix: do not send heartbeat without a valid token
            return;
        }

        syncing = true;
        try { await LoadSnapshotAsync(); }
        catch (Exception ex) { await HandleErrorAsync(ex); }
        finally { syncing = false; }
    }
    private async Task LoadSnapshotAsync()
    {
        if (!IsAuthenticated) return;
        try
        {
            var hb = await api.SendAsync<JsonElement>(HttpMethod.Post, $"machines/{Uri.EscapeDataString(MachineId)}/heartbeat", new { });
            if (hb.TryGetProperty("command", out var c) && c.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(c.GetString()))
            {
                HandleRemoteCommand(MachineId, c.GetString()!);
            }
        }
        catch { }
        var data = await api.GetAsync<Workspace>("workspace");
        var profile = data.Customers.FirstOrDefault(x => x.Id == userId) ?? throw new ApiException("UNAUTHENTICATED", "Không tìm thấy hồ sơ. Vui lòng đăng nhập lại.", HttpStatusCode.Unauthorized);
        if (profile.Status != "Active") throw new ApiException("ACCOUNT_BANNED", "Tài khoản đã bị khóa. Liên hệ nhân viên.", HttpStatusCode.Forbidden);
        var oldSession = ActiveSession?.Id;
        snapshot = data; receivedServerTime = api.ServerTime; receivedLocalTime = clock.GetUtcNow();
        if (snapshot.Games.Count > 0)
        {
            Replace(Games, snapshot.Games.Where(x => x.Active));
            UpdateGameCategories();
        }
        FilterGames();
        try { var combos = await api.GetAsync<List<Combo>>("combos"); Replace(Combos, combos); } catch { }
        if (previousBalance > 0 && profile.Balance > previousBalance)
        {
            OnPlaySound?.Invoke("topup");
            await notifications.ShowSuccess("Nạp tiền thành công", $"Tài khoản vừa được cộng tiền! Số dư mới: {Format.Money(profile.Balance)}");
        }
        previousBalance = profile.Balance;
        Balance = profile.Balance; AccountName = profile.FullName; Tier = profile.Tier;
        UpdateVipProgress(profile.Balance, profile.Tier);
        if (!profileLoaded) { CopyProfile(profile); profileLoaded = true; }
        var orderId = SelectedOrder?.Id; var surveyId = SelectedSurvey?.Id;
        Replace(Orders, data.Orders.OrderByDescending(x => x.CreatedAt)); SelectedOrder = Orders.FirstOrDefault(x => x.Id == orderId);
        Replace(Topups, data.Topups.OrderByDescending(x => x.CreatedAt)); Replace(Transactions, data.Transactions.OrderByDescending(x => x.CreatedAt));
        Replace(Sessions, data.Sessions.OrderByDescending(x => x.StartTime)); Replace(FeedbackItems, data.Feedback.OrderByDescending(x => x.CreatedAt));
        var draft = Answers.ToDictionary(x => x.Id, x => x.SelectedAnswer);
        Replace(Surveys, data.Surveys); SelectedSurvey = Surveys.FirstOrDefault(x => x.Id == surveyId) ?? Surveys.FirstOrDefault();
        if (SelectedSurvey is { Status: "Published" } selected && selected.Id == surveyId && selected.Responses.All(x => x.CustomerId != userId)) foreach (var answer in Answers) if (draft.TryGetValue(answer.Id, out var value) && value is not null && answer.Options.Contains(value)) answer.SelectedAnswer = value;
        var categoryId = SelectedCategory?.Id;
        Replace(Categories, new[] { new Category("", "Tất cả món") }.Concat(data.Categories)); SelectedCategory = Categories.FirstOrDefault(x => x.Id == categoryId) ?? Categories.FirstOrDefault();
        FilterProducts(); MachineName = Machine?.Name ?? MachineId; HourlyRateLabel = Machine is { } machine ? Format.Money(machine.HourlyRate) + "/giờ" : "Chưa tìm thấy máy";
        IsConnected = true; ConnectionStatus = (api is SmartCafeApi { IsStandalone: true }) ? "Chế độ Ngoại tuyến Standalone Demo · Đầy đủ tính năng 100%" : "Đã kết nối máy chủ · HTTP đồng bộ định kỳ"; LastSyncLabel = "Đồng bộ lúc " + clock.GetLocalNow().ToString("HH:mm:ss");
        if (oldSession is not null && ActiveSession is null) { IsAppLocked = true; SessionSummary = Summary(data.Sessions.FirstOrDefault(x => x.Id == oldSession)); await notifications.ShowInfo("Phiên kết thúc", "Phiên đã được kết thúc từ máy chủ."); }
        var otherSession = data.Sessions.FirstOrDefault(x => x.Status == "Active" && x.CustomerId == userId);
        if (otherSession is not null && !IsMatchingMachine(otherSession.ComputerId))
        {
            IsAppLocked = true;
            await notifications.ShowWarning("Chuyển máy", $"Phiên chơi của bạn đã được chuyển sang máy {otherSession.ComputerId.ToUpperInvariant()}. Vui lòng di chuyển sang máy mới!");
        }
        UpdateClock(); NotifyState();
    }
    public void UpdateClock()
    {
        var localNow = clock.GetLocalNow();
        DigitalClock = localNow.ToString("HH:mm:ss");
        DigitalDate = localNow.ToString("dddd, dd/MM/yyyy", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));

        var now = receivedServerTime is { } server ? server + (clock.GetUtcNow() - receivedLocalTime) : clock.GetUtcNow();
        var active = ActiveSession;
        var rate = active is null ? Machine?.HourlyRate ?? 0 : snapshot.Computers.FirstOrDefault(x => IsMatchingMachine(x.Id))?.HourlyRate ?? 0;
        var estimate = SessionEstimate.Calculate(Balance, active?.StartTime, rate, now);
        var currentCost = active is not null ? estimate.EstimatedRental : 0m;
        RealtimeBalance = Math.Max(0m, Balance - currentCost);
        ElapsedLabel = SessionEstimate.Duration(estimate.Elapsed);
        RemainingLabel = estimate.Remaining is { } remaining ? SessionEstimate.Duration(remaining) : "Chưa có đơn giá";
        LowBalance = IsAuthenticated && (Balance <= 10000 || estimate.Remaining is { TotalMinutes: <= 10 });
        SessionStatus = active is null ? (Balance <= 0 ? "Hết số dư · vui lòng nạp tại quầy" : "Chưa bắt đầu phiên") : !IsMatchingMachine(active.ComputerId) ? "Phiên đang hoạt động trên máy khác" : "Phiên đang sử dụng";
        if (active is not null && !isAutoEnding && IsMatchingMachine(active.ComputerId) && estimate.Remaining is { TotalSeconds: <= 0 })
        {
            isAutoEnding = true;
            _ = AutoEndSessionAsync(active);
        }
    }
    private async Task AutoEndSessionAsync(Session active)
    {
        try
        {
            await api.SendAsync<Session>(HttpMethod.Post, $"sessions/{Uri.EscapeDataString(active.Id)}/end", new { });
            IsAppLocked = true;
            await notifications.ShowWarning("Hết số dư", "Phiên sử dụng đã tự động kết thúc do hết tiền.");
            await LoadSnapshotAsync();
        }
        catch (Exception ex) { await HandleErrorAsync(ex); }
        finally { isAutoEnding = false; }
    }
    [RelayCommand] private async Task StartSessionAsync()
    {
        if (!CanStartSession) { await notifications.ShowError("Không thể bắt đầu", "Cần kết nối, số dư dương và máy sẵn sàng."); return; }
        await MutateAsync(async () => { await api.SendAsync<Session>(HttpMethod.Post, "sessions/start", new { computerId = MachineId }); IsAppLocked = false; SessionSummary = ""; }, "Đã bắt đầu phiên sử dụng.");
    }
    [RelayCommand] private async Task EndSessionAsync()
    {
        if (!CanEndSession || ActiveSession is not { } active) return;
        if (!await ConfirmAsync("Kết thúc phiên và để máy chủ tính phí sử dụng?")) return;
        await MutateAsync(async () => { var result = await api.SendAsync<Session>(HttpMethod.Post, $"sessions/{Uri.EscapeDataString(active.Id)}/end", new { }); SessionSummary = Summary(result); IsAppLocked = true; }, "Máy chủ đã kết thúc và tính phí phiên.");
    }
    private static string Summary(Session? session) => session is null ? "" : $"Phiên {session.Id} · {session.Started} → {session.Ended} · Phí máy chủ: {session.Billed}";
    [RelayCommand] private void UnlockApp() => IsAppLocked = false;
    [RelayCommand] private void LockApp() => IsAppLocked = true;
    [RelayCommand]
    private async Task LaunchGame(GameItem? game)
    {
        if (game is null) return;
        try
        {
            var executable = game.ExecutablePath?.Trim() ?? "";
            var isRiotGame = ContainsRiotIdentity(game.Code) || ContainsRiotIdentity(game.Name) || ContainsRiotIdentity(executable);

            if (isRiotGame && FindRiotClient(executable) is { } riotClient)
            {
                StartGameProcess(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = riotClient,
                    Arguments = "--launch-product=valorant --launch-patchline=live",
                    WorkingDirectory = System.IO.Path.GetDirectoryName(riotClient) ?? "",
                    UseShellExecute = true
                });
                await notifications.ShowInfo("Khởi động game", $"Đang khởi động {game.Name} qua Riot Client...");
                OnGameLaunched?.Invoke();
                return;
            }

            if (System.IO.File.Exists(executable))
            {
                var workDir = System.IO.Path.GetDirectoryName(executable);
                StartGameProcess(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = string.IsNullOrEmpty(workDir) ? "" : workDir,
                    UseShellExecute = true
                });
                await notifications.ShowInfo("Khởi động game", $"Đang khởi động {game.Name}...");
                OnGameLaunched?.Invoke();
                return;
            }

            await SimulateGameLaunch(game);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            await SimulateGameLaunch(game);
        }
    }

    private static bool ContainsRiotIdentity(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (value.Contains("valorant", StringComparison.OrdinalIgnoreCase) || value.Contains("riot", StringComparison.OrdinalIgnoreCase));

    private static string? FindRiotClient(string executable)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(executable))
        {
            try
            {
                var directory = System.IO.Path.GetDirectoryName(executable);
                if (!string.IsNullOrEmpty(directory))
                    candidates.Add(System.IO.Path.GetFullPath(System.IO.Path.Combine(directory, @"..\..\..\Riot Client\RiotClientServices.exe")));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or System.IO.PathTooLongException or System.Security.SecurityException) { }
        }

        // BUG-03 fix: scan all fixed drives, not just C/D/E
        foreach (var drive in System.IO.DriveInfo.GetDrives().Where(d => d.DriveType == System.IO.DriveType.Fixed && d.IsReady))
            candidates.Add(System.IO.Path.Combine(drive.RootDirectory.FullName, "Riot Games", "Riot Client", "RiotClientServices.exe"));

        return candidates.FirstOrDefault(System.IO.File.Exists);
    }

    private static void StartGameProcess(System.Diagnostics.ProcessStartInfo startInfo)
    {
        try
        {
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (System.ComponentModel.Win32Exception exception) when (exception.NativeErrorCode == 5)
        {
            startInfo.Verb = "runas";
            startInfo.UseShellExecute = true;
            System.Diagnostics.Process.Start(startInfo);
        }
    }

    private async Task SimulateGameLaunch(GameItem game)
    {
        await notifications.ShowInfo("Mô phỏng game", $"[Mô phỏng] Đang khởi động {game.Name} trên máy trạm {MachineName}! Ứng dụng đã tự động thu nhỏ về Widget theo dõi giờ chơi.");
        OnGameLaunched?.Invoke();
    }
    [RelayCommand] private async Task SelectCombo(Combo? combo)
    {
        if (combo is null) return;
        TopupAmount = combo.Price.ToString("0");
        TopupNote = $"Gói {combo.Name} ({combo.Code})";
        await notifications.ShowSuccess("Đã chọn combo", $"Đã chọn gói {combo.Name}.");
        OnPropertyChanged(nameof(VietQrUrl));
    }
    [RelayCommand] private void LockTemporary()
    {
        if (!HasActiveSession) return;
        IsTemporaryLocked = true;
        TemporaryLockPassword = "";
    }
    [RelayCommand] private async Task UnlockTemporary()
    {
        if (!string.IsNullOrWhiteSpace(TemporaryLockPassword))
        {
            IsTemporaryLocked = false;
            TemporaryLockPassword = "";
            await notifications.ShowSuccess("Mở khóa", "Đã mở khóa màn hình.");
        }
        else await notifications.ShowError("Mở khóa thất bại", "Nhập mật khẩu để mở khóa.");
    }
    [RelayCommand] private async Task AddToCart(Product? product)
    {
        if (product is null || product.Stock <= 0) { await notifications.ShowError("Thêm món thất bại", "Món đã hết hàng."); return; }
        var item = Cart.FirstOrDefault(x => x.ProductId == product.Id);
        if (item is null) { if (Cart.Count >= 30) { await notifications.ShowError("Thêm món thất bại", "Mỗi đơn tối đa 30 món."); return; } Cart.Add(new CartItem(product)); }
        else if (item.Quantity < Math.Min(99, product.Stock)) item.Quantity++;
        else { await notifications.ShowError("Thêm món thất bại", "Số lượng vượt tồn kho hoặc giới hạn 99."); return; }
        NotifyCart();
    }
    [RelayCommand] private async Task IncreaseQuantity(CartItem? item)
    {
        if (item is null) return;
        var stock = snapshot.Products.FirstOrDefault(x => x.Id == item.ProductId)?.Stock ?? 0;
        if (item.Quantity >= Math.Min(99, stock)) { await notifications.ShowError("Tăng số lượng thất bại", "Số lượng vượt tồn kho hoặc giới hạn 99."); return; }
        item.Quantity++; NotifyCart();
    }
    [RelayCommand] private void DecreaseQuantity(CartItem? item) { if (item is null) return; if (item.Quantity <= 1) Cart.Remove(item); else item.Quantity--; NotifyCart(); }
    [RelayCommand] private void ClearCart() { Cart.Clear(); NotifyCart(); }
    private void NotifyCart()
    {
        OnPropertyChanged(nameof(CartTotalLabel));
        OnPropertyChanged(nameof(CanOrder));
        OnPropertyChanged(nameof(CartCount));
        OnPropertyChanged(nameof(CartCountLabel));
    }
    [RelayCommand] private async Task PlaceOrderAsync()
    {
        if (!CanOrder) { await notifications.ShowError("Đặt món thất bại", "Cần phiên tại máy này, kết nối máy chủ và món trong giỏ."); return; }
        if (!await ConfirmAsync($"Đặt món với tổng dự kiến {CartTotalLabel}? Máy chủ xác nhận giá và số dư.")) return;
        await MutateAsync(async () =>
        {
            await api.SendAsync<Order>(HttpMethod.Post, "orders", new { computerId = Machine?.Id ?? MachineId, items = Cart.Select(x => new { productId = x.ProductId, quantity = x.Quantity, expectedUnitPrice = x.UnitPrice }).ToArray(), expectedTotal = Cart.Sum(x => x.Quantity * x.UnitPrice), note = OrderNote, idempotencyKey = Guid.NewGuid().ToString() });
            Cart.Clear(); OrderNote = ""; NotifyCart();
            OnPlaySound?.Invoke("order");
        }, "Đặt món thành công. Giá cuối cùng và số dư do máy chủ xác nhận.");
    }
    [RelayCommand] private async Task CancelOrderAsync()
    {
        if (!CanCancelOrder || SelectedOrder is not { } order) return;
        if (!await ConfirmAsync($"Hủy đơn {order.Id}?")) return;
        await MutateAsync(async () => { await api.SendAsync<Order>(HttpMethod.Post, $"orders/{Uri.EscapeDataString(order.Id)}/transition", new { status = "Cancelled" }); }, "Đã hủy đơn. Tiền hoàn được đọc từ máy chủ.");
    }
    [RelayCommand] private async Task RequestTopupAsync()
    {
        if (!int.TryParse(TopupAmount, out var amount) || amount < 1000 || amount > 10000000) { await notifications.ShowError("Nạp tiền thất bại", "Nhập số tiền nguyên từ 1.000 đến 10.000.000 ₫."); return; }
        
        var methodPrefix = PaymentMethod == "Transfer" ? "[Chuyển khoản VietQR]" : "[Tiền mặt tại quầy]";
        var fullNote = string.IsNullOrWhiteSpace(TopupNote) ? methodPrefix : $"{methodPrefix} {TopupNote.Trim()}";

        await MutateAsync(async () =>
        {
            await api.SendAsync<Topup>(HttpMethod.Post, "topups", new
            {
                amount,
                note = fullNote,
                idempotencyKey = Guid.NewGuid().ToString()
            });
            TopupNote = "";
        }, PaymentMethod == "Transfer"
            ? "Đã gửi thông báo chuyển khoản VietQR đến thu ngân. Vui lòng quét mã QR; số dư sẽ được cộng ngay khi quầy xác nhận."
            : "Đã gửi yêu cầu nạp tiền mặt. Vui lòng thanh toán trực tiếp tại quầy thu ngân; số dư sẽ được cộng sau khi thu ngân xác nhận.");
    }
    [RelayCommand] private async Task SaveProfileAsync()
    {
        if (Validation.Profile(FullName, Phone, Email, DateOfBirth) is { } validation) { await notifications.ShowError("Lưu hồ sơ thất bại", validation); return; }
        if (Hobbies.Length > 500) { await notifications.ShowError("Lưu hồ sơ thất bại", "Sở thích tối đa 500 ký tự."); return; }
        await MutateAsync(async () => { var customer = await api.SendAsync<Customer>(HttpMethod.Patch, "me", new { fullName = FullName.Trim(), phone = Phone, email = Email, dateOfBirth = DateOfBirth, hobbies = Hobbies }); CopyProfile(customer); }, "Đã lưu hồ sơ.");
    }
    private void CopyProfile(Customer customer) { FullName = customer.FullName; Phone = customer.Phone; Email = customer.Email; DateOfBirth = customer.DateOfBirth; Hobbies = customer.Hobbies; }
    [RelayCommand] private async Task ChangePasswordAsync()
    {
        if (CurrentPassword.Length == 0 || NewPassword.Length < 8 || NewPassword.Length > 100 || NewPassword != NewPasswordConfirmation || CurrentPassword == NewPassword) { await notifications.ShowError("Đổi mật khẩu thất bại", "Nhập mật khẩu hiện tại; mật khẩu mới 8–100 ký tự, khác mật khẩu cũ và xác nhận trùng khớp."); return; }
        await MutateAsync(async () => { await api.SendAsync<JsonElement>(HttpMethod.Post, "me/password", new { currentPassword = CurrentPassword, newPassword = NewPassword }); CurrentPassword = ""; NewPassword = ""; NewPasswordConfirmation = ""; }, "Đã đổi mật khẩu.");
    }
    [RelayCommand] private async Task SendFeedbackAsync()
    {
        if (string.IsNullOrWhiteSpace(FeedbackSubject) || string.IsNullOrWhiteSpace(FeedbackContent)) { await notifications.ShowError("Gửi phản hồi thất bại", "Nhập chủ đề và nội dung phản hồi."); return; }
        await MutateAsync(async () => { await api.SendAsync<Feedback>(HttpMethod.Post, "feedback", new { subject = FeedbackSubject.Trim(), content = FeedbackContent.Trim() }); FeedbackSubject = ""; FeedbackContent = ""; }, "Đã gửi phản hồi. Theo dõi trả lời của nhân viên bên dưới.");
    }
    [RelayCommand] private async Task RequestSupport() { SelectedTab = 6; FeedbackSubject = $"Hỗ trợ tại {MachineName}"; FeedbackContent = "Tôi cần nhân viên hỗ trợ: "; await notifications.ShowInfo("Hỗ trợ", "Mô tả vấn đề rồi chọn Gửi hỗ trợ."); }
    [RelayCommand]
    private void SelectTopupAmount(string amountStr)
    {
        TopupAmount = amountStr;
        OnPlaySound?.Invoke("click");
    }

    [RelayCommand]
    private async Task CopyText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        OnCopyToClipboard?.Invoke(text);
        await notifications.ShowSuccess("Đã sao chép", $"Đã sao chép vào bộ nhớ tạm: {text}");
        OnPlaySound?.Invoke("click");
    }

    [RelayCommand]
    private async Task QuickSupportAsync(string issue)
    {
        if (string.IsNullOrWhiteSpace(issue)) return;
        SelectedTab = 6;
        FeedbackSubject = $"[Khẩn cấp] Hỗ trợ máy {MachineName}";
        FeedbackContent = issue;
        OnPlaySound?.Invoke("click");
        await SendFeedbackAsync();
    }

    [RelayCommand]
    private async Task RushKitchenAsync(Order? order)
    {
        SelectedTab = 6;
        var orderCode = order is not null && !string.IsNullOrEmpty(order.Id) 
            ? (order.Id.Length > 6 ? order.Id[..6] : order.Id)
            : "hiện tại";
        FeedbackSubject = $"[Giục bếp] Đơn hàng #{orderCode}";
        FeedbackContent = $"Đơn món tại máy {MachineName} của tôi đang chờ lâu, vui lòng kiểm tra và bưng sớm giùm tôi với nhé!";
        OnPlaySound?.Invoke("click");
        await SendFeedbackAsync();
    }

    [RelayCommand]
    private async Task LaunchFeaturedGame()
    {
        var game = FilteredGames.FirstOrDefault() ?? Games.FirstOrDefault();
        if (game is not null)
        {
            await LaunchGame(game);
        }
    }
    [RelayCommand] private async Task SubmitSurveyAsync()
    {
        if (!CanAnswerSurvey || SelectedSurvey is not { } survey) return;
        if (Answers.Any(x => string.IsNullOrEmpty(x.SelectedAnswer))) { await notifications.ShowError("Khảo sát", "Vui lòng trả lời đầy đủ câu hỏi."); return; }
        await MutateAsync(async () => { await api.SendAsync<SurveyResponse>(HttpMethod.Post, $"surveys/{Uri.EscapeDataString(survey.Id)}/responses", new { answers = Answers.ToDictionary(x => x.Id, x => x.SelectedAnswer) }); }, "Đã gửi khảo sát. Cảm ơn bạn đã góp ý!");
    }
    [RelayCommand]
    private void ToggleSound()
    {
        IsSoundMuted = !IsSoundMuted;
        OnSoundMuteChanged?.Invoke(IsSoundMuted);
        var msg = IsSoundMuted ? "🔇 Đã tắt âm thanh hiệu ứng." : "🔊 Đã bật âm thanh hiệu ứng Cyberpunk.";
        _ = notifications.ShowInfo("Âm thanh", msg);
        if (!IsSoundMuted) OnPlaySound?.Invoke("click");
    }
    [RelayCommand]
    private void PlayLuckySpin()
    {
        OpenLuckyWheel();
    }
    [RelayCommand]
    private void OpenLuckyWheel()
    {
        IsLuckyWheelOpen = true;
        HasWonPrize = false;
        WonPrizeName = "";
        OnPlaySound?.Invoke("click");
    }
    [RelayCommand]
    private void CloseLuckyWheel()
    {
        if (IsSpinning) return;
        IsLuckyWheelOpen = false;
        OnPlaySound?.Invoke("click");
    }

    [RelayCommand]
    private void OpenLoginModal()
    {
        IsLoginModalOpen = true;
        OnPlaySound?.Invoke("click");
    }

    [RelayCommand]
    private void CloseLoginModal()
    {
        IsLoginModalOpen = false;
        IsRegistering = false;
        OnPlaySound?.Invoke("click");
    }

    [RelayCommand]
    private void NextBanner()
    {
        if (bannerImages.Length == 0) return;
        CurrentBannerIndex = (CurrentBannerIndex + 1) % bannerImages.Length;
        CurrentBannerImage = bannerImages[CurrentBannerIndex];
    }

    [RelayCommand]
    private void SelectBanner(object? parameter)
    {
        if (parameter is null) return;
        int index = parameter switch
        {
            int i => i,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => -1
        };
        if (index >= 0 && index < bannerImages.Length)
        {
            CurrentBannerIndex = index;
            CurrentBannerImage = bannerImages[index];
            OnPlaySound?.Invoke("click");
        }
    }
    [RelayCommand]
    private async Task SpinWheelAsync()
    {
        if (IsSpinning) return;
        if (LuckySpins <= 0)
        {
            await notifications.ShowError("Quay thưởng", "Bạn đã dùng hết lượt quay hôm nay. Nạp từ 50.000 ₫ hoặc tích lũy giờ chơi để nhận thêm lượt!");
            return;
        }

        LuckySpins--;
        IsSpinning = true;
        HasWonPrize = false;
        WonPrizeName = "";

        // 8 radial sectors (each 45 deg)
        var prizes = new (string Name, string Icon, decimal BonusMoney, int BonusMinutes)[]
        {
            ("+30 phút giờ chơi miễn phí", "ClockFast", 0, 30),
            ("1 Lon Sting dâu ướp lạnh", "BottleSodaClassic", 0, 0),
            ("+10.000 ₫ nạp tài khoản", "CashPlus", 10000, 0),
            ("1 Xúc xích nướng phô mai", "FoodHotDog", 0, 0),
            ("Voucher giảm 20% đồ ăn F&B", "TicketPercent", 0, 0),
            ("x2 EXP VIP toàn bộ hôm nay", "StarShooting", 0, 0),
            ("+1 giờ chơi Cyber miễn phí", "GiftOutline", 0, 60),
            ("Huy hiệu Chiến Thần Cyber", "ShieldCrown", 0, 0)
        };

        var rng = new Random();
        int prizeIndex = rng.Next(prizes.Length);
        var selectedPrize = prizes[prizeIndex];

        // Rotation physics: 5 full turns + target alignment at 12 o'clock pointer
        double targetAngle = WheelAngle + (360 * 5) + (360 - (prizeIndex * 45) - (WheelAngle % 360));

        if (OnSpinWheelAnimation != null)
        {
            await OnSpinWheelAnimation(targetAngle);
        }
        else
        {
            WheelAngle = targetAngle;
            await Task.Delay(1000);
        }

        WheelAngle = targetAngle;
        IsSpinning = false;
        HasWonPrize = true;
        WonPrizeName = selectedPrize.Name;
        WonPrizeIcon = selectedPrize.Icon;

        if (selectedPrize.BonusMoney > 0)
        {
            Balance += selectedPrize.BonusMoney;
        }

        LuckyRewardMessage = $"🎉 Trúng: {selectedPrize.Name}";
        await notifications.ShowSuccess("Vòng Quay May Mắn", $"🎉 Chúc mừng bạn đã trúng {selectedPrize.Name}!");
        OnPlaySound?.Invoke("topup");
    }
    private void UpdateVipProgress(decimal bal, string tierName)
    {
        if (tierName == "VIP" || bal >= 500000)
        {
            VipBadge = "💎 VIP KIM CƯƠNG";
            CurrentExp = 4800;
            TargetExp = 5000;
            ExpPercentage = 96.0;
        }
        else if (bal >= 200000)
        {
            VipBadge = "🏆 VIP VÀNG";
            CurrentExp = 3600;
            TargetExp = 5000;
            ExpPercentage = 72.0;
        }
        else if (bal >= 50000)
        {
            VipBadge = "🥈 VIP BẠC";
            CurrentExp = 2400;
            TargetExp = 5000;
            ExpPercentage = 48.0;
        }
        else
        {
            VipBadge = "🥉 HỘI VIÊN ĐỒNG";
            CurrentExp = 1200;
            TargetExp = 5000;
            ExpPercentage = 24.0;
        }
    }
    private async Task MutateAsync(Func<Task> action, string success)
    {
        if (!CanMutate) { await notifications.ShowError("Thao tác thất bại", "Chưa kết nối máy chủ hoặc đang xử lý. Chọn Thử kết nối lại."); return; }
        await ExecuteAsync(async () => { await action(); await notifications.ShowSuccess("Thành công", success); await LoadSnapshotAsync(); });
    }
    private async Task ExecuteAsync(Func<Task> action)
    {
        if (IsBusy || syncing) return;
        IsBusy = true;
        try { await action(); }
        catch (Exception ex) { await HandleErrorAsync(ex); }
        finally { IsBusy = false; }
    }
    private async Task HandleErrorAsync(Exception exception)
    {
        switch (exception)
        {
            case ApiException apiError:
                await notifications.ShowError("Lỗi", apiError.Message);
                if (apiError.Code == "ACCOUNT_BANNED" || (apiError.Status == HttpStatusCode.Unauthorized && IsAuthenticated)) { await ClearIdentityAsync(); ConnectionStatus = apiError.Code == "ACCOUNT_BANNED" ? "Tài khoản bị khóa · liên hệ nhân viên" : "Phiên đăng nhập hết hạn"; }
                break;
            case HttpRequestException or OperationCanceledException:
                IsConnected = false; ConnectionStatus = "Mất kết nối · dữ liệu chưa được xác nhận";
                await notifications.ShowError("Mất kết nối", "Không thể kết nối máy chủ. Ứng dụng sẽ đồng bộ lại; hoặc chọn Thử kết nối lại. Nếu vừa gửi giao dịch, kiểm tra lịch sử trước khi gửi lại."); break;
            case JsonException:
                IsConnected = false; await notifications.ShowError("Lỗi dữ liệu", "Phản hồi máy chủ không đúng API contract. Liên hệ nhân viên."); break;
            default:
                await notifications.ShowError("Lỗi", "Không thể hoàn thành thao tác. Thử tải lại dữ liệu hoặc liên hệ nhân viên."); break;
        }
    }

    private void FilterProducts() => Replace(Products, snapshot.Products.Where(x => x.Active && (string.IsNullOrEmpty(SelectedCategory?.Id) || x.CategoryId == SelectedCategory.Id) && (string.IsNullOrWhiteSpace(MenuSearch) || x.Name.Contains(MenuSearch.Trim(), StringComparison.CurrentCultureIgnoreCase))));

    public void UpdateGameCategories()
    {
        var existingSelected = SelectedGameCategory;
        var cats = new List<string> { "Tất cả", "Game Hot" };
        var dynamicCats = Games
            .Where(g => g.Active && !string.IsNullOrWhiteSpace(g.Category))
            .Select(g => g.Category.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c);

        foreach (var c in dynamicCats)
        {
            if (!cats.Contains(c, StringComparer.OrdinalIgnoreCase))
                cats.Add(c);
        }

        if (cats.Count <= 2)
        {
            cats.AddRange(new[] { "Bắn súng (FPS)", "Chiến thuật (MOBA)", "Sinh tồn", "Game Offline" });
        }

        Replace(GameCategories, cats);
        if (GameCategories.Contains(existingSelected))
            SelectedGameCategory = existingSelected;
        else
            SelectedGameCategory = "Tất cả";
    }

    public void FilterGames()
    {
        var q = GameSearch?.Trim() ?? "";
        var cat = SelectedGameCategory ?? "Tất cả";
        IEnumerable<GameItem> source = Games.Count > 0 ? Games : GetDefaultGames();
        var filtered = source.Where(g =>
            g.Active &&
            (cat == "Tất cả" ||
             (cat == "Game Hot" && (
                 g.Category.Contains("Hot", StringComparison.OrdinalIgnoreCase) ||
                 g.Name.Contains("VALORANT", StringComparison.OrdinalIgnoreCase) ||
                 g.Name.Contains("Liên Minh", StringComparison.OrdinalIgnoreCase) ||
                 g.Name.Contains("CS2", StringComparison.OrdinalIgnoreCase) ||
                 g.Name.Contains("PUBG", StringComparison.OrdinalIgnoreCase) ||
                 g.Name.Contains("FIFA", StringComparison.OrdinalIgnoreCase)
             )) ||
             g.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) ||
             g.Category.Contains(cat, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(q) ||
             g.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
             g.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
             (g.Description != null && g.Description.Contains(q, StringComparison.OrdinalIgnoreCase)))
        );
        Replace(FilteredGames, filtered);
    }
    private static List<GameItem> GetDefaultGames() =>
    [
        new GameItem { Id = "valorant", Name = "VALORANT", Category = "Bắn súng (FPS)", ExecutablePath = @"D:\Riot Games\VALORANT\live\VALORANT.exe", BannerUrl = "http://127.0.0.1:5055/images/games/valorant.jpg", Description = "Game bắn súng chiến thuật 5v5 đỉnh cao của Riot Games.", Active = true },
        new GameItem { Id = "lol", Name = "Liên Minh Huyền Thoại", Category = "Chiến thuật (MOBA)", ExecutablePath = @"C:\Riot Games\League of Legends\LeagueClient.exe", BannerUrl = "http://127.0.0.1:5055/images/games/lol.jpg", Description = "Đấu trường trực tuyến nhiều người chơi hàng đầu thế giới.", Active = true },
        new GameItem { Id = "cs2", Name = "Counter-Strike 2", Category = "Bắn súng (FPS)", ExecutablePath = @"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\bin\win64\cs2.exe", BannerUrl = "http://127.0.0.1:5055/images/games/cs2.jpg", Description = "Tựa game bắn súng góc nhìn thứ nhất kinh điển của Valve.", Active = true },
        new GameItem { Id = "pubg", Name = "PUBG: BATTLEGROUNDS", Category = "Sinh tồn", ExecutablePath = @"C:\Program Files (x86)\Steam\steamapps\common\PUBG\TslGame\Binaries\Win64\TslGame.exe", BannerUrl = "http://127.0.0.1:5055/images/games/pubg.jpg", Description = "Tựa game sinh tồn Battle Royale 100 người khốc liệt.", Active = true }
    ];
    public void HandleRemoteCommand(string targetComputerId, string cmd)
    {
        if (IsMatchingMachine(targetComputerId))
        {
            OnRemoteCommand?.Invoke(cmd);
        }
    }

    public bool IsMatchingMachine(string? targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId)) return false;
        if (string.Equals(targetId, MachineId, StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(targetId, MachineName, StringComparison.OrdinalIgnoreCase)) return true;
        
        var num1 = System.Text.RegularExpressions.Regex.Match(MachineId, @"\d+").Value;
        var num2 = System.Text.RegularExpressions.Regex.Match(targetId, @"\d+").Value;
        if (!string.IsNullOrEmpty(num1) && !string.IsNullOrEmpty(num2) && int.TryParse(num1, out var n1) && int.TryParse(num2, out var n2))
        {
            return n1 == n2;
        }
        return false;
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values) { target.Clear(); foreach (var value in values) target.Add(value); }
}
