using System;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.Extensions.Logging;
using Oracle.EntityFrameworkCore.Infrastructure.Internal;
using Oracle.EntityFrameworkCore.Utilities;

namespace Oracle.EntityFrameworkCore.Query.Sql.Internal
{
    public class OracleQuerySqlGeneratorFactorySpacer : OracleQuerySqlGeneratorFactory
    {
        private readonly IOracleOptions _oracleOptions;

        private IDiagnosticsLogger<DbLoggerCategory.Query> m_oracleLogger;

        public OracleQuerySqlGeneratorFactorySpacer([NotNull] QuerySqlGeneratorDependencies dependencies, [NotNull] IOracleOptions oracleOptions, IDiagnosticsLogger<DbLoggerCategory.Query> logger = null)
            : base(dependencies, oracleOptions, logger)
        {
            m_oracleLogger = logger;
            _oracleOptions = oracleOptions;
        }

        public override IQuerySqlGenerator CreateDefault(SelectExpression selectExpression)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleQuerySqlGeneratorFactory, OracleTraceFuncName.CreateDefault);
                }
                return new OracleQuerySqlGeneratorSpacer(Dependencies, Check.NotNull(selectExpression, nameof(selectExpression)), _oracleOptions.OracleSQLCompatibility, m_oracleLogger);
            }
            catch (Exception ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleQuerySqlGeneratorFactory, OracleTraceFuncName.CreateDefault, ex.ToString());
                }
                throw;
            }
            finally
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleQuerySqlGeneratorFactory, OracleTraceFuncName.CreateDefault);
                }
            }
        }

    }
}
