namespace AuthCodePKCEServerSide
{
    public interface ITokenCache
    {
        void SetTokens(string userId, string accessToken, string refreshToken);
        (string AccessToken, string RefreshToken)? GetTokens(string userId);
    }

    public class TokenCache : ITokenCache
    {
        private readonly Dictionary<string, (string AccessToken, string RefreshToken)> _cache = new();

        public void SetTokens(string userId, string accessToken, string refreshToken)
        {
            _cache[userId] = (accessToken, refreshToken);
        }

        public (string AccessToken, string RefreshToken)? GetTokens(string userId)
        {
            if (_cache.TryGetValue(userId, out var tokens))
            {
                return tokens;
            }

            return null;
        }
    }

}
