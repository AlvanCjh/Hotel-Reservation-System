using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class AddModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_UserEmail",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_RoomTypes_RoomTypeTypeId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_RoomTypeTypeId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_UserEmail",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RoomTypeTypeId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "Reservations");

            migrationBuilder.AlterColumn<string>(
                name: "TypeId",
                table: "Rooms",
                type: "nvarchar(5)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Reservations",
                type: "nvarchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ReservationId",
                table: "Reservations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_TypeId",
                table: "Rooms",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Email",
                table: "Reservations",
                column: "Email");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_Email",
                table: "Reservations",
                column: "Email",
                principalTable: "Users",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_RoomTypes_TypeId",
                table: "Rooms",
                column: "TypeId",
                principalTable: "RoomTypes",
                principalColumn: "TypeId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_Email",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_RoomTypes_TypeId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_TypeId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_Email",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Reservations");

            migrationBuilder.AlterColumn<string>(
                name: "TypeId",
                table: "Rooms",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)");

            migrationBuilder.AddColumn<string>(
                name: "RoomTypeTypeId",
                table: "Rooms",
                type: "nvarchar(5)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)");

            migrationBuilder.AlterColumn<string>(
                name: "ReservationId",
                table: "Reservations",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "Reservations",
                type: "nvarchar(100)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomTypeTypeId",
                table: "Rooms",
                column: "RoomTypeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserEmail",
                table: "Reservations",
                column: "UserEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_UserEmail",
                table: "Reservations",
                column: "UserEmail",
                principalTable: "Users",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_RoomTypes_RoomTypeTypeId",
                table: "Rooms",
                column: "RoomTypeTypeId",
                principalTable: "RoomTypes",
                principalColumn: "TypeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
