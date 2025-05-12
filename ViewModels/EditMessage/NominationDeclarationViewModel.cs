using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using _2025毕业设计.Context;
using _2025毕业设计.Models;
using _2025毕业设计.Views.EditMessage.NominationLogViewer;
using _2025毕业设计.ViewModels.EditMessage.NominationLogViewer;
using Prism.Mvvm;

namespace _2025毕业设计.ViewModels.EditMessage
{
    /// <summary>
    /// 提名申报ViewModel
    /// </summary>
    public class NominationDeclarationViewModel : BindableBase
    {
        /// <summary>
        /// 获取提名人姓名
        /// </summary>
        /// <param name="nominationId">提名ID</param>
        /// <returns>提名人姓名</returns>
        private string GetNomineeName(int nominationId)
        {
            using (var context = new DataBaseContext())
            {
                var nomination = context.Nominations
                    .Include(n => n.NominatedEmployee)
                    .Include(n => n.NominatedAdmin)
                    .FirstOrDefault(n => n.NominationId == nominationId);
                
                return nomination?.NominatedEmployee?.EmployeeName ?? 
                       nomination?.NominatedAdmin?.AdminName ?? 
                       "未知";
            }
        }

        /// <summary>
        /// 获取状态文本
        /// </summary>
        /// <param name="status">状态值</param>
        /// <returns>状态文本描述</returns>
        private string GetStatusText(int status)
        {
            switch (status)
            {
                case 0:
                    return "待审核";
                case 1:
                    return "已通过";
                case 2:
                    return "已拒绝";
                default:
                    return "未知状态";
            }
        }
    }
} 