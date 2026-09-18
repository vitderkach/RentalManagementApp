using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalManagementApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDesiredLeaseStartDateToRentalApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DesiredLeaseStartDate",
                table: "RentalApplications",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DesiredLeaseStartDate",
                table: "RentalApplications");
        }
    }
}
