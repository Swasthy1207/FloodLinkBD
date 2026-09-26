using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FloodLink.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationHelpOfferAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationHelpOfferAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HelpOfferId = table.Column<int>(type: "integer", nullable: false),
                    VolunteerProfileId = table.Column<int>(type: "integer", nullable: false),
                    AssignedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Assigned"),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationHelpOfferAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationHelpOfferAssignments_AspNetUsers_AssignedByUser~",
                        column: x => x.AssignedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationHelpOfferAssignments_OrganizationHelpOffers_Hel~",
                        column: x => x.HelpOfferId,
                        principalTable: "OrganizationHelpOffers",
                        principalColumn: "HelpOfferId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationHelpOfferAssignments_VolunteerProfiles_Voluntee~",
                        column: x => x.VolunteerProfileId,
                        principalTable: "VolunteerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationHelpOfferAssignments_AssignedByUserId",
                table: "OrganizationHelpOfferAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationHelpOfferAssignments_HelpOfferId_VolunteerProfi~",
                table: "OrganizationHelpOfferAssignments",
                columns: new[] { "HelpOfferId", "VolunteerProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationHelpOfferAssignments_VolunteerProfileId",
                table: "OrganizationHelpOfferAssignments",
                column: "VolunteerProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationHelpOfferAssignments");
        }
    }
}
