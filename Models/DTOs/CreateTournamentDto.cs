// Models/DTOs/CreateTournamentDto.cs
using RoboticsFixture.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace RoboticsFixture.Models.DTOs
{
    /// <summary>
    /// DTO para crear un nuevo torneo con modo de combate específico.
    /// </summary>
    public class CreateTournamentDto
    {
        [Required(ErrorMessage = "El nombre del torneo es requerido")]
        [StringLength(200)]
        public required string Name { get; set; }

        [Required(ErrorMessage = "La categoría es requerida")]
        [StringLength(50)]
        public required string Category { get; set; }

        /// <summary>
        /// Modo de combate del torneo.
        /// </summary>
        [Required(ErrorMessage = "Debe seleccionar un modo de combate")]
        public CombatMode CombatMode { get; set; }

        /// <summary>
        /// Descripción opcional del torneo.
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Estrategia para determinar qué competidores participan en el combate extra
        /// cuando hay un número impar de competidores.
        /// </summary>
        [Required(ErrorMessage = "Debe seleccionar una estrategia de combate extra")]
        public ExtraMatchStrategy ExtraMatchStrategy { get; set; }

    }
}