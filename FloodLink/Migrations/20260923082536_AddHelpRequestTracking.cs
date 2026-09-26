using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FloodLink.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpRequestTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HelpRequestStatusLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HelpRequestId = table.Column<int>(type: "integer", nullable: false),
                    OldValue = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NewValue = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChangeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Status"),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpRequestStatusLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpRequestStatusLogs_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HelpRequestStatusLogs_HelpRequests_HelpRequestId",
                        column: x => x.HelpRequestId,
                        principalTable: "HelpRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequestStatusLogs_ChangedByUserId",
                table: "HelpRequestStatusLogs",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequestStatusLogs_HelpRequestId",
                table: "HelpRequestStatusLogs",
                column: "HelpRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HelpRequestStatusLogs");
        }
    }
}
