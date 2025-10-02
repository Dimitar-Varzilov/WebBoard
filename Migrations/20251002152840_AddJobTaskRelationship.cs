using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTaskRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "Tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_JobId",
                table: "Tasks",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Jobs_JobId",
                table: "Tasks",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Jobs_JobId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_JobId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Tasks");
        }
    }
}
