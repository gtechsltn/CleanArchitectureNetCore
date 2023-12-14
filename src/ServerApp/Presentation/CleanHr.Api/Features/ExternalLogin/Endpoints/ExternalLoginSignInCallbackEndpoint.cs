﻿using System.Security.Claims;
using CleanHr.Api.Helpers;
using CleanHr.Domain.Aggregates.IdentityAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace CleanHr.Api.Features.ExternalLogin.Endpoints;

[ApiVersion("1.0")]
public class ExternalLoginSignInCallbackEndpoint(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<ExternalLoginSignInCallbackEndpoint> logger,
    TokenManager tokenGenerator) : ExternalLoginEndpointBase
{
    public string ClientLoginUrl => "https://localhost:44364/identity/login";

    public string ErrorMessage { get; set; }

    // GET: /Account/ExternalLoginSignInCallback
    [HttpGet("sign-in-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(string remoteError = null)
    {
        if (remoteError != null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectWithError(ErrorMessage);
        }

        ExternalLoginInfo externalLoginInfo = await signInManager.GetExternalLoginInfoAsync();

        if (externalLoginInfo == null)
        {
            ErrorMessage = "Invalid external login request.";
            return RedirectWithError(ErrorMessage);
        }

        ApplicationUser applicationUser = await userManager.FindByLoginAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey);

        if (applicationUser == null)
        {
            string email = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                ErrorMessage = "The provided external login information is not linked to any account.";
                return RedirectWithError(ErrorMessage);
            }

            applicationUser = await userManager.FindByEmailAsync(email);

            if (applicationUser == null)
            {
                ErrorMessage = "The provided external login information is not linked to any account.";
                return RedirectWithError(ErrorMessage);
            }

            IdentityResult addExternalLoginResult = await userManager.AddLoginAsync(applicationUser, externalLoginInfo);

            if (!addExternalLoginResult.Succeeded)
            {
                ErrorMessage = addExternalLoginResult.Errors.Select(e => e.Description).FirstOrDefault();
                return RedirectWithError(ErrorMessage);
            }
        }

        // Sign in the user with this external login provider if the user already has a login.
        SignInResult signInResult = await signInManager
             .ExternalLoginSignInAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey, isPersistent: false);

        if (signInResult.Succeeded)
        {
            // Update any authentication tokens if login succeeded
            await signInManager.UpdateExternalAuthenticationTokensAsync(externalLoginInfo);

            logger.LogInformation(5, "User logged in with {Name} provider.", externalLoginInfo.LoginProvider);

            string jwt = await tokenGenerator.GetJwtTokenAsync(applicationUser);
            string redirectUrl = QueryHelpers.AddQueryString(ClientLoginUrl, "jwt", jwt);
            return Redirect(redirectUrl);
        }

        if (signInResult.RequiresTwoFactor)
        {
            ErrorMessage = "Require two factor authentication.";
        }
        else if (signInResult.IsLockedOut)
        {
            ErrorMessage = "This account has been locked out, please try again later.";
        }
        else
        {
            ErrorMessage = "The provied external login info is not valid.";
        }

        return RedirectWithError(ErrorMessage);
    }

    private RedirectResult RedirectWithError(string errorMessage)
    {
        string redirectUrl = QueryHelpers.AddQueryString(ClientLoginUrl, "error", errorMessage);
        return Redirect(redirectUrl);
    }
}
