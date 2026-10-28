using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.SQL.Migrations
{
    /// <inheritdoc />
    public partial class InitialQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "quote_reference_seq");

            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identifier = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'QT-' || lpad(nextval('quote_reference_seq')::text, 6, '0')"),
                    adviser_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    client_full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client_date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    client_is_smoker = table.Column<bool>(type: "boolean", nullable: false),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cover_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    term_years = table.Column<int>(type: "integer", nullable: false),
                    monthly_premium = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    pricing_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    rating_factors = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    request_fingerprint = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quote_status_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    quote_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    to_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    changed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quote_status_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_quote_status_changes_quotes_quote_id",
                        column: x => x.quote_id,
                        principalTable: "quotes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_quote_status_changes_quote_id_changed_at",
                table: "quote_status_changes",
                columns: new[] { "quote_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_quotes_adviser_id_idempotency_key",
                table: "quotes",
                columns: new[] { "adviser_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_quotes_adviser_id_request_fingerprint_created_at",
                table: "quotes",
                columns: new[] { "adviser_id", "request_fingerprint", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_quotes_adviser_id_status_created_at",
                table: "quotes",
                columns: new[] { "adviser_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_quotes_identifier",
                table: "quotes",
                column: "identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotes_reference",
                table: "quotes",
                column: "reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quote_status_changes");

            migrationBuilder.DropTable(
                name: "quotes");

            migrationBuilder.DropSequence(
                name: "quote_reference_seq");
        }
    }
}
