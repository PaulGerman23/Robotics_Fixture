namespace RoboticsFixture.Models.Enums
{
    /// <summary>
    /// Estrategia para determinar qué competidores participan en el combate extra
    /// cuando hay un número impar de competidores en una ronda.
    /// </summary>
    public enum ExtraMatchStrategy
    {
        /// <summary>
        /// Los 2 competidores se seleccionan completamente al azar.
        /// </summary>
        Random,

        /// <summary>
        /// Los 2 competidores con menor RatingSeed (peor ranking) se enfrentan.
        /// Si hay empate en el ranking, se sortea aleatoriamente entre los empatados.
        /// </summary>
        ByRanking
    }
}
