using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBoard.Migrations
{
	/// <inheritdoc />
	public partial class AddJobTaskRelationshipAndIndexes : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(
				name: "IX_Tasks_JobId",
				table: "Tasks");

			migrationBuilder.CreateIndex(
				name: "IX_Tasks_CreatedAt",
				table: "Tasks",
				column: "CreatedAt");

			migrationBuilder.CreateIndex(
				name: "IX_Tasks_JobId",
				table: "Tasks",
				column: "JobId",
				filter: "\"JobId\" IS NOT NULL");

			migrationBuilder.CreateIndex(
				name: "IX_Tasks_Status",
				table: "Tasks",
				column: "Status");

			migrationBuilder.CreateIndex(
				name: "IX_Tasks_Status_JobId",
				table: "Tasks",
				columns: new[] { "Status", "JobId" });

			migrationBuilder.CreateIndex(
				name: "IX_Jobs_CreatedAt",
				table: "Jobs",
				column: "CreatedAt");

			migrationBuilder.CreateIndex(
				name: "IX_Jobs_JobType",
				table: "Jobs",
				column: "JobType");

			migrationBuilder.CreateIndex(
				name: "IX_Jobs_ScheduledAt",
				table: "Jobs",
				column: "ScheduledAt",
				filter: "\"ScheduledAt\" IS NOT NULL");

			migrationBuilder.CreateIndex(
				name: "IX_Jobs_Status",
				table: "Jobs",
				column: "Status");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropIndex(
				name: "IX_Tasks_CreatedAt",
				table: "Tasks");

			migrationBuilder.DropIndex(
				name: "IX_Tasks_JobId",
				table: "Tasks");

			migrationBuilder.DropIndex(
				name: "IX_Tasks_Status",
				table: "Tasks");

			migrationBuilder.DropIndex(
				name: "IX_Tasks_Status_JobId",
				table: "Tasks");

			migrationBuilder.DropIndex(
				name: "IX_Jobs_CreatedAt",
				table: "Jobs");

			migrationBuilder.DropIndex(
				name: "IX_Jobs_JobType",
				table: "Jobs");

			migrationBuilder.DropIndex(
				name: "IX_Jobs_ScheduledAt",
				table: "Jobs");

			migrationBuilder.DropIndex(
				name: "IX_Jobs_Status",
				table: "Jobs");

			migrationBuilder.CreateIndex(
				name: "IX_Tasks_JobId",
				table: "Tasks",
				column: "JobId");
		}
	}
}
