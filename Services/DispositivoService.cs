using Npgsql;
using MomosAPI.Models;

namespace MomosAPI.Services;

public class DispositivoService(DbConnectionFactory db)
{
    public DispositivoDto? ObtenerPorToken(string token)
    {
        using var conn = db.Create();
        using var cmd  = new NpgsqlCommand(
            "SELECT Id,Nombre,Token,Activo,PrefixFolio,CreadoEn,UltimaConexion FROM Dispositivos WHERE Token=@t", conn);
        cmd.Parameters.AddWithValue("t", token);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public DispositivoDto? ObtenerPorId(int id)
    {
        using var conn = db.Create();
        using var cmd  = new NpgsqlCommand(
            "SELECT Id,Nombre,Token,Activo,PrefixFolio,CreadoEn,UltimaConexion FROM Dispositivos WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("id", id);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Map(r) : null;
    }

    public List<DispositivoDto> Listar()
    {
        var list = new List<DispositivoDto>();
        using var conn = db.Create();
        using var cmd  = new NpgsqlCommand(
            "SELECT Id,Nombre,Token,Activo,PrefixFolio,CreadoEn,UltimaConexion FROM Dispositivos ORDER BY Id", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(Map(r));
        return list;
    }

    public DispositivoDto Crear(CrearDispositivoRequest req)
    {
        var token = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        using var conn = db.Create();
        int id;
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO Dispositivos(Nombre,Token,PrefixFolio) VALUES(@n,@t,@p) RETURNING Id", conn))
        {
            cmd.Parameters.AddWithValue("n", req.Nombre);
            cmd.Parameters.AddWithValue("t", token);
            cmd.Parameters.AddWithValue("p", req.PrefixFolio);
            id = (int)cmd.ExecuteScalar()!;
        }
        using (var cmd2 = new NpgsqlCommand(
            "INSERT INTO DispositivoFolios(DispositivoId,TipoDocumento,Consecutivo) VALUES(@d,'VENTA',0)", conn))
        {
            cmd2.Parameters.AddWithValue("d", id);
            cmd2.ExecuteNonQuery();
        }
        return ObtenerPorId(id)!;
    }

    public bool Actualizar(int id, CrearDispositivoRequest req)
    {
        using var conn = db.Create();
        using var cmd  = new NpgsqlCommand("UPDATE Dispositivos SET Nombre=@n,PrefixFolio=@p WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("n",   req.Nombre);
        cmd.Parameters.AddWithValue("p",   req.PrefixFolio);
        cmd.Parameters.AddWithValue("id",  id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool ToggleActivo(int id, bool activo)
    {
        using var conn = db.Create();
        using var cmd  = new NpgsqlCommand("UPDATE Dispositivos SET Activo=@a WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("a",  activo);
        cmd.Parameters.AddWithValue("id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public void ActualizarUltimaConexion(int dispositivoId)
    {
        using var conn = db.Create();
        using var cmd  = new NpgsqlCommand("UPDATE Dispositivos SET UltimaConexion=NOW() WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("id", dispositivoId);
        cmd.ExecuteNonQuery();
    }

    public FolioResponse SiguienteFolio(int dispositivoId, string tipo = "VENTA")
    {
        using var conn = db.Create();
        using var tx   = conn.BeginTransaction();

        using (var upd = new NpgsqlCommand(
            "UPDATE DispositivoFolios SET Consecutivo=Consecutivo+1 WHERE DispositivoId=@d AND TipoDocumento=@t AND Activo=true",
            conn, tx))
        {
            upd.Parameters.AddWithValue("d", dispositivoId);
            upd.Parameters.AddWithValue("t", tipo);
            upd.ExecuteNonQuery();
        }

        long   consecutivo;
        string prefix;
        using (var sel = new NpgsqlCommand(
            "SELECT df.Consecutivo, d.PrefixFolio FROM DispositivoFolios df " +
            "JOIN Dispositivos d ON d.Id=df.DispositivoId " +
            "WHERE df.DispositivoId=@d AND df.TipoDocumento=@t", conn, tx))
        {
            sel.Parameters.AddWithValue("d", dispositivoId);
            sel.Parameters.AddWithValue("t", tipo);
            using var r = sel.ExecuteReader();
            r.Read();
            consecutivo = r.GetInt64(0);
            prefix      = r.GetString(1);
        }
        tx.Commit();
        return new FolioResponse { Folio = $"{prefix}-{consecutivo:D6}", Consecutivo = consecutivo };
    }

    private static DispositivoDto Map(NpgsqlDataReader r) => new()
    {
        Id             = r.GetInt32(0),
        Nombre         = r.GetString(1),
        Token          = r.GetString(2),
        Activo         = r.GetBoolean(3),
        PrefixFolio    = r.GetString(4),
        CreadoEn       = r.GetDateTime(5),
        UltimaConexion = r.IsDBNull(6) ? null : r.GetDateTime(6)
    };
}
