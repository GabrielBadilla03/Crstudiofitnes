namespace CrStudioFitnes.Views.Reservas
{
    public class DiaCalendarioVM
    {
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }

        // Si existe BloqueoHorario Activo con Fecha y SIN IdHora => día completo bloqueado
        public bool IsBlockedDay { get; set; }

        // Para dibujar un indicador (opcional) de reservas del día
        public int ReservasCount { get; set; }
    }
}
