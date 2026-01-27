using Microsoft.EntityFrameworkCore;
using MiniNote.Models;
using System;
using System.IO;

namespace MiniNote.Data;

public class AppDbContext : DbContext
{
    public DbSet<TodoItem> TodoItems { get; set; } = null!;
    public DbSet<TodoCategory> Categories { get; set; } = null!;
    public DbSet<AppSettings> Settings { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniNote"
        );

        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        var dbPath = Path.Combine(appDataPath, "mininote.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置 TodoItem 与 Category 的关系
        modelBuilder.Entity<TodoItem>()
            .HasOne(t => t.Category)
            .WithMany(c => c.TodoItems)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // 添加默认分类
        modelBuilder.Entity<TodoCategory>().HasData(
            new TodoCategory { Id = 1, Name = "默认", Color = "#007ACC", SortOrder = 0 }
        );

        // 添加默认设置
        modelBuilder.Entity<AppSettings>().HasData(
            new AppSettings { Id = 1 }
        );
    }
}
