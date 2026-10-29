using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurveyApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BestScore",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BestScoreAchievedAt",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BestWave",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlayCount",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalEnemiesDefeated",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "TotalScore",
                table: "Players",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // xmin は PostgreSQL がすべての行に持つシステム列なので、列の追加は不要（楽観的同時実行制御に使う）

            migrationBuilder.CreateTable(
                name: "Runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Wave = table.Column<int>(type: "integer", nullable: false),
                    EnemiesDefeated = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    IsNewBestScore = table.Column<bool>(type: "boolean", nullable: false),
                    IsNewBestWave = table.Column<bool>(type: "boolean", nullable: false),
                    PlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Runs_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_BestScore_BestScoreAchievedAt",
                table: "Players",
                columns: new[] { "BestScore", "BestScoreAchievedAt" },
                descending: new[] { true, false });

            migrationBuilder.CreateIndex(
                name: "IX_Runs_PlayerId_PlayedAt",
                table: "Runs",
                columns: new[] { "PlayerId", "PlayedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Runs");

            migrationBuilder.DropIndex(
                name: "IX_Players_BestScore_BestScoreAchievedAt",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "BestScore",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "BestScoreAchievedAt",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "BestWave",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PlayCount",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TotalEnemiesDefeated",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TotalScore",
                table: "Players");

        }
    }
}
