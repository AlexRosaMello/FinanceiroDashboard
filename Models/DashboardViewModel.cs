namespace FinanceiroDashboard.Models
{
    public class DashboardViewModel
    {
        // Propriedades dos valores totais
        public decimal TotalEmitidas { get; set; }
        public decimal TotalSemCobranca { get; set; }
        public decimal TotalVencidas { get; set; }
        public decimal TotalAVencer {  get; set; }
        public decimal TotalPagas { get; set; }

        // Propriedades para os gráficos
        public List<string> MesesInadimplencia { get; set; } = new List<string>();
        public List<decimal> ValoresInadimplencia { get; set; } = new List<decimal>();

        public List<string> MesesReceita { get; set; } = new List<string>();
        public List<decimal> ValoresReceita { get; set; } = new List<decimal>();
    }
}
