using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodLink.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityReporterFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AffectedPersonName",
                table: "HelpRequests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyNotes",
                table: "HelpRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "HelpRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VerificationNotes",
                table: "HelpRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "HelpRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedById",
                table: "HelpRequests",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequests_VerifiedById",
                table: "HelpRequests",
                column: "VerifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_HelpRequests_AspNetUsers_VerifiedById",
                table: "HelpRequests",
                column: "VerifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HelpRequests_AspNetUsers_VerifiedById",
                table: "HelpRequests");

            migrationBuilder.DropIndex(
                name: "IX_HelpRequests_VerifiedById",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "AffectedPersonName",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "EmergencyNotes",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "VerificationNotes",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "VerifiedById",
                table: "HelpRequests");
        }
    }
}
