using Reboot.Models;

namespace Reboot.Services;

public interface IUserService
{
    IReadOnlyList<User> GetAllUsers();
    User? GetUserById(string id);
    User? GetUserByEmail(string email);
    (bool Success, string? Error, User? User) Authenticate(string email, string password);
    (bool Success, string? Error, User? User) RegisterUser(string firstName, string lastName, string email, string city, string password);
    bool VerifyUser(string userId);
    IReadOnlyList<User> GetUnverifiedUsers();
    (bool Success, string? Error) UpdateProfile(string userId, string firstName, string lastName, string city);
    (bool Success, string? Error) ChangePassword(string userId, string currentPassword, string newPassword);
    (bool Success, string? ResetToken) GeneratePasswordResetToken(string email);
    (bool Success, string? Error) ResetPasswordWithToken(string email, string token, string newPassword);
}
