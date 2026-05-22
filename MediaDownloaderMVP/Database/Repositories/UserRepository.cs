using MediaDownloaderTgBotMVP.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaDownloaderTgBotMVP.Database.Repositories
{
    public class UserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<User> GetOrCreateAsync(long telegramId, string? username, string? firstName)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);

            if (user == null)
            {
                user = new User
                {
                    TelegramId = telegramId,
                    Username = username,
                    FirstName = firstName
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
                Console.WriteLine($"[DB] Новий юзер: {firstName} (@{username})");
            }
            else
            {
                user.LastActiveAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return user;
        }

    }
}
