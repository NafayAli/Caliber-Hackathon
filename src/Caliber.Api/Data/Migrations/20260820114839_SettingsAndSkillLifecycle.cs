using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caliber.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SettingsAndSkillLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpiresOn",
                table: "EmployeeSkills",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceEmployeeCertificationId",
                table: "EmployeeSkills",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceEmployeeTrainingId",
                table: "EmployeeSkills",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "EmployeeSkills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    OrganizationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SupportPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Tagline = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleModuleAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    ModuleKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleModuleAccess", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillAssignmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SkillId = table.Column<int>(type: "int", nullable: false),
                    RequestedProficiency = table.Column<int>(type: "int", nullable: false),
                    RequestedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillAssignmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillAssignmentRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkillAssignmentRequests_Employees_RequestedByEmployeeId",
                        column: x => x.RequestedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SkillAssignmentRequests_Employees_ReviewedByEmployeeId",
                        column: x => x.ReviewedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SkillAssignmentRequests_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSkills_ExpiresOn",
                table: "EmployeeSkills",
                column: "ExpiresOn");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSkills_SourceEmployeeCertificationId",
                table: "EmployeeSkills",
                column: "SourceEmployeeCertificationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSkills_SourceEmployeeTrainingId",
                table: "EmployeeSkills",
                column: "SourceEmployeeTrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSkills_Status",
                table: "EmployeeSkills",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RoleModuleAccess_AccessLevel_ModuleKey",
                table: "RoleModuleAccess",
                columns: new[] { "AccessLevel", "ModuleKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillAssignmentRequests_EmployeeId_SkillId_Status",
                table: "SkillAssignmentRequests",
                columns: new[] { "EmployeeId", "SkillId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillAssignmentRequests_RequestedByEmployeeId",
                table: "SkillAssignmentRequests",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillAssignmentRequests_ReviewedByEmployeeId",
                table: "SkillAssignmentRequests",
                column: "ReviewedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillAssignmentRequests_SkillId",
                table: "SkillAssignmentRequests",
                column: "SkillId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeSkills_EmployeeCertifications_SourceEmployeeCertificationId",
                table: "EmployeeSkills",
                column: "SourceEmployeeCertificationId",
                principalTable: "EmployeeCertifications",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeSkills_EmployeeTrainings_SourceEmployeeTrainingId",
                table: "EmployeeSkills",
                column: "SourceEmployeeTrainingId",
                principalTable: "EmployeeTrainings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeSkills_EmployeeCertifications_SourceEmployeeCertificationId",
                table: "EmployeeSkills");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeSkills_EmployeeTrainings_SourceEmployeeTrainingId",
                table: "EmployeeSkills");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "RoleModuleAccess");

            migrationBuilder.DropTable(
                name: "SkillAssignmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeSkills_ExpiresOn",
                table: "EmployeeSkills");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeSkills_SourceEmployeeCertificationId",
                table: "EmployeeSkills");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeSkills_SourceEmployeeTrainingId",
                table: "EmployeeSkills");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeSkills_Status",
                table: "EmployeeSkills");

            migrationBuilder.DropColumn(
                name: "ExpiresOn",
                table: "EmployeeSkills");

            migrationBuilder.DropColumn(
                name: "SourceEmployeeCertificationId",
                table: "EmployeeSkills");

            migrationBuilder.DropColumn(
                name: "SourceEmployeeTrainingId",
                table: "EmployeeSkills");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EmployeeSkills");
        }
    }
}
