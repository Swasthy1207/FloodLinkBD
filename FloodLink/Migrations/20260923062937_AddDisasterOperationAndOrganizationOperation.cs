using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FloodLink.Migrations
{
    /// <inheritdoc />
    public partial class AddDisasterOperationAndOrganizationOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisasterOperations",
                columns: table => new
                {
                    DisasterOperationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperationName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DisasterType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedByOrganizationId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisasterOperations", x => x.DisasterOperationId);
                    table.ForeignKey(
                        name: "FK_DisasterOperations_Organizations_CreatedByOrganizationId",
                        column: x => x.CreatedByOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationOperations",
                columns: table => new
                {
                    OrganizationOperationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisasterOperationId = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    ParticipationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    AssignedArea = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationOperations", x => x.OrganizationOperationId);
                    table.ForeignKey(
                        name: "FK_OrganizationOperations_DisasterOperations_DisasterOperation~",
                        column: x => x.DisasterOperationId,
                        principalTable: "DisasterOperations",
                        principalColumn: "DisasterOperationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationOperations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisasterOperations_CreatedByOrganizationId",
                table: "DisasterOperations",
                column: "CreatedByOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationOperations_DisasterOperationId",
                table: "OrganizationOperations",
                column: "DisasterOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationOperations_OrganizationId",
                table: "OrganizationOperations",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationOperations");

            migrationBuilder.DropTable(
                name: "DisasterOperations");
        }
    }
}
