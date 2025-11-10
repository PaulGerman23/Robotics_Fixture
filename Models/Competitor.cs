// Models/Competitor.cs (ACTUALIZADO con RatingSeed)
using System.ComponentModel.DataAnnotations;

namespace RoboticsFixture.Models
{
    public class Competitor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "El equipo es requerido")]
        [StringLength(100)]
        public string Team { get; set; }

        [Required(ErrorMessage = "La categoría es requerida")]
        [StringLength(50)]
        public string Category { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Puntuación de habilidad del competidor (1-100).
        /// Usado para calcular probabilidades en simulaciones de modo autónomo.
        /// Valor por defecto: 50 (habilidad media).
        /// </summary>
        [Range(1, 100)]
        public int RatingSeed { get; set; } = 50;

        /// <summary>
        /// Descripción o notas adicionales sobre el robot.
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }
    }
}