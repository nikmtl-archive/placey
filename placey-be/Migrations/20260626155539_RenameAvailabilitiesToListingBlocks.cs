using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace placey_be.Migrations
{
    /// <inheritdoc />
    public partial class RenameAvailabilitiesToListingBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Availabilities_Listings_ListingId",
                table: "Availabilities"
            );

            migrationBuilder.DropPrimaryKey(name: "PK_Availabilities", table: "Availabilities");

            migrationBuilder.DropIndex(
                name: "IX_Availabilities_ListingId",
                table: "Availabilities"
            );

            migrationBuilder.DropColumn(name: "IsBlocked", table: "Availabilities");

            migrationBuilder.RenameTable(name: "Availabilities", newName: "ListingBlocks");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ListingBlocks",
                table: "ListingBlocks",
                column: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_ListingBlocks_Listings_ListingId",
                table: "ListingBlocks",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.CreateIndex(
                name: "IX_ListingBlocks_ListingId",
                table: "ListingBlocks",
                column: "ListingId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListingBlocks_Listings_ListingId",
                table: "ListingBlocks"
            );

            migrationBuilder.DropPrimaryKey(name: "PK_ListingBlocks", table: "ListingBlocks");

            migrationBuilder.DropIndex(name: "IX_ListingBlocks_ListingId", table: "ListingBlocks");

            migrationBuilder.RenameTable(name: "ListingBlocks", newName: "Availabilities");

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "Availabilities",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_Availabilities",
                table: "Availabilities",
                column: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Availabilities_Listings_ListingId",
                table: "Availabilities",
                column: "ListingId",
                principalTable: "Listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_ListingId",
                table: "Availabilities",
                column: "ListingId"
            );
        }
    }
}
