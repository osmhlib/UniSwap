using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniSwap.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Listings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Listings");
        }
    }
}
