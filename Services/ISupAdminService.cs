using _2025毕业设计.Models;

namespace _2025毕业设计.Services
{
    public interface ISupAdminService
    {
        List<SupAdmin> GetAllSupAdmins();
        SupAdmin GetSupAdminByName(string name);
        SupAdmin GetSupAdminByAccount(string account);
        bool IsSupAdminNameExist(string employeeName);
        bool IsSupAdminAccountExist(string account);
    }
}
