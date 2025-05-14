using System.Globalization;
using System.IO;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using SIASGraduate.Context;
using SIASGraduate.Models;

namespace SIASGraduate.Services
{
    public class AdminService : IAdminService
    {
        private readonly DataBaseContext context;
        public AdminService(DataBaseContext context)
        {
            this.context = context;

        }
        #region 获取所有管理员信息
        public Task<List<Admin>> GetAllAdminsAsync()
        {
            // 使用Task.Run来启动一个新的任务
            return Task.Run(() =>
            {
                using (var context = new DataBaseContext())
                {
                    // 使用同步方法获取数据
                    return context.Admins.ToList();
                }
            });
        }
        #endregion

        #region 根据管理员名称获取管理员信息
        public Task<Admin> GetAdminByNameAsync(string name)
        {
            return Task.Run(() =>
            {
                using (var context = new DataBaseContext())
                {
                    return context.Admins.FirstOrDefault(a => a.AdminName == name);
                }
            });
        }
        #endregion

        #region 管理员名称是否存在
        public bool IsAdminNameExist(string employeeName)
        {
            using (var context = new DataBaseContext())
            {
                return context.Admins.Any(sa => sa.AdminName == employeeName);
            }
        }
        #endregion

        #region 根据账号获取管理员信息
        public Task<Admin> GetAdminByAccountAsync(string account)
        {
            return Task.Run(() =>
            {
                using (var context = new DataBaseContext())
                {
                    return context.Admins.FirstOrDefault(a => a.Account == account);
                }
            });
        }
        #endregion

        #region 管理员账号是否存在
        public bool IsAdminAccountExist(string account)
        {
            using (var context = new DataBaseContext())
            {
                return context.Admins.Any(a => a.Account == account);
            }
        }
        #endregion

        #region 添加管理员
        public void AddAdmin(Admin newAdmin)
        {
            using (var context = new DataBaseContext())
            {
                context.Admins.Add(newAdmin);
                context.SaveChanges();
            }
        }
        #endregion

        #region 级联删除管理员及关联记录
        public async Task<bool> DeleteAdminWithRelatedRecords(int adminId)
        {
            try
            {
                using (var context = new DataBaseContext())
                {
                    // 查找管理员
                    var admin = await context.Admins.FindAsync(adminId);
                    if (admin == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"找不到管理员: ID={adminId}");
                        return false;
                    }

                    // 使用事务确保数据完整性
                    using (var transaction = await context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            // 记录开始删除过程
                            System.Diagnostics.Debug.WriteLine($"开始删除管理员及关联记录: ID={adminId}, 姓名={admin.AdminName}");

                            // 1. 删除管理员作为被提名者的提名记录关联的投票记录
                            var nominatedVoteRecords = await context.VoteRecords
                                .Include(v => v.Nomination)
                                .Where(v => v.Nomination.NominatedAdminId == adminId)
                                .ToListAsync();

                            if (nominatedVoteRecords.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"删除管理员作为被提名者关联的投票记录: {nominatedVoteRecords.Count}条");
                                context.VoteRecords.RemoveRange(nominatedVoteRecords);
                                await context.SaveChangesAsync();
                            }

                            // 2. 删除管理员作为被提名者的提名记录关联的评论记录
                            var nominatedCommentRecords = await context.CommentRecords
                                .Include(c => c.Nomination)
                                .Where(c => c.Nomination.NominatedAdminId == adminId)
                                .ToListAsync();

                            if (nominatedCommentRecords.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"删除管理员作为被提名者关联的评论记录: {nominatedCommentRecords.Count}条");
                                context.CommentRecords.RemoveRange(nominatedCommentRecords);
                                await context.SaveChangesAsync();
                            }

                            // 3. 删除管理员作为被提名者的提名记录
                            var nominatedRecords = await context.Nominations
                                .Where(n => n.NominatedAdminId == adminId)
                                .ToListAsync();

                            if (nominatedRecords.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"删除管理员作为被提名者的提名记录: {nominatedRecords.Count}条");
                                context.Nominations.RemoveRange(nominatedRecords);
                                await context.SaveChangesAsync();
                            }

                            // 4. 删除管理员作为提议人的提名记录关联的投票记录
                            var proposerVoteRecords = await context.VoteRecords
                                .Include(v => v.Nomination)
                                .Where(v => v.Nomination.ProposerAdminId == adminId)
                                .ToListAsync();

                            if (proposerVoteRecords.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"删除管理员作为提议人关联的投票记录: {proposerVoteRecords.Count}条");
                                context.VoteRecords.RemoveRange(proposerVoteRecords);
                                await context.SaveChangesAsync();
                            }

                            // 5. 删除管理员作为提议人的提名记录关联的评论记录
                            var proposerCommentRecords = await context.CommentRecords
                                .Include(c => c.Nomination)
                                .Where(c => c.Nomination.ProposerAdminId == adminId)
                                .ToListAsync();

                            if (proposerCommentRecords.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"删除管理员作为提议人关联的评论记录: {proposerCommentRecords.Count}条");
                                context.CommentRecords.RemoveRange(proposerCommentRecords);
                                await context.SaveChangesAsync();
                            }

                            // 6. 删除管理员作为投票者的投票记录
                            var voterRecords = await context.VoteRecords
                                .Where(v => v.VoterAdminId == adminId)
                                .ToListAsync();

                            if (voterRecords.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"删除管理员作为投票者的投票记录: {voterRecords.Count}条");
                                context.VoteRecords.RemoveRange(voterRecords);
                                await context.SaveChangesAsync();
                            }

                            // 7. 删除管理员作为被提名者的申报记录
                            var nominatedDeclarations = await context.NominationDeclarations
                                .Where(n => n.NominatedAdminId == adminId)
                                .ToListAsync();

                            if (nominatedDeclarations.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"删除管理员作为被提名者的申报记录: {nominatedDeclarations.Count}条");
                                context.NominationDeclarations.RemoveRange(nominatedDeclarations);
                                await context.SaveChangesAsync();
                            }

                            // 8. 删除管理员作为申报人的申报记录
                            var declarerDeclarations = await context.NominationDeclarations
                                .Where(n => n.DeclarerAdminId == adminId)
                                .ToListAsync();

                            if (declarerDeclarations.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"删除管理员作为申报人的申报记录: {declarerDeclarations.Count}条");
                                context.NominationDeclarations.RemoveRange(declarerDeclarations);
                                await context.SaveChangesAsync();
                            }

                            // 9. 最后删除管理员本身
                            context.Admins.Remove(admin);
                            await context.SaveChangesAsync();

                            // 提交事务
                            await transaction.CommitAsync();
                            System.Diagnostics.Debug.WriteLine($"管理员及关联记录删除成功: ID={adminId}");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // 回滚事务
                            await transaction.RollbackAsync();
                            System.Diagnostics.Debug.WriteLine($"删除管理员及关联记录时出错，已回滚: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteAdminWithRelatedRecords方法异常: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 使用直接SQL语句执行级联删除
        public async Task<bool> ExecuteDirectSqlDelete(int adminId)
        {
            try
            {
                // 检查管理员是否存在（通过原始SQL查询）
                bool adminExists = false;
                string adminName = string.Empty;

                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(GetConnectionString()))
                {
                    await connection.OpenAsync();

                    // 检查管理员是否存在
                    using (var checkCommand = new Microsoft.Data.SqlClient.SqlCommand(
                        "SELECT AdminId, AdminName FROM Admins WHERE AdminId = @AdminId", connection))
                    {
                        checkCommand.Parameters.AddWithValue("@AdminId", adminId);
                        using (var reader = await checkCommand.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                adminExists = true;
                                adminName = reader["AdminName"].ToString();
                            }
                        }
                    }

                    if (!adminExists)
                    {
                        System.Diagnostics.Debug.WriteLine($"找不到管理员: ID={adminId}");
                        return false;
                    }

                    System.Diagnostics.Debug.WriteLine($"开始删除管理员ID={adminId}的级联记录");

                    // 使用ADO.NET事务直接执行SQL
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. 获取关联的提名记录ID
                            var nominationIds = new List<int>();
                            using (var idCommand = new Microsoft.Data.SqlClient.SqlCommand(
                                "SELECT NominationId FROM Nominations WHERE NominatedAdminId = @AdminId OR ProposerAdminId = @AdminId",
                                connection, transaction))
                            {
                                idCommand.Parameters.AddWithValue("@AdminId", adminId);
                                using (var reader = await idCommand.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        nominationIds.Add(reader.GetInt32(0));
                                    }
                                }
                            }

                            // 2. 删除管理员作为投票者的投票记录（处理FK_VoteRecords_Admins_VoterAdminId外键约束）
                            using (var voterCommand = new Microsoft.Data.SqlClient.SqlCommand(
                                "DELETE FROM VoteRecords WHERE VoterAdminId = @AdminId",
                                connection, transaction))
                            {
                                voterCommand.Parameters.AddWithValue("@AdminId", adminId);
                                int deletedVotes = await voterCommand.ExecuteNonQueryAsync();
                                if (deletedVotes > 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"删除管理员作为投票者的投票记录: {deletedVotes}条");
                                }
                            }

                            // 3. 如果有关联的提名记录，删除相关的投票和评论
                            if (nominationIds.Any())
                            {
                                // 构建IN参数
                                string nominationsInClause = string.Join(",", nominationIds);

                                // 删除这些提名关联的投票记录
                                using (var voteCommand = new Microsoft.Data.SqlClient.SqlCommand(
                                    $"DELETE FROM VoteRecords WHERE NominationId IN ({nominationsInClause})",
                                    connection, transaction))
                                {
                                    await voteCommand.ExecuteNonQueryAsync();
                                }

                                // 删除这些提名关联的评论记录
                                using (var commentCommand = new Microsoft.Data.SqlClient.SqlCommand(
                                    $"DELETE FROM CommentRecords WHERE NominationId IN ({nominationsInClause})",
                                    connection, transaction))
                                {
                                    await commentCommand.ExecuteNonQueryAsync();
                                }
                            }

                            // 4. 删除管理员关联的提名记录
                            using (var nominationCommand = new Microsoft.Data.SqlClient.SqlCommand(
                                "DELETE FROM Nominations WHERE NominatedAdminId = @AdminId OR ProposerAdminId = @AdminId",
                                connection, transaction))
                            {
                                nominationCommand.Parameters.AddWithValue("@AdminId", adminId);
                                await nominationCommand.ExecuteNonQueryAsync();
                            }

                            // 5. 删除管理员关联的申报记录
                            using (var declarationCommand = new Microsoft.Data.SqlClient.SqlCommand(
                                "DELETE FROM NominationDeclarations WHERE NominatedAdminId = @AdminId OR DeclarerAdminId = @AdminId",
                                connection, transaction))
                            {
                                declarationCommand.Parameters.AddWithValue("@AdminId", adminId);
                                await declarationCommand.ExecuteNonQueryAsync();
                            }

                            // 6. 最后删除管理员本身
                            using (var adminCommand = new Microsoft.Data.SqlClient.SqlCommand(
                                "DELETE FROM Admins WHERE AdminId = @AdminId",
                                connection, transaction))
                            {
                                adminCommand.Parameters.AddWithValue("@AdminId", adminId);
                                await adminCommand.ExecuteNonQueryAsync();
                            }

                            // 提交事务
                            transaction.Commit();
                            System.Diagnostics.Debug.WriteLine($"通过SQL语句成功删除管理员ID={adminId}，姓名={adminName}及关联记录");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // 回滚事务
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"执行SQL删除失败，已回滚: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExecuteDirectSqlDelete方法异常: {ex.Message}");
                return false;
            }
        }

        // 获取连接字符串的辅助方法
        private string GetConnectionString()
        {
            // 通过EF Core获取当前使用的连接字符串
            using (var context = new DataBaseContext())
            {
                return context.Database.GetConnectionString();
            }
        }
        #endregion

        #region 导出管理员信息

        public bool ExportAdmins(List<Admin> admins, string filePath)
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 使用UTF-8编码（带BOM）确保Excel可以正确识别中文
                using var writer = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true));
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                // 创建不包含图片字段的数据列表
                var exportData = admins.Select(a => new
                {
                    管理员ID = a.AdminId,
                    账号 = a.Account,
                    用户名 = a.AdminName,
                    密码 = a.AdminPassword,
                    邮箱 = a.Email,
                    部门ID = a.DepartmentId,
                    部门名称 = a.Department?.DepartmentName,
                    入职日期 = a.HireDate?.ToString("yyyy-MM-dd"),
                    在职状态 = a.IsActive == true ? "在职" : "离职",
                    角色ID = a.RoleId,
                    角色名称 = GetRoleName(a.RoleId)
                }).ToList();

                // 写入数据
                csv.WriteRecords(exportData);

                return true; // 导出成功
            }
            catch (Exception ex)
            {
                // 记录错误
                System.Diagnostics.Debug.WriteLine($"导出管理员数据失败: {ex.Message}");
                return false; // 导出失败
            }
        }

        // 获取角色名称的辅助方法
        private string GetRoleName(int? roleId)
        {
            return roleId switch
            {
                1 => "超级管理员",
                2 => "管理员",
                3 => "员工",
                _ => "未知角色"
            };
        }
        #endregion
    }
}
