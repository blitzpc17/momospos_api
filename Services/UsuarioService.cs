using Npgsql;
using MomosAPI.Models;
using System.Data;

namespace MomosAPI.Services;

public class UsuarioService(DbConnectionFactory db)
{
    public UsuarioDto? Autenticar(string usuarioLogin, string password)
    {
        using var conn = db.Create();
        using var cmd = new NpgsqlCommand(
            "SELECT Id, Nombre, Usuario, EsAdmin, Estado FROM Usuarios WHERE Usuario = @u AND PasswordHash = @p AND Estado = 'ACTIVO'", conn);
        cmd.Parameters.AddWithValue("u", usuarioLogin);
        cmd.Parameters.AddWithValue("p", password);
        
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            return new UsuarioDto
            {
                Id = r.GetInt32(0),
                Nombre = r.GetString(1),
                UsuarioLogin = r.GetString(2),
                EsAdmin = r.GetBoolean(3),
                Estado = r.GetString(4)
            };
        }
        return null;
    }
}
