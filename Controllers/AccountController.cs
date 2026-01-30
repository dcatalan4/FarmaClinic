using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ControlInventario.Models;

namespace ControlInventario.Controllers
{
    public class AccountController : Controller
    {
        private readonly ControlFarmaclinicContext _context;

        public AccountController(ControlFarmaclinicContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Si ya está autenticado, redirigir al home
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Buscar usuario por nombre de usuario
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Usuario1 == model.Usuario && u.Activo == true);

                if (usuario != null)
                {
                    // Verificar contraseña (en producción usar hash)
                    if (VerifyPassword(model.Password, usuario.PasswordHash))
                    {
                        // Crear claims para el usuario
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, usuario.Nombre ?? usuario.Usuario1),
                            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                            new Claim(ClaimTypes.Role, usuario.Rol),
                            new Claim("Usuario", usuario.Usuario1),
                            new Claim("Nombre", usuario.Nombre ?? ""),
                            new Claim("IdUsuario", usuario.IdUsuario.ToString())
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.Recordarme,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                        };

                        // Iniciar sesión
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        // Redirigir a la página solicitada o al home
                        var returnUrl = Request.Query["ReturnUrl"];
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }

                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError("", "Usuario o contraseña incorrectos");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private bool VerifyPassword(string password, string hash)
        {
            // En producción usar BCrypt o similar
            // Por ahora, comparación simple (solo para desarrollo)
            return password == hash;
        }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "El usuario es requerido")]
        [Display(Name = "Usuario")]
        public string Usuario { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Display(Name = "Recordarme")]
        public bool Recordarme { get; set; }
    }
}
