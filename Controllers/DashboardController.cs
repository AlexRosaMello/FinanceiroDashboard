using FinanceiroDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace FinanceiroDashboard.Controllers
{
    public class DashboardController : Controller
    {
        private readonly string _connectionString;

        public DashboardController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index(string FiltroPeriodo, string Periodo, string Trimestre, string Ano)
        {
            DashboardViewModel model = new DashboardViewModel();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Condições para o filtro
                string filtro = "";
                switch (FiltroPeriodo)
                {
                    case "Mes":
                        filtro = GetFilterCondition(FiltroPeriodo, Periodo);
                        break;

                    case "Trimestre":
                        filtro = $" AND DATEPART(QUARTER, dt_emissao) = {Trimestre} AND YEAR(dt_emissao) = {Ano}";
                        break;

                    case "Ano":
                        filtro = $" AND YEAR(dt_emissao) = {Ano}";
                        break;
                }

                // Calcular Total Emitidas
                string queryEmitidas = "SELECT ISNULL(SUM(valor_nf), '0') FROM NotaFiscal WHERE 1 = 1";
                queryEmitidas += " " + filtro;

                using (SqlCommand cmd = new SqlCommand(queryEmitidas, conn))
                {
                    model.TotalEmitidas = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                //Calcular Total sem Cobrança
                string querySemCobranca = "SELECT ISNULL(SUM(valor_nf), '0') FROM NotaFiscal WHERE dt_cobranca IS NULL";
                querySemCobranca += " " + filtro;

                using (SqlCommand cmd = new SqlCommand(querySemCobranca, conn))
                {
                    model.TotalSemCobranca = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                // Calcular Total Vencidas
                string queryVencidas = "SELECT ISNULL(SUM(valor_nf), '0') FROM NotaFiscal WHERE dt_cobranca < GETDATE() AND dt_cobranca IS NOT NULL AND dt_pgto IS NULL";
                queryVencidas += " " + filtro;

                using (SqlCommand cmd = new SqlCommand(queryVencidas, conn))
                {
                    model.TotalVencidas = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                // Calcular Total a Vencer
                string queryAVencer = "SELECT ISNULL(SUM(valor_nf), '0') FROM NotaFiscal WHERE dt_cobranca > GETDATE() AND dt_pgto IS NULL";
                queryAVencer += " " + filtro;

                using (SqlCommand cmd = new SqlCommand(queryAVencer, conn))
                {
                    model.TotalAVencer = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                // Calcular Total Pagas
                string queryPagas = "SELECT ISNULL(SUM(valor_nf), '0') FROM NotaFiscal WHERE dt_pgto IS NOT NULL";
                queryPagas += " " + filtro;

                using (SqlCommand cmd = new SqlCommand(queryPagas, conn))
                {
                    model.TotalPagas = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                // Calcular os valores para os gráficos
                // Gráfico de Inadimplência
                string queryInadimplencia = @"
                    WITH Meses AS (
                      SELECT *
                        FROM (VALUES ('01', 0.0), 
                                     ('02', 0.0), 
                                     ('03', 0.0), 
                                     ('04', 0.0), 
                                     ('05', 0.0), 
                                     ('06', 0.0), 
                                     ('07', 0.0), 
                                     ('08', 0.0), 
                                     ('09', 0.0), 
                                     ('10', 0.0), 
                                     ('11', 0.0), 
                                     ('12', 0.0)
                             ) AS V(Mes, Inadimplencia)
                    ) SELECT CONCAT('2023-', Mes) AS MesAno,
                             Inadimplencia
                        FROM Meses 
                       WHERE Mes NOT IN (SELECT CAST(FORMAT(dt_cobranca, 'MM') AS VARCHAR(2)) 
                                           FROM NotaFiscal 
                                          WHERE dt_cobranca IS NOT NULL AND dt_pgto IS NULL)
                      UNION ALL
                      SELECT FORMAT(dt_cobranca, 'yyyy-MM') AS MesAno, SUM(valor_nf) AS TotalInadimplencia
                        FROM NotaFiscal
                       WHERE dt_pgto IS NULL AND dt_cobranca IS NOT NULL
                       GROUP BY FORMAT(dt_cobranca, 'yyyy-MM')
                       ORDER BY MesAno;
                ";

                using (SqlCommand cmd = new SqlCommand(queryInadimplencia, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.MesesInadimplencia.Add(reader.GetString(0));
                            model.ValoresInadimplencia.Add(reader.GetDecimal(1));
                        }
                    }
                }

                // Gráfico de Receita
                string queryReceita = @"
                    WITH Meses AS (
                      SELECT *
                        FROM (VALUES ('01', 0.0), 
                                     ('02', 0.0), 
                                     ('03', 0.0), 
                                     ('04', 0.0), 
                                     ('05', 0.0), 
                                     ('06', 0.0), 
                                     ('07', 0.0), 
                                     ('08', 0.0), 
                                     ('09', 0.0), 
                                     ('10', 0.0), 
                                     ('11', 0.0), 
                                     ('12', 0.0)
                             ) AS V(Mes, TotalReceita)
                    ) SELECT CONCAT('2023-', Mes) AS MesAno,
                             TotalReceita
                        FROM Meses 
                       WHERE Mes NOT IN (SELECT CAST(FORMAT(dt_pgto, 'MM') AS VARCHAR(2)) 
                                           FROM NotaFiscal 
                                          WHERE dt_pgto IS NOT NULL)
                     UNION ALL
                    SELECT FORMAT(dt_pgto, 'yyyy-MM') AS MesAno, SUM(valor_nf) AS TotalReceita
                      FROM NotaFiscal
                     WHERE dt_pgto IS NOT NULL
                     GROUP BY FORMAT(dt_pgto, 'yyyy-MM')
                     ORDER BY MesAno;
                ";

                using (SqlCommand cmd = new SqlCommand(queryReceita, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.MesesReceita.Add(reader.GetString(0));
                            model.ValoresReceita.Add(reader.GetDecimal(1));
                        }
                    }
                }
            }

            return View(model);
        }

        private string GetFilterCondition(string filtroPeriodo, string periodo)
        {
            if (string.IsNullOrEmpty(periodo)) return "";

            if (filtroPeriodo == "Mes")
            {
                var mes = periodo.Split('-')[1];
                var ano = periodo.Split('-')[0];
                return $" AND MONTH(dt_emissao) = {mes} AND YEAR(dt_emissao) = {ano}";
            }
            else if (filtroPeriodo == "Trimestre")
            {
                var trimestre = Convert.ToInt32(periodo.Split('-')[1]) / 3 + 1;
                var ano = periodo.Split('-')[0];
                return $" AND DATEPART(QUARTER, dt_emissao) = {trimestre} AND YEAR(dt_emissao) = {ano}";
            }
            else if (filtroPeriodo == "Ano")
            {
                var ano = periodo;
                return $" AND YEAR(dt_emissao) = {ano}";
            }

            return "";
        }
    }
}
