using CsvHelper.Configuration;

namespace SIASGraduate.Models
{
    /// <summary>
    /// 部门导出数据的映射类
    /// </summary>
    public class DepartmentExportMap : ClassMap<DepartmentExport>
    {
        public DepartmentExportMap()
        {
            Map(m => m.DepartmentId).Name("部门编号");
            Map(m => m.DepartmentName).Name("部门名称");
        }
    }
}
