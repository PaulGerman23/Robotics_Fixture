// Controllers/TournamentController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticsFixture.Data;
using RoboticsFixture.Models;
using RoboticsFixture.Models.Enums;
using RoboticsFixture.Models.DTOs;
using RoboticsFixture.Services;

namespace RoboticsFixture.Controllers
{
    public class TournamentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICombatSimulationService _simulationService;

        public TournamentController(ApplicationDbContext context, ICombatSimulationService simulationService)
        {
            _context = context;
            _simulationService = simulationService;
        }

        public async Task<IActionResult> Index()
        {
            // Obtener categorías activas con sus torneos
            var categories = await _context.Competitors
                .Where(c => c.IsActive)
                .Select(c => c.Category)
                .Distinct()
                .ToListAsync();

            // Obtener torneos activos por categoría
            var tournaments = await _context.Tournaments
                .Where(t => t.IsActive)
                .ToListAsync();

            ViewBag.Tournaments = tournaments;

            return View(categories);
        }

        /// <summary>
        /// Vista para crear un nuevo torneo con selección de modo de combate.
        /// </summary>
        public IActionResult CreateTournament(string category)
        {
            ViewBag.Category = category;
            return View();
        }

        /// <summary>
        /// Crea un nuevo torneo y genera el fixture inicial.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTournament(CreateTournamentDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Category = dto.Category;
                return View(dto);
            }

            // Crear el torneo
            var tournament = new Tournament
            {
                Name = dto.Name,
                Category = dto.Category,
                CombatMode = dto.CombatMode,
                Description = dto.Description,
                IsActive = true,
                CreatedDate = DateTime.Now,
                RandomSeed = new Random().Next(1, 1000000) // Semilla para reproducibilidad
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            // Redirigir a generar fixture
            return RedirectToAction(nameof(GenerateFixture), new { tournamentId = tournament.Id });
        }

        [HttpPost]
        public async Task<IActionResult> GenerateFixture(int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null)
            {
                TempData["Error"] = "Torneo no encontrado";
                return RedirectToAction(nameof(Index));
            }

            var competitors = await _context.Competitors
                .Where(c => c.IsActive && c.Category == tournament.Category)
                .ToListAsync();

            if (competitors.Count < 2)
            {
                TempData["Error"] = "Se necesitan al menos 2 competidores para generar el fixture";
                return RedirectToAction(nameof(Index));
            }

            // Eliminar matches anteriores del torneo
            await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .ExecuteDeleteAsync();

            var random = new Random(tournament.RandomSeed);
            var shuffled = competitors.OrderBy(x => random.Next()).ToList();

            if (shuffled.Count % 2 != 0)
            {
                var twoCompetitors = shuffled.OrderBy(x => random.Next()).Take(2).ToList();
                var repechaje = new Match
                {
                    Round = 0,
                    Position = 0,
                    Competitor1Id = twoCompetitors[0].Id,
                    Competitor2Id = twoCompetitors[1].Id,
                    IsRepechaje = true,
                    IsCompleted = false,
                    TournamentId = tournamentId,
                    DecisionMethod = tournament.CombatMode == CombatMode.Autonomous
                        ? DecisionMethod.Automatic
                        : DecisionMethod.Manual
                };
                _context.Matches.Add(repechaje);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(ShowRepechaje), new { tournamentId, matchId = repechaje.Id });
            }

            // Crear matches de primera ronda
            for (int i = 0; i < shuffled.Count; i += 2)
            {
                var match = new Match
                {
                    Round = 1,
                    Position = (i / 2) + 1,
                    Competitor1Id = shuffled[i].Id,
                    Competitor2Id = shuffled[i + 1].Id,
                    IsRepechaje = false,
                    IsCompleted = false,
                    TournamentId = tournamentId,
                    DecisionMethod = tournament.CombatMode == CombatMode.Autonomous
                        ? DecisionMethod.Automatic
                        : DecisionMethod.Manual
                };
                _context.Matches.Add(match);
            }

            await _context.SaveChangesAsync();

            // Si es modo autónomo, simular automáticamente todos los combates de esta ronda
            if (tournament.CombatMode == CombatMode.Autonomous)
            {
                await SimulateRoundMatches(tournamentId, 1);
            }

            return RedirectToAction(nameof(Fixture), new { tournamentId });
        }

        public async Task<IActionResult> ShowRepechaje(int tournamentId, int matchId)
        {
            var match = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            ViewBag.TournamentId = tournamentId;
            return View(match);
        }

        public async Task<IActionResult> Fixture(int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var matches = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Winner)
                .Where(m => m.TournamentId == tournamentId)
                .OrderBy(m => m.Round)
                .ThenBy(m => m.Position)
                .ToListAsync();

            ViewBag.Tournament = tournament;
            ViewBag.MaxRound = matches.Any() ? matches.Max(m => m.Round) : 0;

            var allCompleted = matches.Any() && matches.Where(m => m.Round == matches.Max(x => x.Round)).All(m => m.IsCompleted);
            var isFinal = matches.Any() && matches.Max(m => m.Round) > 1 &&
                         matches.Where(m => m.Round == matches.Max(x => x.Round)).Count() == 1;

            ViewBag.ShowPodium = allCompleted && isFinal;

            return View(matches);
        }

        [HttpPost]
        public async Task<IActionResult> SetWinner(int matchId, int winnerId)
        {
            var match = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null) return NotFound();

            var tournament = match.Tournament;
            if (tournament == null) return BadRequest("El combate no está asociado a un torneo");

            // Si es modo radiocontrol y no viene de la API, redirigir a la vista de registro manual
            if (tournament.CombatMode == CombatMode.RadioControl && match.DecisionMethod == DecisionMethod.Manual)
            {
                return RedirectToAction(nameof(RecordResult), new { matchId });
            }

            match.WinnerId = winnerId;
            match.IsCompleted = true;
            match.CompletedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            if (match.IsRepechaje && match.Round == 0)
            {
                await HandleRepechajeCompletion(match, tournament);
            }
            else
            {
                await CheckAndAdvanceRound(tournament.Id);
            }

            return RedirectToAction(nameof(Fixture), new { tournamentId = tournament.Id });
        }

        /// <summary>
        /// Vista para registrar manualmente el resultado de un combate en modo radiocontrol.
        /// </summary>
        public async Task<IActionResult> RecordResult(int matchId)
        {
            var match = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null)
            {
                return NotFound();
            }

            if (match.IsCompleted)
            {
                TempData["Info"] = "Este combate ya ha sido completado";
                return RedirectToAction(nameof(Fixture), new { tournamentId = match.TournamentId });
            }

            return View(match);
        }

        /// <summary>
        /// Registra el resultado manual de un combate (modo radiocontrol).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RecordResult(MatchResultDto resultDto)
        {
            if (!ModelState.IsValid)
            {
                var match = await _context.Matches
                    .Include(m => m.Competitor1)
                    .Include(m => m.Competitor2)
                    .Include(m => m.Tournament)
                    .FirstOrDefaultAsync(m => m.Id == resultDto.MatchId);
                return View(match);
            }

            var matchToUpdate = await _context.Matches
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == resultDto.MatchId);

            if (matchToUpdate == null)
            {
                return NotFound();
            }

            matchToUpdate.WinnerId = resultDto.WinnerId;
            matchToUpdate.OutcomeType = resultDto.OutcomeType;
            matchToUpdate.OutcomeDescription = resultDto.Description;
            matchToUpdate.JudgeName = resultDto.JudgeName;
            matchToUpdate.IsCompleted = true;
            matchToUpdate.CompletedDate = DateTime.Now;
            matchToUpdate.DecisionMethod = DecisionMethod.Manual;

            await _context.SaveChangesAsync();

            // Verificar si es repechaje o avanzar ronda
            if (matchToUpdate.IsRepechaje && matchToUpdate.Round == 0)
            {
                await HandleRepechajeCompletion(matchToUpdate, matchToUpdate.Tournament);
            }
            else
            {
                await CheckAndAdvanceRound(matchToUpdate.TournamentId.Value);
            }

            TempData["Success"] = "Resultado registrado exitosamente";
            return RedirectToAction(nameof(Fixture), new { tournamentId = matchToUpdate.TournamentId });
        }

        /// <summary>
        /// Simula automáticamente todos los combates de una ronda (modo autónomo).
        /// </summary>
        private async Task SimulateRoundMatches(int tournamentId, int round)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null || tournament.CombatMode != CombatMode.Autonomous)
            {
                return;
            }

            var matches = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Where(m => m.TournamentId == tournamentId && m.Round == round && !m.IsCompleted)
                .ToListAsync();

            foreach (var match in matches)
            {
                if (match.Competitor1 != null && match.Competitor2 != null)
                {
                    _simulationService.SimulateAutonomousMatch(match, match.Competitor1, match.Competitor2, tournament.RandomSeed);
                }
            }

            await _context.SaveChangesAsync();

            // Avanzar automáticamente a la siguiente ronda si todas las batallas están completadas
            await CheckAndAdvanceRound(tournamentId);
        }

        private async Task HandleRepechajeCompletion(Match repechajeMatch, Tournament tournament)
        {
            var winner = await _context.Competitors.FindAsync(repechajeMatch.WinnerId);

            var allCompetitors = await _context.Competitors
                .Where(c => c.IsActive &&
                       c.Category == tournament.Category &&
                       c.Id != repechajeMatch.Competitor1Id &&
                       c.Id != repechajeMatch.Competitor2Id)
                .ToListAsync();

            allCompetitors.Add(winner);

            var random = new Random(tournament.RandomSeed + repechajeMatch.Id);
            var shuffled = allCompetitors.OrderBy(x => random.Next()).ToList();

            if (shuffled.Count % 2 != 0)
            {
                var twoCompetitors = shuffled.OrderBy(x => random.Next()).Take(2).ToList();
                var newRepechaje = new Match
                {
                    Round = 1,
                    Position = 0,
                    Competitor1Id = twoCompetitors[0].Id,
                    Competitor2Id = twoCompetitors[1].Id,
                    IsRepechaje = true,
                    IsCompleted = false,
                    TournamentId = tournament.Id,
                    DecisionMethod = tournament.CombatMode == CombatMode.Autonomous
                        ? DecisionMethod.Automatic
                        : DecisionMethod.Manual
                };
                _context.Matches.Add(newRepechaje);

                shuffled.Remove(twoCompetitors[0]);
                shuffled.Remove(twoCompetitors[1]);
            }

            for (int i = 0; i < shuffled.Count; i += 2)
            {
                if (i + 1 < shuffled.Count)
                {
                    var newMatch = new Match
                    {
                        Round = 1,
                        Position = (i / 2) + 1,
                        Competitor1Id = shuffled[i].Id,
                        Competitor2Id = shuffled[i + 1].Id,
                        IsRepechaje = false,
                        IsCompleted = false,
                        TournamentId = tournament.Id,
                        DecisionMethod = tournament.CombatMode == CombatMode.Autonomous
                            ? DecisionMethod.Automatic
                            : DecisionMethod.Manual
                    };
                    _context.Matches.Add(newMatch);
                }
            }
            await _context.SaveChangesAsync();

            // Si es autónomo, simular la nueva ronda
            if (tournament.CombatMode == CombatMode.Autonomous)
            {
                await SimulateRoundMatches(tournament.Id, 1);
            }
        }

        private async Task CheckAndAdvanceRound(int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null) return;

            var currentRoundMatches = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Winner)
                .Where(m => m.TournamentId == tournamentId)
                .Where(m => !m.IsRepechaje || m.Round > 0)
                .ToListAsync();

            var maxRound = currentRoundMatches.Max(m => m.Round);
            var roundMatches = currentRoundMatches.Where(m => m.Round == maxRound).ToList();

            if (roundMatches.All(m => m.IsCompleted))
            {
                var winners = roundMatches.Select(m => m.Winner).Where(w => w != null).ToList();

                if (winners.Count <= 1) return;

                var nextRound = maxRound + 1;
                var random = new Random(tournament.RandomSeed + nextRound);

                if (winners.Count % 2 != 0)
                {
                    var twoWinners = winners.OrderBy(x => random.Next()).Take(2).ToList();
                    var repechaje = new Match
                    {
                        Round = nextRound,
                        Position = 0,
                        Competitor1Id = twoWinners[0].Id,
                        Competitor2Id = twoWinners[1].Id,
                        IsRepechaje = true,
                        IsCompleted = false,
                        TournamentId = tournamentId,
                        DecisionMethod = tournament.CombatMode == CombatMode.Autonomous
                            ? DecisionMethod.Automatic
                            : DecisionMethod.Manual
                    };
                    _context.Matches.Add(repechaje);
                    winners.Remove(twoWinners[0]);
                    winners.Remove(twoWinners[1]);
                }

                for (int i = 0; i < winners.Count; i += 2)
                {
                    var match = new Match
                    {
                        Round = nextRound,
                        Position = (i / 2) + 1,
                        Competitor1Id = winners[i].Id,
                        Competitor2Id = winners[i + 1].Id,
                        IsRepechaje = false,
                        IsCompleted = false,
                        TournamentId = tournamentId,
                        DecisionMethod = tournament.CombatMode == CombatMode.Autonomous
                            ? DecisionMethod.Automatic
                            : DecisionMethod.Manual
                    };
                    _context.Matches.Add(match);
                }

                await _context.SaveChangesAsync();

                // Si es autónomo, simular la nueva ronda
                if (tournament.CombatMode == CombatMode.Autonomous)
                {
                    await SimulateRoundMatches(tournamentId, nextRound);
                }
            }
        }

        public async Task<IActionResult> Podium(int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null)
            {
                return RedirectToAction(nameof(Index));
            }

            var matches = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Winner)
                .Where(m => m.TournamentId == tournamentId)
                .OrderByDescending(m => m.Round)
                .ToListAsync();

            if (!matches.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var maxRound = matches.Max(x => x.Round);
            var finalMatch = matches.FirstOrDefault(m => m.Round == maxRound && m.IsCompleted);

            Competitor winner = null;
            Competitor runnerUp = null;
            Competitor thirdPlace = null;

            if (finalMatch != null && finalMatch.WinnerId.HasValue)
            {
                winner = finalMatch.Winner;
                runnerUp = finalMatch.Competitor1Id == finalMatch.WinnerId
                    ? finalMatch.Competitor2
                    : finalMatch.Competitor1;
            }

            if (maxRound > 1)
            {
                var semiFinalRound = maxRound - 1;
                var semiFinalMatches = matches
                    .Where(m => m.Round == semiFinalRound && m.IsCompleted && !m.IsRepechaje)
                    .ToList();

                if (semiFinalMatches.Any())
                {
                    var semifinalLosers = new List<Competitor>();

                    foreach (var semi in semiFinalMatches)
                    {
                        if (semi.WinnerId.HasValue)
                        {
                            var loser = semi.Competitor1Id == semi.WinnerId
                                ? semi.Competitor2
                                : semi.Competitor1;
                            if (loser != null) semifinalLosers.Add(loser);
                        }
                    }

                    thirdPlace = semifinalLosers.FirstOrDefault(c => c != null && c.Id != runnerUp?.Id);
                    if (thirdPlace == null && semifinalLosers.Any())
                    {
                        thirdPlace = semifinalLosers.FirstOrDefault();
                    }
                }
            }

            ViewBag.Tournament = tournament;
            ViewBag.Winner = winner;
            ViewBag.RunnerUp = runnerUp;
            ViewBag.ThirdPlace = thirdPlace;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetTournament(int tournamentId)
        {
            await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .ExecuteDeleteAsync();

            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament != null)
            {
                tournament.CurrentRound = 0;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}