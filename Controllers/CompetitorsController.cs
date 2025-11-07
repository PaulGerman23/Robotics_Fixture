// Controllers/CompetitorsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoboticsFixture.Data;
using RoboticsFixture.Models;

namespace RoboticsFixture.Controllers
{
    public class CompetitorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CompetitorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Competitors.Where(c => c.IsActive).ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Competitor competitor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(competitor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(competitor);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var competitor = await _context.Competitors.FindAsync(id);
            if (competitor == null) return NotFound();

            return View(competitor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Competitor competitor)
        {
            if (id != competitor.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(competitor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompetitorExists(competitor.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(competitor);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var competitor = await _context.Competitors.FirstOrDefaultAsync(m => m.Id == id);
            if (competitor == null) return NotFound();

            return View(competitor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var competitor = await _context.Competitors.FindAsync(id);
            if (competitor != null)
            {
                competitor.IsActive = false;
                _context.Update(competitor);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CompetitorExists(int id)
        {
            return _context.Competitors.Any(e => e.Id == id);
        }
    }
}