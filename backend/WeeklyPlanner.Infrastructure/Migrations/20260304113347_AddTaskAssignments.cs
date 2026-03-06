using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReady",
                table: "CycleMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TaskAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BacklogItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAssignments_BacklogItems_BacklogItemId",
                        column: x => x.BacklogItemId,
                        principalTable: "BacklogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskAssignments_CycleMembers_CycleMemberId",
                        column: x => x.CycleMemberId,
                        principalTable: "CycleMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_BacklogItemId",
                table: "TaskAssignments",
                column: "BacklogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_CycleMemberId_BacklogItemId",
                table: "TaskAssignments",
                columns: new[] { "CycleMemberId", "BacklogItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskAssignments");

            migrationBuilder.DropColumn(
                name: "IsReady",
                table: "CycleMembers");
        }
    }
}
