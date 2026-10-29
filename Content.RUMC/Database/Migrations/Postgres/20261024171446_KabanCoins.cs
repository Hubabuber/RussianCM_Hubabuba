using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Content.Server.Database._RuCM.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class KabanCoins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rucm_kaban_audit",
                columns: table => new
                {
                    kaban_audit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation = table.Column<Guid>(type: "uuid", nullable: false),
                    actor = table.Column<Guid>(type: "uuid", nullable: false),
                    target = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    detail = table.Column<string>(type: "text", nullable: false),
                    at = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_audit", x => x.kaban_audit_id);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_award",
                columns: table => new
                {
                    kaban_award_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient = table.Column<Guid>(type: "uuid", nullable: false),
                    initiator = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    exceptional = table.Column<bool>(type: "boolean", nullable: false),
                    at = table.Column<long>(type: "bigint", nullable: false),
                    paid_at = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_award", x => x.kaban_award_id);
                    table.CheckConstraint("ck_kaban_award", "amount > 0");
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_lock",
                columns: table => new
                {
                    kaban_lock_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_lock", x => x.kaban_lock_id);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_operation",
                columns: table => new
                {
                    kaban_operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint = table.Column<string>(type: "text", nullable: false),
                    result = table.Column<string>(type: "text", nullable: false),
                    at = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_operation", x => x.kaban_operation_id);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_product",
                columns: table => new
                {
                    kaban_product_id = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_product", x => x.kaban_product_id);
                    table.CheckConstraint("ck_kaban_price", "price > 0");
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_round_request",
                columns: table => new
                {
                    kaban_round_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<string>(type: "text", nullable: false),
                    parameters = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    at = table.Column<long>(type: "bigint", nullable: false),
                    completed_at = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_round_request", x => x.kaban_round_request_id);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_wallet",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    balance = table.Column<long>(type: "bigint", nullable: false),
                    earned = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_wallet", x => x.user_id);
                    table.CheckConstraint("ck_kaban_balance", "balance >= 0");
                    table.CheckConstraint("ck_kaban_earned", "earned >= 0");
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_approval",
                columns: table => new
                {
                    award_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver = table.Column<Guid>(type: "uuid", nullable: false),
                    senior = table.Column<bool>(type: "boolean", nullable: false),
                    at = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_approval", x => new { x.award_id, x.approver });
                    table.ForeignKey(
                        name: "FK_rucm_kaban_approval_rucm_kaban_award_kaban_award_id",
                        column: x => x.award_id,
                        principalTable: "rucm_kaban_award",
                        principalColumn: "kaban_award_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_ledger",
                columns: table => new
                {
                    kaban_ledger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor = table.Column<Guid>(type: "uuid", nullable: false),
                    delta = table.Column<long>(type: "bigint", nullable: false),
                    balance = table.Column<long>(type: "bigint", nullable: false),
                    at = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_ledger", x => x.kaban_ledger_id);
                    table.ForeignKey(
                        name: "FK_rucm_kaban_ledger_rucm_kaban_wallet_kaban_wallet_user_id",
                        column: x => x.user_id,
                        principalTable: "rucm_kaban_wallet",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_purchase",
                columns: table => new
                {
                    kaban_purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    at = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_purchase", x => x.kaban_purchase_id);
                    table.ForeignKey(
                        name: "FK_rucm_kaban_purchase_rucm_kaban_product_kaban_product_id",
                        column: x => x.product_id,
                        principalTable: "rucm_kaban_product",
                        principalColumn: "kaban_product_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rucm_kaban_purchase_rucm_kaban_wallet_kaban_wallet_user_id",
                        column: x => x.user_id,
                        principalTable: "rucm_kaban_wallet",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_discord_role",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false),
                    discord_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    delivery = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_discord_role", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_rucm_kaban_discord_role_rucm_kaban_purchase_kaban_purchase_~",
                        column: x => x.purchase_id,
                        principalTable: "rucm_kaban_purchase",
                        principalColumn: "kaban_purchase_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_token",
                columns: table => new
                {
                    kaban_token_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    multiplier = table.Column<int>(type: "integer", nullable: false),
                    flexible = table.Column<bool>(type: "boolean", nullable: false),
                    expires = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    jobs = table.Column<string>(type: "text", nullable: false),
                    server = table.Column<string>(type: "text", nullable: false),
                    reservation = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_token", x => x.kaban_token_id);
                    table.ForeignKey(
                        name: "FK_rucm_kaban_token_rucm_kaban_purchase_kaban_purchase_id",
                        column: x => x.purchase_id,
                        principalTable: "rucm_kaban_purchase",
                        principalColumn: "kaban_purchase_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_voice",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    speaker = table.Column<string>(type: "text", nullable: false),
                    consent = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_voice", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_rucm_kaban_voice_rucm_kaban_purchase_kaban_purchase_id",
                        column: x => x.purchase_id,
                        principalTable: "rucm_kaban_purchase",
                        principalColumn: "kaban_purchase_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_token_use",
                columns: table => new
                {
                    kaban_token_use_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job = table.Column<string>(type: "text", nullable: false),
                    command = table.Column<bool>(type: "boolean", nullable: false),
                    multiplier = table.Column<int>(type: "integer", nullable: false),
                    at = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    server = table.Column<string>(type: "text", nullable: false),
                    round = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_token_use", x => x.kaban_token_use_id);
                    table.ForeignKey(
                        name: "FK_rucm_kaban_token_use_rucm_kaban_token_kaban_token_id",
                        column: x => x.token_id,
                        principalTable: "rucm_kaban_token",
                        principalColumn: "kaban_token_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "rucm_kaban_lock",
                columns: new[] { "kaban_lock_id", "revision" },
                values: new object[] { 1, 0L });

            migrationBuilder.InsertData(
                table: "rucm_kaban_product",
                columns: new[] { "kaban_product_id", "enabled", "price" },
                values: new object[,]
                {
                    { "discord", true, 300 },
                    { "flex3", true, 100 },
                    { "flex5", true, 190 },
                    { "full", true, 800 },
                    { "govfor", true, 200 },
                    { "map", true, 250 },
                    { "mode", true, 350 },
                    { "opfor", true, 200 },
                    { "pack3", true, 165 },
                    { "pack5", true, 420 },
                    { "priority3", true, 60 },
                    { "priority5", true, 150 },
                    { "rename", true, 50 },
                    { "voice", true, 600 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_audit_target_at",
                table: "rucm_kaban_audit",
                columns: new[] { "target", "at" });

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_award_recipient_paid_at",
                table: "rucm_kaban_award",
                columns: new[] { "recipient", "paid_at" });

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_discord_role_purchase_id",
                table: "rucm_kaban_discord_role",
                column: "purchase_id");

            // CMU14: database-enforced append-only journal, including SQLite REPLACE attempts.
            migrationBuilder.Sql("""
                CREATE FUNCTION rucm_kaban_append_only() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Kaban journal is append-only'; END; $$;
                CREATE TRIGGER rucm_kaban_ledger_immutable BEFORE UPDATE OR DELETE ON rucm_kaban_ledger FOR EACH ROW EXECUTE FUNCTION rucm_kaban_append_only();
                CREATE TRIGGER rucm_kaban_operation_immutable BEFORE UPDATE OR DELETE ON rucm_kaban_operation FOR EACH ROW EXECUTE FUNCTION rucm_kaban_append_only();
                CREATE TRIGGER rucm_kaban_audit_immutable BEFORE UPDATE OR DELETE ON rucm_kaban_audit FOR EACH ROW EXECUTE FUNCTION rucm_kaban_append_only();
                CREATE TRIGGER rucm_kaban_purchase_immutable BEFORE UPDATE OR DELETE ON rucm_kaban_purchase FOR EACH ROW EXECUTE FUNCTION rucm_kaban_append_only();
                """);

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_ledger_user_id_at",
                table: "rucm_kaban_ledger",
                columns: new[] { "user_id", "at" });

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_purchase_product_id",
                table: "rucm_kaban_purchase",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_purchase_user_id_at",
                table: "rucm_kaban_purchase",
                columns: new[] { "user_id", "at" });

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_round_request_status_at",
                table: "rucm_kaban_round_request",
                columns: new[] { "status", "at" });

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_token_purchase_id",
                table: "rucm_kaban_token",
                column: "purchase_id");

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_token_user_id_status",
                table: "rucm_kaban_token",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_token_use_token_id",
                table: "rucm_kaban_token_use",
                column: "token_id");

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_token_use_user_id_at",
                table: "rucm_kaban_token_use",
                columns: new[] { "user_id", "at" });

            migrationBuilder.CreateIndex(
                name: "IX_rucm_kaban_voice_purchase_id",
                table: "rucm_kaban_voice",
                column: "purchase_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION rucm_kaban_append_only() CASCADE;"); // CMU14
            migrationBuilder.DropTable(
                name: "rucm_kaban_approval");

            migrationBuilder.DropTable(
                name: "rucm_kaban_audit");

            migrationBuilder.DropTable(
                name: "rucm_kaban_discord_role");

            migrationBuilder.DropTable(
                name: "rucm_kaban_ledger");

            migrationBuilder.DropTable(
                name: "rucm_kaban_lock");

            migrationBuilder.DropTable(
                name: "rucm_kaban_operation");

            migrationBuilder.DropTable(
                name: "rucm_kaban_round_request");

            migrationBuilder.DropTable(
                name: "rucm_kaban_token_use");

            migrationBuilder.DropTable(
                name: "rucm_kaban_voice");

            migrationBuilder.DropTable(
                name: "rucm_kaban_award");

            migrationBuilder.DropTable(
                name: "rucm_kaban_token");

            migrationBuilder.DropTable(
                name: "rucm_kaban_purchase");

            migrationBuilder.DropTable(
                name: "rucm_kaban_product");

            migrationBuilder.DropTable(
                name: "rucm_kaban_wallet");
        }
    }
}
