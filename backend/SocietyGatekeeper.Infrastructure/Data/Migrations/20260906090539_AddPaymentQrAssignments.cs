using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocietyGatekeeper.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentQrAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentQrAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QrImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayeeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentQrAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentQrAssignments_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentQrAssignments_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentQrAssignments_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentQrAssignments_AssignedToUserId",
                table: "PaymentQrAssignments",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentQrAssignments_BlockId",
                table: "PaymentQrAssignments",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentQrAssignments_SocietyId_BlockId",
                table: "PaymentQrAssignments",
                columns: new[] { "SocietyId", "BlockId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentQrAssignments");
        }
    }
}
