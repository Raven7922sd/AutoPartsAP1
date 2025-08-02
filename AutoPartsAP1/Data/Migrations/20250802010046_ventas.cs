using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartsAP1.Migrations
{
    /// <inheritdoc />
    public partial class ventas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropColumn(
                name: "Monto",
                table: "VentasDetalle");

            migrationBuilder.RenameColumn(
                name: "ValorCobrado",
                table: "VentasDetalle",
                newName: "PrecioUnitario");

            migrationBuilder.RenameColumn(
                name: "DetalleId",
                table: "VentasDetalle",
                newName: "Id");

            migrationBuilder.AlterColumn<double>(
                name: "Cantidad",
                table: "VentasDetalle",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "VentasDetalle",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Ventas",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_VentasDetalle_ProductoId",
                table: "VentasDetalle",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ApplicationUserId",
                table: "Ventas",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_AspNetUsers_ApplicationUserId",
                table: "Ventas",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VentasDetalle_Producto_ProductoId",
                table: "VentasDetalle",
                column: "ProductoId",
                principalTable: "Producto",
                principalColumn: "ProductoId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_AspNetUsers_ApplicationUserId",
                table: "Ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_VentasDetalle_Producto_ProductoId",
                table: "VentasDetalle");

            migrationBuilder.DropIndex(
                name: "IX_VentasDetalle_ProductoId",
                table: "VentasDetalle");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_ApplicationUserId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "VentasDetalle");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Ventas");

            migrationBuilder.RenameColumn(
                name: "PrecioUnitario",
                table: "VentasDetalle",
                newName: "ValorCobrado");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "VentasDetalle",
                newName: "DetalleId");

            migrationBuilder.AlterColumn<int>(
                name: "Cantidad",
                table: "VentasDetalle",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<double>(
                name: "Monto",
                table: "VentasDetalle",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DireccionUsuario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioNombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_Usuario_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_ApplicationUserId",
                table: "Usuario",
                column: "ApplicationUserId");
        }
    }
}
