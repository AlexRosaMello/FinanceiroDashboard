namespace FinanceiroDashboard.Models
{
    public class NotaFiscalViewModel
    {
        public string NumeroNF { get; set; }
        public string Pagador { get; set; }
        public DateTime DtEmissao { get; set; }
        public DateTime? DtCobranca { get; set; }
        public DateTime? DtPgto { get; set; }
        public decimal ValorNF { get; set; }
        public string Status { get; set; }
    }
}
