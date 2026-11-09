using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDayMVC.Models;

namespace RaceDayMVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            var response = await client.PostAsJsonAsync(
                "api/auth/login", model);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

            var root = document.RootElement;
            var user = root.GetProperty("user");

            HttpContext.Session.SetString(
                "JWTToken", root.GetProperty("token").GetString()!);

            HttpContext.Session.SetString(
                "UserID", user.GetProperty("userID").ToString());

            HttpContext.Session.SetString(
                "FirstName", user.GetProperty("firstName").GetString()!);

            HttpContext.Session.SetString(
                "LastName", user.GetProperty("lastName").GetString()!);

            HttpContext.Session.SetString(
                "Email", user.GetProperty("email").GetString()!);

            HttpContext.Session.SetString(
                "Role", user.GetProperty("role").GetString()!);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            var response = await client.PostAsJsonAsync(
                "api/auth/register", model);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                ModelState.AddModelError(
                    "", "Registration failed. " + error);

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Registration successful! Please log in.";

            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}