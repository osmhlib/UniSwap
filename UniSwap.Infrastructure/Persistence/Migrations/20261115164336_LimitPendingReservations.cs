using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniSwap.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LimitPendingReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_ListingId",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "UX_Reservations_PendingRequester",
                table: "Reservations",
                columns: new[] { "ListingId", "RequesterId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Reservations_PendingRequester",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ListingId",
                table: "Reservations",
                column: "ListingId");
        }
    }
}
