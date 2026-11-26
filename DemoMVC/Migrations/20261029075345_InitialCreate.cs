using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DemoMVC.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "Khoa" (
                    "Code" TEXT NOT NULL CONSTRAINT "PK_Khoa" PRIMARY KEY,
                    "Name" TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS "StudentCodeCounter" (
                    "Prefix" TEXT NOT NULL CONSTRAINT "PK_StudentCodeCounter" PRIMARY KEY,
                    "LastNumber" INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS "SinhVien" (
                    "StudentCode" TEXT NOT NULL CONSTRAINT "PK_SinhVien" PRIMARY KEY,
                    "FullName" TEXT NOT NULL,
                    "EntryYear" INTEGER NOT NULL,
                    "TrainingSystemCode" TEXT NOT NULL,
                    "MajorCode" TEXT NOT NULL,
                    "Address" TEXT NULL,
                    "KhoaCode" TEXT NOT NULL,
                    CONSTRAINT "FK_SinhVien_Khoa_KhoaCode"
                        FOREIGN KEY ("KhoaCode") REFERENCES "Khoa" ("Code") ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS "IX_SinhVien_KhoaCode"
                    ON "SinhVien" ("KhoaCode");

                INSERT OR IGNORE INTO "Khoa" ("Code", "Name") VALUES
                    ('CNTT', 'Khoa Công nghệ Thông tin'),
                    ('DC', 'Khoa Địa chất'),
                    ('MO', 'Khoa Kỹ thuật Mỏ'),
                    ('KT', 'Khoa Kinh tế'),
                    ('DCK', 'Khoa Cơ - Điện'),
                    ('DCKT', 'Khoa Dầu khí'),
                    ('XD', 'Khoa Xây dựng'),
                    ('MT', 'Khoa Môi trường'),
                    ('TDBD', 'Khoa Trắc địa - Bản đồ và Quản lý đất đai'),
                    ('KHCB', 'Khoa Khoa học cơ bản'),
                    ('LLCT', 'Khoa Lý luận chính trị'),
                    ('GDQP', 'Khoa Giáo dục quốc phòng');

                INSERT OR IGNORE INTO "StudentCodeCounter" ("Prefix", "LastNumber")
                    VALUES ('212105', 238);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SinhVien");

            migrationBuilder.DropTable(
                name: "StudentCodeCounter");

            migrationBuilder.DropTable(
                name: "Khoa");
        }
    }
}
