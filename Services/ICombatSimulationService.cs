// Services/ICombatSimulationService.cs
using RoboticsFixture.Models;

namespace RoboticsFixture.Services
{
    /// <summary>
    /// Servicio para simular combates en modo autónomo.
    /// </summary>
    public interface ICombatSimulationService
    {
        /// <summary>
        /// Simula un combate completo en modo autónomo (mejor de 3 asaltos).
        /// </summary>
        /// <param name="match">Combate a simular</param>
        /// <param name="competitor1">Competidor 1</param>
        /// <param name="competitor2">Competidor 2</param>
        /// <param name="seed">Semilla para reproducibilidad</param>
        /// <returns>Combate actualizado con resultados</returns>
        Match SimulateAutonomousMatch(Match match, Competitor competitor1, Competitor competitor2, int seed);
    }
}
