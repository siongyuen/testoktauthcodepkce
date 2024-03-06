namespace AuthCodePKCEServerSide
{
    public interface ITokenCache
    {
        void SetTokens(string userId, string refreshToken);
         string? GetRefreshToken(string userId);
    }

    public class TokenCache : ITokenCache
    {
        private readonly Dictionary<string, string> _cache = new();

        public void SetTokens(string userId,  string refreshToken)
        {
            _cache[userId] = refreshToken;
        }

        public string? GetRefreshToken(string userId)
        {
            if (_cache.TryGetValue(userId, out var refreshToken))
            {
                return refreshToken;
            }

            return null;
        }
    }

}
