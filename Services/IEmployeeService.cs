using _2025毕业设计.Models;

namespace _2025毕业设计.Services
{
    public interface IEmployeeService
    {
        List<Employee> GetAllEmployees();
        Employee GetEmployeeByName(string name);
        Employee GetEmployeeById(int id);
        Employee GetEmployeeByAccount(string account);
        void AddEmployee(Employee employee);
        void UpdateEmployee(Employee employee);
        void DeleteEmployee(int id);
        bool IsEmployeeNameExist(string employeeName);
        bool IsEmployeeAccountExist(string account);
        IEnumerable<Employee> GetAllActiveEmployees();
        IEnumerable<Employee> GetAllInactiveEmployees();
        //List<Employee> ImportEmployees(string filePath);
        bool ExportEmployees(List<Employee> employees, string filePath);
    }
}
