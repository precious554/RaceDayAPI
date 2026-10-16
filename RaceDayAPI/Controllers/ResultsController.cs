
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using System.Security.Claims;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/results")]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // An Organiser records a result for an enrolment in their event
        [HttpPost]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> CreateResult(Result result)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int organiserId))
                return Unauthorized();

            var enrolmentInfo = await (
                from enrolment in _context.Enrolments
                join category in _context.Categories
                    on enrolment.CategoryID equals category.CategoryID
                join eventItem in _context.Events
                    on category.EventID equals eventItem.EventID
                where enrolment.EnrolmentID == result.EnrolmentID
                select new
                {
                    enrolment.EnrolmentID,
                    eventItem.OrganiserID
                }).FirstOrDefaultAsync();

            if (enrolmentInfo == null)
                return NotFound(new { message = "Enrolment not found." });

            if (enrolmentInfo.OrganiserID != organiserId)
                return Forbid();

            if (result.Position.HasValue && result.Position.Value <= 0)
                return BadRequest(new { message = "Position must be greater than zero." });

            if (result.FinishTime.HasValue && result.FinishTime.Value < TimeSpan.Zero)
                return BadRequest(new { message = "Finish time cannot be negative." });

            if (result.ResultStatus != "Completed" &&
                result.ResultStatus != "Did Not Finish" &&
                result.ResultStatus != "Disqualified")
            {
                return BadRequest(new
                {
                    message = "ResultStatus must be Completed, Did Not Finish, or Disqualified."
                });
            }

            bool resultExists = await _context.Results
                .AnyAsync(r => r.EnrolmentID == result.EnrolmentID);

            if (resultExists)
                return Conflict(new { message = "A result already exists for this enrolment." });

            var newResult = new Result
            {
                EnrolmentID = result.EnrolmentID,
                FinishTime = result.FinishTime,
                Position = result.Position,
                ResultStatus = result.ResultStatus
            };

            _context.Results.Add(newResult);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Result recorded successfully.",
                newResult.ResultID,
                newResult.EnrolmentID,
                newResult.FinishTime,
                newResult.Position,
                newResult.ResultStatus
            });
        }

        // A Participant views their own results
        [HttpGet("my")]
        [Authorize(Roles = "Participant")]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int participantId))
                return Unauthorized();

            var results = await (
                from result in _context.Results
                join enrolment in _context.Enrolments
                    on result.EnrolmentID equals enrolment.EnrolmentID
                join category in _context.Categories
                    on enrolment.CategoryID equals category.CategoryID
                join eventItem in _context.Events
                    on category.EventID equals eventItem.EventID
                where enrolment.ParticipantID == participantId
                select new
                {
                    result.ResultID,
                    result.FinishTime,
                    result.Position,
                    result.ResultStatus,
                    eventItem.EventID,
                    eventItem.EventName,
                    category.CategoryName,
                    category.Distance
                }).ToListAsync();

            return Ok(results);
        }

        // An Organiser views results for their own event
        [HttpGet("/api/events/{eventId}/results")]
        [Authorize(Roles = "Organiser")]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int organiserId))
                return Unauthorized();

            bool ownsEvent = await _context.Events.AnyAsync(e =>
                e.EventID == eventId && e.OrganiserID == organiserId);

            if (!ownsEvent)
                return NotFound(new
                {
                    message = "Event not found or you do not manage this event."
                });

            var results = await (
                from result in _context.Results
                join enrolment in _context.Enrolments
                    on result.EnrolmentID equals enrolment.EnrolmentID
                join category in _context.Categories
                    on enrolment.CategoryID equals category.CategoryID
                join user in _context.Users
                    on enrolment.ParticipantID equals user.UserID
                where category.EventID == eventId
                select new
                {
                    result.ResultID,
                    result.FinishTime,
                    result.Position,
                    result.ResultStatus,
                    ParticipantName = user.FirstName + " " + user.LastName,
                    category.CategoryName,
                    category.Distance
                }).ToListAsync();

            return Ok(results);
        }
    }
}