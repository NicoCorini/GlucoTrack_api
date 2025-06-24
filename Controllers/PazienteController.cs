using GlucoTrack_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PazienteController : Controller
    {
        private readonly GlucoTrackDBContext _context;

        // Costruttore con Dependency Injection
        public PazienteController(GlucoTrackDBContext context)
        {
            _context = context;
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetPatientInfo([FromQuery] int idUtente)
        {
            if (idUtente <= 0)
                return BadRequest("Invalid user ID.");

            // Recupera i dati dell'utente
            var utente = await _context.TabUtenti
                .FirstOrDefaultAsync(u => u.IdUtente == idUtente);

            if (utente == null)
                return NotFound("User not found.");

            // Recupera il medico attuale
            int idMedicoAttuale = await _context.TabPazientiMedici
                .Where(pm => pm.IdPaziente == idUtente && pm.Al == null)
                .Select(m => m.IdMedico)
                .FirstOrDefaultAsync();

            var medicoAttuale = await _context.TabUtenti
                .FirstOrDefaultAsync(x => x.IdUtente == idMedicoAttuale);

            // Recupera i fattori di rischio
            List<int> idFattoriRischioPaziente = await _context.TabPazientiFattoriRischio
                .Where(fr => fr.IdUtente == idUtente)
                .Select(fr => fr.IdFattoreRischio)
                .ToListAsync();

            var fattoriRischio = await _context.TabFattoriRischio
                .Where(fr => idFattoriRischioPaziente.Contains(fr.IdFattoreRischio))
                .ToListAsync();

            // Recupera le comorbidità
            var comorbidita = await _context.TabComorbiditaPazienti
                .Where(c => c.IdUtente == idUtente)
                .ToListAsync();

            // Recupera le terapie
            var terapie = await _context.TabTerapie
                .Where(t => t.IdUtente == idUtente)
                .ToListAsync();

            // Composizione della risposta
            var response = new
            {
                Paziente = new
                {
                    utente.Nome,
                    utente.Cognome,
                    utente.Email,
                    utente.CreatoIl,
                    utente.UltimoAccesso
                },
                MedicoAttuale = medicoAttuale,
                FattoriRischio = fattoriRischio,
                Comorbidita = comorbidita,
                Terapie = terapie
            };

            return Ok(response);
        }

        [HttpGet("glicemic-resume")]
        public async Task<IActionResult> GetGlicemicResume([FromQuery] int idUtente)
        {
            if (idUtente <= 0)
                return BadRequest("Invalid user ID.");

            // Recupera i dati dell'utente
            var utente = await _context.TabUtenti
                .FirstOrDefaultAsync(u => u.IdUtente == idUtente);

            if (utente == null)
                return NotFound("User not found.");

            // Recupera le misurazioni glicemiche
            var misurazioni = await _context.TabMisurazioniGlicemia
                .Where(m => m.IdUtente == idUtente)
                .ToListAsync();

            // Giorni in italiano
            string[] giorniSettimana = { "dom", "lun", "mar", "mer", "gio", "ven", "sab" }; 

            DateTime oggi = DateTime.Today;
            DateTime setteGiorniFa = oggi.AddDays(-6); // 6 giorni fa + oggi

            var result = new List<object>();
            for (int i = 0; i < 7; i++)
            {
                var giorno = setteGiorniFa.AddDays(i);
                var misurazioniGiorno = misurazioni.Where(m => m.MisuratoIl.Date == giorno.Date).ToList();
                double media = misurazioniGiorno.Any() ? misurazioniGiorno.Average(m => m.Valore) : 0;
                string giornoItaliano = giorniSettimana[(int)giorno.DayOfWeek];
                result.Add(new { giorno = giornoItaliano, media = Math.Round(media, 2) });
            }

            return Ok(result);
        }

        [HttpGet("daily-resume")]
        public async Task<IActionResult> GetDailyResume([FromQuery] int idUtente, [FromQuery] DateOnly date)
        {
            if (idUtente <= 0 || date == default)
                return BadRequest("Invalid parameters.");

            // Recupera le rilevazioni glicemia per la data specificata
            var rilevazioniGlicemia = await _context.TabMisurazioniGlicemia
                .Where(r => r.IdUtente == idUtente && DateOnly.FromDateTime(r.MisuratoIl.Date) == date)
                .ToListAsync();

            // Recupera le assunzioni di farmaco per la data specificata
            var assunzioniFarmaco = await _context.TabAssunzioniFarmaci
                .Where(a => a.IdUtente == idUtente && DateOnly.FromDateTime(a.AssuntoIl) == date)
                .ToListAsync();

            // Recupera i sintomi per la data specificata
            var sintomi = await _context.TabSintomi
                .Where(s => s.IdUtente == idUtente && DateOnly.FromDateTime(s.AvvenutoIl) == date)
                .ToListAsync();

            // Composizione della risposta
            var response = new
            {
                RilevazioniGlicemia = rilevazioniGlicemia,
                AssunzioniFarmaco = assunzioniFarmaco,
                Sintomi = sintomi
            };

            return Ok(response);
        }

        [HttpPost("add-glicemic-log")]
        public async Task<IActionResult> AddGlicemicLog([FromBody] TabMisurazioniGlicemia glicemiaLog)
        {
            if (glicemiaLog == null || glicemiaLog.IdUtente <= 0 || glicemiaLog.Valore <= 0)
                return BadRequest("Invalid glicemic log data.");

            try
            {
                _context.TabMisurazioniGlicemia.Add(glicemiaLog);
                await _context.SaveChangesAsync();
                return Ok("Glicemic log added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("add-symptom-log")]
        public async Task<IActionResult> AddSymptomLog([FromBody] TabSintomi symptomLog)
        {
            if (symptomLog == null || symptomLog.IdUtente <= 0 || string.IsNullOrEmpty(symptomLog.Descrizione))
                return BadRequest("Invalid symptom log data.");

            try
            {
                _context.TabSintomi.Add(symptomLog);
                await _context.SaveChangesAsync();
                return Ok("Symptom log added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("add-medication-log")]
        public async Task<IActionResult> AddMedicationLog([FromBody] TabAssunzioniFarmaci medicationLog)
        {
            if (medicationLog == null || medicationLog.IdUtente <= 0)
                return BadRequest("Invalid medication log data.");

            try
            {
                _context.TabAssunzioniFarmaci.Add(medicationLog);
                await _context.SaveChangesAsync();
                return Ok("Medication log added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("terapies")]
        public async Task<IActionResult> GetTerapiesWithAssunzioni([FromQuery] int idUtente)
        {
            if (idUtente <= 0)
                return BadRequest("Invalid patient ID.");

            // Recupera l'utente
            var utente = await _context.TabUtenti
                .FirstOrDefaultAsync(u => u.IdUtente == idUtente);

            if (utente == null)
                return NotFound("Patient not found.");

            // Recupera le terapie associate all'utente
            var terapie = await _context.TabTerapie
                .Where(t => t.IdUtente == idUtente)
                .Select(t => new
                {
                    t.IdTerapia,
                    t.Indicazioni,
                    t.DataInizio,
                    t.DataFine,
                    ProgrammazioniAssunzioni = _context.TabProgrammazioneAssunzioni
                        .Where(pa => pa.IdTerapia == t.IdTerapia)
                        .Select(pa => new
                        {
                            pa.NomeFarmacoProgrammato,
                            pa.QuantitaPrevistaN,
                            pa.QuantitaPrevistaUn,
                            pa.DataOraPrevista
                        })
                        .ToList()
                })
                .ToListAsync();

            if (terapie == null || !terapie.Any())
                return NotFound("No therapies found for this patient.");

            return Ok(terapie);
        }
    }
}
