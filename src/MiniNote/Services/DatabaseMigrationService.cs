using Microsoft.Data.Sqlite;
using MiniNote.Helpers;
using System;
using System.IO;

namespace MiniNote.Services;

/// <summary>
/// 数据库迁移服务 - 处理版本升级时的表结构变更
/// </summary>
public static class DatabaseMigrationService
{
    private static string GetDbPath()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniNote"
        );
        return Path.Combine(appDataPath, "mininote.db");
    }

    /// <summary>
    /// 执行数据库迁移
    /// </summary>
    public static void Migrate()
    {
        var dbPath = GetDbPath();
        if (!File.Exists(dbPath))
        {
            Logger.Info("DatabaseMigration: Database does not exist, will be created by EF Core");
            return;
        }

        Logger.Info("DatabaseMigration: Starting migration check...");

        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            // 迁移版本 1: 添加 IsDarkTheme 列到 Settings 表
            MigrateV1_AddIsDarkTheme(connection);

            Logger.Success("DatabaseMigration: Migration completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"DatabaseMigration: Migration failed - {ex.Message}");
        }
    }

    /// <summary>
    /// 迁移版本 1: 添加 IsDarkTheme 列
    /// </summary>
    private static void MigrateV1_AddIsDarkTheme(SqliteConnection connection)
    {
        if (!ColumnExists(connection, "Settings", "IsDarkTheme"))
        {
            Logger.Info("DatabaseMigration: Adding IsDarkTheme column to Settings table");
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE Settings ADD COLUMN IsDarkTheme INTEGER NOT NULL DEFAULT 1";
            cmd.ExecuteNonQuery();
            
            Logger.Success("DatabaseMigration: IsDarkTheme column added");
        }
    }

    /// <summary>
    /// 检查列是否存在
    /// </summary>
    private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName})";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(1);
            if (name.Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }
}
