using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Content.Server.Database._RuCM.Migrations.Sqlite
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
                    kaban_audit_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    operation = table.Column<Guid>(type: "TEXT", nullable: false),
                    actor = table.Column<Guid>(type: "TEXT", nullable: false),
                    target = table.Column<Guid>(type: "TEXT", nullable: false),
                    action = table.Column<string>(type: "TEXT", nullable: false),
                    detail = table.Column<string>(type: "TEXT", nullable: false),
                    at = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_audit", x => x.kaban_audit_id);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_award",
                columns: table => new
                {
                    kaban_award_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    recipient = table.Column<Guid>(type: "TEXT", nullable: false),
                    initiator = table.Column<Guid>(type: "TEXT", nullable: false),
                    amount = table.Column<int>(type: "INTEGER", nullable: false),
                    category = table.Column<string>(type: "TEXT", nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    exceptional = table.Column<bool>(type: "INTEGER", nullable: false),
                    at = table.Column<long>(type: "INTEGER", nullable: false),
                    paid_at = table.Column<long>(type: "INTEGER", nullable: false)
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
                    kaban_lock_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    revision = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_lock", x => x.kaban_lock_id);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_operation",
                columns: table => new
                {
                    kaban_operation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    actor = table.Column<Guid>(type: "TEXT", nullable: false),
                    fingerprint = table.Column<string>(type: "TEXT", nullable: false),
                    result = table.Column<string>(type: "TEXT", nullable: false),
                    at = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_operation", x => x.kaban_operation_id);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_product",
                columns: table => new
                {
                    kaban_product_id = table.Column<string>(type: "TEXT", nullable: false),
                    price = table.Column<int>(type: "INTEGER", nullable: false),
                    enabled = table.Column<bool>(type: "INTEGER", nullable: false)
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
                    kaban_round_request_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    product_id = table.Column<string>(type: "TEXT", nullable: false),
                    parameters = table.Column<string>(type: "TEXT", nullable: false),
                    price = table.Column<int>(type: "INTEGER", nullable: false),
                    at = table.Column<long>(type: "INTEGER", nullable: false),
                    completed_at = table.Column<long>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_round_request", x => x.kaban_round_request_id);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_wallet",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    balance = table.Column<long>(type: "INTEGER", nullable: false),
                    earned = table.Column<long>(type: "INTEGER", nullable: false)
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
                    award_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    approver = table.Column<Guid>(type: "TEXT", nullable: false),
                    senior = table.Column<bool>(type: "INTEGER", nullable: false),
                    at = table.Column<long>(type: "INTEGER", nullable: false)
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
                    kaban_ledger_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    actor = table.Column<Guid>(type: "TEXT", nullable: false),
                    delta = table.Column<long>(type: "INTEGER", nullable: false),
                    balance = table.Column<long>(type: "INTEGER", nullable: false),
                    at = table.Column<long>(type: "INTEGER", nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: false)
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
                    kaban_purchase_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    product_id = table.Column<string>(type: "TEXT", nullable: false),
                    price = table.Column<int>(type: "INTEGER", nullable: false),
                    at = table.Column<long>(type: "INTEGER", nullable: false)
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
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    purchase_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    color = table.Column<string>(type: "TEXT", nullable: false),
                    discord_id = table.Column<string>(type: "TEXT", nullable: false),
                    role_id = table.Column<string>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    delivery = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rucm_kaban_discord_role", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_rucm_kaban_discord_role_rucm_kaban_purchase_kaban_purchase_id",
                        column: x => x.purchase_id,
                        principalTable: "rucm_kaban_purchase",
                        principalColumn: "kaban_purchase_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rucm_kaban_token",
                columns: table => new
                {
                    kaban_token_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    purchase_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    multiplier = table.Column<int>(type: "INTEGER", nullable: false),
                    flexible = table.Column<bool>(type: "INTEGER", nullable: false),
                    expires = table.Column<long>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    jobs = table.Column<string>(type: "TEXT", nullable: false),
                    server = table.Column<string>(type: "TEXT", nullable: false),
                    reservation = table.Column<Guid>(type: "TEXT", nullable: false)
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
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    purchase_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    speaker = table.Column<string>(type: "TEXT", nullable: false),
                    consent = table.Column<string>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false)
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
                    kaban_token_use_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    token_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    job = table.Column<string>(type: "TEXT", nullable: false),
                    command = table.Column<bool>(type: "INTEGER", nullable: false),
                    multiplier = table.Column<int>(type: "INTEGER", nullable: false),
                    at = table.Column<long>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: false),
                    server = table.Column<string>(type: "TEXT", nullable: false),
                    round = table.Column<string>(type: "TEXT", nullable: false)
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
                CREATE TRIGGER rucm_kaban_ledger_no_update BEFORE UPDATE ON rucm_kaban_ledger BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
                CREATE TRIGGER rucm_kaban_ledger_no_delete BEFORE DELETE ON rucm_kaban_ledger BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
                CREATE TRIGGER rucm_kaban_ledger_no_replace BEFORE INSERT ON rucm_kaban_ledger WHEN EXISTS(SELECT 1 FROM rucm_kaban_ledger WHERE kaban_ledger_id=NEW.kaban_ledger_id) BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
                CREATE TRIGGER rucm_kaban_operation_no_update BEFORE UPDATE ON rucm_kaban_operation BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
                CREATE TRIGGER rucm_kaban_operation_no_delete BEFORE DELETE ON rucm_kaban_operation BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
                CREATE TRIGGER rucm_kaban_operation_no_replace BEFORE INSERT ON rucm_kaban_operation WHEN EXISTS(SELECT 1 FROM rucm_kaban_operation WHERE kaban_operation_id=NEW.kaban_operation_id) BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
                CREATE TRIGGER rucm_kaban_audit_no_update BEFORE UPDATE ON rucm_kaban_audit BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
                CREATE TRIGGER rucm_kaban_audit_no_delete BEFORE DELETE ON rucm_kaban_audit BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
                CREATE TRIGGER rucm_kaban_audit_no_replace BEFORE INSERT ON rucm_kaban_audit WHEN EXISTS(SELECT 1 FROM rucm_kaban_audit WHERE kaban_audit_id=NEW.kaban_audit_id) BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
                CREATE TRIGGER rucm_kaban_purchase_no_update BEFORE UPDATE ON rucm_kaban_purchase BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
                CREATE TRIGGER rucm_kaban_purchase_no_delete BEFORE DELETE ON rucm_kaban_purchase BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
                CREATE TRIGGER rucm_kaban_purchase_no_replace BEFORE INSERT ON rucm_kaban_purchase WHEN EXISTS(SELECT 1 FROM rucm_kaban_purchase WHERE kaban_purchase_id=NEW.kaban_purchase_id) BEGIN SELECT RAISE(ABORT,'Kaban journal is append-only'); END;
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
