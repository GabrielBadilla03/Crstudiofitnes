
namespace CrStudioFitnes.Views.Reservas
{
    public class ReservasCalendarioVM
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthLabel { get; set; } = "";

        // Siempre 42 celdas (6 semanas x 7 días) para que el calendario quede parejo
        public List<DiaCalendarioVM> Days { get; set; } = new();
    }
}
