using Npgsql;
namespace MomosAPI.Services;
public class DbConnectionFactory
{
    private readonly string _cs;
    public DbConnectionFactory(IConfiguration c) => _cs = c.GetConnectionString("DefaultConnection")!;
    public NpgsqlConnection Create() { var conn = new NpgsqlConnection(_cs); conn.Open(); return conn; }
}
