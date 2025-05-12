using _2025毕业设计.Context;
using _2025毕业设计.Models;
using CsvHelper;
using System.Globalization;
using System.IO;
using System.Linq;

namespace _2025毕业设计.Services
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
