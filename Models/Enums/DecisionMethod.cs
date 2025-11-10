// Models/Enums/DecisionMethod.cs
namespace RoboticsFixture.Models.Enums
{
    /// <summary>
    /// Define cómo se determina el ganador de un combate.
    /// </summary>
    public enum DecisionMethod
    {
        /// <summary>
        /// Ganador definido automáticamente por la lógica de simulación.
        /// Usado típicamente en modo Autónomo.
        /// </summary>
        Automatic = 0,

        /// <summary>
        /// Ganador cargado manualmente por el juez o usuario.
        /// Usado típicamente en modo Radiocontrol.
        /// </summary>
        Manual = 1
    }
}
