﻿using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmulatorHub.MySqlMigrations.Migrations
{
    public partial class Add_TestEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "test_entity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    alias = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    registered_date = table.Column<DateOnly>(type: "DATE", nullable: false),
                    @double = table.Column<double>(name: "double", type: "double", precision: 8, scale: 2, nullable: false),
                    @enum = table.Column<string>(name: "enum", type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    provider_id = table.Column<long>(type: "bigint", nullable: false),
                    soft_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_entity", x => new { x.id, x.timestamp });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_test_entity_name_provider_id",
                table: "test_entity",
                columns: new[] { "name", "provider_id" },
                unique: true,
                filter: "NOT SoftDeleted");

            migrationBuilder.CreateIndex(
                name: "ix_test_entity_provider_id",
                table: "test_entity",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_entity_soft_deleted",
                table: "test_entity",
                column: "soft_deleted");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_entity");
        }
    }
}
