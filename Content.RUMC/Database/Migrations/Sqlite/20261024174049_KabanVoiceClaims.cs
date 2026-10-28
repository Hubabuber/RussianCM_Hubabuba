using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database._RuCM.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class KabanVoiceClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rucm_kaban_voice_claim",
                columns: table => new
                {
                    speaker = table.Column<string>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_voice_claim", x => x.speaker);
                });
            // CMU14: keep any existing personal names private during a staged upgrade.
            migrationBuilder.Sql("INSERT INTO rucm_kaban_voice_claim(speaker,user_id) SELECT speaker,user_id FROM rucm_kaban_voice;");
            migrationBuilder.Sql("""
                CREATE TRIGGER rucm_kaban_claim_no_update BEFORE UPDATE ON rucm_kaban_voice_claim BEGIN SELECT RAISE(ABORT,'Voice claims are immutable'); END;
                CREATE TRIGGER rucm_kaban_claim_no_delete BEFORE DELETE ON rucm_kaban_voice_claim BEGIN SELECT RAISE(ABORT,'Voice claims are immutable'); END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rucm_kaban_voice_claim");
        }
    }
}
