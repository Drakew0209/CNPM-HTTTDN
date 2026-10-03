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
    private readonly TimeProvider clock;
    private Workspace snapshot = new();
    private DateTimeOffset? receivedServerTime;
    private DateTimeOffset receivedLocalTime;
    private bool syncing;
    private string? userId;
    private bool profileLoaded;
    private readonly CancellationTokenSource lifetime = new();
    public async Task ShutdownAsync() { lifetime.Cancel(); await realtime.StopAsync(); api.AccessToken = null; }
    public MainViewModel(ICafeApi api, IRealtimeUpdates realtime, string machineId = "pc-01", TimeProvider? clock = null)
    {
        this.api = api; this.realtime = realtime; this.clock = clock ?? TimeProvider.System; MachineId = machineId;
    }
    public Func<string, Task<bool>> ConfirmAsync { get; set; } = _ => Task.FromResult(false);
    public string MachineId { get; }
    [ObservableProperty] private bool isAuthenticated;
    [ObservableProperty] private bool isRegistering;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isConnected;
    [ObservableProperty] private bool isAppLocked;
    [ObservableProperty] private bool lowBalance;
    [ObservableProperty] private string username = "gamer";
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
    [ObservableProperty] private string error = "";
    [ObservableProperty] private string notice = "";
    [ObservableProperty] private string connectionStatus = "Chưa kết nối máy chủ";
    [ObservableProperty] private string accountName = "Khách hàng";
    [ObservableProperty] private string tier = "";
    [ObservableProperty] private decimal balance;
    [ObservableProperty] private string machineName = "PC-01";
    [ObservableProperty] private string hourlyRateLabel = "—";
    [ObservableProperty] private string elapsedLabel = "00:00:00";
    [ObservableProperty] private string remainingLabel = "—";
    [ObservableProperty] private string sessionStatus = "Chưa bắt đầu phiên";
    [ObservableProperty] private string sessionSummary = "";
    [ObservableProperty] private string lastSyncLabel = "Chưa đồng bộ";
    [ObservableProperty] private string menuSearch = "";
    [ObservableProperty] private Category? selectedCategory;
    [ObservableProperty] private Order? selectedOrder;
    [ObservableProperty] private Survey? selectedSurvey;
    [ObservableProperty] private string orderNote = "";
    [ObservableProperty] private string topupAmount = "50000";
    [ObservableProperty] private string topupNote = "";
    [ObservableProperty] private string feedbackSubject = "";
    [ObservableProperty] private string feedbackContent = "";
    [ObservableProperty] private int selectedTab;
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<CartItem> Cart { get; } = [];
    public ObservableCollection<Order> Orders { get; } = [];
    public ObservableCollection<Topup> Topups { get; } = [];
    public ObservableCollection<Transaction> Transactions { get; } = [];
    public ObservableCollection<Session> Sessions { get; } = [];
    public ObservableCollection<Feedback> FeedbackItems { get; } = [];
    public ObservableCollection<Survey> Surveys { get; } = [];
    public ObservableCollection<AnswerViewModel> Answers { get; } = [];
    public string BalanceLabel => Format.Money(Balance);
    public string CartTotalLabel => Format.Money(Cart.Sum(x => x.Quantity * x.UnitPrice));
    public bool HasActiveSession => ActiveSession is not null;
    public bool CanStartSession => IsAuthenticated && IsConnected && !IsBusy && !HasActiveSession && Balance > 0 && Machine is { Online: true, Status: "Available" };
    public bool CanEndSession => IsAuthenticated && IsConnected && !IsBusy && HasActiveSession;
    public bool CanMutate => IsAuthenticated && IsConnected && !IsBusy;
    public bool CanOrder => CanMutate && ActiveSession?.ComputerId == MachineId && Cart.Count > 0;
    public bool CanCancelOrder => CanMutate && SelectedOrder?.Status == "Pending";
    public bool CanAnswerSurvey => CanMutate && SelectedSurvey is { Status: "Published" } survey && survey.Responses.All(x => x.CustomerId != userId);
    public string SurveyState => SelectedSurvey is null ? "Chọn khảo sát dành cho bạn." : SelectedSurvey.Responses.Any(x => x.CustomerId == userId) ? "Bạn đã gửi câu trả lời. Cảm ơn bạn!" : SelectedSurvey.Status == "Closed" ? "Khảo sát đã đóng." : "Chọn một câu trả lời cho mỗi câu hỏi.";
    private Session? ActiveSession => snapshot.Sessions.FirstOrDefault(x => x.Status == "Active" && x.CustomerId == userId);
    private Computer? Machine => snapshot.Computers.FirstOrDefault(x => x.Id == MachineId);
    partial void OnIsBusyChanged(bool value) => NotifyState();
    partial void OnIsConnectedChanged(bool value) => NotifyState();
    partial void OnIsAuthenticatedChanged(bool value) => NotifyState();
    partial void OnBalanceChanged(decimal value) { OnPropertyChanged(nameof(BalanceLabel)); NotifyState(); }
    partial void OnMenuSearchChanged(string value) => FilterProducts();
    partial void OnSelectedCategoryChanged(Category? value) => FilterProducts();
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
    [RelayCommand] private void ToggleRegistration() { IsRegistering = !IsRegistering; Error = ""; Notice = ""; Password = ""; PasswordConfirmation = ""; }
    [RelayCommand] private async Task LoginAsync()
    {
        if (Validation.Credentials(Username, Password) is { } validation) { Error = validation; return; }
        await ExecuteAsync(async () =>
        {
            var login = await api.SendAsync<LoginResult>(HttpMethod.Post, "auth/login", new { username = Username.Trim(), password = Password });
            if (login.User.Role != "Customer") throw new ApiException("FORBIDDEN", "Ứng dụng máy trạm chỉ dành cho khách hàng. Nhân viên dùng cổng web.", HttpStatusCode.Forbidden);
            api.AccessToken = login.AccessToken; userId = login.User.Id; IsAuthenticated = true; IsRegistering = false; profileLoaded = false; Password = "";
            await LoadSnapshotAsync();
            try { await realtime.StartAsync(() => Task.FromResult<string?>(api.AccessToken)); }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or OperationCanceledException) { Notice = "Realtime chưa sẵn sàng; ứng dụng vẫn đồng bộ qua HTTP."; }
        });
    }
    [RelayCommand] private async Task RegisterAsync()
    {
        if (Validation.Register(Username, Password, PasswordConfirmation, FullName, Phone, Email, DateOfBirth) is { } validation) { Error = validation; return; }
        if (Hobbies.Length > 500) { Error = "Sở thích tối đa 500 ký tự."; return; }
        await ExecuteAsync(async () =>
        {
            await api.SendAsync<Customer>(HttpMethod.Post, "auth/register", new { username = Username.Trim(), password = Password, fullName = FullName.Trim(), phone = Phone, email = Email, dateOfBirth = DateOfBirth, hobbies = Hobbies });
            IsRegistering = false; Password = ""; PasswordConfirmation = ""; Notice = "Đã tạo tài khoản. Đăng nhập và nạp tiền tại quầy để bắt đầu.";
        });
    }
    [RelayCommand] private async Task LogoutAsync()
    {
        if (HasActiveSession) { Error = "Kết thúc phiên đang sử dụng trước khi đăng xuất."; return; }
        await ExecuteAsync(async () => { await api.SendAsync<JsonElement>(HttpMethod.Post, "auth/logout", new { }); await ClearIdentityAsync(); Notice = "Đã đăng xuất."; });
    }
    private async Task ClearIdentityAsync()
    {
        api.AccessToken = null; userId = null; IsAuthenticated = false; IsAppLocked = false; profileLoaded = false; snapshot = new(); Balance = 0;
        Cart.Clear(); Products.Clear(); Categories.Clear(); Orders.Clear(); Topups.Clear(); Transactions.Clear(); Sessions.Clear(); FeedbackItems.Clear(); Surveys.Clear(); Answers.Clear(); SelectedSurvey = null; SelectedOrder = null;
        Password = ""; CurrentPassword = ""; NewPassword = ""; NewPasswordConfirmation = "";
        await realtime.StopAsync(); NotifyState(); NotifyCart();
    }
    [RelayCommand] private async Task RefreshAsync() => await ExecuteAsync(LoadSnapshotAsync);
    public async Task PollAsync()
    {
        if (!IsAuthenticated || IsBusy || syncing) return;
        syncing = true;
        try { await LoadSnapshotAsync(); }
        catch (Exception ex) { await HandleErrorAsync(ex); }
        finally { syncing = false; }
    }
    private async Task LoadSnapshotAsync()
    {
        if (!IsAuthenticated) return;
        var data = await api.GetAsync<Workspace>("workspace");
        var profile = data.Customers.FirstOrDefault(x => x.Id == userId) ?? throw new ApiException("UNAUTHENTICATED", "Không tìm thấy hồ sơ. Vui lòng đăng nhập lại.", HttpStatusCode.Unauthorized);
        if (profile.Status != "Active") throw new ApiException("ACCOUNT_BANNED", "Tài khoản đã bị khóa. Liên hệ nhân viên.", HttpStatusCode.Forbidden);
        var oldSession = ActiveSession?.Id;
        snapshot = data; receivedServerTime = api.ServerTime; receivedLocalTime = clock.GetUtcNow();
        Balance = profile.Balance; AccountName = profile.FullName; Tier = profile.Tier;
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
        IsConnected = true; ConnectionStatus = "Đã kết nối · HTTP đồng bộ định kỳ"; LastSyncLabel = "Đồng bộ lúc " + clock.GetLocalNow().ToString("HH:mm:ss");
        if (oldSession is not null && ActiveSession is null) { IsAppLocked = true; SessionSummary = Summary(data.Sessions.FirstOrDefault(x => x.Id == oldSession)); Notice = "Phiên đã được kết thúc từ máy chủ."; }
        UpdateClock(); NotifyState();
    }
    public void UpdateClock()
    {
        var now = receivedServerTime is { } server ? server + (clock.GetUtcNow() - receivedLocalTime) : clock.GetUtcNow();
        var active = ActiveSession;
        var rate = active is null ? Machine?.HourlyRate ?? 0 : snapshot.Computers.FirstOrDefault(x => x.Id == active.ComputerId)?.HourlyRate ?? 0;
        var estimate = SessionEstimate.Calculate(Balance, active?.StartTime, rate, now);
        ElapsedLabel = SessionEstimate.Duration(estimate.Elapsed);
        RemainingLabel = estimate.Remaining is { } remaining ? SessionEstimate.Duration(remaining) : "Chưa có đơn giá";
        LowBalance = IsAuthenticated && (Balance <= 10000 || estimate.Remaining is { TotalMinutes: <= 10 });
        SessionStatus = active is null ? (Balance <= 0 ? "Hết số dư · vui lòng nạp tại quầy" : "Chưa bắt đầu phiên") : active.ComputerId != MachineId ? "Phiên đang hoạt động trên máy khác" : "Phiên đang sử dụng";
    }
    [RelayCommand] private async Task StartSessionAsync()
    {
        if (!CanStartSession) { Error = "Cần kết nối, số dư dương và máy sẵn sàng."; return; }
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
    [RelayCommand] private void AddToCart(Product? product)
    {
        if (product is null || product.Stock <= 0) { Error = "Món đã hết hàng."; return; }
        var item = Cart.FirstOrDefault(x => x.ProductId == product.Id);
        if (item is null) { if (Cart.Count >= 30) { Error = "Mỗi đơn tối đa 30 món."; return; } Cart.Add(new CartItem(product)); }
        else if (item.Quantity < Math.Min(99, product.Stock)) item.Quantity++;
        else { Error = "Số lượng vượt tồn kho hoặc giới hạn 99."; return; }
        NotifyCart(); Error = "";
    }
    [RelayCommand] private void IncreaseQuantity(CartItem? item)
    {
        if (item is null) return;
        var stock = snapshot.Products.FirstOrDefault(x => x.Id == item.ProductId)?.Stock ?? 0;
        if (item.Quantity >= Math.Min(99, stock)) { Error = "Số lượng vượt tồn kho hoặc giới hạn 99."; return; }
        item.Quantity++; NotifyCart();
    }
    [RelayCommand] private void DecreaseQuantity(CartItem? item) { if (item is null) return; if (item.Quantity <= 1) Cart.Remove(item); else item.Quantity--; NotifyCart(); }
    [RelayCommand] private void ClearCart() { Cart.Clear(); NotifyCart(); }
    private void NotifyCart() { OnPropertyChanged(nameof(CartTotalLabel)); OnPropertyChanged(nameof(CanOrder)); }
    [RelayCommand] private async Task PlaceOrderAsync()
    {
        if (!CanOrder) { Error = "Cần phiên tại máy này, kết nối máy chủ và món trong giỏ."; return; }
        if (!await ConfirmAsync($"Đặt món với tổng dự kiến {CartTotalLabel}? Máy chủ xác nhận giá và số dư.")) return;
        await MutateAsync(async () =>
        {
            await api.SendAsync<Order>(HttpMethod.Post, "orders", new { computerId = MachineId, items = Cart.Select(x => new { productId = x.ProductId, quantity = x.Quantity, expectedUnitPrice = x.UnitPrice }).ToArray(), expectedTotal = Cart.Sum(x => x.Quantity * x.UnitPrice), note = OrderNote, idempotencyKey = Guid.NewGuid().ToString() });
            Cart.Clear(); OrderNote = ""; NotifyCart();
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
        if (!int.TryParse(TopupAmount, out var amount) || amount < 1000 || amount > 10000000) { Error = "Nhập số tiền nguyên từ 1.000 đến 10.000.000 ₫."; return; }
        await MutateAsync(async () => { await api.SendAsync<Topup>(HttpMethod.Post, "topups", new { amount, note = TopupNote, idempotencyKey = Guid.NewGuid().ToString() }); TopupNote = ""; }, "Đã gửi yêu cầu. Thanh toán tại quầy; số dư chỉ tăng sau khi thu ngân xác nhận.");
    }
    [RelayCommand] private async Task SaveProfileAsync()
    {
        if (Validation.Profile(FullName, Phone, Email, DateOfBirth) is { } validation) { Error = validation; return; }
        if (Hobbies.Length > 500) { Error = "Sở thích tối đa 500 ký tự."; return; }
        await MutateAsync(async () => { var customer = await api.SendAsync<Customer>(HttpMethod.Patch, "me", new { fullName = FullName.Trim(), phone = Phone, email = Email, dateOfBirth = DateOfBirth, hobbies = Hobbies }); CopyProfile(customer); }, "Đã lưu hồ sơ.");
    }
    private void CopyProfile(Customer customer) { FullName = customer.FullName; Phone = customer.Phone; Email = customer.Email; DateOfBirth = customer.DateOfBirth; Hobbies = customer.Hobbies; }
    [RelayCommand] private async Task ChangePasswordAsync()
    {
        if (CurrentPassword.Length == 0 || NewPassword.Length < 8 || NewPassword.Length > 100 || NewPassword != NewPasswordConfirmation || CurrentPassword == NewPassword) { Error = "Nhập mật khẩu hiện tại; mật khẩu mới 8–100 ký tự, khác mật khẩu cũ và xác nhận trùng khớp."; return; }
        await MutateAsync(async () => { await api.SendAsync<JsonElement>(HttpMethod.Post, "me/password", new { currentPassword = CurrentPassword, newPassword = NewPassword }); CurrentPassword = ""; NewPassword = ""; NewPasswordConfirmation = ""; }, "Đã đổi mật khẩu.");
    }
    [RelayCommand] private async Task SendFeedbackAsync()
    {
        if (string.IsNullOrWhiteSpace(FeedbackSubject) || string.IsNullOrWhiteSpace(FeedbackContent)) { Error = "Nhập chủ đề và nội dung phản hồi."; return; }
        await MutateAsync(async () => { await api.SendAsync<Feedback>(HttpMethod.Post, "feedback", new { subject = FeedbackSubject.Trim(), content = FeedbackContent.Trim() }); FeedbackSubject = ""; FeedbackContent = ""; }, "Đã gửi phản hồi. Theo dõi trả lời của nhân viên bên dưới.");
    }
    [RelayCommand] private void RequestSupport() { SelectedTab = 5; FeedbackSubject = $"Hỗ trợ tại {MachineName}"; FeedbackContent = "Tôi cần nhân viên hỗ trợ: "; Notice = "Mô tả vấn đề rồi chọn Gửi hỗ trợ."; }
    [RelayCommand] private async Task SubmitSurveyAsync()
    {
        if (!CanAnswerSurvey || SelectedSurvey is not { } survey) return;
        if (Answers.Any(x => string.IsNullOrEmpty(x.SelectedAnswer))) { Error = "Vui lòng trả lời đầy đủ câu hỏi."; return; }
        await MutateAsync(async () => { await api.SendAsync<SurveyResponse>(HttpMethod.Post, $"surveys/{Uri.EscapeDataString(survey.Id)}/responses", new { answers = Answers.ToDictionary(x => x.Id, x => x.SelectedAnswer) }); }, "Đã gửi khảo sát. Cảm ơn bạn đã góp ý!");
    }
    private async Task MutateAsync(Func<Task> action, string success)
    {
        if (!CanMutate) { Error = "Chưa kết nối máy chủ hoặc đang xử lý. Chọn Thử kết nối lại."; return; }
        await ExecuteAsync(async () => { await action(); Notice = success; await LoadSnapshotAsync(); });
    }
    private async Task ExecuteAsync(Func<Task> action)
    {
        if (IsBusy || syncing) return;
        IsBusy = true; Error = ""; Notice = "";
        try { await action(); }
        catch (Exception ex) { await HandleErrorAsync(ex); }
        finally { IsBusy = false; }
    }
    private async Task HandleErrorAsync(Exception exception)
    {
        switch (exception)
        {
            case ApiException apiError:
                Error = apiError.Message;
                if (apiError.Code == "ACCOUNT_BANNED" || (apiError.Status == HttpStatusCode.Unauthorized && IsAuthenticated)) { await ClearIdentityAsync(); ConnectionStatus = apiError.Code == "ACCOUNT_BANNED" ? "Tài khoản bị khóa · liên hệ nhân viên" : "Phiên đăng nhập hết hạn"; }
                break;
            case HttpRequestException or OperationCanceledException:
                IsConnected = false; ConnectionStatus = "Mất kết nối · dữ liệu chưa được xác nhận";
                Error = "Không thể kết nối máy chủ. Ứng dụng sẽ đồng bộ lại; hoặc chọn Thử kết nối lại. Nếu vừa gửi giao dịch, kiểm tra lịch sử trước khi gửi lại."; break;
            case JsonException:
                IsConnected = false; Error = "Phản hồi máy chủ không đúng API contract. Liên hệ nhân viên."; break;
            default:
                Error = "Không thể hoàn thành thao tác. Thử tải lại dữ liệu hoặc liên hệ nhân viên."; break;
        }
    }
    private void FilterProducts() => Replace(Products, snapshot.Products.Where(x => x.Active && (string.IsNullOrEmpty(SelectedCategory?.Id) || x.CategoryId == SelectedCategory.Id) && (string.IsNullOrWhiteSpace(MenuSearch) || x.Name.Contains(MenuSearch.Trim(), StringComparison.CurrentCultureIgnoreCase))));
    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values) { target.Clear(); foreach (var value in values) target.Add(value); }
}
