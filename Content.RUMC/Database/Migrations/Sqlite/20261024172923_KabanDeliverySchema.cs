using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database._RuCM.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class KabanDeliverySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "lease",
                table: "rucm_kaban_discord_role",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "lease_until",
                table: "rucm_kaban_discord_role",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "previous_color",
                table: "rucm_kaban_discord_role",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "previous_name",
                table: "rucm_kaban_discord_role",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_voice_speaker",
                table: "rucm_kaban_voice",
                column: "speaker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_discord_role_status_lease_until",
                table: "rucm_kaban_discord_role",
                columns: new[] { "status", "lease_until" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rucm_kaban_voice_speaker",
                table: "rucm_kaban_voice");

            migrationBuilder.DropIndex(
                name: "IX_rucm_kaban_discord_role_status_lease_until",
                table: "rucm_kaban_discord_role");

            migrationBuilder.DropColumn(
                name: "lease",
                table: "rucm_kaban_discord_role");

            migrationBuilder.DropColumn(
                name: "lease_until",
                table: "rucm_kaban_discord_role");

            migrationBuilder.DropColumn(
                name: "previous_color",
                table: "rucm_kaban_discord_role");

            migrationBuilder.DropColumn(
                name: "previous_name",
                table: "rucm_kaban_discord_role");
        }
    }
}
