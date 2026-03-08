using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Interfaces;
using SalonPro.Domain.Entities;
using SalonPro.Domain.Enums;
using SalonPro.Infrastructure.Persistence;

namespace SalonPro.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordService passwordService)
    {
        await context.Database.MigrateAsync();

        if (await context.Tenants.AnyAsync())
            return;

        // ── Tenant ─────────────────────────────────────────────────
        var tenant = new Tenant
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Demo Salon",
            Slug = "demo-salon",
            Email = "demo@salonpro.com",
            Phone = "+381 11 123 4567",
            Address = "Knez Mihailova 10",
            City = "Beograd",
            Country = "Serbia",
            IsActive = true,
            TimeZone = "Europe/Belgrade",
            Currency = "RSD",
            Language = "sr",
            EmailVerified = true,
            IsTrialing = false,
            SubscriptionStartDate = DateTime.UtcNow,
            SubscriptionEndDate = DateTime.UtcNow.AddYears(10),
            CreatedAt = DateTime.UtcNow
        };
        context.Tenants.Add(tenant);

        // ── Admin User ──────────────────────────────────────────────
        var adminUser = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            TenantId = tenant.Id,
            Email = "admin@salonpro.com",
            PasswordHash = passwordService.HashPassword("Admin123!"),
            FirstName = "Admin",
            LastName = "User",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(adminUser);

        // ── Service Categories ──────────────────────────────────────
        var hairCategory = new ServiceCategory
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
            TenantId = tenant.Id,
            Name = "Šišanje i frizura",
            Description = "Muško i žensko šišanje, farbanje, oblikovanje",
            Color = "#E91E63",
            ColorHex = "#E91E63",
            Type = ServiceCategoryType.Hair,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var nailsCategory = new ServiceCategory
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000011"),
            TenantId = tenant.Id,
            Name = "Manikir i Pedikir",
            Description = "Nega noktiju ruku i stopala",
            Color = "#9C27B0",
            ColorHex = "#9C27B0",
            Type = ServiceCategoryType.Nails,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var massageCategory = new ServiceCategory
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000012"),
            TenantId = tenant.Id,
            Name = "Masaža",
            Description = "Relaksaciona i terapeutska masaža",
            Color = "#009688",
            ColorHex = "#009688",
            Type = ServiceCategoryType.Massage,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.ServiceCategories.AddRange(hairCategory, nailsCategory, massageCategory);

        // ── Services ────────────────────────────────────────────────
        var services = new List<Service>
        {
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000020"),
                TenantId = tenant.Id,
                CategoryId = hairCategory.Id,
                Name = "Muško šišanje",
                Description = "Klasično muško šišanje sa završnom obradom",
                DurationMinutes = 30,
                Price = 1200,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000021"),
                TenantId = tenant.Id,
                CategoryId = hairCategory.Id,
                Name = "Žensko šišanje",
                Description = "Žensko šišanje i oblikovanje",
                DurationMinutes = 60,
                Price = 2500,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000022"),
                TenantId = tenant.Id,
                CategoryId = hairCategory.Id,
                Name = "Farbanje kose",
                Description = "Profesionalno farbanje kose",
                DurationMinutes = 120,
                Price = 5000,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000023"),
                TenantId = tenant.Id,
                CategoryId = nailsCategory.Id,
                Name = "Gel manikir",
                Description = "Gel lak manikir sa klasičnom obradom",
                DurationMinutes = 60,
                Price = 2200,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000024"),
                TenantId = tenant.Id,
                CategoryId = nailsCategory.Id,
                Name = "Pedikir",
                Description = "Kompletna nega stopala",
                DurationMinutes = 60,
                Price = 2000,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000025"),
                TenantId = tenant.Id,
                CategoryId = massageCategory.Id,
                Name = "Relaksaciona masaža (60 min)",
                Description = "Celo telo relaksaciona masaža",
                DurationMinutes = 60,
                Price = 3500,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
        context.Services.AddRange(services);

        // ── Staff Members ───────────────────────────────────────────
        var staff = new List<StaffMember>
        {
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000030"),
                TenantId = tenant.Id,
                FirstName = "Ana",
                LastName = "Jovanović",
                Email = "ana@salonpro.com",
                Phone = "+381 60 111 2233",
                Specialization = "Frizura i farbanje",
                IsActive = true,
                ColorIndex = 0,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000031"),
                TenantId = tenant.Id,
                FirstName = "Marko",
                LastName = "Petrović",
                Email = "marko@salonpro.com",
                Phone = "+381 60 222 3344",
                Specialization = "Muško šišanje",
                IsActive = true,
                ColorIndex = 1,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000032"),
                TenantId = tenant.Id,
                FirstName = "Milica",
                LastName = "Nikolić",
                Email = "milica@salonpro.com",
                Phone = "+381 60 333 4455",
                Specialization = "Manikir i pedikir",
                IsActive = true,
                ColorIndex = 2,
                CreatedAt = DateTime.UtcNow
            },
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000033"),
                TenantId = tenant.Id,
                FirstName = "Stefan",
                LastName = "Đorđević",
                Email = "stefan@salonpro.com",
                Phone = "+381 60 444 5566",
                Specialization = "Masaža",
                IsActive = true,
                ColorIndex = 3,
                CreatedAt = DateTime.UtcNow
            }
        };
        context.StaffMembers.AddRange(staff);

        // ── Working Hours (Mon-Fri 09:00-18:00, Sat 09:00-14:00) ───
        var workingHours = new List<WorkingHours>();
        foreach (var member in staff)
        {
            for (int day = 1; day <= 5; day++) // Mon-Fri
            {
                workingHours.Add(new WorkingHours
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    StaffMemberId = member.Id,
                    DayOfWeek = (DayOfWeek)day,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    IsWorkingDay = true
                });
            }
            workingHours.Add(new WorkingHours
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                StaffMemberId = member.Id,
                DayOfWeek = DayOfWeek.Saturday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(14, 0, 0),
                IsWorkingDay = true
            });
            workingHours.Add(new WorkingHours
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                StaffMemberId = member.Id,
                DayOfWeek = DayOfWeek.Sunday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                IsWorkingDay = false
            });
        }
        context.WorkingHours.AddRange(workingHours);

        // ── Clients ─────────────────────────────────────────────────
        var clients = new List<Client>
        {
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000040"),
                TenantId = tenant.Id,
                FirstName = "Jelena",
                LastName = "Marković",
                Email = "jelena@example.com",
                Phone = "+381 60 555 6677",
                IsActive = true,
                IsVip = false,
                CreatedAt = DateTime.UtcNow.AddDays(-90)
            },
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000041"),
                TenantId = tenant.Id,
                FirstName = "Nikola",
                LastName = "Stojanović",
                Email = "nikola@example.com",
                Phone = "+381 60 666 7788",
                IsActive = true,
                IsVip = true,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            new() {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000042"),
                TenantId = tenant.Id,
                FirstName = "Maja",
                LastName = "Ilić",
                Email = "maja@example.com",
                Phone = "+381 60 777 8899",
                IsActive = true,
                IsVip = false,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            }
        };
        context.Clients.AddRange(clients);

        await context.SaveChangesAsync();
    }
}
