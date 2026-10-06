using InternetCafe.API.Data;
using InternetCafe.API.Data.Entities;
using InternetCafe.API.DTOs.Customers;
using InternetCafe.API.ExceptionHandling;
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
                customer.Tier_ID,
                customer.Username,
                customer.Full_Name,
                customer.Balance ?? 0m,
                tier.Tier_Name,
                customer.Status ?? "Unknown"))
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerListResponse> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        if (await dbContext.Customers.AnyAsync(x => x.Username == username, cancellationToken))
        {
            throw new ConflictException("That username is already in use.");
        }

        var tierExists = await dbContext.MembershipTiers
            .AnyAsync(x => x.TierId == request.Tier_ID, cancellationToken);
        if (!tierExists)
        {
            throw new ArgumentException("The selected membership tier does not exist.");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(request.Password) > 72)
        {
            throw new ArgumentException("Password must not exceed 72 UTF-8 bytes for BCrypt hashing.");
        }

        var customer = new Customer
        {
            Username = username,
            Password_Hash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Full_Name = request.Full_Name.Trim(),
            Balance = 0m,
            Tier_ID = request.Tier_ID,
            Status = "Active",
            Created_Date = DateTime.UtcNow
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(customer.CustomerId, cancellationToken);
    }

    public async Task<CustomerListResponse> UpdateAsync(
        int id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(x => x.CustomerId == id, cancellationToken);
        if (customer is null)
        {
            throw new KeyNotFoundException("Customer was not found.");
        }

        var tierExists = await dbContext.MembershipTiers
            .AnyAsync(x => x.TierId == request.Tier_ID, cancellationToken);
        if (!tierExists)
        {
            throw new ArgumentException("The selected membership tier does not exist.");
        }

        customer.Full_Name = request.Full_Name.Trim();
        customer.Tier_ID = request.Tier_ID;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (System.Text.Encoding.UTF8.GetByteCount(request.Password) > 72)
            {
                throw new ArgumentException("Password must not exceed 72 UTF-8 bytes for BCrypt hashing.");
            }
            customer.Password_Hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(customer.CustomerId, cancellationToken);
    }

    public async Task<CustomerListResponse> UpdateStatusAsync(
        int id,
        UpdateCustomerStatusRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(x => x.CustomerId == id, cancellationToken);
        if (customer is null)
        {
            throw new KeyNotFoundException("Customer was not found.");
        }

        customer.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(customer.CustomerId, cancellationToken);
    }

    private async Task<CustomerListResponse> GetResponseAsync(int id, CancellationToken cancellationToken)
    {
        return await (
            from customer in dbContext.Customers.AsNoTracking()
            join tier in dbContext.MembershipTiers.AsNoTracking()
                on customer.Tier_ID equals tier.TierId
            where customer.CustomerId == id
            select new CustomerListResponse(
                customer.CustomerId,
                customer.Tier_ID,
                customer.Username,
                customer.Full_Name,
                customer.Balance ?? 0m,
                tier.Tier_Name,
                customer.Status ?? "Unknown"))
            .SingleAsync(cancellationToken);
    }
}
