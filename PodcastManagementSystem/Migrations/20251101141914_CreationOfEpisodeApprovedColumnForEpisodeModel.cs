using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PodcastManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class CreationOfEpisodeApprovedColumnForEpisodeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "creationOfEpisodeApproved",
                table: "Episodes",
                newName: "CreationOfEpisodeApproved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreationOfEpisodeApproved",
                table: "Episodes",
                newName: "creationOfEpisodeApproved");
        }
    }
}
