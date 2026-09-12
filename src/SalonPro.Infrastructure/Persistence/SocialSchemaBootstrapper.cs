using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalonPro.Application.Common.Interfaces;

namespace SalonPro.Infrastructure.Persistence;

public class SocialSchemaBootstrapper : ISocialSchemaBootstrapper
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SocialSchemaBootstrapper> _logger;

    private static readonly (string Name, string Sql)[] Steps =
    [
        ("SocialPosts table", @"
            IF OBJECT_ID('SocialPosts', 'U') IS NULL
            BEGIN
                CREATE TABLE SocialPosts (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    TenantId UNIQUEIDENTIFIER NOT NULL,
                    ScheduledAt DATETIME2 NOT NULL,
                    Topic NVARCHAR(120) NOT NULL,
                    Caption NVARCHAR(2200) NOT NULL,
                    Hashtags NVARCHAR(500) NOT NULL DEFAULT '',
                    ImagePrompt NVARCHAR(500) NULL,
                    Status INT NOT NULL DEFAULT 0,
                    PublishedAt DATETIME2 NULL,
                    FailureReason NVARCHAR(500) NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    CreatedBy NVARCHAR(256) NULL,
                    UpdatedAt DATETIME2 NULL,
                    UpdatedBy NVARCHAR(256) NULL,
                    CONSTRAINT FK_SocialPosts_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_SocialPosts_TenantId_ScheduledAt ON SocialPosts (TenantId, ScheduledAt);
            END"),
        ("SocialPosts columns", @"
            IF COL_LENGTH('SocialPosts', 'ImageUrl') IS NULL
                ALTER TABLE SocialPosts ADD ImageUrl NVARCHAR(1000) NULL;
            IF COL_LENGTH('SocialPosts', 'InstagramMediaId') IS NULL
                ALTER TABLE SocialPosts ADD InstagramMediaId NVARCHAR(128) NULL;
            IF COL_LENGTH('SocialPosts', 'LastModifiedAt') IS NULL
                ALTER TABLE SocialPosts ADD LastModifiedAt DATETIME2 NULL;"),
        ("TenantInstagramAccounts table", @"
            IF OBJECT_ID('TenantInstagramAccounts', 'U') IS NULL
            BEGIN
                CREATE TABLE TenantInstagramAccounts (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    TenantId UNIQUEIDENTIFIER NOT NULL,
                    FacebookPageId NVARCHAR(64) NOT NULL,
                    InstagramUserId NVARCHAR(64) NOT NULL,
                    InstagramUsername NVARCHAR(128) NULL,
                    ProtectedAccessToken NVARCHAR(MAX) NOT NULL,
                    TokenExpiresAt DATETIME2 NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    CreatedBy NVARCHAR(256) NULL,
                    UpdatedAt DATETIME2 NULL,
                    UpdatedBy NVARCHAR(256) NULL,
                    CONSTRAINT FK_TenantInstagramAccounts_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(Id) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IX_TenantInstagramAccounts_TenantId ON TenantInstagramAccounts (TenantId);
            END"),
        ("TenantInstagramAccounts LastModifiedAt", @"
            IF COL_LENGTH('TenantInstagramAccounts', 'LastModifiedAt') IS NULL
                ALTER TABLE TenantInstagramAccounts ADD LastModifiedAt DATETIME2 NULL;"),
        ("SocialGalleryImages table", @"
            IF OBJECT_ID('SocialGalleryImages', 'U') IS NULL
            BEGIN
                CREATE TABLE SocialGalleryImages (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    TenantId UNIQUEIDENTIFIER NOT NULL,
                    FileName NVARCHAR(260) NOT NULL,
                    RelativePath NVARCHAR(500) NOT NULL,
                    ContentType NVARCHAR(100) NOT NULL DEFAULT 'image/jpeg',
                    FileSizeBytes BIGINT NOT NULL DEFAULT 0,
                    CaptionHint NVARCHAR(500) NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    CreatedBy NVARCHAR(256) NULL,
                    UpdatedAt DATETIME2 NULL,
                    UpdatedBy NVARCHAR(256) NULL,
                    LastModifiedAt DATETIME2 NULL,
                    CONSTRAINT FK_SocialGalleryImages_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_SocialGalleryImages_TenantId_CreatedAt ON SocialGalleryImages (TenantId, CreatedAt DESC);
            END"),
        ("Tenant branding columns", @"
            IF COL_LENGTH('Tenants', 'PrimaryColor') IS NULL
                ALTER TABLE Tenants ADD PrimaryColor NVARCHAR(20) NULL;
            IF COL_LENGTH('Tenants', 'AccentColor') IS NULL
                ALTER TABLE Tenants ADD AccentColor NVARCHAR(20) NULL;
            IF COL_LENGTH('Tenants', 'OnlineBookingEnabled') IS NULL
                ALTER TABLE Tenants ADD OnlineBookingEnabled BIT NOT NULL CONSTRAINT DF_Tenants_OnlineBookingEnabled DEFAULT 1;"),
    ];

    public SocialSchemaBootstrapper(ApplicationDbContext context, ILogger<SocialSchemaBootstrapper> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SocialSchemaBootstrapResult> EnsureAsync(CancellationToken cancellationToken = default)
    {
        var log = new List<string>();
        var ok = true;

        foreach (var (name, sql) in Steps)
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
                log.Add($"OK: {name}");
                _logger.LogInformation("Social schema bootstrap OK: {Step}", name);
            }
            catch (Exception ex)
            {
                ok = false;
                var msg = $"FAIL: {name} — {ex.Message}";
                log.Add(msg);
                _logger.LogError(ex, "Social schema bootstrap failed: {Step}", name);
            }
        }

        return new SocialSchemaBootstrapResult(ok, log);
    }
}
