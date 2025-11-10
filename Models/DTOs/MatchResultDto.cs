// Models/DTOs/MatchResultDto.cs
using RoboticsFixture.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace RoboticsFixture.Models.DTOs
{
    /// <summary>
    /// DTO para registrar manualmente el resultado de un combate en modo radiocontrol.
    /// </summary>
    public class MatchResultDto
    {
        [Required(ErrorMessage = "El ID del combate es requerido")]
        public int MatchId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un ganador")]
        public int WinnerId { get; set; }

        /// <summary>
        /// Tipo de victoria en modo radiocontrol.
        /// </summary>
        public OutcomeType? OutcomeType { get; set; }

        /// <summary>
        /// Descripción adicional del resultado.
        /// Ej: "3 outs consecutivos", "Inmovilización por 10 segundos", etc.
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Nombre del juez que registra el resultado.
        /// </summary>
        [StringLength(100)]
        public string? JudgeName { get; set; }
    }
}