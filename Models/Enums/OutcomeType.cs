// Models/Enums/OutcomeType.cs
namespace RoboticsFixture.Models.Enums
{
    /// <summary>
    /// Define los tipos de resultado posibles en un combate de radiocontrol.
    /// </summary>
    public enum OutcomeType
    {
        /// <summary>
        /// Victoria por tres salidas del cuadrilátero (3 outs)
        /// </summary>
        ThreeOuts = 0,

        /// <summary>
        /// Victoria por inmovilización del oponente (conteo de 10)
        /// </summary>
        Immobilization = 1,

        /// <summary>
        /// Victoria por volcado del oponente
        /// </summary>
        Overturn = 2,

        /// <summary>
        /// Victoria por descalificación del oponente
        /// </summary>
        Disqualification = 3,

        /// <summary>
        /// Victoria por decisión de los jueces (puntos)
        /// </summary>
        JudgeDecision = 4
    }
}