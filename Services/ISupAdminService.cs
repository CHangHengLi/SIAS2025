using SIASGraduate.Models;

namespace SIASGraduate.Services
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
