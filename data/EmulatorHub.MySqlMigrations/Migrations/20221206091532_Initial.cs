using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmulatorHub.MySqlMigrations.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sec_objs",
                columns: table => new
                {
                    objid = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    obj_name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    obj_desc = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sec_objs", x => x.objid);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sec_role_permissions",
                columns: table => new
                {
                    permid = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    roleid = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    objid = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    perms = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sec_role_permissions", x => x.permid);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sec_roles",
                columns: table => new
                {
                    roleid = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role_name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role_desc = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    providerid = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sec_roles", x => x.roleid);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sec_user_roles",
                columns: table => new
                {
                    urid = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    roleid = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sec_user_roles", x => x.urid);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    userid = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    username = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pwdhash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    regdate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    enddate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    culture_id = table.Column<string>(type: "longtext", nullable: true, defaultValue: "he-IL")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    providerid = table.Column<long>(type: "bigint", nullable: false),
                    log_level = table.Column<byte>(type: "tinyint unsigned", nullable: true, defaultValue: (byte)3)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.userid);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    push_token = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_id = table.Column<long>(type: "bigint", nullable: false),
                    soft_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.id);
                    table.ForeignKey(
                        name: "fk_clients_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "userid");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    userid = table.Column<long>(type: "bigint", nullable: false),
                    token = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expires = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by_ip = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    revoked = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    replaced_by_token = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason_revoked = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => new { x.userid, x.token });
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_entity_id",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    client_id = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_devices_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "sec_roles",
                columns: new[] { "roleid", "role_desc", "role_name", "providerid" },
                values: new object[,]
                {
                    { "f6694d71d26e40f5a2abb357177c9bdt", null, "Support", null },
                    { "f6694d71d26e40f5a2abb357177c9bdx", null, "Administrator", null },
                    { "f6694d71d26e40f5a2abb357177c9bdz", null, "Supervisor", null }
                });

            migrationBuilder.InsertData(
                table: "sec_user_roles",
                columns: new[] { "urid", "roleid", "user_id" },
                values: new object[] { "0e5085516ee34d4bab806757e41f6dd6", "f6694d71d26e40f5a2abb357177c9bdz", 1L });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "userid", "culture_id", "enddate", "email", "pwdhash", "phone", "providerid", "regdate", "user_type", "username" },
                values: new object[] { 1L, "ru-RU", null, "su@vayosoft.com", "VBbXzW7xlaD3YiqcVrVehA==", "0500000000", 1000L, new DateTime(2022, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc), (byte)4, "su" });

            migrationBuilder.CreateIndex(
                name: "ix_clients_id_provider_id",
                table: "clients",
                columns: new[] { "id", "provider_id" },
                unique: true,
                filter: "NOT SoftDeleted");

            migrationBuilder.CreateIndex(
                name: "ix_clients_soft_deleted",
                table: "clients",
                column: "soft_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_clients_user_id",
                table: "clients",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_devices_client_id",
                table: "devices",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_devices_provider_id",
                table: "devices",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_username_providerid",
                table: "users",
                columns: new[] { "username", "providerid" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "sec_objs");

            migrationBuilder.DropTable(
                name: "sec_role_permissions");

            migrationBuilder.DropTable(
                name: "sec_roles");

            migrationBuilder.DropTable(
                name: "sec_user_roles");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
