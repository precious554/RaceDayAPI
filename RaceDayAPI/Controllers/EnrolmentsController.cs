
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using System.Security.Claims;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/enrolments")]
    [Authorize]
    public class EnrolmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EnrolmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // A Participant enrols in a category
        [HttpPost]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CreateEnrolment(Enrolment enrolment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int participantId))
                return Unauthorized();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryID == enrolment.CategoryID);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            bool alreadyEnrolled = await _context.Enrolments.AnyAsync(e =>
                e.ParticipantID == participantId &&
                e.CategoryID == enrolment.CategoryID);

            if (alreadyEnrolled)
                return Conflict(new { message = "You are already enrolled in this category." });

            var newEnrolment = new Enrolment
            {
                ParticipantID = participantId,
                CategoryID = category.CategoryID,
                EnrolmentDate = DateTime.Today,
                Status = "Active"
            };

            _context.Enrolments.Add(newEnrolment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Enrolment successful.",
                newEnrolment.EnrolmentID,
                newEnrolment.ParticipantID,
                newEnrolment.CategoryID,
                newEnrolment.EnrolmentDate,
                newEnrolment.Status
            });
        }

        // A Participant views their own enrolments
        [HttpGet("my")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int participantId))
                return Unauthorized();

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantID == participantId)
                .Join(_context.Categories,
                    enrolment => enrolment.CategoryID,
                    category => category.CategoryID,
                    (enrolment, category) => new
                    {
                        enrolment.EnrolmentID,
                        enrolment.ParticipantID,
                        enrolment.EnrolmentDate,
                        enrolment.Status,
                        category.CategoryID,
                        category.CategoryName,
                        category.EventID,
                        category.Distance,
                        category.EntryFee
                    })
                .ToListAsync();

            return Ok(enrolments);
        }

        // An Organiser views enrolments for their own event
        [HttpGet("/api/events/{eventId}/enrolments")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int organiserId))
                return Unauthorized();

            var eventExists = await _context.Events.AnyAsync(e =>
                e.EventID == eventId && e.OrganiserID == organiserId);

            if (!eventExists)
                return NotFound(new
                {
                    message = "Event not found or you do not manage this event."
                });

            var enrolments = await _context.Enrolments
                .Join(_context.Categories,
                    enrolment => enrolment.CategoryID,
                    category => category.CategoryID,
                    (enrolment, category) => new { enrolment, category })
                .Where(x => x.category.EventID == eventId)
                .Join(_context.Users,
                    x => x.enrolment.ParticipantID,
                    user => user.UserID,
                    (x, user) => new
                    {
                        x.enrolment.EnrolmentID,
                        x.enrolment.EnrolmentDate,
                        x.enrolment.Status,
                        ParticipantName = user.FirstName + " " + user.LastName,
                        ParticipantEmail = user.Email,
                        x.category.CategoryName,
                        x.category.Distance
                    })
                .ToListAsync();

            return Ok(enrolments);
        }

        // A Participant cancels their own enrolment
        [HttpDelete("{id}")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int participantId))
                return Unauthorized();

            var enrolment = await _context.Enrolments.FirstOrDefaultAsync(e =>
                e.EnrolmentID == id && e.ParticipantID == participantId);

            if (enrolment == null)
                return NotFound(new { message = "Enrolment not found." });

            enrolment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}