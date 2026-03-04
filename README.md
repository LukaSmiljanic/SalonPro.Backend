# SalonPro Backend

ASP.NET Core 8 REST API for the SalonPro salon management platform. Built with Clean Architecture (Domain / Application / Infrastructure / API layers), CQRS via MediatR, multi-tenant support, and JWT authentication.

## Architecture

```
SalonPro.sln
├── src/
│   ├── SalonPro.Domain          # Entities, enums, interfaces (no external deps)
│   ├── SalonPro.Application     # CQRS handlers, DTOs, validators, interfaces
│   ├── SalonPro.Infrastructure  # EF Core, JWT, email, seed data
│   └── SalonPro.API             # ASP.NET Core controllers, middleware, DI
└── tests/
    └── (future test projects)
```

## Tech Stack

| Layer | Technology |
|-------|------------|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL |
| CQRS | MediatR 12 |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| Auth | JWT Bearer + Refresh Tokens |
| API Docs | Swagger / Swashbuckle |
| Logging | Serilog |

## Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL 15+
- (optional) Docker

### Configuration

Copy `appsettings.json` and create `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=salonpro;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "your-very-long-secret-key-at-least-32-chars",
    "Issuer": "SalonPro",
    "Audience": "SalonProApp",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "TenantSettings": {
    "DefaultTenantId": "00000000-0000-0000-0000-000000000001"
  }
}
```

### Run

```bash
# Restore & build
dotnet restore
dotnet build

# Apply migrations
dotnet ef database update --project src/SalonPro.Infrastructure --startup-project src/SalonPro.API

# Run
dotnet run --project src/SalonPro.API
```

Swagger UI available at `https://localhost:7xxx/swagger`.

## Project Structure

### Domain Layer (`SalonPro.Domain`)
- **Entities**: Tenant, User, Client, Service, ServiceCategory, StaffMember, Appointment, AppointmentService, ClientNote, WorkingHours
- **Enums**: AppointmentStatus, UserRole, ServiceCategoryType
- **Interfaces**: IRepository<T>, IUnitOfWork, ICurrentTenantService, ICurrentUserService

### Application Layer (`SalonPro.Application`)
- CQRS with MediatR (Commands / Queries)
- FluentValidation validators
- AutoMapper profiles
- Pipeline behaviors: ValidationBehaviour, LoggingBehaviour

### Infrastructure Layer (`SalonPro.Infrastructure`)
- `ApplicationDbContext` (EF Core + PostgreSQL)
- Generic `Repository<T>` and `UnitOfWork`
- Multi-tenant query filters
- JWT token service
- Password hashing (BCrypt)
- Seed data for initial tenant and admin user

### API Layer (`SalonPro.API`)
- RESTful controllers for all resources
- Global exception handling middleware
- JWT Bearer authentication
- Swagger with JWT support
- Serilog request logging
