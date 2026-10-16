using System.Net;
using System.Text.Json;

namespace InternetCafe.Client.Core;

public sealed class StandaloneDemoApi : ICafeApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object syncRoot = new();

    public string? AccessToken { get; set; }
    public DateTimeOffset? ServerTime => DateTimeOffset.UtcNow;

    private readonly Dictionary<string, string> userPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gamer"] = "Demo@123"
    };

    private readonly Workspace workspace = new();

    public StandaloneDemoApi()
    {
        SeedData();
    }

    private void SeedData()
    {
        workspace.Computers.AddRange(new[]
        {
            new Computer { Id = "pc-001", Name = "PC-01", Zone = "VIP Gaming", Status = "Available", Online = true, HourlyRate = 15000 },
            new Computer { Id = "pc-002", Name = "PC-02", Zone = "Standard", Status = "Available", Online = true, HourlyRate = 8000 },
            new Computer { Id = "pc-003", Name = "PC-03", Zone = "Thi Đấu FPS", Status = "Available", Online = true, HourlyRate = 20000 }
        });

        workspace.Customers.Add(new Customer
        {
            Id = "c-001",
            Username = "gamer",
            FullName = "Nguyễn Văn An",
            Phone = "0987654321",
            Email = "gamer@cybercafe.vn",
            DateOfBirth = "2002-05-15",
            Hobbies = "VALORANT, FPS, MOBA, Thức đêm leo rank",
            Tier = "VIP",
            Balance = 120000m,
            Status = "Active"
        });

        workspace.Games.Add(new GameItem
        {
            Id = "valorant",
            Name = "VALORANT",
            Category = "Bắn súng (FPS)",
            ExecutablePath = @"D:\Riot Games\VALORANT\live\VALORANT.exe",
            BannerUrl = "http://127.0.0.1:5055/images/games/valorant.jpg",
            Description = "Game bắn súng chiến thuật 5v5 đỉnh cao của Riot Games.",
            Active = true
        });

        workspace.Combos.AddRange(new[]
        {
            new Combo { Id = "combo-001", Code = "CB50K", Name = "Combo Giờ Vàng 50K", Price = 50000, BonusValue = 10000, DurationHours = 3, Description = "Nạp 50.000 ₫ tặng ngay 10.000 ₫ giờ chơi." },
            new Combo { Id = "combo-002", Code = "CB100K", Name = "Combo Chiến Thần 100K", Price = 100000, BonusValue = 30000, DurationHours = 6, Description = "Nạp 100.000 ₫ tặng ngay 30.000 ₫ giờ chơi." },
            new Combo { Id = "combo-003", Code = "CB200K", Name = "Combo Cày Đêm VIP 200K", Price = 200000, BonusValue = 70000, DurationHours = 12, Description = "Nạp 200.000 ₫ tặng ngay 70.000 ₫ giờ chơi thả ga." },
            new Combo { Id = "combo-004", Code = "CB500K", Name = "Combo Đại Gia Cyber 500K", Price = 500000, BonusValue = 200000, DurationHours = 30, Description = "Nạp 500.000 ₫ nhận thêm 200.000 ₫ vào tài khoản." }
        });

        workspace.Categories.AddRange(new[]
        {
            new Category("cat-001", "Món ăn chính"),
            new Category("cat-002", "Nước giải khát"),
            new Category("cat-003", "Đồ ăn vặt")
        });

        workspace.Products.AddRange(new[]
        {
            new Product { Id = "p-001", Name = "Mì xào bò trứng ốp la", CategoryId = "cat-001", Price = 35000, Stock = 50, Active = true },
            new Product { Id = "p-002", Name = "Cơm chiên dưa bò giòn", CategoryId = "cat-001", Price = 45000, Stock = 40, Active = true },
            new Product { Id = "p-003", Name = "Bánh mì xúc xích pate", CategoryId = "cat-001", Price = 25000, Stock = 30, Active = true },
            new Product { Id = "p-004", Name = "Mì cay hải sản cấp 3", CategoryId = "cat-001", Price = 42000, Stock = 25, Active = true },
            new Product { Id = "p-005", Name = "Nước tăng lực Sting dâu", CategoryId = "cat-002", Price = 15000, Stock = 100, Active = true },
            new Product { Id = "p-006", Name = "Nước tăng lực Red Bull Thái", CategoryId = "cat-002", Price = 20000, Stock = 80, Active = true },
            new Product { Id = "p-007", Name = "Cà phê sữa đá pha phin", CategoryId = "cat-002", Price = 18000, Stock = 60, Active = true },
            new Product { Id = "p-008", Name = "Trà sữa trân châu đường đen", CategoryId = "cat-002", Price = 25000, Stock = 50, Active = true },
            new Product { Id = "p-009", Name = "Khoai tây chiên lắc phô mai", CategoryId = "cat-003", Price = 25000, Stock = 45, Active = true },
            new Product { Id = "p-010", Name = "Xúc xích Đức nướng tiêu", CategoryId = "cat-003", Price = 15000, Stock = 70, Active = true },
            new Product { Id = "p-011", Name = "Snack Oishi tôm cay", CategoryId = "cat-003", Price = 10000, Stock = 90, Active = true }
        });

        var now = DateTimeOffset.UtcNow;
        workspace.Sessions.Add(new Session
        {
            Id = "session-prev",
            CustomerId = "c-001",
            ComputerId = "pc-001",
            StartTime = now.AddHours(-3),
            EndTime = now.AddHours(-1),
            StartBalance = 180000m,
            Amount = 30000m,
            Status = "Completed"
        });

        workspace.Topups.Add(new Topup
        {
            Id = "top-001",
            Amount = 100000m,
            Note = "Nạp tại quầy lễ tân Cybercafe",
            Status = "Approved",
            CreatedAt = now.AddDays(-1)
        });

        workspace.Transactions.AddRange(new[]
        {
            new Transaction { Id = "tx-001", Type = "Topup", Amount = 100000m, ReferenceId = "top-001", CreatedAt = now.AddDays(-1) },
            new Transaction { Id = "tx-002", Type = "Session", Amount = -30000m, ReferenceId = "session-prev", CreatedAt = now.AddHours(-1) }
        });

        workspace.Feedback.Add(new Feedback
        {
            Id = "fb-001",
            Subject = "Độ phân giải màn hình 240Hz rất mượt",
            Content = "Phòng VIP chơi rất thích, gear xịn bấm nảy.",
            Status = "Resolved",
            Response = "Cảm ơn bạn đã ủng hộ Cybercafe!",
            CreatedAt = now.AddDays(-2)
        });

        workspace.Surveys.Add(new Survey
        {
            Id = "survey-001",
            Title = "Khảo sát chất lượng dịch vụ Cybercafe",
            Description = "Hãy đóng góp ý kiến để giúp phòng máy nâng cao trải nghiệm khách hàng.",
            Status = "Published",
            Questions =
            [
                new SurveyQuestion("q1", "Bạn đánh giá độ mượt của PC và màn hình thế nào?", ["Rất mượt mà (240Hz)", "Mượt mà ổn định", "Bình thường", "Cần nâng cấp"]),
                new SurveyQuestion("q2", "Chất lượng và tốc độ phục vụ món ăn nước uống?", ["Rất ngon và phục vụ nhanh", "Hài lòng", "Bình thường", "Cần cải thiện"]),
                new SurveyQuestion("q3", "Không gian, bàn ghế gaming và điều hòa phòng máy?", ["Rất thoải mái và mát lạnh", "Tốt", "Hơi lạnh/nóng", "Bàn ghế cần vệ sinh thường xuyên hơn"])
            ]
        });
    }

    public Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        lock (syncRoot)
        {
            var clean = path.TrimStart('/');
            object? result = clean switch
            {
                "workspace" => workspace,
                "combos" => workspace.Combos,
                _ => throw new ApiException("NOT_FOUND", $"Không tìm thấy endpoint {path}", HttpStatusCode.NotFound)
            };

            return Task.FromResult(ConvertValue<T>(result));
        }
    }

    public Task<T> SendAsync<T>(HttpMethod method, string path, object body, CancellationToken cancellationToken = default)
    {
        lock (syncRoot)
        {
            var clean = path.TrimStart('/');
            var root = JsonSerializer.SerializeToElement(body, JsonOptions);

            object? result;

            if (clean == "auth/login")
            {
                var username = root.TryGetProperty("username", out var u) ? u.GetString() ?? "" : "";
                var password = root.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";

                if (!userPasswords.TryGetValue(username, out var expected) || expected != password)
                {
                    throw new ApiException("BAD_CREDENTIALS", "Tên đăng nhập hoặc mật khẩu không chính xác.", HttpStatusCode.BadRequest);
                }

                var customer = workspace.Customers.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ApiException("UNAUTHENTICATED", "Không tìm thấy hồ sơ người dùng.", HttpStatusCode.Unauthorized);

                AccessToken = "demo-standalone-token-" + customer.Id;
                result = new LoginResult(AccessToken, new User(customer.Id, customer.Username, customer.FullName, "Customer", ["Customer"]));
            }
            else if (clean == "auth/register")
            {
                var username = root.TryGetProperty("username", out var u) ? u.GetString() ?? "" : "";
                var password = root.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";
                var fullName = root.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? "" : "";
                var phone = root.TryGetProperty("phone", out var ph) ? ph.GetString() ?? "" : "";
                var email = root.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
                var dob = root.TryGetProperty("dateOfBirth", out var db) ? db.GetString() ?? "2000-01-01" : "2000-01-01";
                var hobbies = root.TryGetProperty("hobbies", out var hb) ? hb.GetString() ?? "" : "";

                if (userPasswords.ContainsKey(username))
                    throw new ApiException("CONFLICT", "Tên đăng nhập đã được sử dụng.", HttpStatusCode.Conflict);

                userPasswords[username] = password;
                var newCustomer = new Customer
                {
                    Id = $"c-{workspace.Customers.Count + 1:000}",
                    Username = username,
                    FullName = fullName,
                    Phone = phone,
                    Email = email,
                    DateOfBirth = dob,
                    Hobbies = hobbies,
                    Tier = "Member",
                    Balance = 100000m,
                    Status = "Active"
                };
                workspace.Customers.Add(newCustomer);
                result = newCustomer;
            }
            else if (clean == "auth/logout")
            {
                AccessToken = null;
                result = new { success = true };
            }
            else if (clean.StartsWith("machines/") && clean.EndsWith("/heartbeat"))
            {
                var machineId = clean.Substring("machines/".Length, clean.Length - "machines/".Length - "/heartbeat".Length);
                var machine = workspace.Computers.FirstOrDefault(x => x.Id == machineId);
                if (machine is not null) machine.Online = true;
                result = new { success = true };
            }
            else if (clean == "sessions/start")
            {
                var computerId = root.TryGetProperty("computerId", out var cid) ? cid.GetString() ?? "pc-001" : "pc-001";
                var customer = CurrentCustomer();
                var computer = workspace.Computers.FirstOrDefault(x => x.Id == computerId) ?? workspace.Computers[0];

                var existing = workspace.Sessions.FirstOrDefault(x => x.Status == "Active" && x.CustomerId == customer.Id);
                if (existing is not null)
                {
                    result = existing;
                }
                else
                {
                    var session = new Session
                    {
                        Id = $"session-{workspace.Sessions.Count + 1:000}",
                        CustomerId = customer.Id,
                        ComputerId = computer.Id,
                        StartTime = DateTimeOffset.UtcNow,
                        StartBalance = customer.Balance,
                        Status = "Active"
                    };
                    computer.Status = "Occupied";
                    computer.CustomerId = customer.Id;
                    computer.SessionId = session.Id;
                    workspace.Sessions.Insert(0, session);
                    result = session;
                }
            }
            else if (clean.StartsWith("sessions/") && clean.EndsWith("/end"))
            {
                var sessionId = Uri.UnescapeDataString(clean.Substring("sessions/".Length, clean.Length - "sessions/".Length - "/end".Length));
                var session = workspace.Sessions.FirstOrDefault(x => x.Id == sessionId)
                    ?? throw new ApiException("NOT_FOUND", "Không tìm thấy phiên cần kết thúc.", HttpStatusCode.NotFound);

                var customer = workspace.Customers.FirstOrDefault(x => x.Id == session.CustomerId);
                var computer = workspace.Computers.FirstOrDefault(x => x.Id == session.ComputerId);

                var duration = Math.Max(0, (DateTimeOffset.UtcNow - session.StartTime).TotalSeconds);
                var rate = computer?.HourlyRate ?? 15000m;
                var billed = decimal.Ceiling((decimal)duration * rate / 3600m);

                if (customer is not null)
                {
                    customer.Balance = Math.Max(0, customer.Balance - billed);
                }

                session.EndTime = DateTimeOffset.UtcNow;
                session.Amount = billed;
                session.Status = "Completed";

                if (computer is not null && computer.SessionId == session.Id)
                {
                    computer.Status = "Available";
                    computer.CustomerId = null;
                    computer.SessionId = null;
                }

                workspace.Transactions.Insert(0, new Transaction
                {
                    Id = $"tx-{workspace.Transactions.Count + 1:000}",
                    Type = "Session",
                    Amount = -billed,
                    ReferenceId = session.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                result = session;
            }
            else if (clean == "orders")
            {
                var customer = CurrentCustomer();
                var computerId = root.TryGetProperty("computerId", out var cId) ? cId.GetString() ?? "pc-001" : "pc-001";
                var note = root.TryGetProperty("note", out var nt) ? nt.GetString() ?? "" : "";
                var orderItems = new List<OrderLine>();
                decimal total = 0;

                if (root.TryGetProperty("items", out var itemsElem) && itemsElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var it in itemsElem.EnumerateArray())
                    {
                        var prodId = it.GetProperty("productId").GetString() ?? "";
                        var qty = it.GetProperty("quantity").GetInt32();
                        var product = workspace.Products.FirstOrDefault(p => p.Id == prodId);
                        if (product is not null)
                        {
                            var unitPrice = product.Price;
                            total += unitPrice * qty;
                            product.Stock = Math.Max(0, product.Stock - qty);
                            orderItems.Add(new OrderLine { ProductId = prodId, Name = product.Name, Quantity = qty, UnitPrice = unitPrice });
                        }
                    }
                }

                if (customer.Balance < total)
                {
                    throw new ApiException("INSUFFICIENT_BALANCE", $"Số dư không đủ để thanh toán ({Format.Money(total)}). Vui lòng nạp tiền tại quầy.", HttpStatusCode.UnprocessableEntity);
                }

                customer.Balance -= total;
                var order = new Order
                {
                    Id = $"ord-{workspace.Orders.Count + 1:000}",
                    CustomerId = customer.Id,
                    ComputerId = computerId,
                    Items = orderItems,
                    Total = total,
                    Status = "Preparing",
                    Note = note,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                workspace.Orders.Insert(0, order);
                workspace.Transactions.Insert(0, new Transaction
                {
                    Id = $"tx-{workspace.Transactions.Count + 1:000}",
                    Type = "Order",
                    Amount = -total,
                    ReferenceId = order.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                result = order;
            }
            else if (clean.StartsWith("orders/") && clean.EndsWith("/transition"))
            {
                var orderId = Uri.UnescapeDataString(clean.Substring("orders/".Length, clean.Length - "orders/".Length - "/transition".Length));
                var order = workspace.Orders.FirstOrDefault(x => x.Id == orderId)
                    ?? throw new ApiException("NOT_FOUND", "Không tìm thấy đơn hàng.", HttpStatusCode.NotFound);

                var status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
                if (status == "Cancelled")
                {
                    order.Status = "Cancelled";
                    var customer = workspace.Customers.FirstOrDefault(x => x.Id == order.CustomerId);
                    if (customer is not null)
                    {
                        customer.Balance += order.Total;
                        workspace.Transactions.Insert(0, new Transaction
                        {
                            Id = $"tx-{workspace.Transactions.Count + 1:000}",
                            Type = "Refund",
                            Amount = order.Total,
                            ReferenceId = order.Id,
                            CreatedAt = DateTimeOffset.UtcNow
                        });
                    }
                }
                else
                {
                    order.Status = status;
                }
                result = order;
            }
            else if (clean == "topups")
            {
                var customer = CurrentCustomer();
                var amount = root.TryGetProperty("amount", out var am) ? am.GetDecimal() : 50000m;
                var note = root.TryGetProperty("note", out var n) ? n.GetString() ?? "" : "";

                var topup = new Topup
                {
                    Id = $"top-{workspace.Topups.Count + 1:000}",
                    Amount = amount,
                    Note = note,
                    Status = "Approved",
                    CreatedAt = DateTimeOffset.UtcNow
                };

                customer.Balance += amount;
                workspace.Topups.Insert(0, topup);
                workspace.Transactions.Insert(0, new Transaction
                {
                    Id = $"tx-{workspace.Transactions.Count + 1:000}",
                    Type = "Topup",
                    Amount = amount,
                    ReferenceId = topup.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                result = topup;
            }
            else if (clean == "me")
            {
                var customer = CurrentCustomer();
                if (root.TryGetProperty("fullName", out var fn)) customer.FullName = fn.GetString() ?? customer.FullName;
                if (root.TryGetProperty("phone", out var ph)) customer.Phone = ph.GetString() ?? customer.Phone;
                if (root.TryGetProperty("email", out var em)) customer.Email = em.GetString() ?? customer.Email;
                if (root.TryGetProperty("dateOfBirth", out var db)) customer.DateOfBirth = db.GetString() ?? customer.DateOfBirth;
                if (root.TryGetProperty("hobbies", out var hb)) customer.Hobbies = hb.GetString() ?? customer.Hobbies;
                result = customer;
            }
            else if (clean == "me/password")
            {
                var customer = CurrentCustomer();
                var cur = root.TryGetProperty("currentPassword", out var cp) ? cp.GetString() ?? "" : "";
                var np = root.TryGetProperty("newPassword", out var nwp) ? nwp.GetString() ?? "" : "";

                if (userPasswords.TryGetValue(customer.Username, out var exp) && exp != cur)
                {
                    throw new ApiException("BAD_CREDENTIALS", "Mật khẩu hiện tại không đúng.", HttpStatusCode.BadRequest);
                }

                userPasswords[customer.Username] = np;
                result = new { success = true };
            }
            else if (clean == "feedback")
            {
                var subject = root.TryGetProperty("subject", out var sb) ? sb.GetString() ?? "" : "";
                var content = root.TryGetProperty("content", out var ct) ? ct.GetString() ?? "" : "";

                var fb = new Feedback
                {
                    Id = $"fb-{workspace.Feedback.Count + 1:000}",
                    Subject = subject,
                    Content = content,
                    Status = "Resolved",
                    Response = "Cảm ơn bạn! Quầy thu ngân đã tiếp nhận và đang hỗ trợ máy của bạn.",
                    CreatedAt = DateTimeOffset.UtcNow
                };

                workspace.Feedback.Insert(0, fb);
                result = fb;
            }
            else if (clean.StartsWith("surveys/") && clean.EndsWith("/responses"))
            {
                var surveyId = Uri.UnescapeDataString(clean.Substring("surveys/".Length, clean.Length - "surveys/".Length - "/responses".Length));
                var survey = workspace.Surveys.FirstOrDefault(x => x.Id == surveyId)
                    ?? throw new ApiException("NOT_FOUND", "Không tìm thấy khảo sát.", HttpStatusCode.NotFound);

                var customer = CurrentCustomer();
                var answers = new Dictionary<string, string>();
                if (root.TryGetProperty("answers", out var ansElem) && ansElem.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in ansElem.EnumerateObject())
                    {
                        answers[prop.Name] = prop.Value.GetString() ?? "";
                    }
                }

                var resp = new SurveyResponse(customer.Id, answers, DateTimeOffset.UtcNow);
                survey.Responses.Add(resp);
                result = resp;
            }
            else
            {
                throw new ApiException("NOT_FOUND", $"Không tìm thấy endpoint POST {path}", HttpStatusCode.NotFound);
            }

            return Task.FromResult(ConvertValue<T>(result));
        }
    }

    private Customer CurrentCustomer()
    {
        if (string.IsNullOrEmpty(AccessToken))
        {
            return workspace.Customers[0];
        }

        var customerId = AccessToken.Replace("demo-standalone-token-", "");
        return workspace.Customers.FirstOrDefault(x => x.Id == customerId) ?? workspace.Customers[0];
    }

    private static T ConvertValue<T>(object? value)
    {
        if (value is null)
        {
            return default!;
        }

        if (value is T direct)
        {
            return direct;
        }

        var json = JsonSerializer.Serialize(value, JsonOptions);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)!;
    }
}
