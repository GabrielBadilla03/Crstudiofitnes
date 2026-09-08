namespace CrStudioFitnes.ViewModels
{
    public class GrupoPaqueteDetalleVM
    {
        public int IdGrupoPaquete { get; set; }
        public int IdPaquete { get; set; }
        public string NombrePaquete { get; set; } = string.Empty;
        public int CantidadUsuarios { get; set; }
        public bool Activo { get; set; }
        public bool TieneSaldoPendiente => Miembros.Any(m => m.SaldoPendiente > 0);
        public List<GrupoPaqueteMiembroVM> Miembros { get; set; } = new();
    }

    public class GrupoPaqueteMiembroVM
    {
        public string IdUsuario { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public int IdPaqueteUsuario { get; set; }
        public int CantLecciones { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal SaldoPendiente { get; set; }
        public bool EsUsuarioActual { get; set; }
    }
}
