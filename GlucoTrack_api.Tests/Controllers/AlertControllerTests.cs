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
    public class AlertControllerTests
    {
        private GlucoTrackDBContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<GlucoTrackDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new GlucoTrackDBContext(options);
        }

        [Fact]
        public async Task GetUserNotResolvedAlerts_ReturnsBadRequest_OnInvalidUserId()
        {
            var context = GetInMemoryContext();
            var controller = new AlertController(context);
            var result = await controller.GetUserNotResolvedAlerts(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetUserNotResolvedAlerts_ReturnsOk_WithAlerts()
        {
            var context = GetInMemoryContext();
            var user = new Users { UserId = 1, FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 3, Username = "user1", PasswordHash = "pw" };
            var alertType = new AlertTypes { AlertTypeId = 1, Label = "HIGH_GLUCOSE", Description = "desc" };
            var alert = new Alerts { AlertId = 1, UserId = 1, AlertTypeId = 1, Message = "msg", Status = "open", CreatedAt = DateTime.UtcNow };
            var recipient = new AlertRecipients { AlertRecipientId = 1, AlertId = 1, RecipientUserId = 1, IsRead = false };
            context.Users.Add(user);
            context.AlertTypes.Add(alertType);
            context.Alerts.Add(alert);
            context.AlertRecipients.Add(recipient);
            context.SaveChanges();
            var controller = new AlertController(context);
            var result = await controller.GetUserNotResolvedAlerts(1);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var alerts = Assert.IsAssignableFrom<IEnumerable<AlertDto>>(okResult.Value);
            Assert.Single(alerts);
        }

        [Fact]
        public async Task GetUserAlerts_ReturnsBadRequest_OnInvalidUserId()
        {
            var context = GetInMemoryContext();
            var controller = new AlertController(context);
            var result = await controller.GetUserAlerts(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetUserAlerts_ReturnsOk_WithAlerts()
        {
            var context = GetInMemoryContext();
            var user = new Users { UserId = 1, FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 3, Username = "user1", PasswordHash = "pw" };
            var alertType = new AlertTypes { AlertTypeId = 1, Label = "HIGH_GLUCOSE", Description = "desc" };
            var alert = new Alerts { AlertId = 1, UserId = 1, AlertTypeId = 1, Message = "msg", Status = "open", CreatedAt = DateTime.UtcNow };
            var recipient = new AlertRecipients { AlertRecipientId = 1, AlertId = 1, RecipientUserId = 1, IsRead = false };
            context.Users.Add(user);
            context.AlertTypes.Add(alertType);
            context.Alerts.Add(alert);
            context.AlertRecipients.Add(recipient);
            context.SaveChanges();
            var controller = new AlertController(context);
            var result = await controller.GetUserAlerts(1);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var alerts = Assert.IsAssignableFrom<IEnumerable<AlertDto>>(okResult.Value);
            Assert.Single(alerts);
        }

        [Fact]
        public async Task ResolveAlert_ReturnsBadRequest_OnInvalidId()
        {
            var context = GetInMemoryContext();
            var controller = new AlertController(context);
            var result = await controller.ResolveAlert(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ResolveAlert_ReturnsNotFound_WhenRecipientNotExists()
        {
            var context = GetInMemoryContext();
            var controller = new AlertController(context);
            var result = await controller.ResolveAlert(99);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ResolveAlert_ResolvesAlert()
        {
            var context = GetInMemoryContext();
            var alert = new Alerts { AlertId = 1, UserId = 1, AlertTypeId = 1, Message = "msg", Status = "open", CreatedAt = DateTime.UtcNow };
            var recipient = new AlertRecipients { AlertRecipientId = 1, AlertId = 1, RecipientUserId = 1, IsRead = false };
            context.Alerts.Add(alert);
            context.AlertRecipients.Add(recipient);
            context.SaveChanges();
            var controller = new AlertController(context);
            var result = await controller.ResolveAlert(1);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Alert resolved for recipient.", okResult.Value);
            Assert.True(context.AlertRecipients.First().IsRead);
            Assert.Equal("resolved", context.Alerts.First().Status);
        }

        [Fact]
        public async Task CreateGlycemiaAlert_ReturnsBadRequest_OnInvalidData()
        {
            var context = GetInMemoryContext();
            var controller = new AlertController(context);
            var result = await controller.CreateGlycemiaAlert(null);
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
