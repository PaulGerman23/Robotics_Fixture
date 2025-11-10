// Models/Tournament.cs
using RoboticsFixture.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace RoboticsFixture.Models
{
    /// <summary>
    /// Representa un torneo que agrupa etapas de competencia con un modo de combate específico.
    /// </summary>
    public class Tournament
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del torneo es requerido")]
        [StringLength(200)]
        public string Name { get; set; }

        [Required(ErrorMessage = "La categoría es requerida")]
        [StringLength(50)]
        public string Category { get; set; }

        /// <summary>
        /// Modo de combate del torneo (Autónomo o Radiocontrol).
        /// Define cómo se ejecutan y resuelven los combates.
        /// </summary>
        public CombatMode CombatMode { get; set; } = CombatMode.Autonomous;

        public int CurrentRound { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Semilla para el generador de números aleatorios.
        /// Permite reproducibilidad en las simulaciones autónomas.
        /// </summary>
        public int RandomSeed { get; set; }

        /// <summary>
        /// Descripción opcional del torneo.
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }
    }
}