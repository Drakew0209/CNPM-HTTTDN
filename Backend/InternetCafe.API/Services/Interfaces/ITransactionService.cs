using InternetCafe.API.DTOs.Transactions;

namespace InternetCafe.API.Services.Interfaces;

public interface ITransactionService
{
    Task<TopUpResponse> TopUpAsync(TopUpRequest request, int? processedBy, CancellationToken cancellationToken);
}
