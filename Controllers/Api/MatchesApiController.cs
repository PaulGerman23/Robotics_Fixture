// Controllers/Api/MatchesApiController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticsFixture.Data;
using RoboticsFixture.Models;
using RoboticsFixture.Models.DTOs;
using RoboticsFixture.Models.Enums;

namespace RoboticsFixture.Controllers.Api
{
    [Route("api/matches")]
    [ApiController]
    public class MatchesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MatchesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registra manualmente el resultado de un combate (usado en modo radiocontrol).
        /// POST /api/matches/{id}/result
        /// </summary>
        [HttpPost("{id}/result")]
        public async Task<IActionResult> SetMatchResult(int id, [FromBody] MatchResultDto resultDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var match = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
            {
                return NotFound(new { message = $"Combate con ID {id} no encontrado" });
            }

            if (match.IsCompleted)
            {
                return BadRequest(new { message = "Este combate ya ha sido completado" });
            }

            // Validar que el ganador sea uno de los competidores del combate
            if (resultDto.WinnerId != match.Competitor1Id && resultDto.WinnerId != match.Competitor2Id)
            {
                return BadRequest(new { message = "El ganador debe ser uno de los competidores del combate" });
            }

            // Actualizar el combate con el resultado manual
            match.WinnerId = resultDto.WinnerId;
            match.DecisionMethod = DecisionMethod.Manual;
            match.OutcomeType = resultDto.OutcomeType;
            match.OutcomeDescription = resultDto.Description;
            match.JudgeName = resultDto.JudgeName;
            match.IsCompleted = true;
            match.CompletedDate = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error al guardar el resultado", error = ex.Message });
            }

            return Ok(new
            {
                message = "Resultado registrado exitosamente",
                match = new
                {
                    match.Id,
                    match.WinnerId,
                    WinnerName = match.WinnerId == match.Competitor1Id ? match.Competitor1?.Name : match.Competitor2?.Name,
                    match.OutcomeDescription,
                    match.CompletedDate
                }
            });
        }

        /// <summary>
        /// Obtiene el detalle de un combate específico.
        /// GET /api/matches/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMatch(int id)
        {
            var match = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Winner)
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
            {
                return NotFound(new { message = $"Combate con ID {id} no encontrado" });
            }

            return Ok(new
            {
                match.Id,
                match.Round,
                match.Position,
                Competitor1 = match.Competitor1 != null ? new
                {
                    match.Competitor1.Id,
                    match.Competitor1.Name,
                    match.Competitor1.Team
                } : null,
                Competitor2 = match.Competitor2 != null ? new
                {
                    match.Competitor2.Id,
                    match.Competitor2.Name,
                    match.Competitor2.Team
                } : null,
                Winner = match.Winner != null ? new
                {
                    match.Winner.Id,
                    match.Winner.Name,
                    match.Winner.Team
                } : null,
                match.IsCompleted,
                match.DecisionMethod,
                match.RoundsPlayed,
                match.RoundsWonP1,
                match.RoundsWonP2,
                match.OutcomeDescription,
                match.OutcomeType,
                match.CompletedDate,
                match.JudgeName,
                Tournament = match.Tournament != null ? new
                {
                    match.Tournament.Id,
                    match.Tournament.Name,
                    match.Tournament.CombatMode
                } : null
            });
        }

        /// <summary>
        /// Obtiene todos los combates pendientes de resultado manual.
        /// GET /api/matches/pending
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingMatches([FromQuery] string? category = null)
        {
            var query = _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Tournament)
                .Where(m => !m.IsCompleted && m.Competitor1Id != null && m.Competitor2Id != null);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(m =>
                    (m.Competitor1 != null && m.Competitor1.Category == category) ||
                    (m.Competitor2 != null && m.Competitor2.Category == category));
            }

            var matches = await query
                .OrderBy(m => m.Round)
                .ThenBy(m => m.Position)
                .ToListAsync();

            return Ok(matches.Select(m => new
            {
                m.Id,
                m.Round,
                m.Position,
                Competitor1 = new
                {
                    m.Competitor1.Id,
                    m.Competitor1.Name,
                    m.Competitor1.Team
                },
                Competitor2 = new
                {
                    m.Competitor2.Id,
                    m.Competitor2.Name,
                    m.Competitor2.Team
                },
                Tournament = m.Tournament != null ? new
                {
                    m.Tournament.Id,
                    m.Tournament.Name,
                    m.Tournament.CombatMode
                } : null
            }));
        }
    }
}