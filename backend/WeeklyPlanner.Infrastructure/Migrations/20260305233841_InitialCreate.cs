using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeeklyPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CycleMembers_TeamMembers_TeamMemberId",
                table: "CycleMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskAssignments_CycleMembers_CycleMemberId",
                table: "TaskAssignments");

            migrationBuilder.DropTable(
                name: "CategoryBudgets");

            migrationBuilder.DropIndex(
                name: "IX_TaskAssignments_CycleMemberId_BacklogItemId",
                table: "TaskAssignments");

            migrationBuilder.DropColumn(
                name: "PlannedHours",
                table: "TaskAssignments");

            migrationBuilder.DropColumn(
                name: "AllocatedHours",
                table: "CycleMembers");

            migrationBuilder.DropColumn(
                name: "IsReady",
                table: "CycleMembers");

            migrationBuilder.DropColumn(
                name: "EstimatedHours",
                table: "BacklogItems");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "BacklogItems");

            migrationBuilder.RenameColumn(
                name: "CycleMemberId",
                table: "TaskAssignments",
                newName: "MemberPlanId");

            migrationBuilder.RenameColumn(
                name: "WeekStartDate",
                table: "PlanningCycles",
                newName: "PlanningDate");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "PlanningCycles",
                newName: "TeamCapacity");

            migrationBuilder.RenameColumn(
                name: "TeamMemberId",
                table: "CycleMembers",
                newName: "MemberId");

            migrationBuilder.RenameIndex(
                name: "IX_CycleMembers_TeamMemberId",
                table: "CycleMembers",
                newName: "IX_CycleMembers_MemberId");

            migrationBuilder.RenameIndex(
                name: "IX_CycleMembers_CycleId_TeamMemberId",
                table: "CycleMembers",
                newName: "IX_CycleMembers_CycleId_MemberId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "TeamMembers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "CommittedHours",
                table: "TaskAssignments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HoursCompleted",
                table: "TaskAssignments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProgressStatus",
                table: "TaskAssignments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutionEndDate",
                table: "PlanningCycles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutionStartDate",
                table: "PlanningCycles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "PlanningCycles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "BacklogItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BacklogItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "BacklogItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "BacklogItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedEffort",
                table: "BacklogItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoryAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Percentage = table.Column<int>(type: "int", nullable: false),
                    BudgetHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryAllocations_PlanningCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "PlanningCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsReady = table.Column<bool>(type: "bit", nullable: false),
                    TotalPlannedHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberPlans_PlanningCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "PlanningCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberPlans_TeamMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProgressUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousHoursCompleted = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewHoursCompleted = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NewStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressUpdates_TaskAssignments_TaskAssignmentId",
                        column: x => x.TaskAssignmentId,
                        principalTable: "TaskAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProgressUpdates_TeamMembers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_MemberPlanId",
                table: "TaskAssignments",
                column: "MemberPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_BacklogItems_CreatedBy",
                table: "BacklogItems",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BacklogItems_Status_Category",
                table: "BacklogItems",
                columns: new[] { "Status", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAllocations_CycleId_Category",
                table: "CategoryAllocations",
                columns: new[] { "CycleId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberPlans_CycleId_MemberId",
                table: "MemberPlans",
                columns: new[] { "CycleId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberPlans_MemberId",
                table: "MemberPlans",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressUpdates_TaskAssignmentId",
                table: "ProgressUpdates",
                column: "TaskAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressUpdates_UpdatedBy",
                table: "ProgressUpdates",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_BacklogItems_TeamMembers_CreatedBy",
                table: "BacklogItems",
                column: "CreatedBy",
                principalTable: "TeamMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CycleMembers_TeamMembers_MemberId",
                table: "CycleMembers",
                column: "MemberId",
                principalTable: "TeamMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskAssignments_MemberPlans_MemberPlanId",
                table: "TaskAssignments",
                column: "MemberPlanId",
                principalTable: "MemberPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BacklogItems_TeamMembers_CreatedBy",
                table: "BacklogItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CycleMembers_TeamMembers_MemberId",
                table: "CycleMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskAssignments_MemberPlans_MemberPlanId",
                table: "TaskAssignments");

            migrationBuilder.DropTable(
                name: "CategoryAllocations");

            migrationBuilder.DropTable(
                name: "MemberPlans");

            migrationBuilder.DropTable(
                name: "ProgressUpdates");

            migrationBuilder.DropIndex(
                name: "IX_TaskAssignments_MemberPlanId",
                table: "TaskAssignments");

            migrationBuilder.DropIndex(
                name: "IX_BacklogItems_CreatedBy",
                table: "BacklogItems");

            migrationBuilder.DropIndex(
                name: "IX_BacklogItems_Status_Category",
                table: "BacklogItems");

            migrationBuilder.DropColumn(
                name: "CommittedHours",
                table: "TaskAssignments");

            migrationBuilder.DropColumn(
                name: "HoursCompleted",
                table: "TaskAssignments");

            migrationBuilder.DropColumn(
                name: "ProgressStatus",
                table: "TaskAssignments");

            migrationBuilder.DropColumn(
                name: "ExecutionEndDate",
                table: "PlanningCycles");

            migrationBuilder.DropColumn(
                name: "ExecutionStartDate",
                table: "PlanningCycles");

            migrationBuilder.DropColumn(
                name: "State",
                table: "PlanningCycles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "BacklogItems");

            migrationBuilder.DropColumn(
                name: "EstimatedEffort",
                table: "BacklogItems");

            migrationBuilder.RenameColumn(
                name: "MemberPlanId",
                table: "TaskAssignments",
                newName: "CycleMemberId");

            migrationBuilder.RenameColumn(
                name: "TeamCapacity",
                table: "PlanningCycles",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "PlanningDate",
                table: "PlanningCycles",
                newName: "WeekStartDate");

            migrationBuilder.RenameColumn(
                name: "MemberId",
                table: "CycleMembers",
                newName: "TeamMemberId");

            migrationBuilder.RenameIndex(
                name: "IX_CycleMembers_MemberId",
                table: "CycleMembers",
                newName: "IX_CycleMembers_TeamMemberId");

            migrationBuilder.RenameIndex(
                name: "IX_CycleMembers_CycleId_MemberId",
                table: "CycleMembers",
                newName: "IX_CycleMembers_CycleId_TeamMemberId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "TeamMembers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<decimal>(
                name: "PlannedHours",
                table: "TaskAssignments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedHours",
                table: "CycleMembers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsReady",
                table: "CycleMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "BacklogItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "BacklogItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "BacklogItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedHours",
                table: "BacklogItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "BacklogItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CategoryBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    HoursBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryBudgets_PlanningCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "PlanningCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignments_CycleMemberId_BacklogItemId",
                table: "TaskAssignments",
                columns: new[] { "CycleMemberId", "BacklogItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudgets_CycleId",
                table: "CategoryBudgets",
                column: "CycleId");

            migrationBuilder.AddForeignKey(
                name: "FK_CycleMembers_TeamMembers_TeamMemberId",
                table: "CycleMembers",
                column: "TeamMemberId",
                principalTable: "TeamMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskAssignments_CycleMembers_CycleMemberId",
                table: "TaskAssignments",
                column: "CycleMemberId",
                principalTable: "CycleMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
