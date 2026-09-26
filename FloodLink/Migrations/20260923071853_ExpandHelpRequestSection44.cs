using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodLink.Migrations
{
    /// <inheritdoc />
    public partial class ExpandHelpRequestSection44 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VolunteerEmail",
                table: "HelpRequests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "HelpRequests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "HelpRequests",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "HelpRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Submitted",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "HelpRequests",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "HelpRequests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HelpRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "HelpRequests",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ContactInformation",
                table: "HelpRequests",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DisasterOperationId",
                table: "HelpRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "HelpRequests",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfPeople",
                table: "HelpRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "HelpRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Medium");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "HelpRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "HelpRequests",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequests_DisasterOperationId",
                table: "HelpRequests",
                column: "DisasterOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequests_UserId",
                table: "HelpRequests",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_HelpRequests_AspNetUsers_UserId",
                table: "HelpRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HelpRequests_DisasterOperations_DisasterOperationId",
                table: "HelpRequests",
                column: "DisasterOperationId",
                principalTable: "DisasterOperations",
                principalColumn: "DisasterOperationId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HelpRequests_AspNetUsers_UserId",
                table: "HelpRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_HelpRequests_DisasterOperations_DisasterOperationId",
                table: "HelpRequests");

            migrationBuilder.DropIndex(
                name: "IX_HelpRequests_DisasterOperationId",
                table: "HelpRequests");

            migrationBuilder.DropIndex(
                name: "IX_HelpRequests_UserId",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "ContactInformation",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "DisasterOperationId",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "NumberOfPeople",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "HelpRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "HelpRequests");

            migrationBuilder.AlterColumn<string>(
                name: "VolunteerEmail",
                table: "HelpRequests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "HelpRequests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "HelpRequests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "HelpRequests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "Submitted");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "HelpRequests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "HelpRequests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "HelpRequests",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "HelpRequests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);
        }
    }
}
