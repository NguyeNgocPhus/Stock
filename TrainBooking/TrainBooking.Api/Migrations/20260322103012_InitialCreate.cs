using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TrainBooking.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Origin = table.Column<string>(type: "text", nullable: false),
                    Destination = table.Column<string>(type: "text", nullable: false),
                    DepartureTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Seats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainId = table.Column<int>(type: "integer", nullable: false),
                    Coach = table.Column<string>(type: "text", nullable: false),
                    Row = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    IsBooked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seats_Trains_TrainId",
                        column: x => x.TrainId,
                        principalTable: "Trains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingReference = table.Column<string>(type: "text", nullable: false),
                    TrainId = table.Column<int>(type: "integer", nullable: false),
                    SeatId = table.Column<int>(type: "integer", nullable: false),
                    PassengerName = table.Column<string>(type: "text", nullable: false),
                    PassengerEmail = table.Column<string>(type: "text", nullable: false),
                    BookedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Trains_TrainId",
                        column: x => x.TrainId,
                        principalTable: "Trains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Trains",
                columns: new[] { "Id", "ArrivalTime", "DepartureTime", "Destination", "Name", "Origin" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 4, 1, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 1, 8, 0, 0, 0, DateTimeKind.Utc), "Ho Chi Minh City", "Express 101", "Hanoi" },
                    { 2, new DateTime(2026, 4, 2, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 2, 7, 0, 0, 0, DateTimeKind.Utc), "Da Nang", "Express 202", "Ho Chi Minh City" }
                });

            migrationBuilder.InsertData(
                table: "Seats",
                columns: new[] { "Id", "Coach", "IsBooked", "Number", "Row", "TrainId" },
                values: new object[,]
                {
                    { 1, "A", false, "1A", 1, 1 },
                    { 2, "A", false, "2A", 2, 1 },
                    { 3, "A", false, "3A", 3, 1 },
                    { 4, "A", false, "4A", 4, 1 },
                    { 5, "A", false, "5A", 5, 1 },
                    { 6, "A", false, "6A", 6, 1 },
                    { 7, "A", false, "7A", 7, 1 },
                    { 8, "A", false, "8A", 8, 1 },
                    { 9, "A", false, "9A", 9, 1 },
                    { 10, "A", false, "10A", 10, 1 },
                    { 11, "B", false, "1B", 1, 1 },
                    { 12, "B", false, "2B", 2, 1 },
                    { 13, "B", false, "3B", 3, 1 },
                    { 14, "B", false, "4B", 4, 1 },
                    { 15, "B", false, "5B", 5, 1 },
                    { 16, "B", false, "6B", 6, 1 },
                    { 17, "B", false, "7B", 7, 1 },
                    { 18, "B", false, "8B", 8, 1 },
                    { 19, "B", false, "9B", 9, 1 },
                    { 20, "B", false, "10B", 10, 1 },
                    { 21, "A", false, "1A", 1, 2 },
                    { 22, "A", false, "2A", 2, 2 },
                    { 23, "A", false, "3A", 3, 2 },
                    { 24, "A", false, "4A", 4, 2 },
                    { 25, "A", false, "5A", 5, 2 },
                    { 26, "A", false, "6A", 6, 2 },
                    { 27, "A", false, "7A", 7, 2 },
                    { 28, "A", false, "8A", 8, 2 },
                    { 29, "A", false, "9A", 9, 2 },
                    { 30, "A", false, "10A", 10, 2 },
                    { 31, "B", false, "1B", 1, 2 },
                    { 32, "B", false, "2B", 2, 2 },
                    { 33, "B", false, "3B", 3, 2 },
                    { 34, "B", false, "4B", 4, 2 },
                    { 35, "B", false, "5B", 5, 2 },
                    { 36, "B", false, "6B", 6, 2 },
                    { 37, "B", false, "7B", 7, 2 },
                    { 38, "B", false, "8B", 8, 2 },
                    { 39, "B", false, "9B", 9, 2 },
                    { 40, "B", false, "10B", 10, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingReference",
                table: "Bookings",
                column: "BookingReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SeatId",
                table: "Bookings",
                column: "SeatId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TrainId",
                table: "Bookings",
                column: "TrainId");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_TrainId_Number",
                table: "Seats",
                columns: new[] { "TrainId", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Seats");

            migrationBuilder.DropTable(
                name: "Trains");
        }
    }
}
