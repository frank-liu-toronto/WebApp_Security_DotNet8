using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;
using System.ComponentModel.DataAnnotations;
using WebApp.Data.Account;

namespace WebApp.Pages.Account
{
    [Authorize]
    public class AuthenticatorWithMFASetupModel : PageModel
    {
        private readonly UserManager<User> userManager;

        [BindProperty]
        public SetupMFAViewModel ViewModel { get; set; }

        [BindProperty]
        public bool Succeeded { get; set; }

        public AuthenticatorWithMFASetupModel(UserManager<User> userManager)
        {
            this.userManager = userManager;
            this.ViewModel = new SetupMFAViewModel();
            this.Succeeded = false;
        }

        public async Task OnGetAsync()
        {
            var user = await userManager.GetUserAsync(base.User);
            if (user != null)
            {
                await userManager.ResetAuthenticatorKeyAsync(user);
                var key = await userManager.GetAuthenticatorKeyAsync(user);
                this.ViewModel.Key = key??string.Empty;
                this.ViewModel.QRCodeBytes = GenerateQRCodeBytes(
                    "my web app",
                    this.ViewModel.Key,
                    user.Email ?? string.Empty);
            }
            
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await userManager.GetUserAsync(base.User);
            if (user != null && await userManager.VerifyTwoFactorTokenAsync(
                    user, 
                    userManager.Options.Tokens.AuthenticatorTokenProvider,
                    this.ViewModel.SecurityCode))
            {
                await userManager.SetTwoFactorEnabledAsync(user, true);
                this.Succeeded = true;
            }
            else
            {
                ModelState.AddModelError("AuthenticatorSetup", "Something went wrong with the authenticator setup.");
            }

            return Page();
        }

        private Byte[] GenerateQRCodeBytes(string provider, string key, string userEmail)
        {
            var qrCodeGenerator = new QRCodeGenerator();
            var qrCodeData = qrCodeGenerator.CreateQrCode(
                $"otpauth://totp/{provider}:{userEmail}?secret={key}&issuer={provider}",
                QRCodeGenerator.ECCLevel.Q);

            var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }
    }

    public class SetupMFAViewModel
    {
        public string? Key { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string SecurityCode { get; set; } = string.Empty;

        public Byte[]? QRCodeBytes { get; set; }
    }
}
