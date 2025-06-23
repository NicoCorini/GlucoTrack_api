using GlucoTrack_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Controllers
{
    public class MedicoController : Controller
    {
        private readonly GlucoTrackDBContext _context;

        // Costruttore con Dependency Injection
        public MedicoController(GlucoTrackDBContext context)
        {
            _context = context;
        }

        [HttpGet("doctor/dashboard")]
        public async Task<IActionResult> GetDoctorDashboard([FromQuery] int IdMedico)
        {
            return Ok("TBD");
        }

        [HttpGet("doctor/recent-terapies")]
        public async Task<IActionResult> GetDoctorRecentTerapies([FromQuery] int IdMedico)
        {
            List<TabTerapie> terapieRecenti = await _context.TabTerapie
                .Where(t => t.IdMedico == IdMedico)
                .OrderByDescending(t => t.CreatoIl)
                .Take(10)
                .ToListAsync();

            if (terapieRecenti == null || !terapieRecenti.Any())
                return NotFound("No recent therapies found for this doctor.");

            return Ok(terapieRecenti);
        }

        [HttpGet("doctor/patients")]
        public async Task<ActionResult<List<TabPazienti>>> GetDoctorPatients(
            [FromQuery] int IdMedico,
            [FromQuery] int pagina = 0,
            [FromQuery] string filtroTestuale = "",
            [FromQuery] bool flagSoloPazientiMedico = false,
            [FromQuery] int etaMin = 0,
            [FromQuery] int etaMax = 120,
            [FromQuery] string sesso = "",
            [FromQuery] string statoPaziente = "")
        {
            // Validazione dei parametri
            if (pagina < 0 || etaMin < 0 || etaMax < 0 || etaMin > etaMax)
                return BadRequest("Invalid parameters.");

            IQueryable<TabPazienti> pazienti = _context.TabPazienti;

            // Filtro per pazienti del medico
            if (flagSoloPazientiMedico)
            {
                pazienti = pazienti.Where(p => _context.TabPazientiMedici
                    .Any(pm => pm.IdMedico == IdMedico && pm.IdPaziente == p.IdPaziente));
            }

            // Filtro testuale
            if (!string.IsNullOrEmpty(filtroTestuale))
            {
                pazienti = pazienti.Where(p => _context.TabUtenti
                    .Any(u => u.IdUtente == p.IdUtente &&
                              (u.Nome.Contains(filtroTestuale) ||
                               u.Cognome.Contains(filtroTestuale) ||
                               u.Email.Contains(filtroTestuale))));
            }

            // Filtro per sesso
            if (!string.IsNullOrEmpty(sesso))
            {
                pazienti = pazienti.Where(p => p.Sesso == sesso);
            }

            // Filtro per età
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly dataMin = today.AddYears(-etaMax);
            DateOnly dataMax = today.AddYears(-etaMin);
            pazienti = pazienti.Where(p => p.DataNascita >= dataMin && p.DataNascita <= dataMax);

            // Filtro per stato paziente (se implementato nei modelli)
            if (!string.IsNullOrEmpty(statoPaziente))
            {
                //pazienti = pazienti.Where(p => p.Stato == statoPaziente);
                // TODO: Implementare il filtro per stato paziente se necessario
            }

            // Paginazione
            List<TabPazienti> risultatoRicerca = await pazienti
                .Skip(pagina * 10)
                .Take(10)
                .ToListAsync();

            if (!risultatoRicerca.Any())
                return NotFound("No patients found matching the criteria.");

            return Ok(risultatoRicerca);
        }


        public class TerapiaConProgrammazioni
        {
            public TabTerapie Terapia { get; set; }
            public List<TabProgrammazioneAssunzioni> ProgrammazioniAssunzioni { get; set; }
        }

        [HttpPost("doctor/terapia")]
        public async Task<IActionResult> AddTerapia([FromBody] TerapiaConProgrammazioni terapiaConProgrammazioni)
        {
            if (terapiaConProgrammazioni == null || terapiaConProgrammazioni.Terapia == null)
                return BadRequest("Invalid therapy data.");

            try
            {
                // Aggiungi la terapia
                var terapia = terapiaConProgrammazioni.Terapia;
                _context.TabTerapie.Add(terapia);
                await _context.SaveChangesAsync();

                // Aggiungi le programmazioni di assunzioni con l'ID terapia corretto
                if (terapiaConProgrammazioni.ProgrammazioniAssunzioni != null && terapiaConProgrammazioni.ProgrammazioniAssunzioni.Any())
                {
                    foreach (var programmazione in terapiaConProgrammazioni.ProgrammazioniAssunzioni)
                    {
                        programmazione.IdTerapia = terapia.IdTerapia; // Associa l'ID terapia
                        _context.TabProgrammazioneAssunzioni.Add(programmazione);
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok("Therapy and related schedules added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("doctor/terapia")]
        public async Task<IActionResult> GetTerapia([FromQuery] int idTerapia)
        {
            if (idTerapia <= 0)
                return BadRequest("Invalid therapy ID.");

            var terapia = await _context.TabTerapie
                .Where(t => t.IdTerapia == idTerapia)
                .Select(t => new
                {
                    t.IdTerapia,
                    t.Indicazioni,
                    t.DataInizio,
                    t.DataFine,
                    t.IdMedico,
                    t.IdPaziente,
                    ProgrammazioniAssunzioni = _context.TabProgrammazioneAssunzioni
                        .Where(pa => pa.IdTerapia == t.IdTerapia)
                        .Select(pa => new
                        {
                            pa.Farmaco,
                            pa.QuantitaPrevistaN,
                            pa.QuantitaPrevistaUn,
                            pa.DataOraPrevista
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (terapia == null)
                return NotFound("Therapy not found.");

            return Ok(terapia);
        }

        [HttpPut("doctor/terapia")]
        public async Task<IActionResult> UpdateTerapia([FromBody] TerapiaConProgrammazioni terapiaConProgrammazioni)
        {
            if (terapiaConProgrammazioni == null || terapiaConProgrammazioni.Terapia == null || terapiaConProgrammazioni.Terapia.IdTerapia <= 0)
                return BadRequest("Invalid therapy data.");

            var existingTerapia = await _context.TabTerapie
                .FirstOrDefaultAsync(t => t.IdTerapia == terapiaConProgrammazioni.Terapia.IdTerapia);

            if (existingTerapia == null)
                return NotFound("Therapy not found.");

            try
            {
                // Disattiva la terapia precedente
                existingTerapia.DataFine = DateOnly.FromDateTime(DateTime.UtcNow);
                _context.TabTerapie.Update(existingTerapia);

                // Crea una nuova terapia
                var nuovaTerapia = terapiaConProgrammazioni.Terapia;
                nuovaTerapia.IdTerapia = 0; // Reset dell'ID per creare una nuova terapia
                nuovaTerapia.DataInizio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
                _context.TabTerapie.Add(nuovaTerapia);
                await _context.SaveChangesAsync();

                // Aggiungi le programmazioni di assunzioni con l'ID terapia corretto
                if (terapiaConProgrammazioni.ProgrammazioniAssunzioni != null && terapiaConProgrammazioni.ProgrammazioniAssunzioni.Any())
                {
                    foreach (var programmazione in terapiaConProgrammazioni.ProgrammazioniAssunzioni)
                    {
                        programmazione.IdTerapia = nuovaTerapia.IdTerapia; // Associa l'ID terapia
                        _context.TabProgrammazioneAssunzioni.Add(programmazione);
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok("Therapy updated successfully as a new therapy with related schedules.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("doctor/terapia")]
        public async Task<IActionResult> DeleteTerapia([FromQuery] int idTerapia)
        {
            if (idTerapia <= 0)
                return BadRequest("Invalid therapy ID.");

            var terapia = await _context.TabTerapie
                .FirstOrDefaultAsync(t => t.IdTerapia == idTerapia);

            if (terapia == null)
                return NotFound("Therapy not found.");

            try
            {
                terapia.DataFine = DateOnly.FromDateTime(DateTime.UtcNow); // Simula la disattivazione
                _context.TabTerapie.Update(terapia);
                await _context.SaveChangesAsync();
                return Ok("Therapy marked as inactive.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
