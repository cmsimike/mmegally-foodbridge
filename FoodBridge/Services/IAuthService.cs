namespace FoodBridge.Services
{
    public interface IAuthService
    {
        string GenerateToken(Guid userId);
        bool ValidateToken(string token, out Guid userId);
    }
}
