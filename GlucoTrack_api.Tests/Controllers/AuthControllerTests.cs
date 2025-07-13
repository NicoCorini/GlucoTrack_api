using Xunit;
using GlucoTrack_api.Controllers;
using GlucoTrack_api.Data;
using GlucoTrack_api.Models;
using GlucoTrack_api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace GlucoTrack_api.Tests.Controllers
{
    public class AuthControllerTests
    {
        private GlucoTrackDBContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<GlucoTrackDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new GlucoTrackDBContext(options);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenCredentialsInvalid()
        {
            var context = GetInMemoryContext();
            var controller = new AuthController(context);
            var request = new LoginRequestDto { EmailOrUsername = "notfound", Password = "wrong" };
            var result = await controller.Login(request);
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenCredentialsValid()
        {
            var context = GetInMemoryContext();
            context.Users.Add(new Users
            {
                UserId = 1,
                Email = "user@example.com",
                Username = "user",
                PasswordHash = "pass",
                FirstName = "Test",
                LastName = "User",
                RoleId = 3
            });
            context.SaveChanges();
            var controller = new AuthController(context);
            var request = new LoginRequestDto { EmailOrUsername = "user", Password = "pass" };
            var result = await controller.Login(request);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public void Logout_AlwaysReturnsOk()
        {
            var context = GetInMemoryContext();
            var controller = new AuthController(context);
            var result = controller.Logout();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetUserInfo_ReturnsBadRequest_WhenIdInvalid()
        {
            var context = GetInMemoryContext();
            var controller = new AuthController(context);
            var result = await controller.GetUserInfo(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetUserInfo_ReturnsNotFound_WhenUserNotExists()
        {
            var context = GetInMemoryContext();
            var controller = new AuthController(context);
            var result = await controller.GetUserInfo(123);
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetUserInfo_ReturnsOk_WhenUserExists()
        {
            var context = GetInMemoryContext();
            context.Users.Add(new Users
            {
                UserId = 1,
                Email = "user@example.com",
                Username = "user",
                PasswordHash = "pass",
                FirstName = "Test",
                LastName = "User",
                RoleId = 3
            });
            context.SaveChanges();
            var controller = new AuthController(context);
            var result = await controller.GetUserInfo(1);
            Assert.IsType<OkObjectResult>(result.Result);
        }
    }
}
