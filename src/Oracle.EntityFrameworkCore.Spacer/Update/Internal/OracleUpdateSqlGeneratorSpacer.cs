using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.Logging;
using Oracle.EntityFrameworkCore.Migrations;

namespace Oracle.EntityFrameworkCore.Update.Internal
{
    public class OracleUpdateSqlGeneratorSpacer : OracleUpdateSqlGenerator
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        private IDiagnosticsLogger<DbLoggerCategory.Update> m_oracleLogger;

        public OracleUpdateSqlGeneratorSpacer([NotNull] UpdateSqlGeneratorDependencies dependencies, [NotNull] IRelationalTypeMappingSource typeMappingSource, IDiagnosticsLogger<DbLoggerCategory.Update> logger = null)
            : base(dependencies, typeMappingSource, logger)
        {
            _typeMappingSource = typeMappingSource;
            m_oracleLogger = logger;
        }

        public override ResultSetMapping AppendBatchInsertOperation(StringBuilder commandStringBuilder, Dictionary<string, string> variablesInsert, IReadOnlyDictionary<ModificationCommand, int> modificationCommands, int commandPosition, ref int cursorPosition, List<int> _cursorPositionList)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Update>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleUpdateSqlGenerator, OracleTraceFuncName.AppendBatchInsertOperation);
                }
                commandStringBuilder.Clear();
                ResultSetMapping result = ResultSetMapping.NoResultSet;
                KeyValuePair<ModificationCommand, int> keyValuePair = modificationCommands.First();
                string tableName = keyValuePair.Key.TableName;
                string schema = keyValuePair.Key.Schema;
                ColumnModification[] array = keyValuePair.Key.ColumnModifications.Where((ColumnModification o) => o.IsRead).ToArray();
                string text = tableName;
                int num = 28 - commandPosition.ToString().Length;
                if (Encoding.UTF8.GetByteCount(text) > num)
                {
                    text = OracleMigrationsSqlGeneratorSpacer.DeriveObjectName(null, text, num);
                }
                string nameVariable = $"{text}_{commandPosition}";
                if (array.Length != 0)
                {
                    if (!variablesInsert.Any((KeyValuePair<string, string> p) => p.Key == nameVariable))
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.AppendLine("TYPE r" + nameVariable + " IS RECORD").AppendLine("(");
                        stringBuilder.AppendJoin(array, delegate (StringBuilder sb, ColumnModification cm)
                        {
                            sb.Append(SqlGenerationHelper.DelimitIdentifier(cm.ColumnName)).Append(" ").AppendLine(GetVariableType(cm));
                        }, ",").Append(")").AppendLine(SqlGenerationHelper.StatementTerminator);
                        stringBuilder.Append("TYPE t" + nameVariable + " IS TABLE OF r" + nameVariable).AppendLine(SqlGenerationHelper.StatementTerminator).Append("l" + nameVariable + " t" + nameVariable)
                            .AppendLine(SqlGenerationHelper.StatementTerminator);
                        variablesInsert.Add(nameVariable, stringBuilder.ToString());
                    }
                    commandStringBuilder.Append("l").Append(nameVariable).Append(" := ")
                        .Append("t" + nameVariable)
                        .Append("()")
                        .AppendLine(SqlGenerationHelper.StatementTerminator);
                    commandStringBuilder.Append("l" + nameVariable + ".extend(").Append(modificationCommands.Count).Append(")")
                        .AppendLine(SqlGenerationHelper.StatementTerminator);
                }
                int num2 = 0;
                foreach (KeyValuePair<ModificationCommand, int> modificationCommand in modificationCommands)
                {
                    IReadOnlyList<ColumnModification> columnModifications = modificationCommand.Key.ColumnModifications;
                    ColumnModification[] array2 = columnModifications.Where((ColumnModification o) => o.IsRead).ToArray();
                    ColumnModification[] writeOperations = columnModifications.Where((ColumnModification o) => o.IsWrite).ToArray();
                    AppendInsertCommand(commandStringBuilder, tableName, schema, writeOperations, (IReadOnlyCollection<ColumnModification>)(object)array2);
                    AppendReturnInsert(commandStringBuilder, nameVariable, array2, num2);
                    num2++;
                }
                num2 = 0;
                foreach (KeyValuePair<ModificationCommand, int> modificationCommand2 in modificationCommands)
                {
                    ColumnModification[] array3 = modificationCommand2.Key.ColumnModifications.Where((ColumnModification o) => o.IsRead).ToArray();
                    if (array3.Length != 0)
                    {
                        int value = modificationCommand2.Value;
                        AppendReturnCursor(commandStringBuilder, nameVariable, array3, num2, value);
                        result = ResultSetMapping.LastInResultSet;
                    }
                    num2++;
                }
                return result;
            }
            catch (Exception ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Update>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleUpdateSqlGenerator, OracleTraceFuncName.AppendBatchInsertOperation, ex.ToString());
                }
                throw;
            }
            finally
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Update>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleUpdateSqlGenerator, OracleTraceFuncName.AppendBatchInsertOperation);
                }
            }
        }

        private void AppendReturnInsert(StringBuilder commandStringBuilder, string name, IReadOnlyList<ColumnModification> operations, int commandPosition)
        {
            if (operations.Count > 0)
            {
                commandStringBuilder.AppendLine().Append("RETURNING ").AppendJoin(operations, delegate (StringBuilder sb, ColumnModification cm)
                {
                    sb.Append(SqlGenerationHelper.DelimitIdentifier(cm.ColumnName));
                })
                    .Append(" INTO ")
                    .AppendJoin(operations, delegate (StringBuilder sb, ColumnModification cm)
                    {
                        sb.Append($"l{name}({commandPosition + 1}).{SqlGenerationHelper.DelimitIdentifier(cm.ColumnName)}");
                    });
            }
            commandStringBuilder.AppendLine(SqlGenerationHelper.StatementTerminator);
        }

        private void AppendReturnCursor(StringBuilder commandStringBuilder, string name, IReadOnlyList<ColumnModification> operations, int commandPosition, int cursorPosition)
        {
            if (operations.Count > 0)
            {
                commandStringBuilder.Append("OPEN :cur").Append(cursorPosition).Append(" FOR")
                    .Append(" SELECT ")
                    .AppendJoin(operations, delegate (StringBuilder sb, ColumnModification o)
                    {
                        sb.Append("l").Append(name).Append("(")
                            .Append(commandPosition + 1)
                            .Append(").")
                            .Append(SqlGenerationHelper.DelimitIdentifier(o.ColumnName));
                    }, ",")
                    .Append(" FROM DUAL")
                    .AppendLine(SqlGenerationHelper.StatementTerminator);
            }
        }


        private string GetVariableType(ColumnModification columnModification)
        {
            return _typeMappingSource.FindMapping(columnModification.Property).StoreType;
        }

        private void AppendInsertCommand(StringBuilder commandStringBuilder, string name, string schema, IReadOnlyList<ColumnModification> writeOperations, IReadOnlyCollection<ColumnModification> readOperations)
        {
            AppendInsertCommandHeader(commandStringBuilder, name, schema, writeOperations);
            AppendValuesHeader(commandStringBuilder, writeOperations);
            IReadOnlyList<ColumnModification> operations;
            if (writeOperations.Count <= 0)
            {
                IReadOnlyList<ColumnModification> readOnlyList = readOperations.ToArray();
                operations = readOnlyList;
            }
            else
            {
                operations = writeOperations;
            }
            AppendValues(commandStringBuilder, operations);
        }

    }
}
