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
                var allCompetitors = await _context.Competitors
                    .Where(c => c.IsActive && c.Category == category)
                    .ToListAsync();

                var random = new Random();
                var shuffled = allCompetitors.OrderBy(x => random.Next()).ToList();

                for (int i = 0; i < shuffled.Count; i += 2)
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

            var finalMatch = matches.FirstOrDefault(m => m.Round == matches.Max(x => x.Round));
            var semiFinalMatches = matches.Where(m => m.Round == matches.Max(x => x.Round) - 1).ToList();

            var winner = finalMatch?.Winner;
            var runnerUp = finalMatch?.Competitor1Id == finalMatch?.WinnerId ?
                          finalMatch?.Competitor2 : finalMatch?.Competitor1;

            Competitor thirdPlace = null;
            if (semiFinalMatches.Count >= 2)
            {
                var semifinalLosers = semiFinalMatches
                    .Select(m => m.Competitor1Id == m.WinnerId ? m.Competitor2 : m.Competitor1)
                    .Where(c => c != null && c.Id != runnerUp?.Id)
                    .ToList();
                thirdPlace = semifinalLosers.FirstOrDefault();
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