using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Oracle.EntityFrameworkCore.Migrations;
using Oracle.EntityFrameworkCore.Query.Sql.Internal;
using Oracle.EntityFrameworkCore.Storage.Internal;
using Oracle.EntityFrameworkCore.Update.Internal;

namespace Oracle.EntityFrameworkCore
{
    public static class OracleDataBaseSpacer
    {
        public static DbContextOptionsBuilder UseOracleEFCoreSpacer(this DbContextOptionsBuilder options, bool dataBaseIsIgnoreCase = false)
        {
            options.ReplaceService<IUpdateSqlGenerator, OracleUpdateSqlGeneratorSpacer>();
            options.ReplaceService<IMigrationsSqlGenerator, OracleMigrationsSqlGeneratorSpacer>();
            options.ReplaceService<IRelationalDatabaseCreator, OracleDatabaseCreatorSpacer>();
            options.ReplaceService<IQuerySqlGeneratorFactory, OracleQuerySqlGeneratorFactorySpacer>();


            return options;
        }

    }
}
