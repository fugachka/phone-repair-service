using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace PhoneRepairService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Faults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faults", x => x.Id);
                    table.CheckConstraint("CK_Faults_BasePrice", "BasePrice >= 0");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PhoneModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Brand = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ModelName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneModels", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Login = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: true),
                    Specialization = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Imei = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusChangedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Diagnosis = table.Column<string>(type: "longtext", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    PhoneModelId = table.Column<int>(type: "int", nullable: false),
                    MasterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.CheckConstraint("CK_Orders_Status", "Status BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Orders_TotalPrice", "TotalPrice >= 0");
                    table.ForeignKey(
                        name: "FK_Orders_PhoneModels_PhoneModelId",
                        column: x => x.PhoneModelId,
                        principalTable: "PhoneModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_MasterId",
                        column: x => x.MasterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrderFaults",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    FaultId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderFaults", x => new { x.OrderId, x.FaultId });
                    table.ForeignKey(
                        name: "FK_OrderFaults_Faults_FaultId",
                        column: x => x.FaultId,
                        principalTable: "Faults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderFaults_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Faults_Title",
                table: "Faults",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderFaults_FaultId",
                table: "OrderFaults",
                column: "FaultId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ClientId",
                table: "Orders",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_MasterId",
                table: "Orders",
                column: "MasterId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PhoneModelId",
                table: "Orders",
                column: "PhoneModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                table: "Users",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderFaults");

            migrationBuilder.DropTable(
                name: "Faults");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "PhoneModels");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
