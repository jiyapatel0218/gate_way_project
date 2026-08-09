using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocietyGatekeeper.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeMasterDataGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComplaintCategories_Societies_SocietyId",
                table: "ComplaintCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceTypes_Societies_SocietyId",
                table: "MaintenanceTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitorTypes_Societies_SocietyId",
                table: "VisitorTypes");

            migrationBuilder.DropIndex(
                name: "IX_VisitorTypes_SocietyId",
                table: "VisitorTypes");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceTypes_SocietyId",
                table: "MaintenanceTypes");

            migrationBuilder.DropIndex(
                name: "IX_ComplaintCategories_SocietyId",
                table: "ComplaintCategories");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "VisitorTypes");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "MaintenanceTypes");

            migrationBuilder.DropColumn(
                name: "SocietyId",
                table: "ComplaintCategories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SocietyId",
                table: "VisitorTypes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SocietyId",
                table: "MaintenanceTypes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SocietyId",
                table: "ComplaintCategories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_VisitorTypes_SocietyId",
                table: "VisitorTypes",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTypes_SocietyId",
                table: "MaintenanceTypes",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplaintCategories_SocietyId",
                table: "ComplaintCategories",
                column: "SocietyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComplaintCategories_Societies_SocietyId",
                table: "ComplaintCategories",
                column: "SocietyId",
                principalTable: "Societies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceTypes_Societies_SocietyId",
                table: "MaintenanceTypes",
                column: "SocietyId",
                principalTable: "Societies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VisitorTypes_Societies_SocietyId",
                table: "VisitorTypes",
                column: "SocietyId",
                principalTable: "Societies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
