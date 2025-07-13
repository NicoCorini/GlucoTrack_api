using Xunit;
using GlucoTrack_api.Models;

namespace GlucoTrack_api.Tests.Models
{
    public class UsersTests
    {
        [Fact]
        public void User_CanBeCreated_WithValidData()
        {
            var user = new Users
            {
                UserId = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            };

            Assert.Equal(1, user.UserId);
            Assert.Equal("John", user.FirstName);
            Assert.Equal("Doe", user.LastName);
            Assert.Equal("john.doe@example.com", user.Email);
        }
    }
}
