using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDayMVC.Models;

namespace RaceDayMVC.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CategoriesController(IHttpClientFactory httpClientFactory)
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

        // View categories belonging to an event
        public async Task<IActionResult> Manage(int eventId)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            var client = GetApiClient();

            var response = await client.GetAsync(
                $"api/events/{eventId}/categories");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to retrieve categories.";
                return View(new List<CategoryViewModel>());
            }

            var categories =
                await response.Content.ReadFromJsonAsync<List<CategoryViewModel>>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

            ViewBag.EventID = eventId;

            return View(categories ?? new List<CategoryViewModel>());
        }

        // Display category creation form
        [HttpGet]
        public IActionResult Create(int eventId)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            return View(new CategoryViewModel
            {
                EventID = eventId
            });
        }

        // Create category through API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            var client = GetApiClient();

            var response = await client.PostAsJsonAsync(
                $"api/events/{model.EventID}/categories", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("",
                    "Unable to create category. Check your information and event ownership.");

                return View(model);
            }

            TempData["Success"] = "Category created successfully!";

            return RedirectToAction("Manage",
                new { eventId = model.EventID });
        }

        // Display category edit form
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int eventId)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            var client = GetApiClient();

            var response = await client.GetAsync(
                $"api/events/{eventId}/categories");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var categories =
                await response.Content.ReadFromJsonAsync<List<CategoryViewModel>>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

            var category = categories?.FirstOrDefault(
                c => c.CategoryID == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // Update category through API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryViewModel model)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
                return View(model);

            var client = GetApiClient();

            var response = await client.PutAsJsonAsync(
                $"api/categories/{id}", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("",
                    "Unable to update category.");

                return View(model);
            }

            TempData["Success"] = "Category updated successfully!";

            return RedirectToAction("Manage",
                new { eventId = model.EventID });
        }

        // Delete category through API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int eventId)
        {
            if (!IsOrganiser())
                return RedirectToAction("Login", "Auth");

            var client = GetApiClient();

            var response = await client.DeleteAsync(
                $"api/categories/{id}");

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Category deleted successfully!";
            else
                TempData["Error"] = "Unable to delete category.";

            return RedirectToAction("Manage",
                new { eventId });
        }
    }
}