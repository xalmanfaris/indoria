using System.Data;
using Microsoft.Data.SqlClient;

namespace AuraLiving.Services.Database;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
    IDbConnection CreateMasterConnection();
    string ConnectionString { get; }
}

public class DbConnectionFactory : IDbConnectionFactory
{
    public string ConnectionString { get; }

    public DbConnectionFactory(IConfiguration configuration)
    {
        ConnectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=DESKTOP-P2EURLB,19312;Database=IndoriaDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString);
    }

    public IDbConnection CreateMasterConnection()
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString)
        {
            InitialCatalog = "master"
        };
        return new SqlConnection(builder.ConnectionString);
    }
}
