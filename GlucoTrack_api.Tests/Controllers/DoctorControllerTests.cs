using Xunit;
using GlucoTrack_api.Controllers;
using GlucoTrack_api.Data;
using GlucoTrack_api.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;

namespace GlucoTrack_api.Tests.Controllers
{
    public class DoctorControllerTests
    {
        private GlucoTrackDBContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<GlucoTrackDBContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new GlucoTrackDBContext(options);
        }

        [Fact]
        public async Task GetDoctorDashboardSummary_ReturnsBadRequest_WhenDoctorIdInvalid()
        {
            var context = GetInMemoryContext();
            var changeLogService = new Mock<GlucoTrack_api.Utils.ChangeLogService>(context);
            var controller = new DoctorController(context, changeLogService.Object);
            var result = await controller.GetDoctorDashboardSummary(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetDoctorDashboardSummary_ReturnsOk_WhenDoctorIdValid()
        {
            var context = GetInMemoryContext();
            // Arrange: add a doctor and a patient linked
            var doctor = new Users
            {
                UserId = 1,
                FirstName = "Doc",
                LastName = "Tor",
                RoleId = 2,
                Email = "doc@example.com",
                Username = "doctor",
                PasswordHash = "hash",
                BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-40))
            };
            var patient = new Users
            {
                UserId = 2,
                FirstName = "Pat",
                LastName = "Ient",
                RoleId = 3,
                Email = "pat@example.com",
                Username = "patient",
                PasswordHash = "hash",
                BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30))
            };
            context.Users.AddRange(doctor, patient);
            context.PatientDoctors.Add(new PatientDoctors { DoctorId = 1, PatientId = 2 });
            context.SaveChanges();
            var changeLogService = new Mock<GlucoTrack_api.Utils.ChangeLogService>(context);
            var controller = new DoctorController(context, changeLogService.Object);
            var result = await controller.GetDoctorDashboardSummary(1);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetDoctorPatients_ReturnsBadRequest_OnInvalidParams()
        {
            var context = GetInMemoryContext();
            var changeLogService = new Mock<GlucoTrack_api.Utils.ChangeLogService>(context);
            var controller = new DoctorController(context, changeLogService.Object);
            var result = await controller.GetDoctorPatients(1, -1);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetDoctorPatients_ReturnsOk_WithValidDoctor()
        {
            var context = GetInMemoryContext();
            var doctor = new Users
            {
                UserId = 1,
                FirstName = "Doc",
                LastName = "Tor",
                RoleId = 2,
                Email = "doc@example.com",
                Username = "doctor",
                PasswordHash = "hash",
                BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-40))
            };
            var patient = new Users
            {
                UserId = 2,
                FirstName = "Pat",
                LastName = "Ient",
                RoleId = 3,
                Email = "pat@example.com",
                Username = "patient",
                PasswordHash = "hash",
                BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30))
            };
            context.Users.AddRange(doctor, patient);
            context.PatientDoctors.Add(new PatientDoctors { DoctorId = 1, PatientId = 2 });
            context.SaveChanges();
            var changeLogService = new Mock<GlucoTrack_api.Utils.ChangeLogService>(context);
            var controller = new DoctorController(context, changeLogService.Object);
            var result = await controller.GetDoctorPatients(1, 0, "", true);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetTherapy_ReturnsBadRequest_WhenIdInvalid()
        {
            var context = GetInMemoryContext();
            var changeLogService = new Mock<GlucoTrack_api.Utils.ChangeLogService>(context);
            var controller = new DoctorController(context, changeLogService.Object);
            var result = await controller.GetTherapy(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
