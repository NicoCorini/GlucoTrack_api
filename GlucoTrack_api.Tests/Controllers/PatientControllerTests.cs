using Xunit;
using GlucoTrack_api.Controllers;
using GlucoTrack_api.Data;
using GlucoTrack_api.Models;
using GlucoTrack_api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GlucoTrack_api.Tests.Controllers
{
    public class PatientControllerTests
    {
        private GlucoTrackDBContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<GlucoTrackDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new GlucoTrackDBContext(options);
        }
        // ...continua la classe PatientControllerTests...

        [Fact]
        public async Task GetGlycemicResume_ReturnsBadRequest_OnInvalidUserId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.GetGlycemicResume(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetGlycemicResume_ReturnsNotFound_WhenUserNotExists()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.GetGlycemicResume(123);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetGlycemicResume_ReturnsOk_WithData()
        {
            var context = GetInMemoryContext();
            var user = new Users { UserId = 1, FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 3, Username = "user1", PasswordHash = "pw" };
            context.Users.Add(user);
            var today = DateTime.Today;
            for (int i = 0; i < 7; i++)
            {
                context.GlycemicMeasurements.Add(new GlycemicMeasurements
                {
                    UserId = 1,
                    MeasurementDateTime = today.AddDays(-i),
                    Value = (short)(100 + i)
                });
            }
            context.SaveChanges();
            var controller = new PatientController(context);
            var result = await controller.GetGlycemicResume(1);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var data = Assert.IsAssignableFrom<List<GlycemicResumeResponseDto>>(okResult.Value);
            Assert.Equal(7, data.Count);
            Assert.True(data.All(d => d.Average > 0));
        }

        [Fact]
        public async Task GetDailyResume_ReturnsBadRequest_OnInvalidParams()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.GetDailyResume(0, default);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetDailyResume_ReturnsOk_WithData()
        {
            var context = GetInMemoryContext();
            var user = new Users { UserId = 1, FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 3, Username = "user1", PasswordHash = "pw" };
            context.Users.Add(user);
            var date = DateOnly.FromDateTime(DateTime.Today);
            context.GlycemicMeasurements.Add(new GlycemicMeasurements { UserId = 1, MeasurementDateTime = date.ToDateTime(TimeOnly.MinValue), Value = 100 });
            context.MedicationIntakes.Add(new MedicationIntakes { UserId = 1, IntakeDateTime = date.ToDateTime(TimeOnly.MinValue), ExpectedQuantityValue = 1, Unit = "mg" });
            context.Symptoms.Add(new Symptoms { UserId = 1, OccurredAt = date.ToDateTime(TimeOnly.MinValue), Description = "desc" });
            context.ReportedConditions.Add(new ReportedConditions { UserId = 1, Description = "cond", StartDate = date.ToDateTime(TimeOnly.MinValue) });
            context.SaveChanges();
            var controller = new PatientController(context);
            var result = await controller.GetDailyResume(1, date);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var data = Assert.IsType<DailyResumeResponseDto>(okResult.Value);
            Assert.Single(data.GlycemicMeasurements);
            Assert.Single(data.MedicationIntakes);
            Assert.Single(data.Symptoms);
            Assert.Single(data.ReportedConditions);
        }

        [Fact]
        public async Task GetGlycemicMeasurement_ReturnsBadRequest_OnInvalidId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.GetGlycemicMeasurement(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetGlycemicMeasurement_ReturnsNotFound_WhenNotExists()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.GetGlycemicMeasurement(123);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetGlycemicMeasurement_ReturnsOk_WithData()
        {
            var context = GetInMemoryContext();
            var gm = new GlycemicMeasurements { GlycemicMeasurementId = 1, UserId = 1, MeasurementDateTime = DateTime.Now, Value = 100 };
            context.GlycemicMeasurements.Add(gm);
            context.SaveChanges();
            var controller = new PatientController(context);
            var result = await controller.GetGlycemicMeasurement(1);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var data = Assert.IsType<GlycemicMeasurements>(okResult.Value);
            Assert.Equal(1, data.GlycemicMeasurementId);
        }

        [Fact]
        public async Task AddOrUpdateGlycemicLog_ReturnsBadRequest_OnNullOrInvalidData()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result1 = await controller.AddOrUpdateGlycemicLog(null);
            Assert.IsType<BadRequestObjectResult>(result1);
            var invalid = new AddGlycemicLogRequestDto { UserId = 0, Value = 0 };
            var result2 = await controller.AddOrUpdateGlycemicLog(invalid);
            Assert.IsType<BadRequestObjectResult>(result2);
        }

        [Fact]
        public async Task AddOrUpdateGlycemicLog_ReturnsNotFound_OnUpdateOfNonexistent()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var update = new AddGlycemicLogRequestDto
            {
                GlycemicMeasurementId = 99,
                UserId = 1,
                Value = 120,
                MeasurementDateTime = DateTime.Now,
                MeasurementTypeId = 1,
                MealTypeId = 1
            };
            var result = await controller.AddOrUpdateGlycemicLog(update);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AddOrUpdateGlycemicLog_ReturnsOk_OnInsert()
        {
            var context = GetInMemoryContext();
            context.Users.Add(new Users { UserId = 1, FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 3, Username = "user1", PasswordHash = "pw" });
            context.SaveChanges();
            var controller = new PatientController(context);
            var insert = new AddGlycemicLogRequestDto
            {
                UserId = 1,
                Value = 110,
                MeasurementDateTime = DateTime.Now,
                MeasurementTypeId = 1,
                MealTypeId = 1,
                Note = "test"
            };
            var result = await controller.AddOrUpdateGlycemicLog(insert);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("added", ok.Value.ToString());
            Assert.Single(context.GlycemicMeasurements);
        }

        [Fact]
        public async Task AddOrUpdateGlycemicLog_ReturnsOk_OnUpdate()
        {
            var context = GetInMemoryContext();
            context.Users.Add(new Users { UserId = 1, FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 3, Username = "user1", PasswordHash = "pw" });
            var gm = new GlycemicMeasurements { GlycemicMeasurementId = 2, UserId = 1, MeasurementDateTime = DateTime.Now, Value = 100, MeasurementTypeId = 1, MealTypeId = 1 };
            context.GlycemicMeasurements.Add(gm);
            context.SaveChanges();
            var controller = new PatientController(context);
            var update = new AddGlycemicLogRequestDto
            {
                GlycemicMeasurementId = 2,
                UserId = 1,
                Value = 130,
                MeasurementDateTime = DateTime.Now.AddMinutes(10),
                MeasurementTypeId = 2,
                MealTypeId = 2,
                Note = "aggiornato"
            };
            var result = await controller.AddOrUpdateGlycemicLog(update);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("updated", ok.Value.ToString());
            var updated = context.GlycemicMeasurements.First(x => x.GlycemicMeasurementId == 2);
            Assert.Equal(130, updated.Value);
            Assert.Equal("aggiornato", updated.Note);
        }

        [Fact]
        public async Task AddOrUpdateGlycemicLog_ReturnsServerError_OnException()
        {
            // Ora la validazione restituisce BadRequest (400) se i dati sono incompleti o non validi
            var context = GetInMemoryContext();
            context.Users.Add(new Users { UserId = 1, FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 3, Username = "user1", PasswordHash = "pw" });
            context.SaveChanges();
            var controller = new PatientController(context);
            var insert = new AddGlycemicLogRequestDto
            {
                UserId = 1,
                Value = 110,
                MeasurementDateTime = DateTime.Now
                // MeasurementTypeId e MealTypeId mancanti (default 0)
            };
            var result = await controller.AddOrUpdateGlycemicLog(insert);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteGlycemicMeasurement_ReturnsBadRequest_OnInvalidId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.DeleteGlycemicMeasurement(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteGlycemicMeasurement_ReturnsNotFound_OnNonexistentId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.DeleteGlycemicMeasurement(123);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteGlycemicMeasurement_ReturnsOk_OnDelete()
        {
            var context = GetInMemoryContext();
            var gm = new GlycemicMeasurements { GlycemicMeasurementId = 1, UserId = 1, MeasurementDateTime = DateTime.Now, Value = 100, MeasurementTypeId = 1, MealTypeId = 1 };
            context.GlycemicMeasurements.Add(gm);
            context.SaveChanges();
            var controller = new PatientController(context);
            var result = await controller.DeleteGlycemicMeasurement(1);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("deleted", ok.Value.ToString());
            Assert.Empty(context.GlycemicMeasurements);
        }

        [Fact]
        public async Task GetSymtptomLog_ReturnsBadRequest_OnInvalidId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.GetSymtptomLog(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetSymtptomLog_ReturnsNotFound_OnNonexistentId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.GetSymtptomLog(123);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetSymtptomLog_ReturnsOk_WithData()
        {
            var context = GetInMemoryContext();
            var s = new Symptoms { SymptomId = 1, UserId = 1, Description = "desc", OccurredAt = DateTime.Now };
            context.Symptoms.Add(s);
            context.SaveChanges();
            var controller = new PatientController(context);
            var result = await controller.GetSymtptomLog(1);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var data = Assert.IsType<Symptoms>(ok.Value);
            Assert.Equal(1, data.SymptomId);
        }

        [Fact]
        public async Task AddOrUpdateSymptomLog_ReturnsBadRequest_OnNullOrInvalidData()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result1 = await controller.AddOrUpdateSymptomLog(null);
            Assert.IsType<BadRequestObjectResult>(result1);
            var invalid = new AddSymptomLogRequestDto { UserId = 0, Description = null };
            var result2 = await controller.AddOrUpdateSymptomLog(invalid);
            Assert.IsType<BadRequestObjectResult>(result2);
        }

        [Fact]
        public async Task AddOrUpdateSymptomLog_ReturnsNotFound_OnUpdateOfNonexistent()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var update = new AddSymptomLogRequestDto
            {
                SymptomId = 99,
                UserId = 1,
                Description = "desc",
                OccurredAt = DateTime.Now
            };
            var result = await controller.AddOrUpdateSymptomLog(update);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AddOrUpdateSymptomLog_ReturnsOk_OnInsert()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var insert = new AddSymptomLogRequestDto
            {
                UserId = 1,
                Description = "desc",
                OccurredAt = DateTime.Now
            };
            var result = await controller.AddOrUpdateSymptomLog(insert);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("added", ok.Value.ToString());
            Assert.Single(context.Symptoms);
        }

        [Fact]
        public async Task AddOrUpdateSymptomLog_ReturnsOk_OnUpdate()
        {
            var context = GetInMemoryContext();
            var s = new Symptoms { SymptomId = 2, UserId = 1, Description = "old", OccurredAt = DateTime.Now };
            context.Symptoms.Add(s);
            context.SaveChanges();
            var controller = new PatientController(context);
            var update = new AddSymptomLogRequestDto
            {
                SymptomId = 2,
                UserId = 1,
                Description = "newdesc",
                OccurredAt = DateTime.Now.AddMinutes(10)
            };
            var result = await controller.AddOrUpdateSymptomLog(update);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("updated", ok.Value.ToString());
            var updated = context.Symptoms.First(x => x.SymptomId == 2);
            Assert.Equal("newdesc", updated.Description);
        }

        [Fact]
        public async Task DeleteSymptomLog_ReturnsBadRequest_OnInvalidId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.DeleteSymptomLog(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteSymptomLog_ReturnsNotFound_OnNonexistentId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.DeleteSymptomLog(123);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteSymptomLog_ReturnsOk_OnDelete()
        {
            var context = GetInMemoryContext();
            var s = new Symptoms { SymptomId = 1, UserId = 1, Description = "desc", OccurredAt = DateTime.Now };
            context.Symptoms.Add(s);
            context.SaveChanges();
            var controller = new PatientController(context);
            var result = await controller.DeleteSymptomLog(1);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("deleted", ok.Value.ToString());
            Assert.Empty(context.Symptoms);
        }

        [Fact]
        public async Task AddOrUpdateMedicationLog_ReturnsBadRequest_OnNullOrInvalidData()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result1 = await controller.AddOrUpdateMedicationLog(null);
            Assert.IsType<BadRequestObjectResult>(result1);
            var invalid = new AddMedicationLogRequestDto { UserId = 0 };
            var result2 = await controller.AddOrUpdateMedicationLog(invalid);
            Assert.IsType<BadRequestObjectResult>(result2);
        }

        [Fact]
        public async Task AddOrUpdateMedicationLog_ReturnsNotFound_OnUpdateOfNonexistent()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var update = new AddMedicationLogRequestDto
            {
                MedicationIntakeId = 99,
                UserId = 1,
                IntakeDateTime = DateTime.Now,
                ExpectedQuantityValue = 1,
                Unit = "mg"
            };
            var result = await controller.AddOrUpdateMedicationLog(update);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AddOrUpdateMedicationLog_ReturnsOk_OnInsert()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var insert = new AddMedicationLogRequestDto
            {
                UserId = 1,
                IntakeDateTime = DateTime.Now,
                ExpectedQuantityValue = 1,
                Unit = "mg",
                Note = "test",
                MedicationTakenName = "med",
                MedicationScheduleId = 1
            };
            var result = await controller.AddOrUpdateMedicationLog(insert);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("added", ok.Value.ToString());
            Assert.Single(context.MedicationIntakes);
        }

        [Fact]
        public async Task AddOrUpdateMedicationLog_ReturnsOk_OnUpdate()
        {
            var context = GetInMemoryContext();
            var mi = new MedicationIntakes { MedicationIntakeId = 2, UserId = 1, IntakeDateTime = DateTime.Now, ExpectedQuantityValue = 1, Unit = "mg", Note = "old", MedicationTakenName = "med", MedicationScheduleId = 1 };
            context.MedicationIntakes.Add(mi);
            context.SaveChanges();
            var controller = new PatientController(context);
            var update = new AddMedicationLogRequestDto
            {
                MedicationIntakeId = 2,
                UserId = 1,
                IntakeDateTime = DateTime.Now.AddMinutes(10),
                ExpectedQuantityValue = 2,
                Unit = "ml",
                Note = "aggiornato",
                MedicationTakenName = "med2",
                MedicationScheduleId = 2
            };
            var result = await controller.AddOrUpdateMedicationLog(update);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("updated", ok.Value.ToString());
            var updated = context.MedicationIntakes.First(x => x.MedicationIntakeId == 2);
            Assert.Equal(2, updated.ExpectedQuantityValue);
            Assert.Equal("aggiornato", updated.Note);
        }

        [Fact]
        public async Task DeleteMedicationLog_ReturnsBadRequest_OnInvalidId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.DeleteMedicationLog(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteMedicationLog_ReturnsNotFound_OnNonexistentId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.DeleteMedicationLog(123);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteMedicationLog_ReturnsOk_OnDelete()
        {
            var context = GetInMemoryContext();
            var mi = new MedicationIntakes { MedicationIntakeId = 1, UserId = 1, IntakeDateTime = DateTime.Now, ExpectedQuantityValue = 1, Unit = "mg" };
            context.MedicationIntakes.Add(mi);
            context.SaveChanges();
            var controller = new PatientController(context);
            var result = await controller.DeleteMedicationLog(1);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("deleted", ok.Value.ToString());
            Assert.Empty(context.MedicationIntakes);
        }
        [Fact]
        public async Task GetTherapiesWithSchedules_ReturnsBadRequest_OnInvalidUserId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.GetTherapiesWithSchedules(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetTherapiesWithSchedules_ReturnsNotFound_WhenUserNotExists()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.GetTherapiesWithSchedules(123);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetTherapiesWithSchedules_ReturnsNotFound_WhenNoTherapies()
        {
            var context = GetInMemoryContext();
            context.Users.Add(new Users { UserId = 1, FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 3, Username = "user1", PasswordHash = "pw" });
            context.SaveChanges();
            var controller = new PatientController(context);
            var result = await controller.GetTherapiesWithSchedules(1);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetTherapiesWithSchedules_ReturnsOk_WithData()
        {
            var context = GetInMemoryContext();
            context.Users.Add(new Users { UserId = 1, FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 3, Username = "user1", PasswordHash = "pw" });
            context.Therapies.Add(new Therapies { TherapyId = 1, UserId = 1, Title = "Terapia 1", Instructions = "Istruzioni", StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) });
            context.MedicationSchedules.Add(new MedicationSchedules { MedicationScheduleId = 1, TherapyId = 1, MedicationName = "Med1", Quantity = 1, Unit = "mg", DailyIntakes = 2 });
            context.SaveChanges();
            var controller = new PatientController(context);
            var result = await controller.GetTherapiesWithSchedules(1);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var data = Assert.IsAssignableFrom<List<TherapyWithSchedulesResponseDto>>(ok.Value);
            Assert.Single(data);
            Assert.Equal("Terapia 1", data[0].Title);
            Assert.Single(data[0].MedicationSchedules);
            Assert.Equal("Med1", data[0].MedicationSchedules[0].MedicationName);
        }

        [Fact]
        public async Task AddOrUpdateReportedCondition_ReturnsBadRequest_OnNullOrInvalidData()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result1 = await controller.AddOrUpdateReportedCondition(null);
            Assert.IsType<BadRequestObjectResult>(result1);
            var invalid = new AddReportedConditionRequestDto { UserId = 0, Description = null };
            var result2 = await controller.AddOrUpdateReportedCondition(invalid);
            Assert.IsType<BadRequestObjectResult>(result2);
        }

        [Fact]
        public async Task AddOrUpdateReportedCondition_ReturnsNotFound_OnUpdateOfNonexistent()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var update = new AddReportedConditionRequestDto
            {
                ConditionId = 99,
                UserId = 1,
                Description = "condizione",
                StartDate = DateTime.Now
            };
            var result = await controller.AddOrUpdateReportedCondition(update);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AddOrUpdateReportedCondition_ReturnsOk_OnInsert()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var insert = new AddReportedConditionRequestDto
            {
                UserId = 1,
                Description = "condizione",
                StartDate = DateTime.Now
            };
            var result = await controller.AddOrUpdateReportedCondition(insert);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("added", ok.Value.ToString());
            Assert.Single(context.ReportedConditions);
        }

        [Fact]
        public async Task AddOrUpdateReportedCondition_ReturnsOk_OnUpdate()
        {
            var context = GetInMemoryContext();
            var rc = new ReportedConditions { ConditionId = 2, UserId = 1, Description = "old", StartDate = DateTime.Now };
            context.ReportedConditions.Add(rc);
            context.SaveChanges();
            var controller = new PatientController(context);
            var update = new AddReportedConditionRequestDto
            {
                ConditionId = 2,
                UserId = 1,
                Description = "nuova descrizione",
                StartDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now.AddDays(1)
            };
            var result = await controller.AddOrUpdateReportedCondition(update);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("updated", ok.Value.ToString());
            var updated = context.ReportedConditions.First(x => x.ConditionId == 2);
            Assert.Equal("nuova descrizione", updated.Description);
        }

        [Fact]
        public async Task DeleteReportedCondition_ReturnsBadRequest_OnInvalidId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.DeleteReportedCondition(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteReportedCondition_ReturnsNotFound_OnNonexistentId()
        {
            var context = GetInMemoryContext();
            var controller = new PatientController(context);
            var result = await controller.DeleteReportedCondition(123);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteReportedCondition_ReturnsOk_OnDelete()
        {
            var context = GetInMemoryContext();
            var rc = new ReportedConditions { ConditionId = 1, UserId = 1, Description = "cond", StartDate = DateTime.Now };
            context.ReportedConditions.Add(rc);
            context.SaveChanges();
            var controller = new PatientController(context);
            var result = await controller.DeleteReportedCondition(1);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("deleted", ok.Value.ToString());
            Assert.Empty(context.ReportedConditions);
        }
    }
}
