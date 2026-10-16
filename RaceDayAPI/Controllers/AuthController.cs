
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            string role = request.Role.Trim();

            if (role != "Organiser" && role != "Participant")
            {
                return BadRequest(new
                {
                    message = "Role must be Organiser or Participant."
                });
            }

            string email = request.Email.Trim().ToLowerInvariant();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                return Conflict(new
                {
                    message = "An account with this email already exists."
                });
            }

            var user = new User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Role = role
            };

            user.Password = _passwordHasher.HashPassword(
                user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Created("/api/users/profile", new
            {
                user.UserID,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            string email = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            bool passwordIsValid = false;

            // First, check whether the stored password is a hash.
            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                try
                {
                    var verification = _passwordHasher.VerifyHashedPassword(
                        user,
                        user.Password,
                        request.Password
                    );

                    passwordIsValid =
                        verification != PasswordVerificationResult.Failed;
                }
                catch (FormatException)
                {
                    // Existing development account may have a plain-text password.
                    passwordIsValid = false;
                }
            }

            // Temporary development compatibility for existing seed accounts.
            // Remove this fallback after migrating all accounts to password hashes.
            if (!passwordIsValid &&
                user.Password == request.Password &&
                user.Password == "P@ssword123!")
            {
                user.Password = _passwordHasher.HashPassword(
                    user,
                    request.Password
                );

                passwordIsValid = true;

                await _context.SaveChangesAsync();
            }

            if (!passwordIsValid)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            string key = _configuration["Jwt:Key"]!;
            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

            var credentials = new SigningCredentials(
                signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:ExpiryMinutes"]!)),
                signingCredentials: credentials);

            return Ok(new
            {
                message = "Login successful.",
                token = new JwtSecurityTokenHandler().WriteToken(token),
                user = new
                {
                    user.UserID,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Role
                }
            });
        }
    }
}
