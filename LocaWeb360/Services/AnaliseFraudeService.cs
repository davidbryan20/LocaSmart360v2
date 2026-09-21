using System;
using System.Collections.Generic;
using System.Linq;

namespace LocaSmart360.Services
{
    public class AnaliseFraudeService
    {
        public (int Score, string Justificativa) Analisar(string statusOrigem, decimal valor, decimal ticketMedio, int hora)
        {
            var motivos = new List<string>();
            int score = 0;

            if (statusOrigem == "Bloqueada")
            {
                score += 50;
                motivos.Add("Status de origem reportado como 'Bloqueada'");
            }

            if (valor > (ticketMedio * 2.0m) && ticketMedio > 0)
            {
                score += 40;
                motivos.Add($"Valor ({valor:C}) muito acima do ticket médio ({ticketMedio:C})");
            }

            if (hora >= 0 && hora <= 5)
            {
                score += 35;
                motivos.Add($"Compra realizada em horário atípico (Madrugada: {hora}h)");
            }

            string justificativa = motivos.Count > 0
                ? string.Join(" | ", motivos)
                : "Operação dentro do padrão esperado";

            return (Math.Min(score, 100), justificativa);
        }
    }
}