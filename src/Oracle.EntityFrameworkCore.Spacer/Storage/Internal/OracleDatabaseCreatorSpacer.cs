using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Oracle.EntityFrameworkCore.Infrastructure.Internal;
using Oracle.EntityFrameworkCore.Migrations;
using Oracle.ManagedDataAccess.Client;

namespace Oracle.EntityFrameworkCore.Storage.Internal
{
    public class OracleDatabaseCreatorSpacer : OracleDatabaseCreator
    {
        internal static string cleanSchemaPLSQL1 = "BEGIN\n            FOR cur_rec IN(SELECT object_name, object_type\n              FROM user_objects\n              WHERE object_type IN\n              ('TABLE',\n                'VIEW',\n                'PACKAGE',\n                'PROCEDURE',\n                'FUNCTION',\n                'SYNONYM',\n                'SEQUENCE'\n              ) ";

        internal static Func<Func<string, string>, string> cleanSchemaPLSQL2Func = delimitIdentifier => @$")
            LOOP
              BEGIN
                IF cur_rec.object_type = 'TABLE' THEN
                  EXECUTE IMMEDIATE 'DROP ' || cur_rec.object_type || ' {delimitIdentifier("' || SYS_CONTEXT('userenv','current_schema') || '")}.{delimitIdentifier("' || cur_rec.object_name || '")} CASCADE CONSTRAINTS';
                ELSE
                  EXECUTE IMMEDIATE 'DROP ' || cur_rec.object_type || ' {delimitIdentifier("' || SYS_CONTEXT('userenv','current_schema') || '")}.{delimitIdentifier("' || cur_rec.object_name || '")}';
                END IF;
              EXCEPTION
                WHEN OTHERS THEN
                  DBMS_OUTPUT.put_line('FAILED: DROP ' || cur_rec.object_type || ' {delimitIdentifier("' ||SYS_CONTEXT('userenv','current_schema') || '")}.{delimitIdentifier("' || cur_rec.object_name || '")}');
              END;
            END LOOP;
          END;";

        internal static string cleanSchemaPLSQL3 = "BEGIN\n            FOR cur_rec IN(SELECT object_name, object_type, owner \n              FROM all_objects\n              WHERE object_type IN\n              ('TABLE',\n                'VIEW',\n                'PACKAGE',\n                'PROCEDURE',\n                'FUNCTION',\n                'SYNONYM',\n                'SEQUENCE'\n              ) ";

        internal static string notOracleMaintained = " oracle_maintained='N' ";

        internal static Func<Func<string, string>, string> cleanSchemaPLSQL4Func = delimitIdentifier => @$")
            LOOP
              BEGIN
                IF cur_rec.object_type = 'TABLE'
                  THEN
                    EXECUTE IMMEDIATE 'DROP ' || cur_rec.object_type || ' {delimitIdentifier("' || cur_rec.owner || '")}.{delimitIdentifier("' || cur_rec.object_name || '")} CASCADE CONSTRAINTS';
                ELSE
                  EXECUTE IMMEDIATE 'DROP ' || cur_rec.object_type || ' {delimitIdentifier("' || cur_rec.owner || '")}.{delimitIdentifier("' || cur_rec.object_name || '")}';
                END IF;
              EXCEPTION
                WHEN OTHERS THEN
                  DBMS_OUTPUT.put_line('FAILED: DROP ' || cur_rec.object_type || ' {delimitIdentifier("' || cur_rec.owner || '")}.{delimitIdentifier("' || cur_rec.object_name || '")}');
              END;
            END LOOP;
          END;";

        internal static string and = " AND ";

        internal static string schemaFilterSQLUser = " owner IN ({0}) ";

        internal static string[] builtInSchemas = new string[42]
        {
            "ANONYMOUS",
            "APEX_050000",
            "APEX_030200",
            "SYSMAN",
            "EXFSYS",
            "APEX_PUBLIC_USER",
            "APPQOSSYS",
            "AUDSYS",
            "CTXSYS",
            "DBSFWUSER",
            "DBSNMP",
            "DIP",
            "DVSYS",
            "DVF",
            "FLOWS_FILES",
            "GGSYS",
            "GSMADMIN_INTERNAL",
            "GSMCATUSER",
            "GSMUSER",
            "LBACSYS",
            "MDDATA",
            "MDSYS",
            "ORDPLUGINS",
            "ORDSYS",
            "ORDDATA",
            "OUTLN",
            "ORACLE_OCM",
            "REMOTE_SCHEDULER_AGENT",
            "SI_INFORMTN_SCHEMA",
            "SPATIAL_CSW_ADMIN_USR",
            "SYS",
            "SYSTEM",
            "SYSBACKUP",
            "SYSKM",
            "SYSDG",
            "SYSRAC",
            "SYS$UMF",
            "WMSYS",
            "XDB",
            "PUBLIC",
            "OJVMSYS",
            "OLAPSYS",
        };

        internal static string schemaFilterSQLInternal = " owner NOT IN ({0}) ";

        internal string _oracleSQLCompatibility = "12";

        private readonly IOracleConnection _connection;

        private IDiagnosticsLogger<DbLoggerCategory.Database> m_oracleLogger;

        public OracleDatabaseCreatorSpacer([NotNull] RelationalDatabaseCreatorDependencies dependencies, [NotNull] IOracleConnection connection, [NotNull] IRawSqlCommandBuilder rawSqlCommandBuilder, [NotNull] IOracleOptions options, IDiagnosticsLogger<DbLoggerCategory.Database> logger = null)
            : base(dependencies, connection, rawSqlCommandBuilder, options, logger)
        {
            if (options != null && options.OracleSQLCompatibility != null)
            {
                _oracleSQLCompatibility = options.OracleSQLCompatibility;
            }
            _connection = connection;

            m_oracleLogger = logger;
        }

        public override void Delete()
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.Delete);
                }
                _connection.Open();
                DbCommand dbCommand = _connection.DbConnection.CreateCommand();
                if (_oracleSQLCompatibility == "11")
                {
                    dbCommand.CommandText = cleanSchemaPLSQL1 + and + string.Format(" sys_context('userenv', 'current_schema') NOT IN ({0}) ", string.Join(", ", builtInSchemas.Select((string x) => "'" + x + "'"))) + cleanSchemaPLSQL2Func(Dependencies.SqlGenerationHelper.DelimitIdentifier);
                }
                else
                {
                    dbCommand.CommandText = cleanSchemaPLSQL1 + and + string.Format(" sys_context('userenv', 'current_schema') NOT IN ({0}) ", string.Join(", ", builtInSchemas.Select((string x) => "'" + x + "'"))) + and + notOracleMaintained + cleanSchemaPLSQL2Func(Dependencies.SqlGenerationHelper.DelimitIdentifier);
                }
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.SQL, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.Delete, dbCommand.CommandText);
                }
                dbCommand.ExecuteNonQuery();
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    OracleConnectionStringBuilder oracleConnectionStringBuilder = new OracleConnectionStringBuilder(_connection.DbConnection.ConnectionString);
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Connection, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.Exists, $"all objects deleted for user '{oracleConnectionStringBuilder.UserID}'");
                }
                OracleMigrationsSqlGeneratorSpacer.s_seqCount = 0;
            }
            catch (OracleException ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.Delete, "OracleException.Number: " + ex.Number);
                }
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.Delete, ex.ToString());
                }
                throw;
            }
            catch (Exception ex2)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.Delete, ex2.ToString());
                }
                throw;
            }
            finally
            {
                _connection.Close();
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.Delete);
                }
            }
        }

        public new void DeleteObjects(string[] schemas)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjects);
                }
                StringBuilder stringBuilder = new StringBuilder();
                string text = "";
                List<string> list = GenerateSchemaList(schemas);
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(list[i]))
                        {
                            if (i > 0 && stringBuilder.Length != 0)
                            {
                                stringBuilder.Append(", ");
                            }
                            stringBuilder.Append(":s");
                            stringBuilder.Append(i.ToString());
                        }
                    }
                    if (stringBuilder.Length > 0)
                    {
                        text = stringBuilder.ToString();
                    }
                }
                _connection.Open();
                DbCommand dbCommand = _connection.DbConnection.CreateCommand();
                if (_oracleSQLCompatibility == "11")
                {
                    dbCommand.CommandText = cleanSchemaPLSQL3 + and + (text.Equals("*") ? string.Empty : string.Format(schemaFilterSQLUser, text)) + and + string.Format(schemaFilterSQLInternal, string.Join(", ", builtInSchemas.Select((string x) => "'" + x + "'"))) + cleanSchemaPLSQL4Func(Dependencies.SqlGenerationHelper.DelimitIdentifier);
                }
                else
                {
                    dbCommand.CommandText = cleanSchemaPLSQL3 + and + (text.Equals("*") ? string.Empty : string.Format(schemaFilterSQLUser, text)) + and + string.Format(schemaFilterSQLInternal, string.Join(", ", builtInSchemas.Select((string x) => "'" + x + "'"))) + and + notOracleMaintained + cleanSchemaPLSQL4Func(Dependencies.SqlGenerationHelper.DelimitIdentifier);
                }
                if (list != null && !text.Equals("*"))
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (!string.IsNullOrEmpty(list[j]))
                        {
                            dbCommand.Parameters.Add(new OracleParameter(":s" + j, OracleDbType.Varchar2, list[j], ParameterDirection.Input));
                        }
                    }
                }
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.SQL, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjects, dbCommand.CommandText);
                }
                dbCommand.ExecuteNonQuery();
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    OracleConnectionStringBuilder oracleConnectionStringBuilder = new OracleConnectionStringBuilder(_connection.DbConnection.ConnectionString);
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Connection, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjects, $"all objects that user has access to deleted for user '{oracleConnectionStringBuilder.UserID}'");
                }
                OracleMigrationsSqlGeneratorSpacer.s_seqCount = 0;
            }
            catch (OracleException ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjects, "OracleException.Number: " + ex.Number);
                }
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjects, ex.ToString());
                }
                throw;
            }
            catch (Exception ex2)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjects, ex2.ToString());
                }
                throw;
            }
            finally
            {
                _connection.Close();
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjects);
                }
            }
        }

        public new async Task DeleteObjectsAsync(string[] schemas, CancellationToken cancellationToken = default)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjectsAsync);
                }
                StringBuilder stringBuilder = new StringBuilder();
                string text = "";
                List<string> list = GenerateSchemaList(schemas);
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(list[i]))
                        {
                            if (i > 0 && stringBuilder.Length != 0)
                            {
                                stringBuilder.Append(", ");
                            }
                            stringBuilder.Append(":s");
                            stringBuilder.Append(i.ToString());
                        }
                    }
                    if (stringBuilder.Length > 0)
                    {
                        text = stringBuilder.ToString();
                    }
                }
                _connection.Open();
                DbCommand dbCommand = _connection.DbConnection.CreateCommand();
                if (_oracleSQLCompatibility == "11")
                {
                    dbCommand.CommandText = cleanSchemaPLSQL3 + and + (text.Equals("*") ? string.Empty : string.Format(schemaFilterSQLUser, text)) + and + string.Format(schemaFilterSQLInternal, string.Join(", ", builtInSchemas.Select((string x) => "'" + x + "'"))) + cleanSchemaPLSQL4Func(Dependencies.SqlGenerationHelper.DelimitIdentifier);
                }
                else
                {
                    dbCommand.CommandText = cleanSchemaPLSQL3 + and + (text.Equals("*") ? string.Empty : string.Format(schemaFilterSQLUser, text)) + and + string.Format(schemaFilterSQLInternal, string.Join(", ", builtInSchemas.Select((string x) => "'" + x + "'"))) + and + notOracleMaintained + cleanSchemaPLSQL4Func(Dependencies.SqlGenerationHelper.DelimitIdentifier);
                }
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.SQL, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjectsAsync, dbCommand.CommandText);
                }
                if (list != null && !text.Equals("*"))
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (!string.IsNullOrEmpty(list[j]))
                        {
                            dbCommand.Parameters.Add(new OracleParameter(":s" + j, OracleDbType.Varchar2, list[j], ParameterDirection.Input));
                        }
                    }
                }
                await dbCommand.ExecuteNonQueryAsync(cancellationToken);
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    OracleConnectionStringBuilder oracleConnectionStringBuilder = new OracleConnectionStringBuilder(_connection.DbConnection.ConnectionString);
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Connection, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjectsAsync, $"all objects that user has access to deleted for user '{oracleConnectionStringBuilder.UserID}'");
                }
                OracleMigrationsSqlGeneratorSpacer.s_seqCount = 0;
            }
            catch (OracleException ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjectsAsync, "OracleException.Number: " + ex.Number);
                }
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjectsAsync, ex.ToString());
                }
                throw;
            }
            catch (Exception ex2)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjectsAsync, ex2.ToString());
                }
                throw;
            }
            finally
            {
                _connection.Close();
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteObjectsAsync);
                }
            }
        }

        public override async Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteAsync);
                }
                _connection.Open();
                DbCommand dbCommand = _connection.DbConnection.CreateCommand();
                if (_oracleSQLCompatibility == "11")
                {
                    dbCommand.CommandText = cleanSchemaPLSQL1 + and + string.Format(" sys_context('userenv', 'current_schema') NOT IN ({0}) ", string.Join(", ", builtInSchemas.Select((string x) => "'" + x + "'"))) + cleanSchemaPLSQL2Func(Dependencies.SqlGenerationHelper.DelimitIdentifier);
                }
                else
                {
                    dbCommand.CommandText = cleanSchemaPLSQL1 + and + string.Format(" sys_context('userenv', 'current_schema') NOT IN ({0}) ", string.Join(", ", builtInSchemas.Select((string x) => "'" + x + "'"))) + and + notOracleMaintained + cleanSchemaPLSQL2Func(Dependencies.SqlGenerationHelper.DelimitIdentifier);
                }
                await dbCommand.ExecuteNonQueryAsync(cancellationToken);
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    OracleConnectionStringBuilder oracleConnectionStringBuilder = new OracleConnectionStringBuilder(_connection.DbConnection.ConnectionString);
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Connection, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteAsync, $"all objects deleted for user '{oracleConnectionStringBuilder.UserID}'");
                }
                OracleMigrationsSqlGeneratorSpacer.s_seqCount = 0;
            }
            catch (OracleException ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.Delete, "OracleException.Number: " + ex.Number);
                }
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteAsync, ex.ToString());
                }
                throw;
            }
            catch (Exception ex2)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteAsync, ex2.ToString());
                }
                throw;
            }
            finally
            {
                _connection.Close();
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Database>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleDatabaseCreator, OracleTraceFuncName.DeleteAsync);
                }
            }
        }


        private List<string> GenerateSchemaList(string[] schemas)
        {
            List<string> list = null;
            if (0 == 0)
            {
                list = new List<string>();
                for (int i = 0; i < schemas.Length; i++)
                {
                    string text = schemas[i].Trim().Replace("'", "''");
                    if (text.StartsWith("\"") && text.EndsWith("\""))
                    {
                        text = text.Trim(new char[1]
                        {
                            '"',
                        });
                    }
                    list.Add(text);
                }
                string userID = new OracleConnectionStringBuilder(_connection.DbConnection.ConnectionString).UserID;
                userID = userID.Trim();
                userID = ((!userID.StartsWith("\"") || !userID.EndsWith("\"")) ? userID.ToUpper() : userID.Trim(new char[1]
                {
                    '"',
                }));
                userID = userID.Replace("'", "''");
                if (!list.Contains(userID))
                {
                    list.Add(userID);
                }
            }
            return list;
        }

    }
}
