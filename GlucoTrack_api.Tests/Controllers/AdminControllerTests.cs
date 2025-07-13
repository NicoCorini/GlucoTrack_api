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
    public class AdminControllerTests
    {
        private GlucoTrackDBContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<GlucoTrackDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new GlucoTrackDBContext(options);
        }

        [Fact]
        public async Task GetUsers_ReturnsAllUsers()
        {
            var context = GetInMemoryContext();
            context.Users.Add(new Users { UserId = 1, Username = "user1", PasswordHash = "pw1", FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 2 });
            context.Users.Add(new Users { UserId = 2, Username = "user2", PasswordHash = "pw2", FirstName = "C", LastName = "D", Email = "c@d.com", RoleId = 3 });
            context.SaveChanges();
            var controller = new AdminController(context);
            var result = await controller.GetUsers();
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var users = Assert.IsAssignableFrom<IEnumerable<Users>>(okResult.Value);
            Assert.Equal(2, users.Count());
        }

        [Fact]
        public async Task AddOrUpdateUser_CreatesNewUser()
        {
            var context = GetInMemoryContext();
            var controller = new AdminController(context);
            var dto = new UserDto
            {
                Username = "newuser",
                PasswordHash = "pw",
                FirstName = "New",
                LastName = "User",
                Email = "new@user.com",
                RoleId = 3
            };
            var result = await controller.AddOrUpdateUser(dto);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User saved successfully.", okResult.Value);
            Assert.Single(context.Users.ToList());
        }

        [Fact]
        public async Task AddOrUpdateUser_UpdatesExistingUser()
        {
            var context = GetInMemoryContext();
            context.Users.Add(new Users { UserId = 1, Username = "user1", PasswordHash = "pw1", FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 2 });
            context.SaveChanges();
            var controller = new AdminController(context);
            var dto = new UserDto
            {
                UserId = 1,
                Username = "user1",
                PasswordHash = "pw1",
                FirstName = "Updated",
                LastName = "B",
                Email = "a@b.com",
                RoleId = 2
            };
            var result = await controller.AddOrUpdateUser(dto);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User saved successfully.", okResult.Value);
            Assert.Equal("Updated", context.Users.First().FirstName);
        }

        [Fact]
        public async Task AddOrUpdateUser_ReturnsNotFound_WhenUpdatingNonexistentUser()
        {
            var context = GetInMemoryContext();
            var controller = new AdminController(context);
            var dto = new UserDto { UserId = 99, Username = "nouser", PasswordHash = "pw", FirstName = "No", LastName = "User", Email = "no@user.com", RoleId = 3 };
            var result = await controller.AddOrUpdateUser(dto);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFound.Value);
        }

        [Fact]
        public async Task AddOrUpdateUser_ReturnsBadRequest_WhenDtoNull()
        {
            var context = GetInMemoryContext();
            var controller = new AdminController(context);
            var result = await controller.AddOrUpdateUser(null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid user data.", badRequest.Value);
        }

        [Fact]
        public async Task DeleteUser_DeletesUser()
        {
            var context = GetInMemoryContext();
            context.Users.Add(new Users { UserId = 1, Username = "user1", PasswordHash = "pw1", FirstName = "A", LastName = "B", Email = "a@b.com", RoleId = 2 });
            context.SaveChanges();
            var controller = new AdminController(context);
            var result = await controller.DeleteUser(1);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User deleted successfully.", okResult.Value);
            Assert.Empty(context.Users.ToList());
        }

        [Fact]
        public async Task DeleteUser_ReturnsNotFound_WhenUserNotExists()
        {
            var context = GetInMemoryContext();
            var controller = new AdminController(context);
            var result = await controller.DeleteUser(99);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", notFound.Value);
        }

        [Fact]
        public async Task DeleteUser_ReturnsBadRequest_WhenIdInvalid()
        {
            var context = GetInMemoryContext();
            var controller = new AdminController(context);
            var result = await controller.DeleteUser(0);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid userId.", badRequest.Value);
        }
    }
}
