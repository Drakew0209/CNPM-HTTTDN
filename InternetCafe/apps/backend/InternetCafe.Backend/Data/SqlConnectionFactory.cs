using Microsoft.Data.SqlClient;

namespace InternetCafe.Backend.Data;

public sealed class SqlConnectionFactory(IConfiguration configuration)
{
    private readonly string connectionString = configuration.GetConnectionString("InternetCafe")
        ?? throw new InvalidOperationException("ConnectionStrings:InternetCafe is required.");

    public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
