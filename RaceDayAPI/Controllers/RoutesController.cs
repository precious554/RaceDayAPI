
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/routes")]
    [Authorize]
    public class RoutesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoutesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{eventId}")]
        public async Task<IActionResult> GetRoute(int eventId)
        {
            var eventExists = await _context.Events
                .AnyAsync(e => e.EventID == eventId);

            if (!eventExists)
                return NotFound(new { message = "Event not found." });

            var routes = await _context.Routes
                .Where(r => r.EventID == eventId)
                .ToListAsync();

            return Ok(routes);
        }
    }
}