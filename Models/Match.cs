// Models/Match.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace RoboticsFixture.Models
{
    public class Match
    {
        public int Id { get; set; }
        public int Round { get; set; }
        public int Position { get; set; }

        public int? Competitor1Id { get; set; }
        [ForeignKey("Competitor1Id")]
        public Competitor Competitor1 { get; set; }

        public int? Competitor2Id { get; set; }
        [ForeignKey("Competitor2Id")]
        public Competitor Competitor2 { get; set; }

        public int? WinnerId { get; set; }
        [ForeignKey("WinnerId")]
        public Competitor Winner { get; set; }

        public bool IsBye { get; set; }
        public bool IsCompleted { get; set; }
    }
}