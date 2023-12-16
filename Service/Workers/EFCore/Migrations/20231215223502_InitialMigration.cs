using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace AppliedSoftware.Workers.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "package",
                columns: table => new
                {
                    PackageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("package__pk", x => x.PackageId);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    Uid = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    FirebaseDisplayName = table.Column<string>(type: "text", nullable: true),
                    Disabled = table.Column<bool>(type: "boolean", nullable: false),
                    FirebaseDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user__pk", x => x.Uid);
                });

            migrationBuilder.CreateTable(
                name: "package_action",
                columns: table => new
                {
                    PackageActionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PackageId = table.Column<long>(type: "bigint", nullable: false),
                    PackageActionType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("package_action__pk", x => x.PackageActionId);
                    table.ForeignKey(
                        name: "FK_package_action_package_PackageId",
                        column: x => x.PackageId,
                        principalTable: "package",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team",
                columns: table => new
                {
                    TeamId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefaultAllowedPermissions = table.Column<int>(type: "integer", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    PackageId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("team__pk", x => x.TeamId);
                    table.ForeignKey(
                        name: "FK_team_package_PackageId",
                        column: x => x.PackageId,
                        principalTable: "package",
                        principalColumn: "PackageId");
                });

            migrationBuilder.CreateTable(
                name: "PackageDtoUserDto",
                columns: table => new
                {
                    AdministratorsUid = table.Column<string>(type: "text", nullable: false),
                    PackageAdministratorPackageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageDtoUserDto", x => new { x.AdministratorsUid, x.PackageAdministratorPackageId });
                    table.ForeignKey(
                        name: "FK_PackageDtoUserDto_package_PackageAdministratorPackageId",
                        column: x => x.PackageAdministratorPackageId,
                        principalTable: "package",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageDtoUserDto_user_AdministratorsUid",
                        column: x => x.AdministratorsUid,
                        principalTable: "user",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "global_permission",
                columns: table => new
                {
                    GlobalPermissionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    GrantedGlobalPermission = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("global_permission__pk", x => x.GlobalPermissionId);
                    table.ForeignKey(
                        name: "FK_global_permission_user_UserId",
                        column: x => x.UserId,
                        principalTable: "user",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_package_action",
                columns: table => new
                {
                    EmailId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PackageActionId = table.Column<long>(type: "bigint", nullable: false),
                    Recipients = table.Column<string>(type: "text", nullable: true),
                    Sender = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    EmailTsVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "english")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "Subject", "Body", "Recipients", "Sender" }),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("email_id_action__pk", x => x.EmailId);
                    table.ForeignKey(
                        name: "FK_email_package_action_package_action_PackageActionId",
                        column: x => x.PackageActionId,
                        principalTable: "package_action",
                        principalColumn: "PackageActionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_permission_override",
                columns: table => new
                {
                    UserPermissionOverrideId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PackageId = table.Column<long>(type: "bigint", nullable: false),
                    PackageActionId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedPermissions = table.Column<int>(type: "integer", nullable: false),
                    DisallowedPermissions = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_permission_override__pk", x => x.UserPermissionOverrideId);
                    table.ForeignKey(
                        name: "FK_user_permission_override_package_PackageId",
                        column: x => x.PackageId,
                        principalTable: "package",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_permission_override_package_action_PackageActionId",
                        column: x => x.PackageActionId,
                        principalTable: "package_action",
                        principalColumn: "PackageActionId");
                    table.ForeignKey(
                        name: "FK_user_permission_override_user_UserId",
                        column: x => x.UserId,
                        principalTable: "user",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamDtoUserDto",
                columns: table => new
                {
                    TeamsTeamId = table.Column<long>(type: "bigint", nullable: false),
                    UsersUid = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamDtoUserDto", x => new { x.TeamsTeamId, x.UsersUid });
                    table.ForeignKey(
                        name: "FK_TeamDtoUserDto_team_TeamsTeamId",
                        column: x => x.TeamsTeamId,
                        principalTable: "team",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamDtoUserDto_user_UsersUid",
                        column: x => x.UsersUid,
                        principalTable: "user",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_group",
                columns: table => new
                {
                    UserGroupId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AllowedPermissions = table.Column<int>(type: "integer", nullable: true),
                    DisallowedPermissions = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_group__pk", x => x.UserGroupId);
                    table.ForeignKey(
                        name: "FK_user_group_team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "team",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_attachment",
                columns: table => new
                {
                    AttachmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmailPackageActionId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("email_attachment__pk", x => x.AttachmentId);
                    table.ForeignKey(
                        name: "FK_email_attachment_email_package_action_EmailPackageActionId",
                        column: x => x.EmailPackageActionId,
                        principalTable: "email_package_action",
                        principalColumn: "EmailId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDtoUserGroupDto",
                columns: table => new
                {
                    UserGroupsUserGroupId = table.Column<long>(type: "bigint", nullable: false),
                    UsersUid = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDtoUserGroupDto", x => new { x.UserGroupsUserGroupId, x.UsersUid });
                    table.ForeignKey(
                        name: "FK_UserDtoUserGroupDto_user_UsersUid",
                        column: x => x.UsersUid,
                        principalTable: "user",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserDtoUserGroupDto_user_group_UserGroupsUserGroupId",
                        column: x => x.UserGroupsUserGroupId,
                        principalTable: "user_group",
                        principalColumn: "UserGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_group_permission_override",
                columns: table => new
                {
                    UserGroupOverrideId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserGroupId = table.Column<long>(type: "bigint", nullable: false),
                    PackageId = table.Column<long>(type: "bigint", nullable: false),
                    PackageActionId = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    AllowedPermissions = table.Column<int>(type: "integer", nullable: false),
                    DisallowedPermissions = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_group_permission_override__pk", x => x.UserGroupOverrideId);
                    table.ForeignKey(
                        name: "FK_user_group_permission_override_package_PackageId",
                        column: x => x.PackageId,
                        principalTable: "package",
                        principalColumn: "PackageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_group_permission_override_package_action_PackageAction~",
                        column: x => x.PackageActionId,
                        principalTable: "package_action",
                        principalColumn: "PackageActionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_group_permission_override_user_group_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "user_group",
                        principalColumn: "UserGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackageDtoUserDto_PackageAdministratorPackageId",
                table: "PackageDtoUserDto",
                column: "PackageAdministratorPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamDtoUserDto_UsersUid",
                table: "TeamDtoUserDto",
                column: "UsersUid");

            migrationBuilder.CreateIndex(
                name: "IX_UserDtoUserGroupDto_UsersUid",
                table: "UserDtoUserGroupDto",
                column: "UsersUid");

            migrationBuilder.CreateIndex(
                name: "IX_email_attachment_EmailPackageActionId",
                table: "email_attachment",
                column: "EmailPackageActionId");

            migrationBuilder.CreateIndex(
                name: "IX_email_package_action_PackageActionId",
                table: "email_package_action",
                column: "PackageActionId");

            migrationBuilder.CreateIndex(
                name: "email_tsv__indx",
                table: "email_package_action",
                column: "EmailTsVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_global_permission_UserId",
                table: "global_permission",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "global_permission_uid__indx",
                table: "global_permission",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "package_name_unq__indx",
                table: "package",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "package_action_unq__indx",
                table: "package_action",
                columns: new[] { "PackageId", "PackageActionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_team_PackageId",
                table: "team",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "team_name__indx",
                table: "team",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "user_display_name__indx",
                table: "user",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_TeamId",
                table: "user_group",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_permission_override_PackageActionId",
                table: "user_group_permission_override",
                column: "PackageActionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_permission_override_PackageId",
                table: "user_group_permission_override",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_permission_override_UserGroupId",
                table: "user_group_permission_override",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_override_PackageActionId",
                table: "user_permission_override",
                column: "PackageActionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_override_PackageId",
                table: "user_permission_override",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_override_UserId",
                table: "user_permission_override",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageDtoUserDto");

            migrationBuilder.DropTable(
                name: "TeamDtoUserDto");

            migrationBuilder.DropTable(
                name: "UserDtoUserGroupDto");

            migrationBuilder.DropTable(
                name: "email_attachment");

            migrationBuilder.DropTable(
                name: "global_permission");

            migrationBuilder.DropTable(
                name: "user_group_permission_override");

            migrationBuilder.DropTable(
                name: "user_permission_override");

            migrationBuilder.DropTable(
                name: "email_package_action");

            migrationBuilder.DropTable(
                name: "user_group");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "package_action");

            migrationBuilder.DropTable(
                name: "team");

            migrationBuilder.DropTable(
                name: "package");
        }
    }
}
