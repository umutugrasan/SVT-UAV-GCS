using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using GCS_Module.Data;
using GCS_Module.Helpers;
using GCS_Module.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GCS_Module.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
            EnsureAdminExists();
        }

        private void EnsureAdminExists()
        {
            if (!_context.Users.Any())
            {
                _context.Users.Add(new User
                {
                    Username = "admin",
                    PasswordHash = PasswordHasher.HashPassword("admin123"),
                    FullName = "Sistem Yöneticisi",
                    Role = "Admin"
                });
                _context.SaveChanges();
            }
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null ||
                !PasswordHasher.VerifyPassword(model.Password, user.PasswordHash, out bool needsUpgrade))
            {
                ModelState.AddModelError("", "Geçersiz kullanıcı adı veya şifre");
                return View(model);
            }

            // Eski (SHA256) hash ile giriş yapıldıysa, şeffaf şekilde PBKDF2'ye yükselt.
            if (needsUpgrade)
            {
                user.PasswordHash = PasswordHasher.HashPassword(model.Password);
            }

            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName),
                new Claim("UserId", user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            return LocalRedirect(returnUrl ?? "/");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}