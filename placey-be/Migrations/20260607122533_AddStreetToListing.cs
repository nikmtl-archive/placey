using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace placey_be.Migrations
{
    /// <inheritdoc />
    public partial class AddStreetToListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Listings",
                type: "text",
                nullable: false,
                defaultValue: ""
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Street", table: "Listings");
        }
    }
}
