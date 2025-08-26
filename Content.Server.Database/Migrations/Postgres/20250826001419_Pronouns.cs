using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Pronouns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "db_pronoun",
                columns: table => new
                {
                    db_pronoun_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: true),
                    @object = table.Column<string>(name: "object", type: "text", nullable: true),
                    dat_obj = table.Column<string>(type: "text", nullable: true),
                    genitive = table.Column<string>(type: "text", nullable: true),
                    poss_adj = table.Column<string>(type: "text", nullable: true),
                    poss_pronoun = table.Column<string>(type: "text", nullable: true),
                    reflexive = table.Column<string>(type: "text", nullable: true),
                    counter = table.Column<string>(type: "text", nullable: true),
                    plural = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_db_pronoun", x => x.db_pronoun_id);
                    table.ForeignKey(
                        name: "FK_db_pronoun_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "pronoun",
                table: "db_pronoun",
                column: "profile_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "db_pronoun");
        }
    }
}
