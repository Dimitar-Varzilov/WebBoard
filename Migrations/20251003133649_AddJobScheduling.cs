using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBoard.Migrations
{
	/// <inheritdoc />
	public partial class AddJobScheduling : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<DateTime>(
				name: "ScheduledAt",
				table: "Jobs",
				type: "timestamp with time zone",
				nullable: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "ScheduledAt",
				table: "Jobs");
		}
	}
}
