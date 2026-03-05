// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore; // <-- para AnyAsync
using Microsoft.Extensions.Logging;
using CrStudioFitnes.Models;
using Microsoft.AspNetCore.Identity;

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

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
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
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Normalizaciones simples
            Input.Nombre = Input.Nombre?.Trim();
            Input.Apellidos = Input.Apellidos?.Trim();
            Input.Cedula = Input.Cedula?.Trim();
            Input.TelefonoPersonal = string.IsNullOrWhiteSpace(Input.TelefonoPersonal) ? null : Input.TelefonoPersonal.Trim();
            Input.TelefonoEmergencia = string.IsNullOrWhiteSpace(Input.TelefonoEmergencia) ? null : Input.TelefonoEmergencia.Trim();
            Input.LesionOperacion = string.IsNullOrWhiteSpace(Input.LesionOperacion) ? null : Input.LesionOperacion.Trim();
            Input.Patologia = string.IsNullOrWhiteSpace(Input.Patologia) ? null : Input.Patologia.Trim();

            // Validación extra: evitar cédula duplicada
            if (!string.IsNullOrWhiteSpace(Input.Cedula))
            {
                var cedulaExiste = await _userManager.Users.AnyAsync(u => u.Cedula == Input.Cedula);
                if (cedulaExiste)
                {
                    ModelState.AddModelError("Input.Cedula", "Ya existe un usuario registrado con esa cédula.");
                }
            }

            if (!ModelState.IsValid)
                return Page();

            var user = CreateUser();

            // Username + email (como viene por defecto)
            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

            // Campos personalizados de tu ApplicationUser
            user.Nombre = Input.Nombre;
            user.Apellidos = Input.Apellidos;
            user.Cedula = Input.Cedula;
            user.TelefonoPersonal = Input.TelefonoPersonal;
            user.TelefonoEmergencia = Input.TelefonoEmergencia;
            user.LesionOperacion = Input.LesionOperacion;
            user.Patologia = Input.Patologia;

            user.EmailConfirmed = true;               // ? SIEMPRE confirmado
            user.NormalizedEmail = Input.Email?.Trim().ToUpperInvariant();
            user.NormalizedUserName = Input.Email?.Trim().ToUpperInvariant();

            // Opcional: también llenar PhoneNumber de Identity
            if (!string.IsNullOrWhiteSpace(Input.TelefonoPersonal))
                user.PhoneNumber = Input.TelefonoPersonal;

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                const string defaultRole = "Usuario";

                // (Opcional pero recomendado) Asegurar que el rol exista
                if (!await _roleManager.RoleExistsAsync(defaultRole))
                {
                    var createRole = await _roleManager.CreateAsync(new IdentityRole(defaultRole));
                    if (!createRole.Succeeded)
                    {
                        foreach (var e in createRole.Errors)
                            ModelState.AddModelError(string.Empty, e.Description);

                        return Page();
                    }
                }

                // Asignar rol por defecto
                var addRole = await _userManager.AddToRoleAsync(user, defaultRole);
                if (!addRole.Succeeded)
                {
                    foreach (var e in addRole.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);

                    return Page();
                }

                // Loguear directo (DESPUÉS de asignar rol)
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
