using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database._RuCM.Migrations.Postgres
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
                    speaker = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_voice_claim", x => x.speaker);
                });
            // CMU14: keep any existing personal names private during a staged upgrade.
            migrationBuilder.Sql("INSERT INTO rucm_kaban_voice_claim(speaker,user_id) SELECT speaker,user_id FROM rucm_kaban_voice;");
            migrationBuilder.Sql("CREATE TRIGGER rucm_kaban_claim_immutable BEFORE UPDATE OR DELETE ON rucm_kaban_voice_claim FOR EACH ROW EXECUTE FUNCTION rucm_kaban_append_only();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rucm_kaban_voice_claim");
        }
    }
}
