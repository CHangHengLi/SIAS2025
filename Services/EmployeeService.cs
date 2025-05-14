using System.Globalization;
using System.IO;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using SIASGraduate.Context;
using SIASGraduate.Models;

namespace SIASGraduate.Services
{
    public class EmployeeService : IEmployeeService
    {
        #region  构造函数
        public EmployeeService(DataBaseContext context)
        {
            // 构造函数保留但不再保存context
        }
        #endregion

        #region 添加员工
        public void AddEmployee(Employee employee)
        {
            using (var context = new DataBaseContext())
            {
                context.Employees.Add(employee);
                context.SaveChanges();
            }
        }
        #endregion

        #region 根据Id删除员工
        public void DeleteEmployee(int id)
        {
            using (var context = new DataBaseContext())
            {
                var employee = context.Employees.Find(id);
                if (employee != null)
                {
                    context.Employees.Remove(employee);
                    context.SaveChanges();
                }
            }
        }
        #endregion

        #region 检查员工是否有关联记录
        public (bool hasRelated, int nominationCount, int declarationCount, int voteCount) CheckEmployeeRelatedRecords(int employeeId)
        {
            using (var context = new DataBaseContext())
            {
                // 查询员工作为被提名者的提名记录
                int nominatedCount = context.Nominations.Count(n => n.NominatedEmployeeId == employeeId);

                // 查询员工作为提议人的提名记录
                int proposerCount = context.Nominations.Count(n => n.ProposerEmployeeId == employeeId);

                // 查询员工作为被提名者的申报记录
                int declaredCount = context.NominationDeclarations.Count(n => n.NominatedEmployeeId == employeeId);

                // 查询员工作为申报人的申报记录
                int declarerCount = context.NominationDeclarations.Count(n => n.DeclarerEmployeeId == employeeId);

                // 查询员工提交的投票记录
                int voteCount = context.VoteRecords.Count(v => v.VoterEmployeeId == employeeId);

                int nominationCount = nominatedCount + proposerCount;
                int declarationCount = declaredCount + declarerCount;

                bool hasRelated = nominationCount > 0 || declarationCount > 0 || voteCount > 0;

                return (hasRelated, nominationCount, declarationCount, voteCount);
            }
        }
        #endregion

        #region 级联删除员工及关联记录
        public bool DeleteEmployeeWithRelatedRecords(int employeeId)
        {
            try
            {
                // 不要使用Task.Run或异步方法，直接执行同步操作
                using (var context = new DataBaseContext())
                {
                    // 查找员工
                    var employee = context.Employees.Find(employeeId);
                    if (employee == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"找不到员工: ID={employeeId}");
                        return false;
                    }

                    // 使用事务确保数据完整性
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        try
                        {
                            // 记录开始删除过程
                            System.Diagnostics.Debug.WriteLine($"开始删除员工: ID={employeeId}, 名称={employee.EmployeeName}");

                            // 1. 首先删除所有直接引用该员工ID的投票记录 - 使用SQL语句
                            string deleteVotesSql = "DELETE FROM VoteRecords WHERE VoterEmployeeId = @p0";
                            var voteDeleteResult = context.Database.ExecuteSqlRaw(deleteVotesSql, employeeId);
                            System.Diagnostics.Debug.WriteLine($"删除员工投票记录: {voteDeleteResult}条");

                            // 2. 获取相关提名ID
                            var nominationIds = context.Nominations
                                .Where(n => n.NominatedEmployeeId == employeeId || n.ProposerEmployeeId == employeeId)
                                .Select(n => n.NominationId)
                                .ToList();

                            // 删除提名关联的评论和投票
                            foreach (var nominationId in nominationIds)
                            {
                                // 删除评论
                                string deleteCommentsSql = "DELETE FROM CommentRecords WHERE NominationId = @p0";
                                var commentDeleteResult = context.Database.ExecuteSqlRaw(deleteCommentsSql, nominationId);
                                System.Diagnostics.Debug.WriteLine($"删除提名ID={nominationId}的评论: {commentDeleteResult}条");

                                // 删除投票
                                string deleteNominationVotesSql = "DELETE FROM VoteRecords WHERE NominationId = @p0";
                                var voteRecDeleteResult = context.Database.ExecuteSqlRaw(deleteNominationVotesSql, nominationId);
                                System.Diagnostics.Debug.WriteLine($"删除提名ID={nominationId}的投票: {voteRecDeleteResult}条");
                            }

                            // 3. 删除提名记录
                            string deleteNominationsSql1 = "DELETE FROM Nominations WHERE NominatedEmployeeId = @p0";
                            var nom1Result = context.Database.ExecuteSqlRaw(deleteNominationsSql1, employeeId);
                            System.Diagnostics.Debug.WriteLine($"删除员工被提名记录: {nom1Result}条");

                            string deleteNominationsSql2 = "DELETE FROM Nominations WHERE ProposerEmployeeId = @p0";
                            var nom2Result = context.Database.ExecuteSqlRaw(deleteNominationsSql2, employeeId);
                            System.Diagnostics.Debug.WriteLine($"删除员工提议记录: {nom2Result}条");

                            // 4. 删除申报记录
                            string deleteDeclarationsSql = "DELETE FROM NominationDeclarations WHERE NominatedEmployeeId = @p0 OR DeclarerEmployeeId = @p0";
                            var declResult = context.Database.ExecuteSqlRaw(deleteDeclarationsSql, employeeId);
                            System.Diagnostics.Debug.WriteLine($"删除员工申报记录: {declResult}条");

                            // 5. 解除部门关联
                            employee.DepartmentId = null;
                            context.SaveChanges();

                            // 6. 最后删除员工本身
                            context.Employees.Remove(employee);
                            context.SaveChanges();

                            // 提交事务
                            transaction.Commit();
                            System.Diagnostics.Debug.WriteLine($"员工ID={employeeId}级联删除成功");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // 回滚事务
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"级联删除失败，回滚事务: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"异常详情: {ex.StackTrace}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除员工失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常详情: {ex.StackTrace}");
                return false;
            }
        }
        #endregion

        #region 获取所有在职员工
        public IEnumerable<Employee> GetAllActiveEmployees()
        {
            using (var context = new DataBaseContext())
            {
                return context.Employees.Where(e => e.IsActive == true).ToList();
            }
        }
        #endregion

        #region 获取所有员工
        public List<Employee> GetAllEmployees()
        {
            using (var context = new DataBaseContext())
            {
                return context.Employees.ToList();
            }
        }
        #endregion

        #region 获取所有离职员工
        public IEnumerable<Employee> GetAllInactiveEmployees()
        {
            using (var context = new DataBaseContext())
            {
                return context.Employees.Where(e => e.IsActive == false).ToList();
            }
        }
        #endregion

        #region 根据id获取员工
        public Employee GetEmployeeById(int id)
        {
            using (var context = new DataBaseContext())
            {
                return context.Employees.Find(id);
            }
        }
        #endregion

        #region 根据姓名获取员工
        public Employee GetEmployeeByName(string name)
        {
            using (var context = new DataBaseContext())
            {
                return context.Employees.FirstOrDefault(sa => sa.EmployeeName == name);
            }
        }

        public bool IsEmployeeNameExist(string employeeName)
        {
            using (var context = new DataBaseContext())
            {
                return context.Employees.Any(e => e.EmployeeName == employeeName);
            }
        }
        #endregion

        #region 根据账号获取员工
        public Employee GetEmployeeByAccount(string account)
        {
            using (var context = new DataBaseContext())
            {
                return context.Employees.FirstOrDefault(e => e.Account == account);
            }
        }

        public bool IsEmployeeAccountExist(string account)
        {
            using (var context = new DataBaseContext())
            {
                return context.Employees.Any(e => e.Account == account);
            }
        }
        #endregion

        #region 更新员工信息
        public void UpdateEmployee(Employee employee)
        {
            using (var context = new DataBaseContext())
            {
                try
                {
                    // 查询任何可能包含Nomination且与此员工相关的数据
                    var relatedNominations = context.Nominations
                        .Where(n => n.NominatedEmployeeId == employee.EmployeeId ||
                                   n.ProposerEmployeeId == employee.EmployeeId)
                        .ToList();

                    // 将这些相关的Nomination实体分离出来，避免EF Core追踪它们
                    foreach (var nomination in relatedNominations)
                    {
                        context.Entry(nomination).State = EntityState.Detached;
                    }

                    // 手动更新员工信息
                    var existingEmployee = context.Employees.Find(employee.EmployeeId);
                    if (existingEmployee != null)
                    {
                        // 更新每个属性
                        existingEmployee.EmployeeName = employee.EmployeeName;
                        existingEmployee.EmployeePassword = employee.EmployeePassword;
                        existingEmployee.Email = employee.Email;
                        existingEmployee.DepartmentId = employee.DepartmentId;
                        existingEmployee.IsActive = employee.IsActive;
                        existingEmployee.HireDate = employee.HireDate;
                        existingEmployee.RoleId = employee.RoleId;
                        existingEmployee.EmployeeImage = employee.EmployeeImage;
                        existingEmployee.EmployeeFileData = employee.EmployeeFileData;
                        existingEmployee.Account = employee.Account;

                        // 保存更改
                        context.SaveChanges();

                        // 如果涉及部门变更，则使用SQL语句更新相关提名记录的部门
                        if (relatedNominations.Any())
                        {
                            string updateNominationDepartmentSql =
                                "UPDATE Nominations SET DepartmentId = @p0 WHERE NominatedEmployeeId = @p1";
                            context.Database.ExecuteSqlRaw(updateNominationDepartmentSql,
                                employee.DepartmentId, employee.EmployeeId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 特殊处理IsUserVoted错误
                    if (ex.Message.Contains("IsUserVoted"))
                    {
                        // 使用SQL语句直接更新员工记录，避开EF Core追踪问题
                        string updateSql = @"
                            UPDATE Employees 
                            SET EmployeeName = @p1, 
                                EmployeePassword = @p2, 
                                Email = @p3, 
                                DepartmentId = @p4, 
                                IsActive = @p5, 
                                HireDate = @p6, 
                                RoleId = @p7, 
                                Account = @p8
                            WHERE EmployeeId = @p0";

                        context.Database.ExecuteSqlRaw(updateSql,
                            employee.EmployeeId,
                            employee.EmployeeName,
                            employee.EmployeePassword,
                            employee.Email,
                            employee.DepartmentId,
                            employee.IsActive,
                            employee.HireDate,
                            employee.RoleId,
                            employee.Account);

                        // 如果需要更新二进制数据(图片等)，单独处理
                        if (employee.EmployeeImage != null || employee.EmployeeFileData != null)
                        {
                            var existing = context.Employees.Find(employee.EmployeeId);
                            if (existing != null)
                            {
                                if (employee.EmployeeImage != null)
                                    existing.EmployeeImage = employee.EmployeeImage;
                                if (employee.EmployeeFileData != null)
                                    existing.EmployeeFileData = employee.EmployeeFileData;

                                context.SaveChanges();
                            }
                        }

                        // 如果部门变更，更新相关提名记录的部门
                        string updateNominationDepartmentSql =
                            "UPDATE Nominations SET DepartmentId = @p0 WHERE NominatedEmployeeId = @p1";
                        context.Database.ExecuteSqlRaw(updateNominationDepartmentSql,
                            employee.DepartmentId, employee.EmployeeId);
                    }
                    else
                    {
                        // 重新抛出其他异常
                        throw;
                    }
                }
            }
        }
        #endregion

        #region 导入,导出
        public bool ExportEmployees(List<Employee> employees, string filePath)
        {
            try
            {
                // 使用UTF-8编码（带BOM）确保Excel可以正确识别中文
                using var writer = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true));
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                // 创建不包含图片字段的数据列表
                var exportData = employees.Select(e => new
                {
                    员工ID = e.EmployeeId,
                    账号 = e.Account,
                    姓名 = e.EmployeeName,
                    密码 = e.EmployeePassword,
                    邮箱 = e.Email,
                    部门ID = e.DepartmentId,
                    部门名称 = e.Department?.DepartmentName,
                    入职日期 = e.HireDate?.ToString("yyyy-MM-dd"),
                    在职状态 = e.IsActive == true ? "在职" : "离职",
                    角色ID = e.RoleId
                }).ToList();

                // 写入数据
                csv.WriteRecords(exportData);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion

        /// <summary>
        /// 使用直接SQL语句执行级联删除，适用于EF Core约束处理失败的情况
        /// </summary>
        public bool ExecuteDirectSqlDelete(int employeeId)
        {
            try
            {
                using (var context = new DataBaseContext())
                {
                    // 检查员工是否存在
                    var employee = context.Employees.Find(employeeId);
                    if (employee == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"找不到员工: ID={employeeId}");
                        return false;
                    }

                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"开始删除员工ID={employeeId}的级联记录");

                        // 分两步处理投票记录，先获取关联的提名记录
                        var nominationIds = context.Nominations
                            .Where(n => n.NominatedEmployeeId == employeeId || n.ProposerEmployeeId == employeeId)
                            .Select(n => n.NominationId)
                            .ToList();

                        System.Diagnostics.Debug.WriteLine($"找到关联提名ID: {string.Join(",", nominationIds)}");

                        // 1. 首先清空所有投票记录 - 这是最关键的一步，因为投票记录会和员工以及提名记录双重关联
                        string deleteVotesSql = "DELETE FROM VoteRecords WHERE VoterEmployeeId = @p0";
                        var result = context.Database.ExecuteSqlRaw(deleteVotesSql, employeeId);
                        System.Diagnostics.Debug.WriteLine($"删除员工投票记录: {result}行受影响");

                        // 删除提名关联的投票记录
                        if (nominationIds.Any())
                        {
                            foreach (var nominationId in nominationIds)
                            {
                                string deleteNominationVotesSql = "DELETE FROM VoteRecords WHERE NominationId = @p0";
                                var voteResult = context.Database.ExecuteSqlRaw(deleteNominationVotesSql, nominationId);
                                System.Diagnostics.Debug.WriteLine($"删除提名ID={nominationId}的投票记录: {voteResult}行受影响");
                            }
                        }

                        // 2. 删除评论记录
                        if (nominationIds.Any())
                        {
                            foreach (var nominationId in nominationIds)
                            {
                                string deleteCommentsSql = "DELETE FROM CommentRecords WHERE NominationId = @p0";
                                var commentResult = context.Database.ExecuteSqlRaw(deleteCommentsSql, nominationId);
                                System.Diagnostics.Debug.WriteLine($"删除提名ID={nominationId}的评论记录: {commentResult}行受影响");
                            }
                        }

                        // 3. 删除提名记录
                        string deleteNominationsSql1 = "DELETE FROM Nominations WHERE NominatedEmployeeId = @p0";
                        var nom1Result = context.Database.ExecuteSqlRaw(deleteNominationsSql1, employeeId);
                        System.Diagnostics.Debug.WriteLine($"删除员工被提名记录: {nom1Result}行受影响");

                        string deleteNominationsSql2 = "DELETE FROM Nominations WHERE ProposerEmployeeId = @p0";
                        var nom2Result = context.Database.ExecuteSqlRaw(deleteNominationsSql2, employeeId);
                        System.Diagnostics.Debug.WriteLine($"删除员工提议记录: {nom2Result}行受影响");

                        // 4. 删除申报记录
                        string deleteDeclarationsSql1 = "DELETE FROM NominationDeclarations WHERE NominatedEmployeeId = @p0";
                        var decl1Result = context.Database.ExecuteSqlRaw(deleteDeclarationsSql1, employeeId);
                        System.Diagnostics.Debug.WriteLine($"删除员工被申报记录: {decl1Result}行受影响");

                        string deleteDeclarationsSql2 = "DELETE FROM NominationDeclarations WHERE DeclarerEmployeeId = @p0";
                        var decl2Result = context.Database.ExecuteSqlRaw(deleteDeclarationsSql2, employeeId);
                        System.Diagnostics.Debug.WriteLine($"删除员工申报人记录: {decl2Result}行受影响");

                        // 5. 解除部门关联
                        if (employee.DepartmentId != null)
                        {
                            string updateDepSql = "UPDATE Employees SET DepartmentId = NULL WHERE EmployeeId = @p0";
                            var depResult = context.Database.ExecuteSqlRaw(updateDepSql, employeeId);
                            System.Diagnostics.Debug.WriteLine($"解除部门关联: {depResult}行受影响");
                        }

                        // 6. 最后删除员工本身
                        string deleteEmployeeSql = "DELETE FROM Employees WHERE EmployeeId = @p0";
                        var empResult = context.Database.ExecuteSqlRaw(deleteEmployeeSql, employeeId);
                        System.Diagnostics.Debug.WriteLine($"删除员工: {empResult}行受影响");

                        System.Diagnostics.Debug.WriteLine($"员工ID={employeeId}级联删除成功");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"级联删除失败: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"异常详情: {ex.StackTrace}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"直接SQL删除最终失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常详情: {ex.StackTrace}");
                return false;
            }
        }
    }
}
