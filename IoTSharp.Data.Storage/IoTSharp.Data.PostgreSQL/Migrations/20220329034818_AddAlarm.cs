﻿using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace IoTSharp.Migrations
{
    public partial class AddAlarm : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alarms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlarmType = table.Column<string>(type: "text", nullable: true),
                    AlarmDetail = table.Column<string>(type: "text", nullable: true),
                    AckDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClearDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AlarmStatus = table.Column<int>(type: "integer", nullable: false),
                    Serverity = table.Column<int>(type: "integer", nullable: false),
                    Propagate = table.Column<bool>(type: "boolean", nullable: false),
                    OriginatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginatorType = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alarms_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Alarms_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_CustomerId",
                table: "Alarms",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_TenantId",
                table: "Alarms",
                column: "TenantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alarms");
        }
    }
}