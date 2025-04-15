using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace FoodBridge.Services
{
    public class CustomAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IAuthService _authService;

        public CustomAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IAuthService authService)
            : base(options, logger, encoder, clock)
        {
            _authService = authService;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing or invalid authorization header"));
            }

            var token = authHeader.Substring("Bearer ".Length);
            if (_authService.ValidateToken(token, out var userId))
            {
                var claims = new[] { new Claim(ClaimTypes.Name, userId.ToString()) };
                var identity = new ClaimsIdentity(claims, "Bearer");
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, "Bearer");

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        }
    }
}
