using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Janus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEndpointTargetUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetUrl",
                table: "Endpoints",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetUrl",
                table: "Endpoints");
        }
    }
}
