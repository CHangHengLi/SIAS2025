using SIASGraduate.Models;

namespace SIASGraduate.Services
{
    public interface IEmployeeService
    {
        List<Employee> GetAllEmployees();
        Task<Employee> GetEmployeeByNameAsync(string name);
        Employee GetEmployeeByName(string name);
        Employee GetEmployeeById(int id);
        Task<Employee> GetEmployeeByAccountAsync(string account);
        Employee GetEmployeeByAccount(string account);
        void AddEmployee(Employee employee);
        void UpdateEmployee(Employee employee);
        void DeleteEmployee(int id);
        (bool hasRelated, int nominationCount, int declarationCount, int voteCount) CheckEmployeeRelatedRecords(int employeeId);
        bool DeleteEmployeeWithRelatedRecords(int employeeId);

        /// <summary>
        /// 使用直接SQL语句执行级联删除，适用于EF Core约束处理失败的情况
        /// </summary>
        /// <param name="employeeId">要删除的员工ID</param>
        /// <returns>删除是否成功</returns>
        bool ExecuteDirectSqlDelete(int employeeId);
        Task<bool> IsEmployeeNameExistAsync(string employeeName);
        bool IsEmployeeNameExist(string employeeName);
        Task<bool> IsEmployeeAccountExistAsync(string account);
        bool IsEmployeeAccountExist(string account);
        IEnumerable<Employee> GetAllActiveEmployees();
        IEnumerable<Employee> GetAllInactiveEmployees();
        //List<Employee> ImportEmployees(string filePath);
        bool ExportEmployees(List<Employee> employees, string filePath);
    }
}
