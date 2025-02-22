namespace FoodBridge.Services
{
    public class InMemoryAuthService : IAuthService
    {
        private readonly Dictionary<string, TokenInfo> _tokens = new();
        private readonly TimeSpan _tokenExpiration = TimeSpan.FromHours(24);

        private class TokenInfo
        {
            public Guid UserId { get; set; }
            public DateTime ExpirationTime { get; set; }
        }

        public string GenerateToken(Guid userId)
        {
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            _tokens[token] = new TokenInfo
            {
                UserId = userId,
                ExpirationTime = DateTime.UtcNow.Add(_tokenExpiration),
            };
            return token;
        }

        public bool ValidateToken(string token, out Guid userId)
        {
            userId = Guid.Empty;
            if (!_tokens.TryGetValue(token, out var tokenInfo))
                return false;

            if (tokenInfo.ExpirationTime < DateTime.UtcNow)
            {
                _tokens.Remove(token);
                return false;
            }

            userId = tokenInfo.UserId;
            return true;
        }
    }
}
