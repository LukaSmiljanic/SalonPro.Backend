using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalonPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSubscriptionAndVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "Tenants",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenExpiry",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionStartDate",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionEndDate",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrialing",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Existing tenants (Demo Salon) should be verified and have active subscription
            migrationBuilder.Sql(@"
                UPDATE Tenants 
                SET EmailVerified = 1, 
                    IsTrialing = 0,
                    SubscriptionStartDate = GETUTCDATE(),
                    SubscriptionEndDate = DATEADD(YEAR, 10, GETUTCDATE())
                WHERE EmailVerified = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EmailVerified", table: "Tenants");
            migrationBuilder.DropColumn(name: "EmailVerificationToken", table: "Tenants");
            migrationBuilder.DropColumn(name: "EmailVerificationTokenExpiry", table: "Tenants");
            migrationBuilder.DropColumn(name: "SubscriptionStartDate", table: "Tenants");
            migrationBuilder.DropColumn(name: "SubscriptionEndDate", table: "Tenants");
            migrationBuilder.DropColumn(name: "IsTrialing", table: "Tenants");
        }
    }
}
