using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Oracle.EntityFrameworkCore.Utilities;

namespace Oracle.EntityFrameworkCore.Storage.Internal
{
    public class OracleSqlGenerationHelperExtensions : OracleSqlGenerationHelper
    {
        private IDiagnosticsLogger<DbLoggerCategory.Database.Command> m_oracleLogger;

        public OracleSqlGenerationHelperExtensions([NotNull] RelationalSqlGenerationHelperDependencies dependencies, IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger) 
            : base(dependencies, logger)
        {
            m_oracleLogger = logger;

        }


        public override string DelimitIdentifier(string identifier)
        {
            if (OracleDataBaseExtensions.DataBaseIsIgnoreCase && !identifier.Contains("."))
            {
                return EscapeIdentifier(Check.NotEmpty(identifier, nameof(identifier)));
            }
            else
            {
                return base.DelimitIdentifier(identifier);
            }
        }

        public override void DelimitIdentifier(StringBuilder builder, string identifier)
        {
            if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
            {
                Trace<DbLoggerCategory.Database.Command>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleSqlGenerationHelper, OracleTraceFuncName.DelimitIdentifier);
            }
            try
            {
                Check.NotEmpty(identifier, nameof(identifier));
                if (OracleDataBaseExtensions.DataBaseIsIgnoreCase && !identifier.Contains("."))
                {
                    base.EscapeIdentifier(builder, identifier);
                }
                else
                {
                    base.DelimitIdentifier(builder, identifier);
                }
            }
            catch (Exception ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database.Command>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleSqlGenerationHelper, OracleTraceFuncName.DelimitIdentifier, ex.ToString());
                }
                throw;
            }
            finally
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database.Command>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleSqlGenerationHelper, OracleTraceFuncName.DelimitIdentifier);
                }
            }
        }

    }
}
