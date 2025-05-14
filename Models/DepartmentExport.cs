using CsvHelper.Configuration.Attributes;

namespace SIASGraduate.Models
{
    /// <summary>
    /// 用于导出的部门数据模型
    /// </summary>
    public class DepartmentExport
    {
        [Name("部门编号")]
        public int DepartmentId { get; set; }

        [Name("部门名称")]
        public string DepartmentName { get; set; }
    }
}
