﻿// <auto-generated />
using System;
using EmulatorHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EmulatorHub.MySqlMigrations.Migrations
{
    [DbContext(typeof(HubDbContext))]
    [Migration("20221119133246_add_providerId")]
    partial class add_providerId
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("EmulatorHub.Domain.Entities.TestEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id")
                        .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("timestamp");

                    b.Property<long>("ProviderId")
                        .HasColumnType("bigint")
                        .HasColumnName("provider_id");

                    b.Property<bool>("SoftDeleted")
                        .HasColumnType("tinyint(1)")
                        .HasColumnName("soft_deleted");

                    b.Property<string>("TestProperty")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("test_property");

                    b.HasKey("Id", "Timestamp")
                        .HasName("pk_test_entity");

                    b.ToTable("test_entity", (string)null);
                });

            modelBuilder.Entity("EmulatorHub.Domain.Entities.UserEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    b.Property<string>("CultureId")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("culture_id");

                    b.Property<DateTime?>("Deregistered")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("deregistered");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("email");

                    b.Property<int?>("LogLevel")
                        .HasColumnType("int")
                        .HasColumnName("log_level");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("password_hash");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasColumnType("longtext")
                        .HasColumnName("phone");

                    b.Property<long>("ProviderId")
                        .HasColumnType("bigint")
                        .HasColumnName("provider_id");

                    b.Property<DateTime?>("Registered")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("registered");

                    b.Property<int>("Type")
                        .HasColumnType("int")
                        .HasColumnName("type");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("varchar(255)")
                        .HasColumnName("username");

                    b.HasKey("Id")
                        .HasName("pk_users");

                    b.HasIndex("Username")
                        .IsUnique()
                        .HasDatabaseName("ix_users_username");

                    b.ToTable("users", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 1L,
                            CultureId = "ru-RU",
                            Email = "su@vayosoft.com",
                            PasswordHash = "VBbXzW7xlaD3YiqcVrVehA==",
                            Phone = "0500000000",
                            ProviderId = 1000L,
                            Registered = new DateTime(2022, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc),
                            Type = 4,
                            Username = "su"
                        });
                });

            modelBuilder.Entity("Vayosoft.Identity.Tokens.RefreshToken", b =>
                {
                    b.Property<long>("UserId")
                        .HasColumnType("bigint")
                        .HasColumnName("user_id");

                    b.Property<string>("Token")
                        .HasColumnType("varchar(255)")
                        .HasColumnName("token");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("created");

                    b.Property<string>("CreatedByIp")
                        .HasColumnType("longtext")
                        .HasColumnName("created_by_ip");

                    b.Property<DateTime>("Expires")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("expires");

                    b.Property<string>("ReasonRevoked")
                        .HasColumnType("longtext")
                        .HasColumnName("reason_revoked");

                    b.Property<string>("ReplacedByToken")
                        .HasColumnType("longtext")
                        .HasColumnName("replaced_by_token");

                    b.Property<DateTime?>("Revoked")
                        .HasColumnType("datetime(6)")
                        .HasColumnName("revoked");

                    b.Property<string>("RevokedByIp")
                        .HasColumnType("longtext")
                        .HasColumnName("revoked_by_ip");

                    b.HasKey("UserId", "Token")
                        .HasName("pk_refresh_token");

                    b.ToTable("refresh_token", (string)null);
                });

            modelBuilder.Entity("Vayosoft.Identity.Tokens.RefreshToken", b =>
                {
                    b.HasOne("EmulatorHub.Domain.Entities.UserEntity", "User")
                        .WithMany("RefreshTokens")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_refresh_token_users_user_entity_id");

                    b.Navigation("User");
                });

            modelBuilder.Entity("EmulatorHub.Domain.Entities.UserEntity", b =>
                {
                    b.Navigation("RefreshTokens");
                });
#pragma warning restore 612, 618
        }
    }
}
