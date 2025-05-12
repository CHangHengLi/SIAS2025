using SIASGraduate.Models;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using System;

namespace SIASGraduate.Context
{
    public class DataBaseContext : DbContext
    {
        public DbSet<SupAdmin> SupAdmins { get; set; } // 超级管理员
        public DbSet<Admin> Admins { get; set; } // 管理员
        public DbSet<Employee> Employees { get; set; } // 员工
        public DbSet<Department> Departments { get; set; } // 部门
        public DbSet<Award> Awards { get; set; }
        public DbSet<Nomination> Nominations { get; set; }
        public DbSet<VoteRecord> VoteRecords { get; set; }
        public DbSet<CommentRecord> CommentRecords { get; set; }
        public DbSet<NominationDeclaration> NominationDeclarations { get; set; } // 申报提名
        public DbSet<NominationLog> NominationLogs { get; set; } // 申报审核日志
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // 直接从配置文件获取连接字符串，不使用加密
            string connectionString = ConfigurationManager.ConnectionStrings["CoonStr"].ConnectionString;
            
            optionsBuilder.UseSqlServer(connectionString, options =>
                {
                    options.EnableRetryOnFailure(
                        maxRetryCount: 5, // 最大重试次数
                        maxRetryDelay: TimeSpan.FromSeconds(30), // 每次重试之间的最大延迟时间
                        errorNumbersToAdd: null); // 要添加的错误编号
                    
                    // 增加命令超时时间，默认是30秒
                    options.CommandTimeout(180); // 设置为3分钟
                });
                  // 默认跟踪实体变化，以确保更新操作正常工作
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // SupAdmin 表的配置
            modelBuilder.Entity<SupAdmin>()
                .HasIndex(e => e.SupAdminName)
                .IsUnique(); // 设置唯一索引
                             // Admin 表的配置
            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.AdminName)
                .IsUnique(); // 设置唯一索引
                             // Employee 表的配置
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique(false); // 假设 Email 不需要唯一，视需求而定
                                  // Role 表的配置
            modelBuilder.Entity<Department>()
                .HasIndex(r => r.DepartmentName)
                .IsUnique(); // 设置唯一索引
            
            // CommentRecord关系配置
            modelBuilder.Entity<CommentRecord>()
                .HasOne(cr => cr.Nomination)
                .WithMany(n => n.CommentRecords)
                .HasForeignKey(cr => cr.NominationId)
                .OnDelete(DeleteBehavior.NoAction); // 避免级联删除
                
            modelBuilder.Entity<CommentRecord>()
                .HasOne(cr => cr.Award)
                .WithMany()
                .HasForeignKey(cr => cr.AwardId)
                .OnDelete(DeleteBehavior.NoAction); // 避免级联删除
                
            // 配置Award、Nomination、VoteRecord之间的关系
            modelBuilder.Entity<Award>()
                .HasMany(a => a.Nominations)
                .WithOne(n => n.Award)
                .HasForeignKey(n => n.AwardId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Nomination>()
                .HasMany(n => n.VoteRecords)
                .WithOne(v => v.Nomination)
                .HasForeignKey(v => v.NominationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VoteRecord>()
                .HasOne(v => v.Award)
                .WithMany()
                .HasForeignKey(v => v.AwardId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // 配置NominationDeclaration与Award之间的关系
            modelBuilder.Entity<NominationDeclaration>()
                .HasOne(nd => nd.Award)
                .WithMany()
                .HasForeignKey(nd => nd.AwardId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // 配置NominationDeclaration与Employee（被提名者）之间的关系
            modelBuilder.Entity<NominationDeclaration>()
                .HasOne(nd => nd.NominatedEmployee)
                .WithMany()
                .HasForeignKey(nd => nd.NominatedEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // 配置NominationDeclaration与Admin（被提名者）之间的关系
            modelBuilder.Entity<NominationDeclaration>()
                .HasOne(nd => nd.NominatedAdmin)
                .WithMany()
                .HasForeignKey(nd => nd.NominatedAdminId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // 配置NominationDeclaration与Department之间的关系
            modelBuilder.Entity<NominationDeclaration>()
                .HasOne(nd => nd.Department)
                .WithMany()
                .HasForeignKey(nd => nd.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // 配置NominationDeclaration与申报人之间的关系
            modelBuilder.Entity<NominationDeclaration>()
                .HasOne(nd => nd.DeclarerEmployee)
                .WithMany()
                .HasForeignKey(nd => nd.DeclarerEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<NominationDeclaration>()
                .HasOne(nd => nd.DeclarerAdmin)
                .WithMany()
                .HasForeignKey(nd => nd.DeclarerAdminId)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<NominationDeclaration>()
                .HasOne(nd => nd.DeclarerSupAdmin)
                .WithMany()
                .HasForeignKey(nd => nd.DeclarerSupAdminId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // 配置NominationDeclaration与审核人之间的关系
            modelBuilder.Entity<NominationDeclaration>()
                .HasOne(nd => nd.ReviewerEmployee)
                .WithMany()
                .HasForeignKey(nd => nd.ReviewerEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<NominationDeclaration>()
                .HasOne(nd => nd.ReviewerAdmin)
                .WithMany()
                .HasForeignKey(nd => nd.ReviewerAdminId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<NominationDeclaration>()
                .HasOne(nd => nd.ReviewerSupAdmin)
                .WithMany()
                .HasForeignKey(nd => nd.ReviewerSupAdminId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // 配置NominationLog与NominationDeclaration之间的关系
            modelBuilder.Entity<NominationLog>()
                .HasOne(nl => nl.Declaration)
                .WithMany()
                .HasForeignKey(nl => nl.DeclarationId)
                .OnDelete(DeleteBehavior.Cascade); // 删除申报时级联删除日志
                
            // 配置NominationLog与操作人之间的关系
            modelBuilder.Entity<NominationLog>()
                .HasOne(nl => nl.OperatorEmployee)
                .WithMany()
                .HasForeignKey(nl => nl.OperatorEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<NominationLog>()
                .HasOne(nl => nl.OperatorAdmin)
                .WithMany()
                .HasForeignKey(nl => nl.OperatorAdminId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<NominationLog>()
                .HasOne(nl => nl.OperatorSupAdmin)
                .WithMany()
                .HasForeignKey(nl => nl.OperatorSupAdminId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
