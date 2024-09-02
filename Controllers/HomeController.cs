using FinanceiroDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Diagnostics;

namespace FinanceiroDashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string _connectionString;
        string status;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index(string MesFiltro, string Status)
        {
            List<NotaFiscalViewModel> notas = new List<NotaFiscalViewModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT numero_nf, pagador, dt_emissao, dt_cobranca, dt_pgto, valor_nf FROM NotaFiscal WHERE 1 = 1";

                switch (Status) {
                    case "E":
                        if (!string.IsNullOrEmpty(MesFiltro))
                            query += " AND MONTH(dt_emissao) = @MesFiltro AND YEAR(dt_emissao) = @AnoFiltro";

                        query += " AND dt_emissao IS NOT NULL";
                        status = "Emitida";
                        break;
                    case "CR":
                        if (!string.IsNullOrEmpty(MesFiltro))
                            query += " AND MONTH(dt_cobranca) = @MesFiltro AND YEAR(dt_cobranca) = @AnoFiltro";

                        query += " AND dt_cobranca IS NOT NULL";
                        status = "Cobrança Realizada";
                        break;
                    case "PA":
                        if (!string.IsNullOrEmpty(MesFiltro))
                            query += " AND MONTH(dt_pgto) = @MesFiltro AND YEAR(dt_pgto) = @AnoFiltro";
                        
                        query += " AND dt_pgto IS NULL";
                        status = "Pagamento Atrasado";
                        break;
                    case "PR":
                        if (!string.IsNullOrEmpty(MesFiltro))
                            query += " AND MONTH(dt_pgto) = @MesFiltro AND YEAR(dt_pgto) = @AnoFiltro";

                        query += " AND dt_pgto IS NOT NULL";
                        status = "Pagamento Realizado";
                        break;
                    default:
                        break;
                }

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(MesFiltro))
                    {
                        cmd.Parameters.AddWithValue("@MesFiltro", MesFiltro.Split('-')[1]);
                        cmd.Parameters.AddWithValue("@AnoFiltro", MesFiltro.Split('-')[0]);
                    }

                    SqlDataReader rs = cmd.ExecuteReader();
                    while (rs.Read())
                    {
                        NotaFiscalViewModel nota = new NotaFiscalViewModel()
                        {
                            NumeroNF = rs.GetString(0),
                            Pagador = rs.GetString(1),
                            DtEmissao = rs.GetDateTime(2),
                            DtCobranca = rs.IsDBNull(3) ? (DateTime?)null : rs.GetDateTime(3),
                            DtPgto = rs.IsDBNull(4) ? (DateTime?)null : rs.GetDateTime(4),
                            ValorNF = rs.GetDecimal(5),
                            Status = status
                        };

                        notas.Add(nota);
                    }
                }

                return View(notas);
            }

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
