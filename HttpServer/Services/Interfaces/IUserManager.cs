using Backend.Models;

namespace Backend.asp.Services.Interfaces;

public interface IUserManager
{

    public Task<List<User>> GetUsers();
    public Task UpdateUser(int userId, string? newEmail = null, string? newFirstname = null, string? newLastname = null, string? newphoneNumber = null, string? newPasswordHash = null, string? postalCode = null, string? country = null, string? city = null);
    public Task<User?> CreateUser(User user);
    public Task<User?> LogInUser(LogInModel user);

    public Task<User?> GetUserById(int id);

}

