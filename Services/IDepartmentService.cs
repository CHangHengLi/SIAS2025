using _2025毕业设计.Models;

namespace _2025毕业设计.Services
{
    public interface IDepartmentService
    {

        List<Department> GetAllDepartments();
        bool ExportDepartments(List<Department> departments, string filePath);
        bool DepartmentNameExists(string departmentName);
        void AddDepartment(Department addDepartment);
    }
}
