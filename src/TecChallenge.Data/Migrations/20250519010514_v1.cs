using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TecChallenge.Data.Migrations
{
    /// <inheritdoc />
    public partial class v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Game",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "varchar(100)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Log",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ApplicationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(128)", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserLibrary",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLibrary", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserWallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWallet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionGame",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionGame", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionGame_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionGame_Promotion_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LibraryItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserLibraryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibraryItem_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LibraryItem_UserLibrary_UserLibraryId",
                        column: x => x.UserLibraryId,
                        principalTable: "UserLibrary",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PromotionGameId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "VARCHAR(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletTransaction_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletTransaction_PromotionGame_PromotionGameId",
                        column: x => x.PromotionGameId,
                        principalTable: "PromotionGame",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WalletTransaction_UserWallet_WalletId",
                        column: x => x.WalletId,
                        principalTable: "UserWallet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Game_Name",
                table: "Game",
                column: "Name")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_GameId",
                table: "LibraryItem",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryItem_UserLibraryId",
                table: "LibraryItem",
                column: "UserLibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionGame_GameId",
                table: "PromotionGame",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionGame_PromotionId",
                table: "PromotionGame",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWallet_UserId",
                table: "UserWallet",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_GameId",
                table: "WalletTransaction",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_PromotionGameId",
                table: "WalletTransaction",
                column: "PromotionGameId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_WalletId_CreatedAt",
                table: "WalletTransaction",
                columns: new[] { "WalletId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibraryItem");

            migrationBuilder.DropTable(
                name: "Log");

            migrationBuilder.DropTable(
                name: "WalletTransaction");

            migrationBuilder.DropTable(
                name: "UserLibrary");

            migrationBuilder.DropTable(
                name: "PromotionGame");

            migrationBuilder.DropTable(
                name: "UserWallet");

            migrationBuilder.DropTable(
                name: "Game");

            migrationBuilder.DropTable(
                name: "Promotion");
        }
    }
}
