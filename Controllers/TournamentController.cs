using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticsFixture.Data;
using RoboticsFixture.Models;

namespace RoboticsFixture.Controllers
{
    public class TournamentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TournamentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Competitors
                .Where(c => c.IsActive)
                .Select(c => c.Category)
                .Distinct()
                .ToListAsync();

            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateFixture(string category)
        {
            var competitors = await _context.Competitors
                .Where(c => c.IsActive && c.Category == category)
                .ToListAsync();

            if (competitors.Count < 2)
            {
                TempData["Error"] = "Se necesitan al menos 2 competidores para generar el fixture";
                return RedirectToAction(nameof(Index));
            }

            await _context.Matches
                .Where(m => (m.Competitor1 != null && m.Competitor1.Category == category) ||
                           (m.Competitor2 != null && m.Competitor2.Category == category))
                .ExecuteDeleteAsync();

            var random = new Random();
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
                    IsCompleted = false
                };
                _context.Matches.Add(repechaje);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(ShowRepechaje), new { category, matchId = repechaje.Id });
            }

            for (int i = 0; i < shuffled.Count; i += 2)
            {
                var match = new Match
                {
                    Round = 1,
                    Position = (i / 2) + 1,
                    Competitor1Id = shuffled[i].Id,
                    Competitor2Id = shuffled[i + 1].Id,
                    IsRepechaje = false,
                    IsCompleted = false
                };
                _context.Matches.Add(match);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Fixture), new { category });
        }

        public async Task<IActionResult> ShowRepechaje(string category, int matchId)
        {
            var match = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            ViewBag.Category = category;
            return View(match);
        }

        public async Task<IActionResult> Fixture(string category)
        {
            var matches = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Winner)
                .Where(m => (m.Competitor1 != null && m.Competitor1.Category == category) ||
                           (m.Competitor2 != null && m.Competitor2.Category == category))
                .OrderBy(m => m.Round)
                .ThenBy(m => m.Position)
                .ToListAsync();

            ViewBag.Category = category;
            ViewBag.MaxRound = matches.Any() ? matches.Max(m => m.Round) : 0;

            var allCompleted = matches.Any() && matches.Where(m => m.Round == matches.Max(x => x.Round)).All(m => m.IsCompleted);
            var isFinal = matches.Any() && matches.Max(m => m.Round) > 1 &&
                         matches.Where(m => m.Round == matches.Max(x => x.Round)).Count() == 1;

            ViewBag.ShowPodium = allCompleted && isFinal;

            var repechajeMatch = matches.FirstOrDefault(m => m.IsRepechaje && !m.IsCompleted);
            ViewBag.HasPendingRepechaje = repechajeMatch != null;

            return View(matches);
        }

        [HttpPost]
        public async Task<IActionResult> SetWinner(int matchId, int winnerId)
        {
            var match = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .FirstOrDefaultAsync(m => m.Id == matchId);

            if (match == null) return NotFound();

            match.WinnerId = winnerId;
            match.IsCompleted = true;
            await _context.SaveChangesAsync();

            var category = match.Competitor1?.Category ?? match.Competitor2?.Category;

            if (match.IsRepechaje && match.Round == 0)
            {
                // Obtener ganador del repechaje
                var winner = await _context.Competitors.FindAsync(winnerId);

                // Obtener todos los competidores activos EXCEPTO los que participaron en el repechaje
                var allCompetitors = await _context.Competitors
                    .Where(c => c.IsActive &&
                           c.Category == category &&
                           c.Id != match.Competitor1Id &&
                           c.Id != match.Competitor2Id)
                    .ToListAsync();

                // Agregar el ganador del repechaje a la lista
                allCompetitors.Add(winner);

                var random = new Random();
                var shuffled = allCompetitors.OrderBy(x => random.Next()).ToList();

                // VALIDACIÓN: Si después de agregar el ganador sigue siendo impar, crear otro repechaje
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
                        IsCompleted = false
                    };
                    _context.Matches.Add(newRepechaje);

                    // Remover los dos competidores del repechaje de la lista
                    shuffled.Remove(twoCompetitors[0]);
                    shuffled.Remove(twoCompetitors[1]);
                }

                // Crear matches normales con los competidores restantes (ahora es número par)
                for (int i = 0; i < shuffled.Count; i += 2)
                {
                    // Verificación adicional por seguridad
                    if (i + 1 < shuffled.Count)
                    {
                        var newMatch = new Match
                        {
                            Round = 1,
                            Position = (i / 2) + 1,
                            Competitor1Id = shuffled[i].Id,
                            Competitor2Id = shuffled[i + 1].Id,
                            IsRepechaje = false,
                            IsCompleted = false
                        };
                        _context.Matches.Add(newMatch);
                    }
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                await CheckAndAdvanceRound(category);
            }

            return RedirectToAction(nameof(Fixture), new { category });
        }

        private async Task CheckAndAdvanceRound(string category)
        {
            var currentRoundMatches = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Winner)
                .Where(m => (m.Competitor1 != null && m.Competitor1.Category == category) ||
                           (m.Competitor2 != null && m.Competitor2.Category == category))
                .Where(m => !m.IsRepechaje || m.Round > 0)
                .ToListAsync();

            var maxRound = currentRoundMatches.Max(m => m.Round);
            var roundMatches = currentRoundMatches.Where(m => m.Round == maxRound).ToList();

            if (roundMatches.All(m => m.IsCompleted))
            {
                var winners = roundMatches.Select(m => m.Winner).Where(w => w != null).ToList();

                if (winners.Count <= 1) return;

                var nextRound = maxRound + 1;
                var random = new Random();

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
                        IsCompleted = false
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
                        IsCompleted = false
                    };
                    _context.Matches.Add(match);
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task<IActionResult> Podium(string category)
        {
            var matches = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Winner)
                .Where(m => (m.Competitor1 != null && m.Competitor1.Category == category) ||
                           (m.Competitor2 != null && m.Competitor2.Category == category))
                .OrderByDescending(m => m.Round)
                .ToListAsync();

            if (!matches.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var maxRound = matches.Max(x => x.Round);
            var finalMatch = matches.FirstOrDefault(m => m.Round == maxRound && m.IsCompleted);

            // Inicializar variables
            Competitor winner = null;
            Competitor runnerUp = null;
            Competitor thirdPlace = null;

            // Obtener ganador y subcampeón de la final
            if (finalMatch != null && finalMatch.WinnerId.HasValue)
            {
                winner = finalMatch.Winner;

                // El subcampeón es el perdedor de la final
                if (finalMatch.Competitor1Id == finalMatch.WinnerId)
                {
                    runnerUp = finalMatch.Competitor2;
                }
                else
                {
                    runnerUp = finalMatch.Competitor1;
                }
            }

            // Obtener tercer lugar de las semifinales
            if (maxRound > 1)
            {
                var semiFinalRound = maxRound - 1;
                var semiFinalMatches = matches
                    .Where(m => m.Round == semiFinalRound && m.IsCompleted && !m.IsRepechaje)
                    .ToList();

                if (semiFinalMatches.Count >= 2)
                {
                    // Obtener los perdedores de las semifinales
                    var semifinalLosers = new List<Competitor>();

                    foreach (var semi in semiFinalMatches)
                    {
                        if (semi.WinnerId.HasValue)
                        {
                            if (semi.Competitor1Id == semi.WinnerId && semi.Competitor2 != null)
                            {
                                semifinalLosers.Add(semi.Competitor2);
                            }
                            else if (semi.Competitor2Id == semi.WinnerId && semi.Competitor1 != null)
                            {
                                semifinalLosers.Add(semi.Competitor1);
                            }
                        }
                    }

                    // El tercer lugar es el perdedor de semifinal que NO es el subcampeón
                    thirdPlace = semifinalLosers
                        .FirstOrDefault(c => c != null && c.Id != runnerUp?.Id);

                    // Si no se encontró, tomar cualquier perdedor disponible
                    if (thirdPlace == null && semifinalLosers.Any())
                    {
                        thirdPlace = semifinalLosers.FirstOrDefault();
                    }
                }
                else if (semiFinalMatches.Count == 1)
                {
                    // Si solo hay una semifinal, el perdedor es el tercer lugar
                    var semi = semiFinalMatches.First();
                    if (semi.WinnerId.HasValue)
                    {
                        if (semi.Competitor1Id == semi.WinnerId && semi.Competitor2 != null)
                        {
                            thirdPlace = semi.Competitor2;
                        }
                        else if (semi.Competitor2Id == semi.WinnerId && semi.Competitor1 != null)
                        {
                            thirdPlace = semi.Competitor1;
                        }
                    }
                }
            }

            // Si no hay tercer lugar de semifinales, buscar en rondas anteriores
            if (thirdPlace == null && maxRound > 2)
            {
                var previousRound = maxRound - 2;
                var previousMatches = matches
                    .Where(m => m.Round == previousRound && m.IsCompleted)
                    .ToList();

                foreach (var match in previousMatches)
                {
                    if (match.WinnerId.HasValue)
                    {
                        Competitor loser = null;
                        if (match.Competitor1Id == match.WinnerId && match.Competitor2 != null)
                        {
                            loser = match.Competitor2;
                        }
                        else if (match.Competitor2Id == match.WinnerId && match.Competitor1 != null)
                        {
                            loser = match.Competitor1;
                        }

                        // Verificar que no sea el ganador ni el subcampeón
                        if (loser != null &&
                            loser.Id != winner?.Id &&
                            loser.Id != runnerUp?.Id)
                        {
                            thirdPlace = loser;
                            break;
                        }
                    }
                }
            }

            ViewBag.Category = category;
            ViewBag.Winner = winner;
            ViewBag.RunnerUp = runnerUp;
            ViewBag.ThirdPlace = thirdPlace;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetTournament(string category)
        {
            await _context.Matches
                .Where(m => (m.Competitor1 != null && m.Competitor1.Category == category) ||
                           (m.Competitor2 != null && m.Competitor2.Category == category))
                .ExecuteDeleteAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}