// Models/Enums/CombatMode.cs
namespace RoboticsFixture.Models.Enums
{
    /// <summary>
    /// Define los modos de combate disponibles en el sistema.
    /// Autónomo: Robots completamente autónomos basados en el reglamento de Sumo LNR.
    /// RadioControl: Robots controlados a distancia basados en el reglamento Batalla Robot.
    /// </summary>
    public enum CombatMode
    {
        /// <summary>
        /// Modo Autónomo: Los robots deben ser completamente autónomos, sin control directo.
        /// Combates de 3 asaltos de hasta 3 minutos, ganador determinado por puntos Yuhkoh.
        /// </summary>
        Autonomous = 0,

        /// <summary>
        /// Modo Radiocontrol: Los robots son comandados a distancia (radiocontrol).
        /// Objetivo: Dejar fuera de combate al contrincante sacándolo del cuadrilátero
        /// o inmovilizándolo por un conteo de diez.
        /// </summary>
        RadioControl = 1
    }
}
