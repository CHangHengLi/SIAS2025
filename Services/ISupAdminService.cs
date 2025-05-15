using SIASGraduate.Models;

namespace SIASGraduate.Services
{
    public interface ISupAdminService
    {
        List<SupAdmin> GetAllSupAdmins();
        Task<SupAdmin> GetSupAdminByNameAsync(string name);
        SupAdmin GetSupAdminByName(string name);
        Task<SupAdmin> GetSupAdminByAccountAsync(string account);
        SupAdmin GetSupAdminByAccount(string account);
        Task<bool> IsSupAdminNameExistAsync(string employeeName);
        bool IsSupAdminNameExist(string employeeName);
        Task<bool> IsSupAdminAccountExistAsync(string account);
        bool IsSupAdminAccountExist(string account);
    }
}
