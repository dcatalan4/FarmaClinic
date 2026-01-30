using System;

namespace ControlInventario.Helpers
{
    public static class DateTimeHelper
    {
        /// <summary>
        /// Obtiene la fecha y hora actual del cliente (timezone local)
        /// </summary>
        /// <param name="clientDateTime">Fecha enviada desde el cliente</param>
        /// <returns>DateTime local del cliente o DateTime.Now si no se proporciona</returns>
        public static DateTime GetClientDateTime(DateTime? clientDateTime = null)
        {
            if (clientDateTime.HasValue)
            {
                // Si la fecha viene en formato UTC, convertirla a local
                if (clientDateTime.Value.Kind == DateTimeKind.Utc)
                {
                    return clientDateTime.Value.ToLocalTime();
                }
                // Si ya es local, usarla directamente
                else if (clientDateTime.Value.Kind == DateTimeKind.Local)
                {
                    return clientDateTime.Value;
                }
                // Si es Unspecified, asumir que es local
                else
                {
                    return DateTime.SpecifyKind(clientDateTime.Value, DateTimeKind.Local);
                }
            }
            
            // Fallback a hora del servidor si no se proporciona fecha del cliente
            return DateTime.Now;
        }

        /// <summary>
        /// Obtiene solo la fecha actual del cliente
        /// </summary>
        /// <param name="clientDateTime">Fecha enviada desde el cliente</param>
        /// <returns>Fecha local del cliente</returns>
        public static DateTime GetClientDate(DateTime? clientDateTime = null)
        {
            return GetClientDateTime(clientDateTime).Date;
        }

        /// <summary>
        /// Formatea fecha para mostrar al usuario
        /// </summary>
        /// <param name="dateTime">Fecha a formatear</param>
        /// <param name="format">Formato de fecha (opcional)</param>
        /// <returns>Fecha formateada</returns>
        public static string FormatClientDateTime(DateTime dateTime, string format = "dd/MM/yyyy HH:mm")
        {
            return dateTime.ToString(format);
        }

        /// <summary>
        /// Genera número de venta usando fecha del cliente
        /// </summary>
        /// <param name="clientDateTime">Fecha del cliente</param>
        /// <returns>Número de venta único</returns>
        public static int GenerateVentaNumber(DateTime? clientDateTime = null)
        {
            var fecha = GetClientDateTime(clientDateTime);
            return int.Parse(fecha.ToString("yyMMdd") + fecha.ToString("mm").PadLeft(2, '0'));
        }
    }
}
