using System.Security.Claims;

namespace AuthCodePKCEServerSide.TokenValidators
{
    public class GoogleTokenValidator : ICustomTokenHelper

    {
        public Task<ClaimsPrincipal> GetContextPrincipal(string token)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateToken(string token, IdpSettings idpSettings)
        {
            throw new NotImplementedException();
        }
    }
}
