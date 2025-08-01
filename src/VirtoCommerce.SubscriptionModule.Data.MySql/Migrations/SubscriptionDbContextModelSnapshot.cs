﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VirtoCommerce.SubscriptionModule.Data.Repositories;

#nullable disable

namespace VirtoCommerce.SubscriptionModule.Data.MySql.Migrations
{
    [DbContext(typeof(SubscriptionDbContext))]
    partial class SubscriptionDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("VirtoCommerce.SubscriptionModule.Data.Model.PaymentPlanEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Interval")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<int>("IntervalCount")
                        .HasColumnType("int");

                    b.Property<string>("ModifiedBy")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime?>("ModifiedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("ProductId")
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<int>("TrialPeriodDays")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("PaymentPlan", (string)null);
                });

            modelBuilder.Entity("VirtoCommerce.SubscriptionModule.Data.Model.SubscriptionEntity", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<decimal>("Balance")
                        .HasPrecision(18, 4)
                        .HasColumnType("decimal");

                    b.Property<string>("CancelReason")
                        .HasMaxLength(2048)
                        .HasColumnType("varchar(2048)");

                    b.Property<DateTime?>("CancelledDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Comment")
                        .HasColumnType("longtext");

                    b.Property<string>("CreatedBy")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("CurrentPeriodEnd")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime?>("CurrentPeriodStart")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("CustomerId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("CustomerName")
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("CustomerOrderPrototypeId")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Interval")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<int>("IntervalCount")
                        .HasColumnType("int");

                    b.Property<bool>("IsCancelled")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("ModifiedBy")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime?>("ModifiedDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Number")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("OuterId")
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<DateTime?>("StartDate")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Status")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("StoreId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime?>("TrialEnd")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("TrialPeriodDays")
                        .HasColumnType("int");

                    b.Property<DateTime?>("TrialStart")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("Subscription", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
