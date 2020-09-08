using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Oracle.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Storage.Internal;
using Oracle.EntityFrameworkCore.Utilities;
using System;
using System.Text;

namespace ConsoleApp_EFCore_Oracle
{
    public class TestContext : DbContext
    {
        static string ConnectionStrings = "User ID=fgxt_yw;Password=fgxt_yw;Data Source=127.0.0.1:1521/XE;";         // Oracle


        public DbSet<xhhTest> XhhTests { get; set; }

        // 输出到Console
        static readonly LoggerFactory ConsoleLoggerFactory = new LoggerFactory(
            new[] {
#pragma warning disable CS0618 // 类型或成员已过时
                new ConsoleLoggerProvider(
                    (category, level) => string.Equals(category, DbLoggerCategory.Database.Command.Name, System.StringComparison.Ordinal) && (level == LogLevel.Error ||level == LogLevel.Information),
                    true
                )
#pragma warning restore CS0618 // 类型或成员已过时
            }
        );

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            base.OnConfiguring(options);
            options.UseOracle(
                ConnectionStrings, 
                oracleOptionsAction => 
                    oracleOptionsAction.UseOracleSQLCompatibility("11")
            );
#if DEBUG
            options.UseLoggerFactory(ConsoleLoggerFactory);
#endif
            //options.ReplaceService<ISqlGenerationHelper, SqlGenerationHelper>();

            options.UseOracleDataBaseExtensions(true);

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (this.Database.IsOracle())
            {
                modelBuilder.HasDefaultSchema("FGXT_YW");
            }
            base.OnModelCreating(modelBuilder);


            //modelBuilder
            //    .Entity<xhhTest>()
            //        .Property(x => x.Id)
            //        .ForOracleUseSequenceHiLo("SEQ_XHHTEST_ID");

        }

    }

}
