// Models/Match.cs
using RoboticsFixture.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoboticsFixture.Models
{
    /// <summary>
    /// Representa un combate entre dos competidores.
    /// Soporta tanto simulación automática (modo Autónomo) como entrada manual (modo Radiocontrol).
    /// </summary>
    public class Match
    {
        public int Id { get; set; }

        public int Round { get; set; }

        public int Position { get; set; }

        public int? Competitor1Id { get; set; }
        [ForeignKey("Competitor1Id")]
        public Competitor? Competitor1 { get; set; }

        public int? Competitor2Id { get; set; }
        [ForeignKey("Competitor2Id")]
        public Competitor? Competitor2 { get; set; }

        public int? WinnerId { get; set; }
        [ForeignKey("WinnerId")]
        public Competitor? Winner { get; set; }

        public bool IsBye { get; set; }

        public bool IsRepechaje { get; set; }

        public bool IsCompleted { get; set; }

        /// <summary>
        /// ID del torneo al que pertenece este combate.
        /// Permite agrupar combates por torneo.
        /// </summary>
        public int? TournamentId { get; set; }
        [ForeignKey("TournamentId")]
        public Tournament? Tournament { get; set; }

        // ========== CAMPOS PARA MODO AUTÓNOMO (SUMO LNR) ==========

        /// <summary>
        /// Número de asaltos (rounds) jugados en el combate.
        /// En modo autónomo: hasta 3 asaltos.
        /// </summary>
        public int RoundsPlayed { get; set; } = 0;

        /// <summary>
        /// Número de asaltos ganados por el Competidor 1.
        /// </summary>
        public int RoundsWonP1 { get; set; } = 0;

        /// <summary>
        /// Número de asaltos ganados por el Competidor 2.
        /// </summary>
        public int RoundsWonP2 { get; set; } = 0;

        /// <summary>
        /// Registro detallado de los resultados de cada asalto.
        /// Formato JSON: [{round: 1, winner: 1}, {round: 2, winner: 2}, ...]
        /// </summary>
        [StringLength(1000)]
        public string? RoundResults { get; set; }

        // ========== CAMPOS PARA AMBOS MODOS ==========

        /// <summary>
        /// Método de decisión del ganador: Automático o Manual.
        /// </summary>
        public DecisionMethod DecisionMethod { get; set; } = DecisionMethod.Automatic;

        /// <summary>
        /// Descripción del resultado del combate.
        /// Para modo radiocontrol: razón de victoria (ej: "3 outs", "inmovilización").
        /// Para modo autónomo: resumen de asaltos.
        /// </summary>
        [StringLength(500)]
        public string? OutcomeDescription { get; set; }

        /// <summary>
        /// Tipo de resultado en modo radiocontrol.
        /// </summary>
        public OutcomeType? OutcomeType { get; set; }

        /// <summary>
        /// Fecha y hora en que se completó el combate.
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Nombre o ID del juez que registró el resultado (modo radiocontrol).
        /// </summary>
        [StringLength(100)]
        public string? JudgeName { get; set; }
    }
}