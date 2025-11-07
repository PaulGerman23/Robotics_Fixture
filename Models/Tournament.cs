// Models/Tournament.cs
namespace RoboticsFixture.Models
{
    public class Tournament
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int CurrentRound { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}