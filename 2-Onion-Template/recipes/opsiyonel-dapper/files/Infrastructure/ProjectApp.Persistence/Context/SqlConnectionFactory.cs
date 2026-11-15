using System.Data;
using Microsoft.Data.SqlClient;

namespace ProjectApp.Persistence.Context;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public IDbConnection CreateConnection() => new SqlConnection(connectionString);
}
