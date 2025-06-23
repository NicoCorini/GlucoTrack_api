using GlucoTrack_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlucoTrack_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

            // Recupera i dati del paziente
            var paziente = await _context.TabPazienti
                .FirstOrDefaultAsync(p => p.IdUtente == idUtente);

            if (paziente == null)
                return NotFound("Patient not found.");

            // Recupera i dati dell'utente
            var utente = await _context.TabUtenti
                .FirstOrDefaultAsync(u => u.IdUtente == idUtente);

            if (utente == null)
                return NotFound("User not found.");

            // Recupera il medico attuale
            int idMedicoAttuale = await _context.TabPazientiMedici
                .Where(pm => pm.IdPaziente == paziente.IdPaziente && pm.Al == null)
                .Select(m => m.IdMedico)
                .FirstOrDefaultAsync();

            TabMedici? medicoAttuale = await _context.TabMedici
                .Where(x => x.IdMedico == idMedicoAttuale)
                .FirstOrDefaultAsync();

            if (medicoAttuale == null)
                return NotFound("Doctor not found");


            // Recupera i fattori di rischio
            List<int> idFattoriRischioPaziente = await _context.TabPazientiFattoriRischio
                .Where(fr => fr.IdPaziente == paziente.IdPaziente)
                .Select(fr => fr.IdFattoreRischio)
                .ToListAsync();

            var fattoriRischio = await _context.TabFattoriRischio
                .Where(fr => idFattoriRischioPaziente.Contains(fr.IdFattoreRischio))
                .ToListAsync();


            // Recupera le comorbidità
            var comorbidita = await _context.TabComorbiditaPazienti
                .Where(c => c.IdPaziente == paziente.IdPaziente)
                .ToListAsync();

            // Recupera le terapie
            var terapie = await _context.TabTerapie
                .Where(t => t.IdPaziente == paziente.IdPaziente)
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
        public async Task<IActionResult> GetGlicemicResume([FromQuery] int IdPaziente)
        {
            if (IdPaziente <= 0)
                return BadRequest("Invalid user ID.");

            // Recupera i dati del paziente
            var paziente = await _context.TabPazienti
                .FirstOrDefaultAsync(p => p.IdPaziente == IdPaziente);

            if (paziente == null)
                return NotFound("Patient not found.");

            // Recupera le misurazioni glicemiche
            var misurazioni = await _context.TabMisurazioniGlicemia
                .Where(m => m.IdPaziente == paziente.IdPaziente)
                .OrderByDescending(m => m.MisuratoIl)
                .ToListAsync();

            if (misurazioni == null || !misurazioni.Any())
                return NotFound("No glicemic measurements found for this patient.");

            // Calcolo della media giornaliera degli ultimi 7 giorni

            DateTime oggi = DateTime.Today;
            DateTime setteGiorniFa = oggi.AddDays(-6); // Include oggi e i 6 giorni precedenti

            var mediaGiornaliera = misurazioni
                .Where(m => m.MisuratoIl.Date >= setteGiorniFa && m.MisuratoIl.Date <= oggi)
                .GroupBy(m => m.MisuratoIl.Date)
                .Select(g => new
                {
                    Giorno = g.Key.ToString("ddd"),
                    Media = g.Average(m => m.Valore)
                })
                .OrderBy(r => r.Giorno)
                .ToList();

            return Ok(mediaGiornaliera);

        }


        [HttpGet("daily-resume")]
        public async Task<IActionResult> GetDailyResume([FromQuery] int idPaziente, [FromQuery] DateOnly date)
        {
            if (idPaziente <= 0 || date == default)
                return BadRequest("Invalid parameters.");

            // Recupera le rilevazioni glicemia per la data specificata
            var rilevazioniGlicemia = await _context.TabMisurazioniGlicemia
                .Where(r => r.IdPaziente == idPaziente && DateOnly.FromDateTime(r.MisuratoIl.Date) == date)
                .ToListAsync();

            // Recupera le assunzioni di farmaco per la data specificata
            var assunzioniFarmaco = await _context.TabAssunzioniFarmaci
                .Where(a => a.IdPaziente == idPaziente && DateOnly.FromDateTime(a.AssuntoIl) == date)
                .ToListAsync();

            // Recupera i sintomi per la data specificata
            var sintomi = await _context.TabSintomi
                .Where(s => s.IdPaziente == idPaziente && DateOnly.FromDateTime(s.AvvenutoIl) == date)
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
            if (glicemiaLog == null || glicemiaLog.IdPaziente <= 0 || glicemiaLog.Valore <= 0)
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
            if (symptomLog == null || symptomLog.IdPaziente <= 0 || string.IsNullOrEmpty(symptomLog.Descrizione))
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
            if (medicationLog == null || medicationLog.IdPaziente <= 0)
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
        public async Task<IActionResult> GetTerapiesWithAssunzioni([FromQuery] int idPaziente)
        {
            if (idPaziente <= 0)
                return BadRequest("Invalid patient ID.");

            // Recupera il paziente
            var paziente = await _context.TabPazienti
                .FirstOrDefaultAsync(p => p.IdPaziente == idPaziente);

            if (paziente == null)
                return NotFound("Patient not found.");

            // Recupera le terapie associate al paziente
            var terapie = await _context.TabTerapie
                .Where(t => t.IdPaziente == idPaziente)
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
                            pa.Farmaco,
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
