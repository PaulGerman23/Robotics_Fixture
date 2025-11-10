// Services/CombatSimulationService.cs
using RoboticsFixture.Models;
using RoboticsFixture.Models.Enums;
using System.Text.Json;

namespace RoboticsFixture.Services
{
    /// <summary>
    /// Implementación del servicio de simulación de combates autónomos.
    /// Basado en el reglamento de Sumo de la Liga Nacional de Robótica (LNR).
    /// </summary>
    public class CombatSimulationService : ICombatSimulationService
    {
        /// <summary>
        /// Simula un combate autónomo al mejor de 3 asaltos.
        /// El ganador es el primero en ganar 2 asaltos.
        /// </summary>
        public Match SimulateAutonomousMatch(Match match, Competitor competitor1, Competitor competitor2, int seed)
        {
            if (competitor1 == null || competitor2 == null)
            {
                throw new ArgumentException("Ambos competidores deben estar definidos para simular el combate.");
            }

            // Usar semilla derivada del torneo y el número de ronda para reproducibilidad
            var random = new Random(seed + match.Id);

            var roundResults = new List<RoundResult>();
            int roundsWonP1 = 0;
            int roundsWonP2 = 0;
            int roundNumber = 1;

            // Simular hasta que alguien gane 2 asaltos (máximo 3 asaltos)
            while (roundsWonP1 < 2 && roundsWonP2 < 2 && roundNumber <= 3)
            {
                int winnerId = SimulateRound(competitor1, competitor2, random);

                if (winnerId == competitor1.Id)
                {
                    roundsWonP1++;
                }
                else
                {
                    roundsWonP2++;
                }

                roundResults.Add(new RoundResult
                {
                    Round = roundNumber,
                    WinnerId = winnerId,
                    WinnerName = winnerId == competitor1.Id ? competitor1.Name : competitor2.Name
                });

                roundNumber++;
            }

            // Actualizar el combate con los resultados
            match.RoundsPlayed = roundNumber - 1;
            match.RoundsWonP1 = roundsWonP1;
            match.RoundsWonP2 = roundsWonP2;
            match.RoundResults = JsonSerializer.Serialize(roundResults);

            // Determinar el ganador final
            if (roundsWonP1 > roundsWonP2)
            {
                match.WinnerId = competitor1.Id;
                match.OutcomeDescription = $"{competitor1.Name} ganó {roundsWonP1}-{roundsWonP2} en {match.RoundsPlayed} asaltos";
            }
            else
            {
                match.WinnerId = competitor2.Id;
                match.OutcomeDescription = $"{competitor2.Name} ganó {roundsWonP2}-{roundsWonP1} en {match.RoundsPlayed} asaltos";
            }

            match.DecisionMethod = DecisionMethod.Automatic;
            match.IsCompleted = true;
            match.CompletedDate = DateTime.Now;

            return match;
        }

        /// <summary>
        /// Simula un único asalto entre dos competidores.
        /// Usa RatingSeed para calcular probabilidades de victoria.
        /// </summary>
        private int SimulateRound(Competitor competitor1, Competitor competitor2, Random random)
        {
            // Calcular probabilidad basada en RatingSeed
            // Fórmula: P(A gana) = rating_A / (rating_A + rating_B)
            double totalRating = competitor1.RatingSeed + competitor2.RatingSeed;
            double probabilityP1Wins = competitor1.RatingSeed / totalRating;

            // Generar número aleatorio y determinar ganador
            double randomValue = random.NextDouble();

            return randomValue < probabilityP1Wins ? competitor1.Id : competitor2.Id;
        }

        /// <summary>
        /// Clase auxiliar para almacenar resultados de asaltos individuales.
        /// </summary>
        private class RoundResult
        {
            public int Round { get; set; }
            public int WinnerId { get; set; }
            public string WinnerName { get; set; }
        }
    }
}