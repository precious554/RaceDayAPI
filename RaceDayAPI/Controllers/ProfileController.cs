using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Services;
using System.Security.Claims;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/users/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AzureBlobStorageService _blobStorage;

        public ProfileController(
            ApplicationDbContext context,
            AzureBlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }

        // View profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int id))
                return Unauthorized();

            var user = await _context.Users
                .Where(u => u.UserID == id)
                .Select(u => new
                {
                    u.UserID,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.Role,
                    u.ProfilePictureUrl
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "Profile not found." });

            return Ok(user);
        }

        // Update profile
        [HttpPut]
        public async Task<IActionResult> UpdateProfile(
            UpdateProfileRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int id))
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
                return NotFound(new { message = "Profile not found." });

            string email = request.Email.Trim().ToLowerInvariant();

            bool emailUsed = await _context.Users.AnyAsync(u =>
                u.Email.ToLower() == email && u.UserID != id);

            if (emailUsed)
                return Conflict(new
                {
                    message = "That email address is already in use."
                });

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Email = email;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully.",
                user.UserID,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.ProfilePictureUrl
            });
        }

        // Upload profile picture
        [HttpPost("picture")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int id))
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest(new
                {
                    message = "Please select a picture."
                });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new
                {
                    message = "Picture must not exceed 5 MB."
                });

            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedTypes.Contains(
                file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(new
                {
                    message = "Only JPG, PNG and WEBP pictures are allowed."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
                return NotFound(new { message = "Profile not found." });

            string imageUrl = await _blobStorage.UploadImageAsync(
                file,
                "profile-pictures");

            user.ProfilePictureUrl = imageUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile picture uploaded successfully.",
                profilePictureUrl = user.ProfilePictureUrl
            });
        }

        // Retrieve the logged-in user's private profile picture
        [HttpGet("picture")]
        public async Task<IActionResult> GetProfilePicture()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int id))
                return Unauthorized();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null)
                return NotFound();

            if (string.IsNullOrEmpty(user.ProfilePictureUrl))
                return NotFound(new
                {
                    message = "No profile picture has been uploaded."
                });

            var image = await _blobStorage.DownloadImageAsync(
                user.ProfilePictureUrl);

            if (image == null)
                return NotFound(new
                {
                    message = "Profile picture could not be found."
                });

            return File(
                image.Content,
                image.Details.ContentType ?? "application/octet-stream");
        }
    }

    public class UpdateProfileRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Email { get; set; } = string.Empty;
    }
}