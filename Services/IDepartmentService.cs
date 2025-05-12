using SIASGraduate.Models;

namespace SIASGraduate.Services
{
    public interface IDepartmentService
    {

        List<Department> GetAllDepartments();
        bool ExportDepartments(List<Department> departments, string filePath);
        bool DepartmentNameExists(string departmentName);
        void AddDepartment(Department addDepartment);
    }
}
