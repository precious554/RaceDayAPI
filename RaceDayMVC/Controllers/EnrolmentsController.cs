using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDayMVC.Models;

namespace RaceDayMVC.Controllers
{
    public class EnrolmentsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EnrolmentsController(IHttpClientFactory httpClientFactory)
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
                var error = await response.Content.ReadAsStringAsync();

                ViewBag.Error =
                    $"API Error: {(int)response.StatusCode} ({response.StatusCode}). Details: {error}";

                return View("Manage", new List<EnrolmentViewModel>());
            }
            var enrolments =
                await response.Content.ReadFromJsonAsync<List<EnrolmentViewModel>>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return View("Manage",
                enrolments ?? new List<EnrolmentViewModel>());
        }

        // Participant views their own enrolments
        public async Task<IActionResult> MyEvents()
        {
            if (HttpContext.Session.GetString("Role") != "Participant")
                return RedirectToAction("Login", "Auth");

            var client = GetApiClient();

            var response = await client.GetAsync("api/enrolments/my");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to retrieve your events.";

                return View("MyEvents", new List<EnrolmentViewModel>());
            }

            var enrolments =
                await response.Content.ReadFromJsonAsync<List<EnrolmentViewModel>>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return View("MyEvents",
                enrolments ?? new List<EnrolmentViewModel>());
        }
    }
}