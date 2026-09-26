using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodLink.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingModelColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "HelpRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletedByVolunteerId",
                table: "HelpRequests",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VolunteerProgressNote",
                table: "HelpRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequests_CompletedByVolunteerId",
                table: "HelpRequests",
                column: "CompletedByVolunteerId");

            migrationBuilder.AddForeignKey(
                name: "FK_HelpRequests_AspNetUsers_CompletedByVolunteerId",
                table: "HelpRequests",
                column: "CompletedByVolunteerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HelpRequests_AspNetUsers_CompletedByVolunteerId",
                table: "HelpRequests");

            migrationBuilder.DropIndex(
                name: "IX_HelpRequests_CompletedByVolunteerId",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "CompletedByVolunteerId",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "VolunteerProgressNote",
                table: "HelpRequests");
        }
    }
}
