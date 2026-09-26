using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FloodLink.Migrations
{
    /// <inheritdoc />
    public partial class AddFeaturesAttachmentsReactionsRatingsCommentsMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Shelters",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Shelters",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "HighRiskAreas",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "HighRiskAreas",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "HelpRequests",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "HelpRequests",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOtpSentAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OtpAttempts",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "HelpRequestAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HelpRequestId = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpRequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpRequestAttachments_HelpRequests_HelpRequestId",
                        column: x => x.HelpRequestId,
                        principalTable: "HelpRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HelpRequestComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HelpRequestId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpRequestComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpRequestComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HelpRequestComments_HelpRequests_HelpRequestId",
                        column: x => x.HelpRequestId,
                        principalTable: "HelpRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HelpRequestRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HelpRequestId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RatingValue = table.Column<int>(type: "integer", nullable: false),
                    Review = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpRequestRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpRequestRatings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HelpRequestRatings_HelpRequests_HelpRequestId",
                        column: x => x.HelpRequestId,
                        principalTable: "HelpRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HelpRequestReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HelpRequestId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    IsLike = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpRequestReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpRequestReactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HelpRequestReactions_HelpRequests_HelpRequestId",
                        column: x => x.HelpRequestId,
                        principalTable: "HelpRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequestAttachments_HelpRequestId",
                table: "HelpRequestAttachments",
                column: "HelpRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequestComments_HelpRequestId",
                table: "HelpRequestComments",
                column: "HelpRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequestComments_UserId",
                table: "HelpRequestComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequestRatings_HelpRequestId_UserId",
                table: "HelpRequestRatings",
                columns: new[] { "HelpRequestId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequestRatings_UserId",
                table: "HelpRequestRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequestReactions_HelpRequestId_UserId",
                table: "HelpRequestReactions",
                columns: new[] { "HelpRequestId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequestReactions_UserId",
                table: "HelpRequestReactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HelpRequestAttachments");

            migrationBuilder.DropTable(
                name: "HelpRequestComments");

            migrationBuilder.DropTable(
                name: "HelpRequestRatings");

            migrationBuilder.DropTable(
                name: "HelpRequestReactions");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Shelters");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Shelters");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "HighRiskAreas");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "HighRiskAreas");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "LastOtpSentAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OtpAttempts",
                table: "AspNetUsers");
        }
    }
}
