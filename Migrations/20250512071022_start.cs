using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIASGraduate.Migrations
{
    /// <inheritdoc />
    public partial class start : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Awards",
                columns: table => new
                {
                    AwardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AwardName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AwardDescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaxVoteCount = table.Column<int>(type: "int", nullable: false),
                    CoverImage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CoverImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Awards", x => x.AwardId);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "SupAdmins",
                columns: table => new
                {
                    SupAdminId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupAdminImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Account = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    SupAdminName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SupAdminPassword = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupAdmins", x => x.SupAdminId);
                });

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    AdminId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Account = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    AdminName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AdminPassword = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.AdminId);
                    table.ForeignKey(
                        name: "FK_Admins_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId");
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Account = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EmployeePassword = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    EmployeeFileData = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId");
                });

            migrationBuilder.CreateTable(
                name: "NominationDeclarations",
                columns: table => new
                {
                    DeclarationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AwardId = table.Column<int>(type: "int", nullable: false),
                    NominatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    NominatedAdminId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Introduction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    DeclarationReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeclarerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    DeclarerAdminId = table.Column<int>(type: "int", nullable: true),
                    DeclarerSupAdminId = table.Column<int>(type: "int", nullable: true),
                    DeclarationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ReviewerAdminId = table.Column<int>(type: "int", nullable: true),
                    ReviewerSupAdminId = table.Column<int>(type: "int", nullable: true),
                    ReviewTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPromoted = table.Column<bool>(type: "bit", nullable: false),
                    PromotedNominationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NominationDeclarations", x => x.DeclarationId);
                    table.ForeignKey(
                        name: "FK_NominationDeclarations_Admins_DeclarerAdminId",
                        column: x => x.DeclarerAdminId,
                        principalTable: "Admins",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NominationDeclarations_Admins_NominatedAdminId",
                        column: x => x.NominatedAdminId,
                        principalTable: "Admins",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NominationDeclarations_Admins_ReviewerAdminId",
                        column: x => x.ReviewerAdminId,
                        principalTable: "Admins",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NominationDeclarations_Awards_AwardId",
                        column: x => x.AwardId,
                        principalTable: "Awards",
                        principalColumn: "AwardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NominationDeclarations_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NominationDeclarations_Employees_DeclarerEmployeeId",
                        column: x => x.DeclarerEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NominationDeclarations_Employees_NominatedEmployeeId",
                        column: x => x.NominatedEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NominationDeclarations_Employees_ReviewerEmployeeId",
                        column: x => x.ReviewerEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NominationDeclarations_SupAdmins_DeclarerSupAdminId",
                        column: x => x.DeclarerSupAdminId,
                        principalTable: "SupAdmins",
                        principalColumn: "SupAdminId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NominationDeclarations_SupAdmins_ReviewerSupAdminId",
                        column: x => x.ReviewerSupAdminId,
                        principalTable: "SupAdmins",
                        principalColumn: "SupAdminId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Nominations",
                columns: table => new
                {
                    NominationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AwardId = table.Column<int>(type: "int", nullable: false),
                    NominatedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    NominatedAdminId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Introduction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    NominateReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ProposerAdminId = table.Column<int>(type: "int", nullable: true),
                    ProposerSupAdminId = table.Column<int>(type: "int", nullable: true),
                    NominationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommentCount = table.Column<int>(type: "int", nullable: false),
                    LastCommentTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCommenterEmployeeId = table.Column<int>(type: "int", nullable: true),
                    LastCommenterAdminId = table.Column<int>(type: "int", nullable: true),
                    LastCommenterSupAdminId = table.Column<int>(type: "int", nullable: true),
                    LastCommentPreview = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nominations", x => x.NominationId);
                    table.ForeignKey(
                        name: "FK_Nominations_Admins_NominatedAdminId",
                        column: x => x.NominatedAdminId,
                        principalTable: "Admins",
                        principalColumn: "AdminId");
                    table.ForeignKey(
                        name: "FK_Nominations_Admins_ProposerAdminId",
                        column: x => x.ProposerAdminId,
                        principalTable: "Admins",
                        principalColumn: "AdminId");
                    table.ForeignKey(
                        name: "FK_Nominations_Awards_AwardId",
                        column: x => x.AwardId,
                        principalTable: "Awards",
                        principalColumn: "AwardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Nominations_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId");
                    table.ForeignKey(
                        name: "FK_Nominations_Employees_NominatedEmployeeId",
                        column: x => x.NominatedEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_Nominations_Employees_ProposerEmployeeId",
                        column: x => x.ProposerEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_Nominations_SupAdmins_ProposerSupAdminId",
                        column: x => x.ProposerSupAdminId,
                        principalTable: "SupAdmins",
                        principalColumn: "SupAdminId");
                });

            migrationBuilder.CreateTable(
                name: "NominationLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeclarationId = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    OperationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperatorEmployeeId = table.Column<int>(type: "int", nullable: true),
                    OperatorAdminId = table.Column<int>(type: "int", nullable: true),
                    OperatorSupAdminId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NominationLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_NominationLogs_Admins_OperatorAdminId",
                        column: x => x.OperatorAdminId,
                        principalTable: "Admins",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NominationLogs_Employees_OperatorEmployeeId",
                        column: x => x.OperatorEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NominationLogs_NominationDeclarations_DeclarationId",
                        column: x => x.DeclarationId,
                        principalTable: "NominationDeclarations",
                        principalColumn: "DeclarationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NominationLogs_SupAdmins_OperatorSupAdminId",
                        column: x => x.OperatorSupAdminId,
                        principalTable: "SupAdmins",
                        principalColumn: "SupAdminId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommentRecords",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommenterEmployeeId = table.Column<int>(type: "int", nullable: true),
                    CommenterAdminId = table.Column<int>(type: "int", nullable: true),
                    CommenterSupAdminId = table.Column<int>(type: "int", nullable: true),
                    CommentTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NominationId = table.Column<int>(type: "int", nullable: false),
                    AwardId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByAdminId = table.Column<int>(type: "int", nullable: true),
                    DeletedBySupAdminId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentRecords", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_CommentRecords_Admins_CommenterAdminId",
                        column: x => x.CommenterAdminId,
                        principalTable: "Admins",
                        principalColumn: "AdminId");
                    table.ForeignKey(
                        name: "FK_CommentRecords_Admins_DeletedByAdminId",
                        column: x => x.DeletedByAdminId,
                        principalTable: "Admins",
                        principalColumn: "AdminId");
                    table.ForeignKey(
                        name: "FK_CommentRecords_Awards_AwardId",
                        column: x => x.AwardId,
                        principalTable: "Awards",
                        principalColumn: "AwardId");
                    table.ForeignKey(
                        name: "FK_CommentRecords_Employees_CommenterEmployeeId",
                        column: x => x.CommenterEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_CommentRecords_Nominations_NominationId",
                        column: x => x.NominationId,
                        principalTable: "Nominations",
                        principalColumn: "NominationId");
                    table.ForeignKey(
                        name: "FK_CommentRecords_SupAdmins_CommenterSupAdminId",
                        column: x => x.CommenterSupAdminId,
                        principalTable: "SupAdmins",
                        principalColumn: "SupAdminId");
                    table.ForeignKey(
                        name: "FK_CommentRecords_SupAdmins_DeletedBySupAdminId",
                        column: x => x.DeletedBySupAdminId,
                        principalTable: "SupAdmins",
                        principalColumn: "SupAdminId");
                });

            migrationBuilder.CreateTable(
                name: "VoteRecords",
                columns: table => new
                {
                    VoteRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AwardId = table.Column<int>(type: "int", nullable: false),
                    NominationId = table.Column<int>(type: "int", nullable: false),
                    VoterEmployeeId = table.Column<int>(type: "int", nullable: true),
                    VoterAdminId = table.Column<int>(type: "int", nullable: true),
                    VoteTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteRecords", x => x.VoteRecordId);
                    table.ForeignKey(
                        name: "FK_VoteRecords_Admins_VoterAdminId",
                        column: x => x.VoterAdminId,
                        principalTable: "Admins",
                        principalColumn: "AdminId");
                    table.ForeignKey(
                        name: "FK_VoteRecords_Awards_AwardId",
                        column: x => x.AwardId,
                        principalTable: "Awards",
                        principalColumn: "AwardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoteRecords_Employees_VoterEmployeeId",
                        column: x => x.VoterEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                    table.ForeignKey(
                        name: "FK_VoteRecords_Nominations_NominationId",
                        column: x => x.NominationId,
                        principalTable: "Nominations",
                        principalColumn: "NominationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admins_AdminName",
                table: "Admins",
                column: "AdminName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Admins_DepartmentId",
                table: "Admins",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRecords_AwardId",
                table: "CommentRecords",
                column: "AwardId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRecords_CommenterAdminId",
                table: "CommentRecords",
                column: "CommenterAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRecords_CommenterEmployeeId",
                table: "CommentRecords",
                column: "CommenterEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRecords_CommenterSupAdminId",
                table: "CommentRecords",
                column: "CommenterSupAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRecords_DeletedByAdminId",
                table: "CommentRecords",
                column: "DeletedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRecords_DeletedBySupAdminId",
                table: "CommentRecords",
                column: "DeletedBySupAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentRecords_NominationId",
                table: "CommentRecords",
                column: "NominationId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentName",
                table: "Departments",
                column: "DepartmentName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_NominationDeclarations_AwardId",
                table: "NominationDeclarations",
                column: "AwardId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationDeclarations_DeclarerAdminId",
                table: "NominationDeclarations",
                column: "DeclarerAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationDeclarations_DeclarerEmployeeId",
                table: "NominationDeclarations",
                column: "DeclarerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationDeclarations_DeclarerSupAdminId",
                table: "NominationDeclarations",
                column: "DeclarerSupAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationDeclarations_DepartmentId",
                table: "NominationDeclarations",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationDeclarations_NominatedAdminId",
                table: "NominationDeclarations",
                column: "NominatedAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationDeclarations_NominatedEmployeeId",
                table: "NominationDeclarations",
                column: "NominatedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationDeclarations_ReviewerAdminId",
                table: "NominationDeclarations",
                column: "ReviewerAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationDeclarations_ReviewerEmployeeId",
                table: "NominationDeclarations",
                column: "ReviewerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationDeclarations_ReviewerSupAdminId",
                table: "NominationDeclarations",
                column: "ReviewerSupAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationLogs_DeclarationId",
                table: "NominationLogs",
                column: "DeclarationId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationLogs_OperatorAdminId",
                table: "NominationLogs",
                column: "OperatorAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationLogs_OperatorEmployeeId",
                table: "NominationLogs",
                column: "OperatorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_NominationLogs_OperatorSupAdminId",
                table: "NominationLogs",
                column: "OperatorSupAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Nominations_AwardId",
                table: "Nominations",
                column: "AwardId");

            migrationBuilder.CreateIndex(
                name: "IX_Nominations_DepartmentId",
                table: "Nominations",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Nominations_NominatedAdminId",
                table: "Nominations",
                column: "NominatedAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Nominations_NominatedEmployeeId",
                table: "Nominations",
                column: "NominatedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Nominations_ProposerAdminId",
                table: "Nominations",
                column: "ProposerAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Nominations_ProposerEmployeeId",
                table: "Nominations",
                column: "ProposerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Nominations_ProposerSupAdminId",
                table: "Nominations",
                column: "ProposerSupAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_SupAdmins_SupAdminName",
                table: "SupAdmins",
                column: "SupAdminName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VoteRecords_AwardId",
                table: "VoteRecords",
                column: "AwardId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteRecords_NominationId",
                table: "VoteRecords",
                column: "NominationId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteRecords_VoterAdminId",
                table: "VoteRecords",
                column: "VoterAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteRecords_VoterEmployeeId",
                table: "VoteRecords",
                column: "VoterEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentRecords");

            migrationBuilder.DropTable(
                name: "NominationLogs");

            migrationBuilder.DropTable(
                name: "VoteRecords");

            migrationBuilder.DropTable(
                name: "NominationDeclarations");

            migrationBuilder.DropTable(
                name: "Nominations");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Awards");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "SupAdmins");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
