using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodLink.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VolunteerEmail",
                table: "HelpRequests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VolunteerEmail",
                table: "HelpRequests");
        }
    }
}
