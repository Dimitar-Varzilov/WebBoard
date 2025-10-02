using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBoard.Migrations
{
	/// <inheritdoc />
	public partial class InitialCreate : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "Jobs",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					JobType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
					Status = table.Column<int>(type: "integer", nullable: false),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Jobs", x => x.Id);
				});

			migrationBuilder.CreateTable(
				name: "Tasks",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "uuid", nullable: false),
					CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
					Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
					Status = table.Column<int>(type: "integer", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_Tasks", x => x.Id);
				});
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "Jobs");

			migrationBuilder.DropTable(
				name: "Tasks");
		}
	}
}
