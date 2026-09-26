using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FloodLink.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationHelpOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationHelpOffers",
                columns: table => new
                {
                    HelpOfferId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HelpNeedId = table.Column<int>(type: "integer", nullable: false),
                    OfferingOrganizationId = table.Column<int>(type: "integer", nullable: false),
                    OfferedHelpType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OfferedQuantity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationHelpOffers", x => x.HelpOfferId);
                    table.ForeignKey(
                        name: "FK_OrganizationHelpOffers_OrganizationHelpNeeds_HelpNeedId",
                        column: x => x.HelpNeedId,
                        principalTable: "OrganizationHelpNeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationHelpOffers_Organizations_OfferingOrganizationId",
                        column: x => x.OfferingOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationHelpOffers_HelpNeedId",
                table: "OrganizationHelpOffers",
                column: "HelpNeedId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationHelpOffers_OfferingOrganizationId",
                table: "OrganizationHelpOffers",
                column: "OfferingOrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationHelpOffers");
        }
    }
}
