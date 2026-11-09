using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using RaceDayAPI.Services;
using System.Security.Claims;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AzureBlobStorageService _blobStorage;

        // This tells ASP.NET Core to use this constructor.
        [ActivatorUtilitiesConstructor]
        public EventsController(
            ApplicationDbContext context,
            AzureBlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }

        // Allows unit tests to create the controller without Azure.
        public EventsController(ApplicationDbContext context)
        {
            _context = context;
            _blobStorage = null!;
        }

        // Anyone can view all events
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events.ToListAsync();
            return Ok(events);
        }

        // Anyone can view one event
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            return Ok(eventItem);
        }

        // Anyone can view an event banner
        [AllowAnonymous]
        [HttpGet("{id}/banner")]
        public async Task<IActionResult> GetEventBanner(int id)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (eventItem == null ||
                string.IsNullOrEmpty(eventItem.ImageUrl))
                return NotFound();

            if (_blobStorage == null)
                return NotFound();

            var image = await _blobStorage.DownloadImageAsync(
                eventItem.ImageUrl);

            if (image == null)
                return NotFound();

            return File(
                image.Content,
                image.Details.ContentType ?? "application/octet-stream");
        }

        // Organisers can upload event banners
        [Authorize(Roles = "Organiser")]
        [HttpPost("{id}/banner")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadEventBanner(
            int id,
            IFormFile file)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            var principal = ControllerContext?.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
                return Unauthorized();

            var userId = principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int organiserId) ||
                eventItem.OrganiserID != organiserId)
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest("Please select an image.");

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("Image must not exceed 5 MB.");

            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedTypes.Contains(
                file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(
                    "Only JPG, PNG and WEBP images are allowed.");
            }

            if (_blobStorage == null)
                return StatusCode(500, "Image storage is unavailable.");

            var imageUrl = await _blobStorage.UploadImageAsync(
                file,
                "event-banners");

            eventItem.ImageUrl = imageUrl;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Event banner uploaded successfully.",
                imageUrl = eventItem.ImageUrl
            });
        }

        // Only Organisers can create events
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<IActionResult> CreateEvent(Event eventItem)
        {
            var principal = ControllerContext?.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
                return Unauthorized();

            var userId = principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int organiserId))
                return Unauthorized();

            var organiserExists = await _context.Users
                .AnyAsync(u =>
                    u.UserID == organiserId &&
                    u.Role == "Organiser");

            if (!organiserExists)
                return BadRequest(
                    new { message = "A valid Organiser account is required." });

            if (eventItem.Distance <= 0)
                return BadRequest(
                    "Event distance must be greater than zero.");

            if (string.IsNullOrWhiteSpace(eventItem.EventType) ||
                !new[] { "Run", "Walk", "Cycle" }
                    .Contains(
                        eventItem.EventType,
                        StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Event type must be Run, Walk, or Cycle.");
            }

            eventItem.OrganiserID = organiserId;
            eventItem.ImageUrl = null;

            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = eventItem.EventID },
                eventItem);
        }

        // Only the organiser who owns the event can update it
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            Event updatedEvent)
        {
            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (existingEvent == null)
                return NotFound(
                    new { message = "Event not found." });

            var principal = ControllerContext?.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
                return Unauthorized();

            var userId = principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int organiserId) ||
                existingEvent.OrganiserID != organiserId)
                return Forbid();

            if (updatedEvent.Distance <= 0)
                return BadRequest(
                    "Event distance must be greater than zero.");

            if (string.IsNullOrWhiteSpace(updatedEvent.EventType) ||
                !new[] { "Run", "Walk", "Cycle" }
                    .Contains(
                        updatedEvent.EventType,
                        StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Event type must be Run, Walk, or Cycle.");
            }

            existingEvent.EventName = updatedEvent.EventName;
            existingEvent.EventDate = updatedEvent.EventDate;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Distance = updatedEvent.Distance;
            existingEvent.EventType = updatedEvent.EventType;

            await _context.SaveChangesAsync();

            return Ok(existingEvent);
        }

        // Only the organiser who owns the event can delete it
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (existingEvent == null)
                return NotFound(
                    new { message = "Event not found." });

            var principal = ControllerContext?.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
                return Unauthorized();

            var userId = principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int organiserId) ||
                existingEvent.OrganiserID != organiserId)
                return Forbid();

            _context.Events.Remove(existingEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}