using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDayMVC.Models;

namespace RaceDayMVC.Controllers
{
    public class EventsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EventsController(IHttpClientFactory httpClientFactory)
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

        private bool IsOrganiser()
        {
            return HttpContext.Session.GetString("Role") == "Organiser";
        }

        private bool IsParticipant()
        {
            return HttpContext.Session.GetString("Role") == "Participant";
        }

        // Retrieve events
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            try
            {
                var response = await client.GetAsync("api/events");
                var content = await response.Content.ReadAsStringAsync();

                return new ContentResult
                {
                    Content = content,
                    ContentType = "application/json",
                    StatusCode = (int)response.StatusCode
                };
            }
            catch
            {
                return StatusCode(503, new
                {
                    message = "Unable to connect to the RaceDay API."
                });
            }
        }

        // Participant views event details and categories
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!IsParticipant())
                return RedirectToAction("Login", "Auth");

            var client = GetApiClient();

            var eventResponse = await client.GetAsync($"api/events/{id}");

            if (!eventResponse.IsSuccessStatusCode)
                return NotFound();

            var eventItem =
                await eventResponse.Content.ReadFromJsonAsync<EventViewModel>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (eventItem == null)
                return NotFound();

            var categoryResponse =
                await client.GetAsync($"api/events/{id}/categories");

            if (!categoryResponse.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to retrieve event categories.";
                ViewBag.Categories = new List<CategoryViewModel>();
            }
            else
            {
                var categories =
                    await categoryResponse.Content.ReadFromJsonAsync<List<CategoryViewModel>>(
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));

                ViewBag.Categories = categories ?? new List<CategoryViewModel>();
            }

            return View(eventItem);
        }

        // Display event banner through the API
        [HttpGet]
        public async Task<IActionResult> Banner(int id)
        {
            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            var response = await client.GetAsync($"api/events/{id}/banner");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var imageBytes = await response.Content.ReadAsByteArrayAsync();

            var contentType =
                response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            return File(imageBytes, contentType);
        }

        // Participant enrols in a category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            if (!IsParticipant())
                return RedirectToAction("Login", "Auth");

            var client = GetApiClient();

            var response = await client.PostAsJsonAsync(
                "api/enrolments",
                new { CategoryID = categoryId });

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "You have successfully enrolled in this event!";
            }
            else
            {
                TempData["Error"] =
                    "Enrolment failed. Please check your selection and try again.";
            }

            return RedirectToAction("Details", new { id = eventId });
        }

        // Organiser event management page
        public async Task<IActionResult> Manage()
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            var client = GetApiClient();

            var response = await client.GetAsync("api/events");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to retrieve events.";
                return View(new List<EventViewModel>());
            }

            var events = await response.Content.ReadFromJsonAsync<List<EventViewModel>>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return View(events ?? new List<EventViewModel>());
        }

        // Display create form
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            return View();
        }

        // Create event through API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventViewModel model)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            var client = GetApiClient();

            var response = await client.PostAsJsonAsync("api/events", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Could not create event. Check your information.");
                return View(model);
            }

            TempData["Success"] = "Event created successfully!";

            return RedirectToAction("Manage");
        }

        // Display edit form
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            var client = GetApiClient();

            var response = await client.GetAsync($"api/events/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var eventItem = await response.Content.ReadFromJsonAsync<EventViewModel>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (eventItem == null)
                return NotFound();

            return View(eventItem);
        }

        // Update event through API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventViewModel model)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            var client = GetApiClient();

            var response = await client.PutAsJsonAsync(
                $"api/events/{id}", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Could not update event.");
                return View(model);
            }

            TempData["Success"] = "Event updated successfully!";

            return RedirectToAction("Manage");
        }

        // Upload event banner through the API to Azure Blob Storage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadBanner(int id, IFormFile file)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select an event banner.";
                return RedirectToAction("Edit", new { id });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "Banner must not exceed 5 MB.";
                return RedirectToAction("Edit", new { id });
            }

            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                TempData["Error"] = "Only JPG, PNG and WEBP images are allowed.";
                return RedirectToAction("Edit", new { id });
            }

            using var content = new MultipartFormDataContent();

            await using var stream = file.OpenReadStream();

            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue(file.ContentType);

            content.Add(
                fileContent,
                "file",
                Path.GetFileName(file.FileName));

            var client = GetApiClient();

            var response = await client.PostAsync(
                $"api/events/{id}/banner",
                content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Event banner uploaded successfully!";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();

                TempData["Error"] =
                    $"Banner upload failed. API Status: {(int)response.StatusCode} ({response.StatusCode}). Details: {error}";
            }

            return RedirectToAction("Edit", new { id });
        }

        // Delete event through API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            var client = GetApiClient();

            var response = await client.DeleteAsync($"api/events/{id}");

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Event deleted successfully!";
            else
                TempData["Error"] = "Could not delete event.";

            return RedirectToAction("Manage");
        }
    }
}