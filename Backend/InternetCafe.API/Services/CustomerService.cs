using InternetCafe.API.Data;
using InternetCafe.API.DTOs.Customers;
using InternetCafe.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Services;

public sealed class CustomerService(InternetCafeDbContext dbContext) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerListResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await (
            from customer in dbContext.Customers.AsNoTracking()
            join tier in dbContext.MembershipTiers.AsNoTracking()
                on customer.Tier_ID equals tier.TierId
            orderby customer.CustomerId descending
            select new CustomerListResponse(
                customer.CustomerId,
                customer.Username,
                customer.Full_Name,
                customer.Balance ?? 0m,
                tier.Tier_Name,
                customer.Status ?? "Unknown"))
            .Take(500)
            .ToListAsync(cancellationToken);
    }
}
