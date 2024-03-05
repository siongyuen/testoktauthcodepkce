namespace AuthCodePKCEServerSide.TokenValidators
{
    public interface ICustomTokenHelper
    {
        Task<bool> ValidateToken(string token, IdpSettings idpSettings);
        Task<System.Security.Claims.ClaimsPrincipal> GetContextPrincipal(string token);
    }
}
