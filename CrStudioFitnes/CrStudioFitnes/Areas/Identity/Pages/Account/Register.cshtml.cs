#nullable disable

using System.ComponentModel.DataAnnotations;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CrStudioFitnes.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required, StringLength(60)]
            [Display(Name = "Nombre")]
            public string Nombre { get; set; }

            [Required, StringLength(80)]
            [Display(Name = "Apellidos")]
            public string Apellidos { get; set; }

            [Required, StringLength(25)]
            [Display(Name = "Cédula")]
            public string Cedula { get; set; }

            [StringLength(25)]
            [Display(Name = "Teléfono personal")]
            public string TelefonoPersonal { get; set; }

            [StringLength(25)]
            [Display(Name = "Teléfono de emergencia")]
            public string TelefonoEmergencia { get; set; }

            [StringLength(120)]
            [Display(Name = "Lesión u operación")]
            public string LesionOperacion { get; set; }

            [StringLength(120)]
            [Display(Name = "Patología")]
            public string Patologia { get; set; }

            [Required, EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(
                100,
                ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.",
                MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync())
                .ToList();

            Input.Nombre = Input.Nombre?.Trim();
            Input.Apellidos = Input.Apellidos?.Trim();
            Input.Cedula = NormalizarCedula(Input.Cedula);
            Input.Email = Input.Email?.Trim();
            Input.TelefonoPersonal = LimpiarOpcional(Input.TelefonoPersonal);
            Input.TelefonoEmergencia = LimpiarOpcional(Input.TelefonoEmergencia);
            Input.LesionOperacion = LimpiarOpcional(Input.LesionOperacion);
            Input.Patologia = LimpiarOpcional(Input.Patologia);

            if (string.IsNullOrWhiteSpace(Input.Cedula))
            {
                ModelState.AddModelError("Input.Cedula", "Ingresá una cédula válida.");
            }
            else
            {
                // También detecta usuarios históricos guardados con guiones,
                // puntos o espacios.
                var cedulaNormalizada = Input.Cedula;

                var cedulaExiste = await _userManager.Users
                    .AsNoTracking()
                    .AnyAsync(u =>
                        u.Cedula
                            .Replace("-", "")
                            .Replace(".", "")
                            .Replace(" ", "") == cedulaNormalizada);

                if (cedulaExiste)
                {
                    ModelState.AddModelError(
                        "Input.Cedula",
                        "Ya existe un usuario registrado con esa cédula.");
                }
            }

            if (!ModelState.IsValid)
                return Page();

            var user = CreateUser();

            await _userStore.SetUserNameAsync(
                user,
                Input.Email,
                CancellationToken.None);

            await _emailStore.SetEmailAsync(
                user,
                Input.Email,
                CancellationToken.None);

            user.Nombre = Input.Nombre;
            user.Apellidos = Input.Apellidos;
            user.Cedula = Input.Cedula;
            user.TelefonoPersonal = Input.TelefonoPersonal;
            user.TelefonoEmergencia = Input.TelefonoEmergencia;
            user.LesionOperacion = Input.LesionOperacion;
            user.Patologia = Input.Patologia;
            user.CantidadFamilia = null;
            user.Familiar = false;

            if (!string.IsNullOrWhiteSpace(Input.TelefonoPersonal))
                user.PhoneNumber = Input.TelefonoPersonal;

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                const string defaultRole = "Usuario";

                if (!await _roleManager.RoleExistsAsync(defaultRole))
                {
                    var createRole = await _roleManager.CreateAsync(
                        new IdentityRole(defaultRole));

                    if (!createRole.Succeeded)
                    {
                        foreach (var error in createRole.Errors)
                            ModelState.AddModelError(string.Empty, error.Description);

                        // Evitar dejar una cuenta creada sin rol si no pudo
                        // completarse el registro.
                        await _userManager.DeleteAsync(user);
                        return Page();
                    }
                }

                var addRole = await _userManager.AddToRoleAsync(user, defaultRole);

                if (!addRole.Succeeded)
                {
                    foreach (var error in addRole.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    await _userManager.DeleteAsync(user);
                    return Page();
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

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

        private static string LimpiarOpcional(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class " +
                    "and has a parameterless constructor.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException(
                    "The default UI requires a user store with email support.");
            }

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
