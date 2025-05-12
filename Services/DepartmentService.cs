using _2025毕业设计.Context;
using _2025毕业设计.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;

namespace _2025毕业设计.Services
{
    public class DepartmentService : IDepartmentService
    {

        #region 构造函数
        private readonly DataBaseContext context;

        public DepartmentService(DataBaseContext context)
        {
            this.context = context;
        }
        #endregion

        #region 得到所有的部门
        public List<Department> GetAllDepartments()
        {
            return context.Departments.ToList();
        }
        #endregion

        #region 导出数据
        public bool ExportDepartments(List<Department> departments, string filePath)
        {
            try
            {
                // 检查参数
                if (departments == null || departments.Count == 0)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    return false;
                }

                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 准备数据 - 转换为匿名对象，避免序列化实体间的循环引用
                var exportData = departments.Select(d => new
                {
                    部门编号 = d.DepartmentId,
                    部门名称 = d.DepartmentName ?? string.Empty
                }).ToList();

                // 使用UTF-8编码（带BOM）确保Excel可以正确识别中文
                using (var writer = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true)))
                using (var csv = new CsvWriter(writer, CultureInfo.CurrentCulture))
                {
                    // 写入数据
                    csv.WriteRecords(exportData);
                }

                return true;
            }
            catch (Exception ex)
            {
                // 记录异常信息
                System.Diagnostics.Debug.WriteLine($"导出部门数据时发生错误: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region  是否存在部门同名
        public bool DepartmentNameExists(string departmentName)
        {
            return context.Departments.Any(e => e.DepartmentName == departmentName);
        }
        #endregion

        #region 新增一个部门
        public void AddDepartment(Department addDepartment)
        {
            try
            {
                // 检查是否有同名部门
                if (context.Departments.Any(d => d.DepartmentName == addDepartment.DepartmentName))
                {
                    throw new Exception("部门名称已存在");
                }
                context.Departments.Add(addDepartment);
                context.SaveChanges();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                // 记录并抛出友好异常
                System.Diagnostics.Debug.WriteLine($"并发冲突: {ex.Message}");
                throw new Exception("添加部门时发生并发冲突，请刷新后重试。", ex);
            }
            catch (Exception ex)
            {
                // 记录并抛出其他异常
                System.Diagnostics.Debug.WriteLine($"添加部门异常: {ex.Message}");
                throw;
            }
        }
        #endregion

        #region 删除部门
        public bool DeleteDepartment(int departmentId)
        {
            try
            {
                using (var context = new DataBaseContext())
                {
                    // 1. 查找要删除的部门
                    var department = context.Departments.Find(departmentId);
                    if (department == null)
                        return false;

                    // 2. 查找并更新所有引用该部门的员工记录
                    var employees = context.Employees.Where(e => e.DepartmentId == departmentId).ToList();
                    foreach (var employee in employees)
                    {
                        employee.DepartmentId = null;
                        employee.Department = null;
                    }

                    // 3. 查找并更新所有引用该部门的管理员记录
                    var admins = context.Admins.Where(a => a.DepartmentId == departmentId).ToList();
                    foreach (var admin in admins)
                    {
                        admin.DepartmentId = null;
                        admin.Department = null;
                    }

                    // 4. 查找并更新所有引用该部门的奖项提名记录
                    var nominations = context.Nominations.Where(n => n.DepartmentId == departmentId).ToList();
                    foreach (var nomination in nominations)
                    {
                        nomination.DepartmentId = null;
                        nomination.Department = null;
                    }

                    // 5. 保存对所有相关实体的修改
                    context.SaveChanges();

                    // 6. 删除部门
                    context.Departments.Remove(department);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        #endregion
    }
}
