﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Backend.Migrations
{
    [DbContext(typeof(BankaDB))]
    [Migration("20250331184236_StatisticsUpdate")]
    partial class StatisticsUpdate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("DBFreeAccount", b =>
                {
                    b.Property<int>("AccID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("AccID"));

                    b.Property<decimal>("Balance")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("BalanceHistory")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("BalanceLastUpdated")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(21)
                        .HasColumnType("varchar(21)");

                    b.Property<int>("UserID")
                        .HasColumnType("int");

                    b.HasKey("AccID");

                    b.ToTable("FreeAccounts");

                    b.HasDiscriminator().HasValue("DBFreeAccount");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("DBLog", b =>
                {
                    b.Property<int>("LogID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("LogID"));

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int>("DestAccID")
                        .HasColumnType("int");

                    b.Property<int>("DestAccType")
                        .HasColumnType("int");

                    b.Property<int>("SrcAccID")
                        .HasColumnType("int");

                    b.Property<int>("SrcAccType")
                        .HasColumnType("int");

                    b.Property<bool>("Success")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("Time")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UserID")
                        .HasColumnType("int");

                    b.HasKey("LogID");

                    b.ToTable("Logs");
                });

            modelBuilder.Entity("DBStatistics", b =>
                {
                    b.Property<int>("CreditAccountCount")
                        .HasColumnType("int");

                    b.Property<int>("FreeAccountCount")
                        .HasColumnType("int");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("SavingAccountCount")
                        .HasColumnType("int");

                    b.Property<int>("TotalAccountCount")
                        .HasColumnType("int");

                    b.Property<int>("TotalAdminCount")
                        .HasColumnType("int");

                    b.Property<int>("TotalBankerCount")
                        .HasColumnType("int");

                    b.Property<string>("TotalDebt30Days")
                        .HasColumnType("longtext");

                    b.Property<int>("TotalTransactionCount")
                        .HasColumnType("int");

                    b.Property<int>("TotalUserCount")
                        .HasColumnType("int");

                    b.Property<int>("TotalUsersCount")
                        .HasColumnType("int");

                    b.ToTable("Statistics");
                });

            modelBuilder.Entity("DBUser", b =>
                {
                    b.Property<int>("UserID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("UserID"));

                    b.Property<bool>("Bankrupt")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("Role")
                        .HasColumnType("int");

                    b.HasKey("UserID");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("DBCreditAccount", b =>
                {
                    b.HasBaseType("DBFreeAccount");

                    b.Property<DateTime>("MaturityDate")
                        .HasColumnType("datetime(6)");

                    b.HasDiscriminator().HasValue("DBCreditAccount");
                });

            modelBuilder.Entity("DBSavingAccount", b =>
                {
                    b.HasBaseType("DBFreeAccount");

                    b.Property<decimal>("DailyWithdrawal")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("MonthlyHistory")
                        .HasColumnType("longtext");

                    b.Property<DateTime>("MonthlyHistoryLastUpdated")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Student")
                        .HasColumnType("tinyint(1)");

                    b.HasDiscriminator().HasValue("DBSavingAccount");
                });
#pragma warning restore 612, 618
        }
    }
}
