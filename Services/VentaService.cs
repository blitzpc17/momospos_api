using Npgsql;
using MomosAPI.Models;

namespace MomosAPI.Services;

public class VentaService(DbConnectionFactory db)
{
    public VentaResponse RegistrarVenta(VentaRequest req, int dispositivoId)
    {
        using var conn = db.Create();
        using var tx   = conn.BeginTransaction();
        try
        {
            int cajaSesionId = ObtenerSesionMovil(conn, tx);

            int ventaId;
            const string sqlV = @"INSERT INTO Ventas
                (Folio,CajaSesionId,ClienteId,Total,Pagado,Cambio,Estado,UsuarioId,
                 DescuentoTotal,DescuentoManual,DispositivoId,Origen)
                VALUES(@fo,@cs,@cl,@to,@pa,@ca,'CONFIRMADO',
                       (SELECT Id FROM Usuarios WHERE EsAdmin=true ORDER BY Id LIMIT 1),
                       @dt,0,@di,'MOVIL') RETURNING Id";
            using (var cmd = new NpgsqlCommand(sqlV, conn, tx))
            {
                cmd.Parameters.AddWithValue("fo", req.Folio);
                cmd.Parameters.AddWithValue("cs", cajaSesionId);
                cmd.Parameters.AddWithValue("cl", (object?)req.ClienteId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("to", req.Total);
                cmd.Parameters.AddWithValue("pa", req.Pagado);
                cmd.Parameters.AddWithValue("ca", req.Cambio);
                cmd.Parameters.AddWithValue("dt", req.DescuentoTotal);
                cmd.Parameters.AddWithValue("di", dispositivoId);
                ventaId = (int)cmd.ExecuteScalar()!;
            }

            foreach (var d in req.Detalles)
            {
                using (var cmd2 = new NpgsqlCommand(
                    "INSERT INTO VentaDetalles(VentaId,ProductoId,Descripcion,Cantidad,PrecioUnitario,Subtotal,DescuentoManual) VALUES(@v,@p,@de,@ca,@pu,@su,@dm)",
                    conn, tx))
                {
                    cmd2.Parameters.AddWithValue("v",  ventaId);
                    cmd2.Parameters.AddWithValue("p",  d.ProductoId);
                    cmd2.Parameters.AddWithValue("de", d.Descripcion);
                    cmd2.Parameters.AddWithValue("ca", d.Cantidad);
                    cmd2.Parameters.AddWithValue("pu", d.PrecioUnitario);
                    cmd2.Parameters.AddWithValue("su", d.Subtotal);
                    cmd2.Parameters.AddWithValue("dm", d.DescuentoManual);
                    cmd2.ExecuteNonQuery();
                }
                using (var stk = new NpgsqlCommand(
                    "UPDATE Productos SET StockActual=StockActual-@ca WHERE Id=@p AND EsServicio=false",
                    conn, tx))
                {
                    stk.Parameters.AddWithValue("ca", d.Cantidad);
                    stk.Parameters.AddWithValue("p",  d.ProductoId);
                    stk.ExecuteNonQuery();
                }
            }

            foreach (var p in req.Pagos)
            {
                using var cp = new NpgsqlCommand(
                    "INSERT INTO VentaPagos(VentaId,MetodoPago,Importe) VALUES(@v,@m,@i)", conn, tx);
                cp.Parameters.AddWithValue("v", ventaId);
                cp.Parameters.AddWithValue("m", p.MetodoPago);
                cp.Parameters.AddWithValue("i", p.Importe);
                cp.ExecuteNonQuery();
            }

            tx.Commit();
            return new VentaResponse { Success = true, VentaId = ventaId, Folio = req.Folio };
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return new VentaResponse { Success = false, Mensaje = ex.Message };
        }
    }

    private static int ObtenerSesionMovil(NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        using var sel = new NpgsqlCommand(
            "SELECT Id FROM CajaSesiones WHERE Estado='ABIERTA' AND CajaId=1 ORDER BY FechaApertura DESC LIMIT 1",
            conn, tx);
        var r = sel.ExecuteScalar();
        if (r != null) return (int)r;

        using var ins = new NpgsqlCommand(
            "INSERT INTO CajaSesiones(CajaId,UsuarioAperturaId,FondoInicial,Estado) " +
            "VALUES(1,(SELECT Id FROM Usuarios WHERE EsAdmin=true ORDER BY Id LIMIT 1),0,'ABIERTA') RETURNING Id",
            conn, tx);
        return (int)ins.ExecuteScalar()!;
    }
}
