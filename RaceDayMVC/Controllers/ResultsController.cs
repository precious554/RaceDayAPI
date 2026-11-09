using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDayMVC.Models;

namespace RaceDayMVC.Controllers
{
    public class ResultsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ResultsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient GetApiClient()
        {
            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            var token = HttpContext.Session.GetString("JWTToken");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        // Participant views their own results
        public async Task<IActionResult> MyResults()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Auth");

            var client = GetApiClient();

            var response = await client.GetAsync("api/results/my");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to retrieve your results.";

                return View("MyResults", new List<ResultViewModel>());
            }

            var results =
                await response.Content.ReadFromJsonAsync<List<ResultViewModel>>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return View("MyResults",
                results ?? new List<ResultViewModel>());
        }

        // Organiser views participants enrolled in an event
        public async Task<IActionResult> Manage(int eventId)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            var client = GetApiClient();

            var response = await client.GetAsync(
                $"api/events/{eventId}/enrolments");

            ViewBag.EventID = eventId;

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to retrieve event participants.";

                return View("Manage",
                    new List<EnrolmentViewModel>());
            }

            var enrolments =
                await response.Content.ReadFromJsonAsync<List<EnrolmentViewModel>>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return View("Manage",
                enrolments ?? new List<EnrolmentViewModel>());
        }

        // Organiser records a participant's result
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Record(ResultEntryViewModel model)
        {
            if (HttpContext.Session.GetString("Role") != "Organiser")
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please check the result details.";
                return RedirectToAction("Manage",
                    new { eventId = model.EventID });
            }

            TimeSpan? finishTime = null;

            if (!string.IsNullOrWhiteSpace(model.FinishTime))
            {
                if (!TimeSpan.TryParse(model.FinishTime, out TimeSpan parsedTime)
                    || parsedTime < TimeSpan.Zero)
                {
                    TempData["Error"] = "Please enter a valid finish time.";

                    return RedirectToAction("Manage",
                        new { eventId = model.EventID });
                }

                finishTime = parsedTime;
            }

            var client = GetApiClient();

            var result = new
            {
                EnrolmentID = model.EnrolmentID,
                FinishTime = finishTime,
                Position = model.Position,
                ResultStatus = model.ResultStatus
            };

            var response = await client.PostAsJsonAsync(
                "api/results", result);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Result recorded successfully.";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();

                TempData["Error"] =
                    $"Unable to record result. {error}";
            }

            return RedirectToAction("Manage",
                new { eventId = model.EventID });
        }
    }
}