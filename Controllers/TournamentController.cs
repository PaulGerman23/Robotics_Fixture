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
                RandomSeed = new Random().Next(1, 1000000), // Semilla para reproducibilidad
                ExtraMatchStrategy = dto.ExtraMatchStrategy
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            // Generar fixture
            var (success, repechajeId) = await ProcessFixtureGeneration(tournament.Id);
            if (!success) return RedirectToAction(nameof(Index));

            if (repechajeId.HasValue)
            {
                return RedirectToAction(nameof(ShowRepechaje), new { tournamentId = tournament.Id, matchId = repechajeId.Value });
            }

            return RedirectToAction(nameof(Fixture), new { tournamentId = tournament.Id });
        }

        [HttpPost]
        public async Task<IActionResult> GenerateFixture(int tournamentId)
        {
            var (success, repechajeId) = await ProcessFixtureGeneration(tournamentId);

            if (!success) return RedirectToAction(nameof(Index));

            if (repechajeId.HasValue)
            {
                return RedirectToAction(nameof(ShowRepechaje), new { tournamentId, matchId = repechajeId.Value });
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
            // Excluir Round 0 (Play-In) del cálculo de maxRound
            var regularMatches = matches.Where(m => m.Round > 0).ToList();
            ViewBag.MaxRound = regularMatches.Any() ? regularMatches.Max(m => m.Round) : 0;

            // Verificar si hay Play-In pendiente
            var pendingPlayIn = matches.Any(m => m.Round == 0 && m.IsExtraMatch && !m.IsCompleted);
            
            // Solo considerar matches regulares (Round > 0) para el cálculo de Final
            var maxRegularRound = regularMatches.Any() ? regularMatches.Max(m => m.Round) : 0;
            var finalRoundMatches = regularMatches.Where(m => m.Round == maxRegularRound).ToList();
            var allCompleted = finalRoundMatches.Any() && finalRoundMatches.All(m => m.IsCompleted);
            var isFinal = maxRegularRound > 0 && finalRoundMatches.Count == 1;

            // NO mostrar podio si hay Play-In pendiente
            ViewBag.ShowPodium = allCompleted && isFinal && !pendingPlayIn;

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

            // Verificar si es combate extra de Round 0 vs repechaje completo
            if (match.IsRepechaje && match.Round == 0 && match.Position == 1)
            {
                // Combate extra generado por número impar - NO regenerar todo
                await HandleExtraMatchCompletion(match, tournament);
            }
            else if (match.IsRepechaje && match.Round == 0)
            {
                // Repechaje tradicional - regenerar fixture completo
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

            // Verificar si es combate extra de Round 0 vs repechaje completo
            if (matchToUpdate.IsRepechaje && matchToUpdate.Round == 0 && matchToUpdate.Position == 1)
            {
                // Combate extra generado por número impar - NO regenerar todo
                await HandleExtraMatchCompletion(matchToUpdate, matchToUpdate.Tournament!);
            }
            else if (matchToUpdate.IsRepechaje && matchToUpdate.Round == 0)
            {
                // Repechaje tradicional - regenerar fixture completo
                await HandleRepechajeCompletion(matchToUpdate, matchToUpdate.Tournament!);
            }
            else
            {
                await CheckAndAdvanceRound(matchToUpdate.TournamentId!.Value);
            }

            TempData["Success"] = "Resultado registrado exitosamente";
            return RedirectToAction(nameof(Fixture), new { tournamentId = matchToUpdate.TournamentId });
        }

        // NOTA: SimulateRoundMatches eliminado - el Modo Autónomo ahora usa selección manual de ganadores

        /// <summary>
        /// Maneja la finalización de un combate extra (Play-In) en Round 0.
        /// El ganador se inserta en el match pendiente de Round 1+ que tenga un slot vacío.
        /// </summary>
        private async Task HandleExtraMatchCompletion(Match extraMatch, Tournament tournament)
        {
            if (!extraMatch.WinnerId.HasValue)
            {
                return;
            }

            // =====================================================
            // INSERTAR GANADOR EN EL FLUJO DEL TORNEO
            // =====================================================
            var winner = await _context.Competitors.FindAsync(extraMatch.WinnerId);
            if (winner == null)
            {
                Console.WriteLine($"[ERROR] No se encontró el ganador del Play-In");
                return;
            }

            Console.WriteLine($"[PLAY-IN COMPLETADO] Ganador: {winner.Name}");

            // Buscar un match en Round 1+ que esté esperando un competidor (slot vacío)
            var matchNeedingWinner = await _context.Matches
                .Where(m => m.TournamentId == tournament.Id 
                         && m.Round >= 1 
                         && !m.IsCompleted
                         && (m.Competitor1Id == null || m.Competitor2Id == null))
                .OrderBy(m => m.Round)
                .ThenBy(m => m.Position)
                .FirstOrDefaultAsync();

            if (matchNeedingWinner != null)
            {
                // Asignar el ganador al slot vacío
                if (matchNeedingWinner.Competitor1Id == null)
                {
                    matchNeedingWinner.Competitor1Id = winner.Id;
                    Console.WriteLine($"[PLAY-IN] Ganador asignado a Match {matchNeedingWinner.Id} (Slot Competitor1)");
                }
                else if (matchNeedingWinner.Competitor2Id == null)
                {
                    matchNeedingWinner.Competitor2Id = winner.Id;
                    Console.WriteLine($"[PLAY-IN] Ganador asignado a Match {matchNeedingWinner.Id} (Slot Competitor2)");
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                Console.WriteLine($"[PLAY-IN] No hay match pendiente con slot vacío - el ganador avanzará en la próxima ronda");
            }

            // Ahora sí, intentar avanzar (si corresponde)
            await CheckAndAdvanceRound(tournament.Id);
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

            if (winner != null)
            {
                allCompetitors.Add(winner);
            }

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
                    // CAMBIO: Siempre Manual - el usuario selecciona el ganador
                    DecisionMethod = DecisionMethod.Manual
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
                        // CAMBIO: Siempre Manual - el usuario selecciona el ganador
                        DecisionMethod = DecisionMethod.Manual
                    };
                    _context.Matches.Add(newMatch);
                }
            }
            await _context.SaveChangesAsync();
            // NOTA: Ya no se simula automáticamente - el usuario selecciona los ganadores
        }

        private async Task CheckAndAdvanceRound(int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null) return;

            // =====================================================
            // REGLA CRÍTICA: NO avanzar si hay Play-In pendiente
            // =====================================================
            var pendingPlayIn = await _context.Matches
                .AnyAsync(m => m.TournamentId == tournamentId 
                            && m.IsExtraMatch 
                            && !m.IsCompleted);
            
            if (pendingPlayIn)
            {
                Console.WriteLine("[CHECKPOINT] Play-In pendiente detectado - NO se puede avanzar ronda");
                return;
            }

            var currentRoundMatches = await _context.Matches
                .Include(m => m.Competitor1)
                .Include(m => m.Competitor2)
                .Include(m => m.Winner)
                .Where(m => m.TournamentId == tournamentId)
                // Excluir Round 0 (Play-In) del cálculo
                .Where(m => m.Round > 0)
                .ToListAsync();

            // Verificar que hay matches para procesar
            if (!currentRoundMatches.Any()) return;

            var maxRound = currentRoundMatches.Max(m => m.Round);
            var roundMatches = currentRoundMatches.Where(m => m.Round == maxRound).ToList();

            if (roundMatches.All(m => m.IsCompleted))
            {
                var winners = roundMatches.Select(m => m.Winner).Where(w => w != null).Cast<Competitor>().ToList();

                if (winners.Count <= 1) return;

                var nextRound = maxRound + 1;
                var random = new Random(tournament.RandomSeed + nextRound);

                if (winners.Count % 2 != 0)
                {
                    // Generar combate extra para determinar quién avanza
                    var extraMatchPlayers = SelectExtraMatchPlayers(winners, tournament.ExtraMatchStrategy, random, tournamentId, maxRound);
                    
                    if (extraMatchPlayers != null)
                    {
                        // Logging del combate extra
                        Console.WriteLine($"[COMBATE EXTRA] Generando combate extra para Round {nextRound}");
                        Console.WriteLine($"[COMBATE EXTRA] Competidores: {extraMatchPlayers.Value.Item1.Name} vs {extraMatchPlayers.Value.Item2.Name}");
                        Console.WriteLine($"[COMBATE EXTRA] Estrategia: {tournament.ExtraMatchStrategy}");

                        // Crear combate extra con Position = 0 para identificarlo
                        var extraMatch = new Match
                        {
                            Round = nextRound,
                            Position = 0, // Position 0 indica combate extra
                            Competitor1Id = extraMatchPlayers.Value.Item1.Id,
                            Competitor2Id = extraMatchPlayers.Value.Item2.Id,
                            IsBye = false, // Ya no es BYE
                            IsRepechaje = true, // Mantener para compatibilidad interna
                            IsExtraMatch = true, // CAMPO PRINCIPAL para UI y lógica nueva
                            IsCompleted = false,
                            TournamentId = tournamentId,
                            // CAMBIO: Siempre Manual - el usuario selecciona el ganador
                            DecisionMethod = DecisionMethod.Manual
                        };
                        _context.Matches.Add(extraMatch);
                        await _context.SaveChangesAsync();
                        
                        // Combate extra creado - el usuario debe seleccionar el ganador manualmente
                        // Esperar a que se complete antes de continuar
                        return;
                    }
                }

                for (int i = 0; i < winners.Count; i += 2)
                {
                    // Asegurar par (aunque debería serlo tras sacar el BYE)
                    if (i + 1 < winners.Count) 
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
                            // CAMBIO: Siempre Manual - el usuario selecciona el ganador
                            DecisionMethod = DecisionMethod.Manual
                        };
                        _context.Matches.Add(match);
                    }
                }

                await _context.SaveChangesAsync();
                // NOTA: Ya no se simula automáticamente - el usuario selecciona los ganadores
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

            Competitor? winner = null;
            Competitor? runnerUp = null;
            Competitor? thirdPlace = null;

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

        private async Task<(bool Success, int? RepechajeMatchId)> ProcessFixtureGeneration(int tournamentId)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament == null)
            {
                TempData["Error"] = "Torneo no encontrado";
                return (false, null);
            }

            var competitors = await _context.Competitors
                .Where(c => c.IsActive && c.Category == tournament.Category)
                .ToListAsync();

            if (competitors.Count < 2)
            {
                TempData["Error"] = "Se necesitan al menos 2 competidores para generar el fixture";
                return (false, null);
            }

            // Eliminar matches anteriores del torneo
            await _context.Matches
                .Where(m => m.TournamentId == tournamentId)
                .ExecuteDeleteAsync();

            // Resetear ExtraMatchCount de todos los competidores (regla de justicia por torneo)
            foreach (var competitor in competitors)
            {
                competitor.ExtraMatchCount = 0;
            }
            await _context.SaveChangesAsync();

            var random = new Random(tournament.RandomSeed);
            var playersToMatch = new List<Competitor>(competitors);
            
            // Verificar si es impar - generar combate extra en Round 0
            if (playersToMatch.Count % 2 != 0)
            {
                var extraMatchPlayers = SelectExtraMatchPlayers(playersToMatch, tournament.ExtraMatchStrategy, random);
                
                if (extraMatchPlayers != null)
                {
                    // Logging del combate extra
                    Console.WriteLine($"[COMBATE EXTRA] Generando combate extra para Round 0");
                    Console.WriteLine($"[COMBATE EXTRA] Competidores: {extraMatchPlayers.Value.Item1.Name} vs {extraMatchPlayers.Value.Item2.Name}");
                    Console.WriteLine($"[COMBATE EXTRA] Estrategia: {tournament.ExtraMatchStrategy}");

                    // Crear combate extra en Round 0
                    var extraMatch = new Match
                    {
                        Round = 0, // Round 0 indica combate extra previo
                        Position = 1,
                        Competitor1Id = extraMatchPlayers.Value.Item1.Id,
                        Competitor2Id = extraMatchPlayers.Value.Item2.Id,
                        IsBye = false, // Ya no es BYE, es un combate real
                        IsRepechaje = true, // Mantener para compatibilidad interna
                        IsExtraMatch = true, // CAMPO PRINCIPAL para UI y lógica nueva
                        IsCompleted = false,
                        TournamentId = tournamentId,
                        // CAMBIO: Siempre Manual - el usuario selecciona el ganador
                        DecisionMethod = DecisionMethod.Manual
                    };
                    _context.Matches.Add(extraMatch);
                    await _context.SaveChangesAsync();
                    
                    // Play-In creado - el usuario debe resolver este combate manualmente
                    // Comportamiento unificado para Autónomo y RadioControl
                    TempData["Info"] = "Se ha generado un combate extra (Play-In). Complete este combate para continuar.";
                    
                    // Remover los 2 competidores del combate extra de la lista
                    playersToMatch.Remove(extraMatchPlayers.Value.Item1);
                    playersToMatch.Remove(extraMatchPlayers.Value.Item2);
                    
                    // CREAR UN MATCH CON SLOT VACÍO para el ganador del Play-In
                    // Tomamos el último competidor y lo dejamos esperando
                    var lastPlayer = playersToMatch.LastOrDefault();
                    if (lastPlayer != null)
                    {
                        playersToMatch.Remove(lastPlayer);
                        
                        // Crear match con slot vacío (Competitor2 = null)
                        var matchWaitingForPlayIn = new Match
                        {
                            Round = 1,
                            Position = 0, // Se ajustará después
                            Competitor1Id = lastPlayer.Id,
                            Competitor2Id = null, // Slot vacío para el ganador del Play-In
                            IsRepechaje = false,
                            IsCompleted = false,
                            TournamentId = tournamentId,
                            DecisionMethod = DecisionMethod.Manual
                        };
                        _context.Matches.Add(matchWaitingForPlayIn);
                    }
                }
            }

            // Barajar los jugadores restantes
            var shuffled = playersToMatch.OrderBy(x => random.Next()).ToList();

            // Crear matches de primera ronda
            for (int i = 0; i < shuffled.Count; i += 2)
            {
                if (i + 1 < shuffled.Count) // Asegurar par
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
                        // CAMBIO: Siempre Manual - el usuario selecciona el ganador
                        DecisionMethod = DecisionMethod.Manual
                    };
                    _context.Matches.Add(match);
                }
            }

            await _context.SaveChangesAsync();

            // NO simular automáticamente los combates regulares
            // Los combates extra ya fueron simulados si era necesario

            return (true, null);
        }

        /// <summary>
        /// Selecciona los 2 competidores que participarán en el combate extra.
        /// 
        /// ORDEN DE PRIORIDAD:
        /// 1. Menor ExtraMatchCount (regla de justicia)
        /// 2. Peor ranking dinámico (solo si es válido - todos con mismos combates)
        /// 3. Peor RatingSeed (fallback)
        /// 4. Aleatorio (último recurso para desempatar)
        /// </summary>
        private (Competitor, Competitor)? SelectExtraMatchPlayers(
            List<Competitor> candidates, 
            ExtraMatchStrategy strategy, 
            Random random,
            int tournamentId = 0,
            int currentRound = 0)
        {
            if (candidates.Count < 2) return null;

            // ==================================================================
            // PASO 1: REGLA DE JUSTICIA - Priorizar los que menos han jugado extra
            // ==================================================================
            var minExtraCount = candidates.Min(c => c.ExtraMatchCount);
            var eligibleCandidates = candidates.Where(c => c.ExtraMatchCount == minExtraCount).ToList();
            
            if (eligibleCandidates.Count < 2)
            {
                var remaining = candidates.Except(eligibleCandidates).OrderBy(c => c.ExtraMatchCount);
                eligibleCandidates.AddRange(remaining.Take(2 - eligibleCandidates.Count));
            }

            // Si quedan exactamente 2, ya tenemos los seleccionados
            if (eligibleCandidates.Count == 2)
            {
                eligibleCandidates[0].ExtraMatchCount++;
                eligibleCandidates[1].ExtraMatchCount++;
                Console.WriteLine($"[COMBATE EXTRA] Seleccionados por regla de justicia: {eligibleCandidates[0].Name}, {eligibleCandidates[1].Name}");
                return (eligibleCandidates[0], eligibleCandidates[1]);
            }

            // ==================================================================
            // PASO 2: Aplicar estrategia (Random o ByRanking) sobre elegibles
            // ==================================================================
            Competitor selected1, selected2;
            
            if (strategy == ExtraMatchStrategy.Random)
            {
                var shuffled = eligibleCandidates.OrderBy(x => random.Next()).ToList();
                selected1 = shuffled[0];
                selected2 = shuffled[1];
                Console.WriteLine($"[COMBATE EXTRA] Seleccionados por sorteo aleatorio");
            }
            else // ByRanking
            {
                // ==================================================================
                // RANKING: Usar RatingSeed como criterio (ranking dinámico futuro)
                // En rondas intermedias todos pasaron mismas rondas, usar RatingSeed
                // Los combates extra NO cuentan para ranking dinámico
                // ==================================================================
                bool useDynamicRanking = CanUseDynamicRanking(eligibleCandidates, tournamentId, currentRound);
                
                List<Competitor> sortedCandidates;
                
                if (useDynamicRanking)
                {
                    // Usar victorias del torneo actual como ranking dinámico
                    sortedCandidates = eligibleCandidates
                        .OrderBy(c => GetTournamentWins(c.Id, tournamentId))
                        .ThenBy(c => c.RatingSeed)
                        .ToList();
                    Console.WriteLine($"[RANKING] Usando ranking dinámico (victorias del torneo)");
                }
                else
                {
                    // Fallback: usar RatingSeed (seed inicial)
                    sortedCandidates = eligibleCandidates.OrderBy(c => c.RatingSeed).ToList();
                    Console.WriteLine($"[RANKING] Usando RatingSeed (no hay ranking dinámico válido)");
                }
                
                // Manejar empates con random
                var minRating = sortedCandidates.First().RatingSeed;
                var lowestRated = sortedCandidates.Where(c => c.RatingSeed == minRating).ToList();
                
                if (lowestRated.Count >= 2)
                {
                    var shuffled = lowestRated.OrderBy(x => random.Next()).ToList();
                    selected1 = shuffled[0];
                    selected2 = shuffled[1];
                }
                else
                {
                    selected1 = sortedCandidates[0];
                    selected2 = sortedCandidates[1];
                }
                Console.WriteLine($"[COMBATE EXTRA] Seleccionados por ranking: {selected1.Name} (Rating:{selected1.RatingSeed}), {selected2.Name} (Rating:{selected2.RatingSeed})");
            }
            
            // ==================================================================
            // PASO 3: Incrementar ExtraMatchCount de los seleccionados
            // ==================================================================
            selected1.ExtraMatchCount++;
            selected2.ExtraMatchCount++;
            
            return (selected1, selected2);
        }

        /// <summary>
        /// Verifica si se puede usar ranking dinámico.
        /// CONDICIÓN: Todos los competidores deben tener el mismo número de combates jugados.
        /// </summary>
        private bool CanUseDynamicRanking(List<Competitor> candidates, int tournamentId, int currentRound)
        {
            if (currentRound <= 1 || tournamentId == 0) return false;
            
            var matchCounts = candidates.Select(c => GetMatchesPlayed(c.Id, tournamentId)).Distinct().ToList();
            
            // Solo es válido si TODOS tienen exactamente el mismo número de combates
            return matchCounts.Count == 1 && matchCounts[0] > 0;
        }

        /// <summary>
        /// Obtiene el número de victorias del competidor en el torneo actual.
        /// (Los combates extra NO suman al ranking)
        /// </summary>
        private int GetTournamentWins(int competitorId, int tournamentId)
        {
            return _context.Matches
                .Count(m => m.TournamentId == tournamentId 
                         && m.WinnerId == competitorId 
                         && m.IsCompleted 
                         && !m.IsExtraMatch);
        }

        /// <summary>
        /// Obtiene el número de combates jugados por el competidor en el torneo.
        /// </summary>
        private int GetMatchesPlayed(int competitorId, int tournamentId)
        {
            return _context.Matches
                .Count(m => m.TournamentId == tournamentId 
                         && m.IsCompleted 
                         && !m.IsExtraMatch
                         && (m.Competitor1Id == competitorId || m.Competitor2Id == competitorId));
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