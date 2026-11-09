using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDayMVC.Models;

namespace RaceDayMVC.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProfileController(IHttpClientFactory httpClientFactory)
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

        // View profile
        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("JWTToken")))
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = GetApiClient();

            var response = await client.GetAsync("api/users/profile");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to retrieve your profile.";
                return View(new ProfileViewModel());
            }

            var profile =
                await response.Content.ReadFromJsonAsync<ProfileViewModel>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return View(profile);
        }

        // Update profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("JWTToken")))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = GetApiClient();

            var update = new
            {
                model.FirstName,
                model.LastName,
                model.Email
            };

            var response = await client.PutAsJsonAsync(
                "api/users/profile", update);

            if (response.IsSuccessStatusCode)
            {
                HttpContext.Session.SetString(
                    "FirstName", model.FirstName);

                TempData["Success"] = "Profile updated successfully.";

                return RedirectToAction("Index");
            }

            var error = await response.Content.ReadAsStringAsync();

            ViewBag.Error =
                $"Unable to update profile. {error}";

            return View(model);
        }
    }
}