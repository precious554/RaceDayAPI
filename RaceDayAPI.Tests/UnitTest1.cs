using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RaceDayAPI.Controllers;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using Xunit;

namespace RaceDayAPI.Tests
{
    public class AuthControllerTests
    {
        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);

        }

        private static IConfiguration CreateConfiguration()
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsADevelopmentKeyThatIsLongEnough123!",
                ["Jwt:Issuer"] = "RaceDayAPI",
                ["Jwt:Audience"] = "RaceDayUsers",
                ["Jwt:ExpiryMinutes"] = "60"
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        [Fact]
        public async Task Register_WithValidOrganiserRole_CreatesAccount()
        {
            using var context = CreateContext();
            var controller = new AuthController(
                context, CreateConfiguration());

            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "Organiser",
                Email = "organiser@example.com",
                Password = "StrongPass123!",
                Role = "Organiser"
            };

            var result = await controller.Register(request);

            Assert.Equal(1, await context.Users.CountAsync());
            Assert.Equal("Organiser",
                (await context.Users.FirstAsync()).Role);
        }

        [Fact]
        public async Task Register_WithValidParticipantRole_CreatesAccount()
        {
            using var context = CreateContext();
            var controller = new AuthController(
                context, CreateConfiguration());

            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "Participant",
                Email = "participant@example.com",
                Password = "StrongPass123!",
                Role = "Participant"
            };

            await controller.Register(request);

            Assert.Equal(1, await context.Users.CountAsync());
            Assert.Equal("Participant",
                (await context.Users.FirstAsync()).Role);
        }
        [Fact]
        public async Task Register_WithDuplicateEmail_ReturnsConflict()
        {
            using var context = CreateContext();
            var controller = new AuthController(context, CreateConfiguration());

            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "duplicate@example.com",
                Password = "StrongPass123!",
                Role = "Participant"
            };

            await controller.Register(request);

            var duplicateRequest = new RegisterRequest
            {
                FirstName = "Another",
                LastName = "User",
                Email = "duplicate@example.com",
                Password = "AnotherPass123!",
                Role = "Participant"
            };

            var result = await controller.Register(duplicateRequest);

            Assert.Equal(1, await context.Users.CountAsync());
            Assert.IsType<Microsoft.AspNetCore.Mvc.ConflictObjectResult>(result);
        }
        [Fact]
        public async Task Register_WithInvalidRole_ReturnsBadRequest()
        {
            using var context = CreateContext();
            var controller = new AuthController(context, CreateConfiguration());

            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "invalidrole@example.com",
                Password = "StrongPass123!",
                Role = "Administrator"
            };

            var result = await controller.Register(request);

            Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
            Assert.Equal(0, await context.Users.CountAsync());
        }
        [Fact]
        public async Task Login_WithValidCredentials_ReturnsToken()
        {
            using var context = CreateContext();
            var controller = new AuthController(context, CreateConfiguration());

            var registerRequest = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "login@example.com",
                Password = "StrongPass123!",
                Role = "Participant"
            };

            await controller.Register(registerRequest);

            var result = await controller.Login(
                new RaceDayAPI.DTOs.LoginRequest
                {
                    Email = "login@example.com",
                    Password = "StrongPass123!"
                });

            Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
        }
        [Fact]
        public async Task Login_WithWrongPassword_ReturnsUnauthorized()
        {
            using var context = CreateContext();
            var controller = new AuthController(context, CreateConfiguration());

            var registerRequest = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "wrongpassword@example.com",
                Password = "StrongPass123!",
                Role = "Participant"
            };

            await controller.Register(registerRequest);

            var result = await controller.Login(
                new RaceDayAPI.DTOs.LoginRequest
                {
                    Email = "wrongpassword@example.com",
                    Password = "WrongPassword123!"
                });

            Assert.IsType<Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult>(result);
        }
        [Fact]
        public async Task Register_StoresHashedPassword()
        {
            using var context = CreateContext();
            var controller = new AuthController(context, CreateConfiguration());

            await controller.Register(new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "hash@example.com",
                Password = "StrongPass123!",
                Role = "Participant"
            });

            var user = await context.Users.FirstAsync();

            Assert.NotEqual("StrongPass123!", user.Password);
        }
    }
}