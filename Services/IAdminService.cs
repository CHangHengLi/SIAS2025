using _2025毕业设计.Models;

namespace _2025毕业设计.Services
{
    public interface IAdminService
    {
        Task<List<Admin>> GetAllAdminsAsync();
        Task<Admin> GetAdminByNameAsync(string name);
        Task<Admin> GetAdminByAccountAsync(string account);
        bool IsAdminNameExist(string employeeName);
        bool IsAdminAccountExist(string account);
        void AddAdmin(Admin newAdmin);
        bool ExportAdmins(List<Admin> admins, string filePath);
    }
}
