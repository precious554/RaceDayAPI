
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using System.Security.Claims;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
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

        // Only Organisers can create events
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<IActionResult> CreateEvent(Event eventItem)
        {
            if (eventItem.OrganiserID <= 0)
                return BadRequest(new { message = "A valid OrganiserID is required." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null || eventItem.OrganiserID != int.Parse(userId))
                return Forbid();
            if (eventItem.Distance <= 0)
            {
                return BadRequest("Event distance must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(eventItem.EventType) ||
                !new[] { "Run", "Walk", "Cycle" }
                    .Contains(eventItem.EventType, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Event type must be Run, Walk, or Cycle.");
            }
            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = eventItem.EventID },
                eventItem);
        }

        // Only the Organiser who owns the event can update it
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, Event updatedEvent)
        {
            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (existingEvent == null)
                return NotFound(new { message = "Event not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null || existingEvent.OrganiserID != int.Parse(userId))
                return Forbid();

            existingEvent.EventName = updatedEvent.EventName;
            existingEvent.EventDate = updatedEvent.EventDate;
            existingEvent.Location = updatedEvent.Location;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Distance = updatedEvent.Distance;
            existingEvent.EventType = updatedEvent.EventType;
            if (updatedEvent.Distance <= 0)
            {
                return BadRequest("Event distance must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(updatedEvent.EventType) ||
                !new[] { "Run", "Walk", "Cycle" }
                    .Contains(updatedEvent.EventType, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Event type must be Run, Walk, or Cycle.");
            }
            await _context.SaveChangesAsync();

            return Ok(existingEvent);
        }

        // Only the Organiser who owns the event can delete it
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (existingEvent == null)
                return NotFound(new { message = "Event not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null || existingEvent.OrganiserID != int.Parse(userId))
                return Forbid();

            _context.Events.Remove(existingEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}