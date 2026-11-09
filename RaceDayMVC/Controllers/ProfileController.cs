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

        // Upload profile picture
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile file)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("JWTToken")))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a picture.";
                return RedirectToAction("Index");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] =
                    "Picture must not exceed 5 MB.";

                return RedirectToAction("Index");
            }

            var allowedTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedTypes.Contains(
                file.ContentType.ToLowerInvariant()))
            {
                TempData["Error"] =
                    "Only JPG, PNG and WEBP pictures are allowed.";

                return RedirectToAction("Index");
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
                "api/users/profile/picture",
                content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] =
                    "Profile picture uploaded successfully.";
            }
            else
            {
                var error =
                    await response.Content.ReadAsStringAsync();

                TempData["Error"] =
                    $"Unable to upload profile picture. {error}";
            }

            return RedirectToAction("Index");
        }

        // Retrieve profile picture through the API
        [HttpGet]
        public async Task<IActionResult> Picture()
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("JWTToken")))
            {
                return Unauthorized();
            }

            var client = GetApiClient();

            var response = await client.GetAsync(
                "api/users/profile/picture");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var imageBytes =
                await response.Content.ReadAsByteArrayAsync();

            var contentType =
                response.Content.Headers.ContentType?.MediaType
                ?? "image/jpeg";

            return File(imageBytes, contentType);
        }
    }
}