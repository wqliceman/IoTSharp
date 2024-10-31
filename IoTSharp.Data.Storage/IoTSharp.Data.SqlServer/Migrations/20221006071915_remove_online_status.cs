﻿using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace IoTSharp.Data.SqlServer.Migrations
{
    public partial class remove_online_status : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Device_DeviceModels_DeviceModelId",
                table: "Device");

            migrationBuilder.DropIndex(
                name: "IX_Device_DeviceModelId",
                table: "Device");

            migrationBuilder.DropColumn(
                name: "LastActive",
                table: "Device");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Device");

            migrationBuilder.RenameColumn(
                name: "Online",
                table: "Device",
                newName: "Deleted");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Deleted",
                table: "Device",
                newName: "Online");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActive",
                table: "Device",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Device",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Device_DeviceModelId",
                table: "Device",
                column: "DeviceModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Device_DeviceModels_DeviceModelId",
                table: "Device",
                column: "DeviceModelId",
                principalTable: "DeviceModels",
                principalColumn: "DeviceModelId");
        }
    }
}