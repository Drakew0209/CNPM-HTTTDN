using InternetCafe.Backend.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InternetCafe.Backend.Services;

public sealed class BusinessService(SqlConnectionFactory connections, IHubContext<CafeHub> hub)
{
    public async Task<OperationResult<object>> StartSessionAsync(IdentityContext identity, StartSessionRequest request, CancellationToken ct)
    {
        if (identity.CustomerId is null) return OperationResult<object>.Fail("FORBIDDEN", "Chỉ khách hàng có thể bắt đầu phiên.", StatusCodes.Status403Forbidden);
        var computerId = TryId(request.ComputerId, "pc");
        if (computerId is null) return OperationResult<object>.Fail("VALIDATION_ERROR", "Mã máy không hợp lệ.");
        await using var connection = await connections.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await using var computer = new SqlCommand("SELECT Status, Hourly_Rate FROM dbo.Computers WITH (UPDLOCK, HOLDLOCK) WHERE Computer_ID = @id", connection, (SqlTransaction)transaction);
            computer.Parameters.AddWithValue("@id", computerId.Value);
            await using var machine = await computer.ExecuteReaderAsync(ct);
            if (!await machine.ReadAsync(ct)) return await Rollback(transaction, OperationResult<object>.Fail("NOT_FOUND", "Không tìm thấy máy.", StatusCodes.Status404NotFound), ct);
            var status = machine.GetString(0); var hourlyRate = machine.GetDecimal(1);
            await machine.CloseAsync();
            if (status != "Available") return await Rollback(transaction, OperationResult<object>.Fail("COMPUTER_UNAVAILABLE", "Máy chưa sẵn sàng để bắt đầu phiên.", StatusCodes.Status409Conflict), ct);
            await using var customer = new SqlCommand("SELECT Balance FROM dbo.Customers WITH (UPDLOCK, HOLDLOCK) WHERE Customer_ID = @id", connection, (SqlTransaction)transaction);
            customer.Parameters.AddWithValue("@id", identity.CustomerId.Value);
            var balance = (decimal?)await customer.ExecuteScalarAsync(ct);
            if (balance is null || balance <= 0) return await Rollback(transaction, OperationResult<object>.Fail("INSUFFICIENT_BALANCE", "Số dư cần lớn hơn 0 để bắt đầu phiên.", StatusCodes.Status409Conflict), ct);
            await using var active = new SqlCommand("SELECT COUNT(*) FROM dbo.Usage_Sessions WITH (UPDLOCK, HOLDLOCK) WHERE Customer_ID = @customerId AND Status = 'Active'", connection, (SqlTransaction)transaction);
            active.Parameters.AddWithValue("@customerId", identity.CustomerId.Value);
            if (Convert.ToInt32(await active.ExecuteScalarAsync(ct)) > 0) return await Rollback(transaction, OperationResult<object>.Fail("ACTIVE_SESSION", "Tài khoản đang có phiên sử dụng khác.", StatusCodes.Status409Conflict), ct);
            // Usage_Sessions has an AFTER trigger that updates the computer state. SQL Server
            // rejects INSERT ... OUTPUT without INTO against a table with an enabled trigger.
            // Read the inserted row back in the same serializable transaction instead.
            await using var insert = new SqlCommand("""
                INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Start_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
                VALUES (@customerId, @computerId, SYSUTCDATETIME(), @balance, @rate, 0, 'Active');
                DECLARE @sessionId INT = CAST(SCOPE_IDENTITY() AS INT);
                SELECT Session_ID, Start_Time, Start_Balance, Amount, Status, Applied_Hourly_Rate
                FROM dbo.Usage_Sessions WHERE Session_ID = @sessionId;
                """, connection, (SqlTransaction)transaction);
            insert.Parameters.AddWithValue("@customerId", identity.CustomerId.Value);
            insert.Parameters.AddWithValue("@computerId", computerId.Value);
            insert.Parameters.AddWithValue("@balance", balance.Value);
            insert.Parameters.AddWithValue("@rate", hourlyRate);
            await using var created = await insert.ExecuteReaderAsync(ct);
            await created.ReadAsync(ct);
            var session = new SessionDto(CafeRepository.Id("session", created.GetInt32(0)), CafeRepository.Id("c", identity.CustomerId.Value), CafeRepository.Id("pc", computerId.Value), created.GetDateTime(1), null, created.GetDecimal(2), created.GetDecimal(3), created.GetString(4), created.GetDecimal(5));
            await created.CloseAsync();
            await transaction.CommitAsync(ct);
            await NotifyAsync("SessionStarted", session.Id, new { sessionId = session.Id, computerId = session.ComputerId }, ct);
            return OperationResult<object>.Ok(session);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(ct);
            return OperationResult<object>.Fail("COMPUTER_UNAVAILABLE", "Máy vừa được sử dụng bởi phiên khác.", StatusCodes.Status409Conflict);
        }
    }

    public async Task<OperationResult<object>> EndSessionAsync(IdentityContext identity, string id, CancellationToken ct, DateTime? endedAtUtc = null)
    {
        var sessionId = TryId(id, "session");
        if (sessionId is null) return OperationResult<object>.Fail("NOT_FOUND", "Không tìm thấy phiên.", StatusCodes.Status404NotFound);
        await using var connection = await connections.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await using var find = new SqlCommand("""
            SELECT s.Customer_ID, s.Computer_ID, s.Start_Time, s.Start_Balance, s.Amount, s.Status, s.Applied_Hourly_Rate, c.Balance
            FROM dbo.Usage_Sessions s WITH (UPDLOCK, HOLDLOCK) JOIN dbo.Customers c WITH (UPDLOCK, HOLDLOCK) ON c.Customer_ID = s.Customer_ID
            WHERE s.Session_ID = @id
            """, connection, (SqlTransaction)transaction);
        find.Parameters.AddWithValue("@id", sessionId.Value);
        await using var reader = await find.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return await Rollback(transaction, OperationResult<object>.Fail("NOT_FOUND", "Không tìm thấy phiên.", StatusCodes.Status404NotFound), ct);
        var customerId = reader.GetInt32(0); var computerId = reader.GetInt32(1); var start = reader.GetDateTime(2); var startBalance = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3); var currentAmount = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4); var status = reader.GetString(5); var rate = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6); var balance = reader.GetDecimal(7);
        await reader.CloseAsync();
        if (identity.Role == Roles.Customer && identity.CustomerId != customerId) return await Rollback(transaction, OperationResult<object>.Fail("FORBIDDEN", "Bạn không sở hữu phiên này.", StatusCodes.Status403Forbidden), ct);
        if (identity.Role is Roles.Staff or Roles.Admin) return await Rollback(transaction, OperationResult<object>.Fail("FORBIDDEN", "Tài khoản không có quyền kết thúc phiên.", StatusCodes.Status403Forbidden), ct);
        if (status != "Active") { await transaction.CommitAsync(ct); return OperationResult<object>.Ok(new SessionDto(id, CafeRepository.Id("c", customerId), CafeRepository.Id("pc", computerId), start, null, startBalance, currentAmount, status, rate)); }
        var endTime = endedAtUtc is { } value
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : DateTime.UtcNow;
        if (endTime < start) endTime = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        var elapsedSeconds = Math.Max(0, (endTime - DateTime.SpecifyKind(start, DateTimeKind.Utc)).TotalSeconds);
        var amount = Math.Min(balance, decimal.Ceiling((decimal)elapsedSeconds * rate / 3600m));
        await using var update = new SqlCommand("UPDATE dbo.Usage_Sessions SET End_Time = @endTime, Amount = @amount, Status = 'Completed' WHERE Session_ID = @id", connection, (SqlTransaction)transaction);
        update.Parameters.AddWithValue("@endTime", endTime); update.Parameters.AddWithValue("@amount", amount); update.Parameters.AddWithValue("@id", sessionId.Value);
        await update.ExecuteNonQueryAsync(ct);
        if (amount > 0)
        {
            await using var ledger = new SqlCommand("IF NOT EXISTS (SELECT 1 FROM dbo.Transactions WHERE Session_ID = @sessionId) INSERT INTO dbo.Transactions (Customer_ID, Session_ID, Trans_Type, Amount, Trans_Date) VALUES (@customerId, @sessionId, 'Rental', @amount, SYSUTCDATETIME())", connection, (SqlTransaction)transaction);
            ledger.Parameters.AddWithValue("@customerId", customerId); ledger.Parameters.AddWithValue("@sessionId", sessionId.Value); ledger.Parameters.AddWithValue("@amount", amount);
            await ledger.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        var result = new SessionDto(id, CafeRepository.Id("c", customerId), CafeRepository.Id("pc", computerId), start, endTime, startBalance, amount, "Completed", rate);
        await NotifyAsync("SessionEnded", id, new { sessionId = id, computerId = result.ComputerId, amount }, ct);
        return OperationResult<object>.Ok(result);
    }

    public async Task<OperationResult<object>> CreateTopupAsync(IdentityContext identity, CreateTopupRequest request, CancellationToken ct)
    {
        if (identity.CustomerId is null) return OperationResult<object>.Fail("FORBIDDEN", "Chỉ khách hàng có thể tạo yêu cầu nạp.", StatusCodes.Status403Forbidden);
        if (request.Amount is < 1000 or > 10000000) return OperationResult<object>.Fail("VALIDATION_ERROR", "Số tiền nạp từ 1.000 đến 10.000.000 đồng.");
        await using var connection = await connections.OpenAsync(ct);
        try
        {
            await using var command = new SqlCommand("""
                INSERT INTO dbo.TopUp_Requests (Customer_ID, Amount, Note, Idempotency_Key) OUTPUT inserted.TopUp_ID, inserted.Requested_At
                VALUES (@customerId, @amount, @note, @key)
                """, connection);
            command.Parameters.AddWithValue("@customerId", identity.CustomerId.Value); command.Parameters.AddWithValue("@amount", request.Amount); command.Parameters.AddWithValue("@note", (object?)request.Note?.Trim() ?? DBNull.Value); command.Parameters.AddWithValue("@key", (object?)request.IdempotencyKey ?? DBNull.Value);
            await using var reader = await command.ExecuteReaderAsync(ct); await reader.ReadAsync(ct);
            var item = new TopupDto(CafeRepository.Id("topup", reader.GetInt32(0)), CafeRepository.Id("c", identity.CustomerId.Value), request.Amount, "Pending", request.Note?.Trim() ?? "", reader.GetDateTime(1));
            await NotifyAsync("TopupRequested", item.Id, new { topupId = item.Id }, ct);
            return OperationResult<object>.Ok(item);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            await using var duplicate = new SqlCommand("SELECT TopUp_ID, Amount, Status, Note, Requested_At FROM dbo.TopUp_Requests WHERE Customer_ID = @customerId AND Idempotency_Key = @key", connection);
            duplicate.Parameters.AddWithValue("@customerId", identity.CustomerId.Value); duplicate.Parameters.AddWithValue("@key", request.IdempotencyKey ?? "");
            await using var reader = await duplicate.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct)) return OperationResult<object>.Ok(new TopupDto(CafeRepository.Id("topup", reader.GetInt32(0)), CafeRepository.Id("c", identity.CustomerId.Value), reader.GetDecimal(1), reader.GetString(2), reader.IsDBNull(3) ? "" : reader.GetString(3), reader.GetDateTime(4)));
            throw;
        }
    }

    public async Task<OperationResult<object>> DecideTopupAsync(IdentityContext identity, string id, DecisionRequest request, CancellationToken ct)
    {
        if (!identity.IsOperator) return OperationResult<object>.Fail("FORBIDDEN", "Chỉ vận hành quán có thể duyệt nạp tiền.", StatusCodes.Status403Forbidden);
        if (request.Status is not ("Approved" or "Rejected")) return OperationResult<object>.Fail("VALIDATION_ERROR", "Trạng thái phải là Approved hoặc Rejected.");
        var topupId = TryId(id, "topup"); if (topupId is null) return OperationResult<object>.Fail("NOT_FOUND", "Không tìm thấy yêu cầu nạp.", StatusCodes.Status404NotFound);
        await using var connection = await connections.OpenAsync(ct); await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await using var find = new SqlCommand("SELECT Customer_ID, Amount, Status FROM dbo.TopUp_Requests WITH (UPDLOCK, HOLDLOCK) WHERE TopUp_ID = @id", connection, (SqlTransaction)transaction);
        find.Parameters.AddWithValue("@id", topupId.Value); await using var reader = await find.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return await Rollback(transaction, OperationResult<object>.Fail("NOT_FOUND", "Không tìm thấy yêu cầu nạp.", StatusCodes.Status404NotFound), ct);
        var customerId = reader.GetInt32(0); var amount = reader.GetDecimal(1); var status = reader.GetString(2); await reader.CloseAsync();
        if (status != "Pending") return await Rollback(transaction, OperationResult<object>.Fail("ALREADY_PROCESSED", "Yêu cầu này đã được xử lý.", StatusCodes.Status409Conflict), ct);
        int? transactionId = null;
        if (request.Status == "Approved")
        {
            await using var ledger = new SqlCommand("INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Trans_Type, Amount, Trans_Date) VALUES (@customerId, @employeeId, 'TopUp', @amount, SYSUTCDATETIME()); SELECT CAST(SCOPE_IDENTITY() AS int);", connection, (SqlTransaction)transaction);
            ledger.Parameters.AddWithValue("@customerId", customerId); ledger.Parameters.AddWithValue("@employeeId", (object?)identity.EmployeeId ?? DBNull.Value); ledger.Parameters.AddWithValue("@amount", amount);
            transactionId = Convert.ToInt32(await ledger.ExecuteScalarAsync(ct));
        }
        await using var update = new SqlCommand("UPDATE dbo.TopUp_Requests SET Status = @status, Decided_At = SYSUTCDATETIME(), Decided_By = @employeeId, Transaction_ID = @transactionId WHERE TopUp_ID = @id", connection, (SqlTransaction)transaction);
        update.Parameters.AddWithValue("@status", request.Status); update.Parameters.AddWithValue("@employeeId", (object?)identity.EmployeeId ?? DBNull.Value); update.Parameters.AddWithValue("@transactionId", (object?)transactionId ?? DBNull.Value); update.Parameters.AddWithValue("@id", topupId.Value);
        await update.ExecuteNonQueryAsync(ct); await transaction.CommitAsync(ct);
        var result = new { id, customerId = CafeRepository.Id("c", customerId), amount, status = request.Status };
        await NotifyAsync("TopupDecided", id, result, ct); if (request.Status == "Approved") await NotifyAsync("BalanceChanged", CafeRepository.Id("c", customerId), new { customerId = CafeRepository.Id("c", customerId) }, ct);
        return OperationResult<object>.Ok(result);
    }

    public async Task<OperationResult<object>> CreateOrderAsync(IdentityContext identity, CreateOrderRequest request, CancellationToken ct)
    {
        if (identity.CustomerId is null) return OperationResult<object>.Fail("FORBIDDEN", "Chỉ khách hàng có thể đặt món.", StatusCodes.Status403Forbidden);
        var computerId = TryId(request.ComputerId, "pc"); if (computerId is null || request.Items.Count == 0 || request.Items.Any(x => x.Quantity <= 0)) return OperationResult<object>.Fail("VALIDATION_ERROR", "Giỏ hàng hoặc mã máy không hợp lệ.");
        await using var connection = await connections.OpenAsync(ct); await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var requestHash = request.IdempotencyKey is null ? null : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { request.ComputerId, request.Items, request.Note }))));
        if (request.IdempotencyKey is not null)
        {
            await using var previous = new SqlCommand("SELECT Request_Hash, Response_Json FROM dbo.Idempotency_Records WITH (UPDLOCK,HOLDLOCK) WHERE Account_ID=@account AND Route='POST /orders' AND Idempotency_Key=@key", connection, (SqlTransaction)transaction);
            previous.Parameters.AddWithValue("@account", identity.AccountId); previous.Parameters.AddWithValue("@key", request.IdempotencyKey);
            await using var reader = await previous.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                var previousHash = reader.IsDBNull(0) ? null : reader.GetString(0);
                var responseJson = reader.GetString(1);
                await reader.CloseAsync(); await transaction.CommitAsync(ct);
                if (!string.Equals(previousHash, requestHash, StringComparison.Ordinal))
                    return OperationResult<object>.Fail("IDEMPOTENCY_CONFLICT", "Idempotency key đã được dùng với nội dung khác.", StatusCodes.Status409Conflict);
                var replay = JsonSerializer.Deserialize<OrderDto>(responseJson);
                return replay is null
                    ? OperationResult<object>.Fail("IDEMPOTENCY_REPLAY_ERROR", "Không thể đọc lại kết quả đơn hàng trước đó.", StatusCodes.Status409Conflict)
                    : OperationResult<object>.Ok(replay);
            }
        }
        await using var session = new SqlCommand("SELECT COUNT(*) FROM dbo.Usage_Sessions WITH (UPDLOCK, HOLDLOCK) WHERE Customer_ID = @customerId AND Computer_ID = @computerId AND Status = 'Active'", connection, (SqlTransaction)transaction);
        session.Parameters.AddWithValue("@customerId", identity.CustomerId.Value); session.Parameters.AddWithValue("@computerId", computerId.Value);
        if (Convert.ToInt32(await session.ExecuteScalarAsync(ct)) == 0) return await Rollback(transaction, OperationResult<object>.Fail("NO_ACTIVE_SESSION", "Cần có phiên đang hoạt động trên máy này để đặt món.", StatusCodes.Status409Conflict), ct);
        var lines = new List<(int ProductId, string Name, int Quantity, decimal Price)>();
        foreach (var item in request.Items)
        {
            var productId = TryId(item.ProductId, "p"); if (productId is null) return await Rollback(transaction, OperationResult<object>.Fail("VALIDATION_ERROR", "Sản phẩm không hợp lệ."), ct);
            await using var product = new SqlCommand("SELECT Product_Name, Price, Stock_Quantity, Status FROM dbo.Products WITH (UPDLOCK, HOLDLOCK) WHERE Product_ID = @id", connection, (SqlTransaction)transaction);
            product.Parameters.AddWithValue("@id", productId.Value); await using var reader = await product.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct) || reader.GetString(3) != "Active") return await Rollback(transaction, OperationResult<object>.Fail("NOT_FOUND", "Sản phẩm không còn phục vụ.", StatusCodes.Status404NotFound), ct);
            var price = reader.GetDecimal(1); if (reader.GetInt32(2) < item.Quantity) return await Rollback(transaction, OperationResult<object>.Fail("OUT_OF_STOCK", "Sản phẩm không đủ tồn kho.", StatusCodes.Status409Conflict), ct);
            if (item.ExpectedUnitPrice is not null && item.ExpectedUnitPrice != price) return await Rollback(transaction, OperationResult<object>.Fail("PRICE_CHANGED", "Giá món đã thay đổi.", StatusCodes.Status409Conflict), ct);
            lines.Add((productId.Value, reader.GetString(0), item.Quantity, price));
        }
        var total = lines.Sum(x => x.Quantity * x.Price);
        await using var balanceCommand = new SqlCommand("SELECT Balance FROM dbo.Customers WITH (UPDLOCK, HOLDLOCK) WHERE Customer_ID = @id", connection, (SqlTransaction)transaction); balanceCommand.Parameters.AddWithValue("@id", identity.CustomerId.Value);
        if (Convert.ToDecimal(await balanceCommand.ExecuteScalarAsync(ct)) < total) return await Rollback(transaction, OperationResult<object>.Fail("INSUFFICIENT_BALANCE", "Số dư không đủ để đặt món.", StatusCodes.Status409Conflict), ct);
        await using var order = new SqlCommand("INSERT INTO dbo.Orders (Customer_ID, Computer_ID, Total_Amount, Status, Notes, Order_Date) OUTPUT inserted.Order_ID, inserted.Order_Date VALUES (@customerId, @computerId, @total, 'Pending', @note, SYSUTCDATETIME())", connection, (SqlTransaction)transaction);
        order.Parameters.AddWithValue("@customerId", identity.CustomerId.Value); order.Parameters.AddWithValue("@computerId", computerId.Value); order.Parameters.AddWithValue("@total", total); order.Parameters.AddWithValue("@note", (object?)request.Note?.Trim() ?? DBNull.Value);
        await using var created = await order.ExecuteReaderAsync(ct); await created.ReadAsync(ct); var orderId = created.GetInt32(0); var createdAt = created.GetDateTime(1); await created.CloseAsync();
        foreach (var line in lines)
        {
            await using var detail = new SqlCommand("INSERT INTO dbo.Order_Details (Order_ID, Product_ID, Quantity, Unit_Price) VALUES (@orderId, @productId, @quantity, @price)", connection, (SqlTransaction)transaction);
            detail.Parameters.AddWithValue("@orderId", orderId); detail.Parameters.AddWithValue("@productId", line.ProductId); detail.Parameters.AddWithValue("@quantity", line.Quantity); detail.Parameters.AddWithValue("@price", line.Price); await detail.ExecuteNonQueryAsync(ct);
        }
        await using var ledger = new SqlCommand("INSERT INTO dbo.Transactions (Customer_ID, Order_ID, Trans_Type, Amount, Trans_Date) VALUES (@customerId, @orderId, 'FoodOrder', @amount, SYSUTCDATETIME())", connection, (SqlTransaction)transaction);
        ledger.Parameters.AddWithValue("@customerId", identity.CustomerId.Value); ledger.Parameters.AddWithValue("@orderId", orderId); ledger.Parameters.AddWithValue("@amount", total); await ledger.ExecuteNonQueryAsync(ct);
        var result = new OrderDto(CafeRepository.Id("order", orderId), CafeRepository.Id("c", identity.CustomerId.Value), request.ComputerId, lines.Select(x => new OrderItemDto(CafeRepository.Id("p", x.ProductId), x.Name, x.Quantity, x.Price)).ToList(), total, "Pending", request.Note?.Trim() ?? "", createdAt);
        if (request.IdempotencyKey is not null)
        {
            await using var idempotency = new SqlCommand("INSERT INTO dbo.Idempotency_Records (Account_ID,Route,Idempotency_Key,Request_Hash,Response_Json) VALUES (@account,'POST /orders',@key,@hash,@response)", connection, (SqlTransaction)transaction);
            idempotency.Parameters.AddWithValue("@account", identity.AccountId); idempotency.Parameters.AddWithValue("@key", request.IdempotencyKey); idempotency.Parameters.AddWithValue("@hash", requestHash!); idempotency.Parameters.AddWithValue("@response", JsonSerializer.Serialize(result));
            await idempotency.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
        await NotifyAsync("OrderCreated", result.Id, new { orderId = result.Id }, ct); await NotifyAsync("BalanceChanged", CafeRepository.Id("c", identity.CustomerId.Value), new { customerId = CafeRepository.Id("c", identity.CustomerId.Value) }, ct);
        return OperationResult<object>.Ok(result);
    }

    public async Task<OperationResult<object>> TransitionOrderAsync(IdentityContext identity, string id, TransitionRequest request, CancellationToken ct)
    {
        var orderId = TryId(id, "order"); if (orderId is null) return OperationResult<object>.Fail("NOT_FOUND", "Không tìm thấy đơn hàng.", StatusCodes.Status404NotFound);
        await using var connection = await connections.OpenAsync(ct); await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await using var find = new SqlCommand("SELECT Customer_ID, Status, Total_Amount FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK) WHERE Order_ID = @id", connection, (SqlTransaction)transaction); find.Parameters.AddWithValue("@id", orderId.Value);
        await using var reader = await find.ExecuteReaderAsync(ct); if (!await reader.ReadAsync(ct)) return await Rollback(transaction, OperationResult<object>.Fail("NOT_FOUND", "Không tìm thấy đơn hàng.", StatusCodes.Status404NotFound), ct);
        var customerId = reader.GetInt32(0); var current = reader.GetString(1); var total = reader.GetDecimal(2); await reader.CloseAsync();
        var allowed = identity.IsOperator ? (current == "Pending" && request.Status is "Preparing" or "Cancelled") || (current == "Preparing" && request.Status is "Served" or "Cancelled") : identity.CustomerId == customerId && current == "Pending" && request.Status == "Cancelled";
        if (!allowed) return await Rollback(transaction, OperationResult<object>.Fail("INVALID_TRANSITION", "Không thể chuyển trạng thái đơn theo yêu cầu.", StatusCodes.Status409Conflict), ct);
        await using var update = new SqlCommand("UPDATE dbo.Orders SET Status = @status WHERE Order_ID = @id", connection, (SqlTransaction)transaction); update.Parameters.AddWithValue("@status", request.Status); update.Parameters.AddWithValue("@id", orderId.Value); await update.ExecuteNonQueryAsync(ct);
        if (request.Status == "Cancelled")
        {
            await using var restore = new SqlCommand("UPDATE p SET Stock_Quantity = Stock_Quantity + d.Quantity FROM dbo.Products p JOIN dbo.Order_Details d ON d.Product_ID = p.Product_ID WHERE d.Order_ID = @orderId", connection, (SqlTransaction)transaction); restore.Parameters.AddWithValue("@orderId", orderId.Value); await restore.ExecuteNonQueryAsync(ct);
            await using var refund = new SqlCommand("IF NOT EXISTS (SELECT 1 FROM dbo.Transactions WHERE Order_ID = @orderId AND Trans_Type = 'Refund') INSERT INTO dbo.Transactions (Customer_ID, Order_ID, Trans_Type, Amount, Trans_Date) VALUES (@customerId, @orderId, 'Refund', @amount, SYSUTCDATETIME())", connection, (SqlTransaction)transaction); refund.Parameters.AddWithValue("@customerId", customerId); refund.Parameters.AddWithValue("@orderId", orderId.Value); refund.Parameters.AddWithValue("@amount", total); await refund.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct); await NotifyAsync("OrderChanged", id, new { orderId = id, status = request.Status }, ct);
        return OperationResult<object>.Ok(new { id, status = request.Status });
    }

    public async Task<OperationResult<object>> CommandComputerAsync(IdentityContext identity, string id, ComputerCommandRequest request, CancellationToken ct)
    {
        if (!identity.IsOperator) return OperationResult<object>.Fail("FORBIDDEN", "Không có quyền điều khiển máy.", StatusCodes.Status403Forbidden);
        var computerId = TryId(id, "pc"); if (computerId is null) return OperationResult<object>.Fail("NOT_FOUND", "Không tìm thấy máy.", StatusCodes.Status404NotFound);
        if (request.Command == "EndSession")
        {
            await using var connection = await connections.OpenAsync(ct); await using var command = new SqlCommand("SELECT TOP 1 Session_ID FROM dbo.Usage_Sessions WHERE Computer_ID = @id AND Status = 'Active' ORDER BY Session_ID DESC", connection); command.Parameters.AddWithValue("@id", computerId.Value);
            var sessionId = await command.ExecuteScalarAsync(ct); return sessionId is null ? OperationResult<object>.Fail("NO_ACTIVE_SESSION", "Máy không có phiên đang hoạt động.", StatusCodes.Status409Conflict) : await EndSessionAsync(identity, CafeRepository.Id("session", Convert.ToInt32(sessionId)), ct);
        }
        if (request.Command is not ("Maintenance" or "Available")) return OperationResult<object>.Fail("VALIDATION_ERROR", "Lệnh máy không hợp lệ.");
        await using var updateConnection = await connections.OpenAsync(ct); await using var update = new SqlCommand("UPDATE dbo.Computers SET Status = @status WHERE Computer_ID = @id", updateConnection); update.Parameters.AddWithValue("@status", request.Command); update.Parameters.AddWithValue("@id", computerId.Value);
        try { if (await update.ExecuteNonQueryAsync(ct) == 0) return OperationResult<object>.Fail("NOT_FOUND", "Không tìm thấy máy.", StatusCodes.Status404NotFound); }
        catch (SqlException) { return OperationResult<object>.Fail("ACTIVE_SESSION", "Không thể đổi trạng thái khi máy có phiên hoạt động.", StatusCodes.Status409Conflict); }
        await NotifyAsync("ComputerChanged", id, new { computerId = id, status = request.Command }, ct); return OperationResult<object>.Ok(new { id, status = request.Command });
    }

    public async Task<OperationResult<object>> HeartbeatAsync(IdentityContext identity, string id, CancellationToken ct)
    {
        var computerId = TryId(id, "pc"); if (computerId is null) return OperationResult<object>.Fail("NOT_FOUND", "Không tìm thấy máy.", StatusCodes.Status404NotFound);
        await using var connection = await connections.OpenAsync(ct); await using var command = new SqlCommand("""
            MERGE dbo.Machine_Heartbeats AS target USING (SELECT @id AS Computer_ID) AS source ON target.Computer_ID = source.Computer_ID
            WHEN MATCHED THEN UPDATE SET Last_Seen_At = SYSUTCDATETIME(), Last_Account_ID = @accountId
            WHEN NOT MATCHED THEN INSERT (Computer_ID, Last_Seen_At, Last_Account_ID) VALUES (@id, SYSUTCDATETIME(), @accountId);
            """, connection); command.Parameters.AddWithValue("@id", computerId.Value); command.Parameters.AddWithValue("@accountId", identity.AccountId); await command.ExecuteNonQueryAsync(ct);
        return OperationResult<object>.Ok(new { id, online = true });
    }

    public async Task CloseExpiredSessionsAsync(TimeSpan offlineSessionGrace, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct); await using var command = new SqlCommand("""
            SELECT s.Session_ID,
                CASE WHEN c.Balance <= CEILING(DATEDIFF_BIG(second, s.Start_Time, SYSUTCDATETIME()) * s.Applied_Hourly_Rate / 3600.0)
                     THEN SYSUTCDATETIME()
                     WHEN h.Last_Seen_At IS NULL OR h.Last_Seen_At < s.Start_Time THEN s.Start_Time
                     ELSE h.Last_Seen_At END AS End_Time
            FROM dbo.Usage_Sessions s
            JOIN dbo.Customers c ON c.Customer_ID = s.Customer_ID
            LEFT JOIN dbo.Machine_Heartbeats h ON h.Computer_ID = s.Computer_ID
            WHERE s.Status = 'Active' AND s.Applied_Hourly_Rate > 0
              AND (
                  c.Balance <= CEILING(DATEDIFF_BIG(second, s.Start_Time, SYSUTCDATETIME()) * s.Applied_Hourly_Rate / 3600.0)
                  OR COALESCE(CASE WHEN h.Last_Seen_At < s.Start_Time THEN s.Start_Time ELSE h.Last_Seen_At END, s.Start_Time) <= DATEADD(second, -@offlineSeconds, SYSUTCDATETIME())
              )
            """, connection);
        command.Parameters.AddWithValue("@offlineSeconds", (int)offlineSessionGrace.TotalSeconds);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var expired = new List<(int SessionId, DateTime EndTime)>();
        while (await reader.ReadAsync(ct)) expired.Add((reader.GetInt32(0), reader.GetDateTime(1)));
        foreach (var session in expired)
            await EndSessionAsync(new IdentityContext(0, Roles.Owner, null, null, "system"), CafeRepository.Id("session", session.SessionId), ct, session.EndTime);
    }

    private Task NotifyAsync(string type, string entityId, object data, CancellationToken ct) => hub.Clients.All.SendAsync(type, new { eventId = Guid.NewGuid(), type, entityId, occurredAt = DateTime.UtcNow, data }, ct);
    private static int? TryId(string value, string prefix) => value.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase) && int.TryParse(value[(prefix.Length + 1)..], out var id) ? id : null;
    private static async Task<OperationResult<object>> Rollback(DbTransaction transaction, OperationResult<object> result, CancellationToken ct) { await transaction.RollbackAsync(ct); return result; }
}
