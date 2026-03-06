using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalonPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StaffMemberUserNoAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffMembers_Users_UserId",
                table: "StaffMembers");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMembers_Users_UserId",
                table: "StaffMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffMembers_Users_UserId",
                table: "StaffMembers");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMembers_Users_UserId",
                table: "StaffMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
