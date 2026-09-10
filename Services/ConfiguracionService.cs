using Npgsql;
namespace MomosAPI.Services;
public class ConfiguracionService(DbConnectionFactory db)
{
    public string ObtenerValor(string clave, string def = "")
    {
        using var conn = db.Create();
        using var cmd = new NpgsqlCommand("SELECT Valor FROM Configuracion WHERE Clave=@c", conn);
        cmd.Parameters.AddWithValue("c", clave);
        return cmd.ExecuteScalar()?.ToString() ?? def;
    }
    public bool EsVerdadero(string clave) =>
        ObtenerValor(clave).Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
}
