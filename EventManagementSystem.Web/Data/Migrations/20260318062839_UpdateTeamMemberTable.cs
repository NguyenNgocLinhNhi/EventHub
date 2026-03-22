using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventManagementSystem.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTeamMemberTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TwitterUrl",
                table: "TeamMembers",
                newName: "ZaloUrl");

            migrationBuilder.RenameColumn(
                name: "InstagramUrl",
                table: "TeamMembers",
                newName: "GithubUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ZaloUrl",
                table: "TeamMembers",
                newName: "TwitterUrl");

            migrationBuilder.RenameColumn(
                name: "GithubUrl",
                table: "TeamMembers",
                newName: "InstagramUrl");
        }
    }
}
