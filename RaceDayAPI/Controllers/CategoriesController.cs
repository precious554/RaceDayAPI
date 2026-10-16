
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using System.Security.Claims;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Both Participants and Organisers can view categories
        [AllowAnonymous]
        [HttpGet("events/{eventId}/categories")]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            var categories = await _context.Categories
                .Where(c => c.EventID == eventId)
                .ToListAsync();

            return Ok(categories);
        }

        // Only the Organiser who owns the event can add categories
        [Authorize(Roles = "Organiser")]
        [HttpPost("events/{eventId}/categories")]
        public async Task<IActionResult> CreateCategory(int eventId, Category category)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
                return NotFound(new { message = "Event not found." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null || eventItem.OrganiserID != int.Parse(userId))
                return Forbid();

            if (category.Distance <= 0 || category.EntryFee < 0)
                return BadRequest(new { message = "Distance must be positive and entry fee cannot be negative." });

            category.EventID = eventId;
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(category);
        }

        // Only the Organiser who owns the event can update its categories
        [Authorize(Roles = "Organiser")]
        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, Category updatedCategory)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == category.EventID);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (eventItem == null || userId == null ||
                eventItem.OrganiserID != int.Parse(userId))
                return Forbid();

            if (updatedCategory.Distance <= 0 || updatedCategory.EntryFee < 0)
                return BadRequest(new { message = "Distance must be positive and entry fee cannot be negative." });

            category.CategoryName = updatedCategory.CategoryName;
            category.Distance = updatedCategory.Distance;
            category.EntryFee = updatedCategory.EntryFee;

            await _context.SaveChangesAsync();

            return Ok(category);
        }

        // Only the Organiser who owns the event can delete its categories
        [Authorize(Roles = "Organiser")]
        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == category.EventID);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (eventItem == null || userId == null ||
                eventItem.OrganiserID != int.Parse(userId))
                return Forbid();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}