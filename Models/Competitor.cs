// Models/Competitor.cs
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
    }
}
