using SIASGraduate.Context;
using SIASGraduate.Models;

namespace SIASGraduate.Services
{
    public class SupAdminService : ISupAdminService
    {
        public SupAdminService(DataBaseContext context)
        {
            // 构造函数保留但不再保存context
        }
        
        public List<SupAdmin> GetAllSupAdmins()
        {
            using var context = new DataBaseContext();
            return [.. context.SupAdmins];
        }
        
        public SupAdmin GetSupAdminByName(string name)
        {
            using (var context = new DataBaseContext())
            {
                return context.SupAdmins.FirstOrDefault(sa => sa.SupAdminName == name);
            }
        }

        public SupAdmin GetSupAdminByAccount(string account)
        {
            using (var context = new DataBaseContext())
            {
                return context.SupAdmins.FirstOrDefault(sa => sa.Account == account);
            }
        }

        public bool IsSupAdminNameExist(string employeeName)
        {
            using (var context = new DataBaseContext())
            {
                return context.SupAdmins.Any(sa => sa.SupAdminName == employeeName);
            }
        }

        public bool IsSupAdminAccountExist(string account)
        {
            using (var context = new DataBaseContext())
            {
                return context.SupAdmins.Any(sa => sa.Account == account);
            }
        }
    }
}
