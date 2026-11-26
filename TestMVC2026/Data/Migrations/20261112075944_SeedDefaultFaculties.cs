using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TestMVC2026.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultFaculties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Faculties",
                columns: new[] { "Code", "Name" },
                values: new object[,]
                {
                    { "CNTT", "Công nghệ thông tin" },
                    { "KT", "Kinh tế" },
                    { "XD", "Xây dựng" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "Code",
                keyValue: "CNTT");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "Code",
                keyValue: "KT");

            migrationBuilder.DeleteData(
                table: "Faculties",
                keyColumn: "Code",
                keyValue: "XD");
        }
    }
}
