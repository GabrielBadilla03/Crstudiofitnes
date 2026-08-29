#nullable disable

using System.ComponentModel.DataAnnotations;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CrStudioFitnes.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Ingresá tu cédula o correo.")]
            [Display(Name = "Cédula o correo")]
            public string Usuario { get; set; }

            [Required(ErrorMessage = "Ingresá tu contraseña.")]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [Display(Name = "Recordarme")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();

            if (!ModelState.IsValid)
                return Page();

            var identificador = (Input.Usuario ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(identificador))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Ingresá tu cédula o correo.");
                return Page();
            }

            ApplicationUser user = null;

            if (identificador.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(identificador);
            }

            if (user == null)
            {
                var cedula = NormalizarCedula(identificador);

                user = await _userManager.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u =>
                        u.Cedula
                            .Replace("-", "")
                            .Replace(".", "")
                            .Replace(" ", "") == cedula
                        || u.UserName == identificador);
            }

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Datos incorrectos. Verificá cédula/correo y contraseña.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage(
                    "./LoginWith2fa",
                    new
                    {
                        ReturnUrl = returnUrl,
                        RememberMe = Input.RememberMe
                    });
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tu cuenta está temporalmente bloqueada o desactivada. Contactá al administrador si el problema continúa.");
                return Page();
            }

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No se permite el acceso a esta cuenta.");
                return Page();
            }

            ModelState.AddModelError(
                string.Empty,
                "Datos incorrectos. Verificá cédula/correo y contraseña.");
            return Page();
        }

        private static string NormalizarCedula(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace("-", "")
                .Replace(".", "")
                .Replace(" ", "");
        }
    }
}
