using SIASGraduate.Context;
using SIASGraduate.Models;
using CsvHelper;
using System.Globalization;
using System.IO;
using System.Linq;

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
                context.Employees.Update(employee);
                context.SaveChanges();
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
    }
}
