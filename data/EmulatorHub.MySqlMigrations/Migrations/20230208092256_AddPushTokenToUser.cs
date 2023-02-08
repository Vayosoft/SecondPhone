using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmulatorHub.MySqlMigrations.Migrations
{
    public partial class AddPushTokenToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_refresh_tokens_users_user_entity_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "push_token",
                table: "clients");

            migrationBuilder.AddColumn<string>(
                name: "push_token",
                table: "users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "userid",
                keyValue: 1L,
                column: "providerid",
                value: 0L);

            migrationBuilder.AddForeignKey(
                name: "fk_refresh_tokens_users_application_user_id",
                table: "refresh_tokens",
                column: "userid",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_refresh_tokens_users_application_user_id",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "push_token",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "push_token",
                table: "clients",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "userid",
                keyValue: 1L,
                column: "providerid",
                value: 1000L);

            migrationBuilder.AddForeignKey(
                name: "fk_refresh_tokens_users_user_entity_id",
                table: "refresh_tokens",
                column: "userid",
                principalTable: "users",
                principalColumn: "userid",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
