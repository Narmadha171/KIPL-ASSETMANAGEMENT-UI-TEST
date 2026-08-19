using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIPL.AssetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    StoredPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CapturedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CapturedBy = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    AssetId = table.Column<int>(type: "int", nullable: true),
                    AssetRequestId = table.Column<int>(type: "int", nullable: true),
                    AssetReturnId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetPhotos_AssetRequests_AssetRequestId",
                        column: x => x.AssetRequestId,
                        principalTable: "AssetRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssetPhotos_AssetReturns_AssetReturnId",
                        column: x => x.AssetReturnId,
                        principalTable: "AssetReturns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssetPhotos_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPhotos_AssetId",
                table: "AssetPhotos",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPhotos_AssetRequestId",
                table: "AssetPhotos",
                column: "AssetRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPhotos_AssetReturnId",
                table: "AssetPhotos",
                column: "AssetReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetPhotos_Kind",
                table: "AssetPhotos",
                column: "Kind");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetPhotos");
        }
    }
}
