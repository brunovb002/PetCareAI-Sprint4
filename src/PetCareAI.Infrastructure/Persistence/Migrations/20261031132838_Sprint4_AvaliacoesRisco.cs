using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCareAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4_AvaliacoesRisco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvaliacoesRisco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PetId = table.Column<int>(type: "int", nullable: false),
                    Prioridade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Confianca = table.Column<double>(type: "float", nullable: false),
                    HistoricoMedico = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Sintomas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Fatores = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Recomendacao = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    AvaliadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvaliacoesRisco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvaliacoesRisco_Pets_PetId",
                        column: x => x.PetId,
                        principalTable: "Pets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvaliacoesRisco_PetId_AvaliadoEm",
                table: "AvaliacoesRisco",
                columns: new[] { "PetId", "AvaliadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvaliacoesRisco");
        }
    }
}
