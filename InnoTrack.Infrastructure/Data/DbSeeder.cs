using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace InnoTrack.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var adminExists = await unitOfWork.Repository<User>().FindAsync(u => u.Role == UserRole.Admin);

            if (adminExists == null)
            {
                var adminUser = new User
                {
                    FirstName = "System",
                    LastName = "Admin",
                    Email = "admin@innotrack.com",
                    PasswordHash = passwordHasher.Hash("Admin@1234"),
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await unitOfWork.Repository<User>().AddAsync(adminUser);
                await unitOfWork.CompleteAsync();
            }
        }
    }
}