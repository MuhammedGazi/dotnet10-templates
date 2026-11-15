using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectApp.Repositories;

namespace ProjectApp.UnitTests;

internal static class TestDb
{
    public static AppDbContext Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new UnitTestDbContext(options, connection);
        context.Database.EnsureCreated();
        return context;
    }
}

internal sealed class UnitTestDbContext(DbContextOptions<AppDbContext> options, SqliteConnection connection) : AppDbContext(options)
{
    public override void Dispose()
    {
        base.Dispose();
        connection.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await connection.DisposeAsync();
    }
}
