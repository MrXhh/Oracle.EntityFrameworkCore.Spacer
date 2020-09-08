using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Oracle.EntityFrameworkCore.Infrastructure.Internal;
using Oracle.EntityFrameworkCore.Metadata;
using Oracle.EntityFrameworkCore.Utilities;

namespace Oracle.EntityFrameworkCore.Migrations
{
    public class OracleMigrationsSqlGeneratorSpacer : OracleMigrationsSqlGenerator
    {
        internal static int s_seqCount = 0;

        internal bool m_bCreatingPlSqlBlock;

        internal string _oracleSQLCompatibility = "12";

        internal IDiagnosticsLogger<DbLoggerCategory.Migrations> m_oracleLogger;

        internal static int MaxIdentifierLengthBytes;

        internal static string SequencePrefix = "SQ";

        internal static string TriggerPrefix = "TR";

        internal static string NameSeparator = "_";

        public OracleMigrationsSqlGeneratorSpacer([NotNull] MigrationsSqlGeneratorDependencies dependencies, [NotNull] IOracleOptions options, IDiagnosticsLogger<DbLoggerCategory.Migrations> logger = null)
            : base(dependencies, options, logger)
        {
            try
            {
                if (logger != null && logger.Logger != null && logger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.ctor);
                }
                if (options != null && options.OracleSQLCompatibility != null)
                {
                    _oracleSQLCompatibility = options.OracleSQLCompatibility;
                }
                m_oracleLogger = logger;
                if (_oracleSQLCompatibility == "11")
                {
                    MaxIdentifierLengthBytes = 30;
                }
                else
                {
                    MaxIdentifierLengthBytes = 128;
                }
            }
            catch (Exception ex)
            {
                if (logger != null && logger.Logger != null && logger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(logger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.ctor, ex.ToString());
                }
                throw;
            }
            finally
            {
                if (logger != null && logger.Logger != null && logger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(logger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.ctor);
                }
            }
        }

        protected override void Generate(AlterColumnOperation operation, IModel model, MigrationCommandListBuilder builder)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.Generate, "AlterColumnOperation");
                }
                Check.NotNull(operation, nameof(operation));
                Check.NotNull(builder, nameof(builder));
                FindProperty(model, operation.Schema, operation.Table, operation.Name);
                if (operation.ComputedColumnSql != null)
                {
                    DropColumnOperation operation2 = new DropColumnOperation
                    {
                        Schema = operation.Schema,
                        Table = operation.Table,
                        Name = operation.Name,
                    };
                    AddColumnOperation addColumnOperation = new AddColumnOperation
                    {
                        Schema = operation.Schema,
                        Table = operation.Table,
                        Name = operation.Name,
                        ClrType = operation.ClrType,
                        ColumnType = operation.ColumnType,
                        IsUnicode = operation.IsUnicode,
                        MaxLength = operation.MaxLength,
                        IsRowVersion = operation.IsRowVersion,
                        IsNullable = operation.IsNullable,
                        DefaultValue = operation.DefaultValue,
                        DefaultValueSql = operation.DefaultValueSql,
                        ComputedColumnSql = operation.ComputedColumnSql,
                        IsFixedLength = operation.IsFixedLength,
                    };
                    addColumnOperation.AddAnnotations(operation.GetAnnotations());
                    Generate(operation2, model, builder);
                    Generate(addColumnOperation, model, builder);
                    return;
                }
                bool flag = operation["Oracle:ValueGenerationStrategy"] as OracleValueGenerationStrategy? == OracleValueGenerationStrategy.IdentityColumn;
                if (IsOldColumnSupported(model))
                {
                    if (operation.OldColumn["Oracle:ValueGenerationStrategy"] as OracleValueGenerationStrategy? == OracleValueGenerationStrategy.IdentityColumn && !flag)
                    {
                        if (_oracleSQLCompatibility == "11")
                        {
                            DropIdentityForDB11(operation, builder);
                        }
                        else
                        {
                            DropIdentity(operation, builder);
                        }
                    }
                    if (operation.OldColumn.DefaultValue != null || (operation.OldColumn.DefaultValueSql != null && (operation.DefaultValue == null || operation.DefaultValueSql == null)))
                    {
                        DropDefaultConstraint(operation.Schema, operation.Table, operation.Name, builder);
                    }
                }
                else
                {
                    if (!flag)
                    {
                        if (_oracleSQLCompatibility == "11")
                        {
                            DropIdentityForDB11(operation, builder);
                        }
                        else
                        {
                            DropIdentity(operation, builder);
                        }
                    }
                    if (operation.DefaultValue == null && operation.DefaultValueSql == null)
                    {
                        DropDefaultConstraint(operation.Schema, operation.Table, operation.Name, builder);
                    }
                }
                builder.AppendLine("declare");
                if (operation.Schema != null)
                {
                    builder.AppendLine("   l_nullable all_tab_columns.nullable % type" + Dependencies.SqlGenerationHelper.StatementTerminator);
                }
                else
                {
                    builder.AppendLine("   l_nullable user_tab_columns.nullable % type" + Dependencies.SqlGenerationHelper.StatementTerminator);
                }
                builder.AppendLine("begin ").AppendLine("   select nullable into l_nullable ");
                if (operation.Schema != null)
                {
                    builder.AppendLine("   from all_tab_columns ");
                }
                else
                {
                    builder.AppendLine("   from user_tab_columns ");
                }
                builder.AppendLine("  where table_name = '" + operation.Table + "' ").AppendLine("  and column_name = '" + operation.Name + "' ");
                if (operation.Schema != null)
                {
                    builder.AppendLine("   and owner = '" + operation.Schema + "'" + Dependencies.SqlGenerationHelper.StatementTerminator + " ");
                }
                else
                {
                    builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator ?? "");
                }
                builder.AppendLine("   if l_nullable = 'N' then ");
                builder.Append("        EXECUTE IMMEDIATE 'ALTER TABLE ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, operation.Schema)).Append(" MODIFY ");
                if (operation.IsNullable)
                {
                    ColumnDefinition(operation.Schema, operation.Table, operation.Name, operation.ClrType, operation.ColumnType, operation.IsUnicode, operation.MaxLength, operation.IsFixedLength, operation.IsRowVersion, operation.IsNullable, operation.DefaultValue, operation.DefaultValueSql, operation.ComputedColumnSql, flag, operation, model, builder);
                    builder.AppendLine(" NULL'" + Dependencies.SqlGenerationHelper.StatementTerminator);
                }
                else
                {
                    ColumnDefinition(operation.Schema, operation.Table, operation.Name, operation.ClrType, operation.ColumnType, operation.IsUnicode, operation.MaxLength, operation.IsFixedLength, operation.IsRowVersion, operation.IsNullable, operation.DefaultValue, operation.DefaultValueSql, operation.ComputedColumnSql, flag, operation, model, builder, addNotNullKeyword: false);
                    builder.AppendLine(" '" + Dependencies.SqlGenerationHelper.StatementTerminator);
                }
                builder.AppendLine(" else ");
                builder.Append("        EXECUTE IMMEDIATE 'ALTER TABLE ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, operation.Schema)).Append(" MODIFY ");
                ColumnDefinition(operation.Schema, operation.Table, operation.Name, operation.ClrType, operation.ColumnType, operation.IsUnicode, operation.MaxLength, operation.IsFixedLength, operation.IsRowVersion, operation.IsNullable, operation.DefaultValue, operation.DefaultValueSql, operation.ComputedColumnSql, flag, operation, model, builder);
                builder.AppendLine("'" + Dependencies.SqlGenerationHelper.StatementTerminator).AppendLine(" end if" + Dependencies.SqlGenerationHelper.StatementTerminator).AppendLine("end" + Dependencies.SqlGenerationHelper.StatementTerminator);
                builder.EndCommand();
            }
            catch (Exception ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.Generate, ex.ToString());
                }
                throw;
            }
            finally
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.Generate, "AlterColumnOperation");
                }
            }
        }

        private void DropIdentityForDB11(AlterColumnOperation operation, MigrationCommandListBuilder builder)
        {
            string text = SequencePrefix + NameSeparator + operation.Name;
            if (Encoding.UTF8.GetByteCount(text) > MaxIdentifierLengthBytes)
            {
                text = DeriveObjectName(null, text);
            }
            string schema = operation.Schema;
            string text2 = "";
            text2 = ((schema != null) ? ("\nbegin\nexecute immediate\n'drop sequence " + schema + "." + text + "';\nexception\nwhen others then\nif sqlcode <> -2289 then\nraise;\nend if;\nend;") : ("\nbegin\nexecute immediate\n'drop sequence " + text + "';\nexception\nwhen others then\nif sqlcode <> -2289 then\nraise;\nend if;\nend;"));
            builder.AppendLine(text2).EndCommand();
            string text3 = TriggerPrefix + NameSeparator + operation.Name;
            if (Encoding.UTF8.GetByteCount(text3) > MaxIdentifierLengthBytes)
            {
                text3 = DeriveObjectName(null, text3);
            }
            string text4 = "";
            text4 = ((schema != null) ? ("\nbegin\nexecute immediate\n'drop trigger " + schema + "." + text3 + "';\nexception\nwhen others then\nif sqlcode <> -4080 then\nraise;\nend if;\nend;") : ("\nbegin\nexecute immediate\n'drop trigger " + text3 + "';\nexception\nwhen others then\nif sqlcode <> -4080 then\nraise;\nend if;\nend;"));
            builder.AppendLine(text4).EndCommand();
        }

        private void DropIdentity(AlterColumnOperation operation, MigrationCommandListBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));
            string o = @$"
DECLARE
   v_Count INTEGER;
BEGIN
  SELECT COUNT(*) INTO v_Count
  FROM ALL_TAB_IDENTITY_COLS T
  WHERE T.TABLE_NAME = N'{operation.Table}'
    AND T.COLUMN_NAME = '{operation.Name}';
  IF v_Count > 0 THEN
    EXECUTE IMMEDIATE 'ALTER  TABLE ${Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table)} MODIFY {Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name)} DROP IDENTITY';
  END IF;
END;";
            builder.AppendLine(o).EndCommand();
        }

        internal string CreateTrigger(string SchemaName, string TableName, string ColumnName, string Operation, string SequencName)
        {
            string text = TriggerPrefix + NameSeparator + TableName;
            if (Encoding.UTF8.GetByteCount(text) > MaxIdentifierLengthBytes)
            {
                text = DeriveObjectName(null, text);
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("create or replace trigger ");
            stringBuilder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(text, SchemaName));
            stringBuilder.AppendLine();
            stringBuilder.Append("before ");
            stringBuilder.Append(Operation);
            stringBuilder.Append(" on ");
            stringBuilder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(TableName, SchemaName));
            stringBuilder.Append(" for each row ");
            stringBuilder.AppendLine();
            stringBuilder.Append("begin ");
            stringBuilder.AppendLine();
            if (Operation == "insert")
            {
                stringBuilder.Append("  if :new.");
                stringBuilder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(ColumnName));
                stringBuilder.Append(" is NULL then ");
                stringBuilder.AppendLine();
            }
            stringBuilder.Append("    select ");
            stringBuilder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(SequencName, SchemaName));
            stringBuilder.Append(".nextval ");
            stringBuilder.Append("into ");
            stringBuilder.Append(":new.");
            stringBuilder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(ColumnName));
            stringBuilder.Append(" from dual;  ");
            stringBuilder.AppendLine();
            if (Operation == "insert")
            {
                stringBuilder.Append("  end if; ");
                stringBuilder.AppendLine();
            }
            stringBuilder.Append("end;");
            return stringBuilder.ToString();
        }

        private void GenerateFor12cDB([NotNull] CreateTableOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder, bool terminate)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));
            builder
                .AppendLine("EXECUTE IMMEDIATE 'CREATE TABLE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
                .AppendLine(" (");
            using (builder.Indent())
            {
                for (int i = 0; i < operation.Columns.Count; i++)
                {
                    AddColumnOperation operation2 = operation.Columns[i];
                    ColumnDefinition(operation2, model, builder);
                    if (i != operation.Columns.Count - 1)
                    {
                        builder.AppendLine(",");
                    }
                }
                if (operation.PrimaryKey != null)
                {
                    builder.AppendLine(",");
                    PrimaryKeyConstraint(operation.PrimaryKey, model, builder);
                }
                foreach (AddUniqueConstraintOperation uniqueConstraint in operation.UniqueConstraints)
                {
                    builder.AppendLine(",");
                    UniqueConstraint(uniqueConstraint, model, builder);
                }
                foreach (AddForeignKeyOperation foreignKey in operation.ForeignKeys)
                {
                    builder.AppendLine(",");
                    ForeignKeyConstraint(foreignKey, model, builder);
                }
                builder.AppendLine();
            }
            builder.Append(")';");
            builder.AppendLine();
            if (terminate)
            {
                builder.AppendLine("END;");
                EndStatement(builder);
            }
        }

        private void GenerateForLoweThan12cDB([NotNull] CreateTableOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder, bool terminate)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));
            builder.AppendLine("");
            List<string> list = new List<string>();
            for (int i = 0; i < operation.Columns.Count; i++)
            {
                if (OracleValueGenerationStrategy.IdentityColumn == operation.Columns[i]["Oracle:ValueGenerationStrategy"] as OracleValueGenerationStrategy?)
                {
                    string name = operation.Name;
                    string name2 = operation.Columns[i].Name;
                    string schema = operation.Schema;
                    string text = SequencePrefix + NameSeparator + operation.Name;
                    if (Encoding.UTF8.GetByteCount(text) > MaxIdentifierLengthBytes)
                    {
                        text = DeriveObjectName(null, text);
                    }
                    builder.Append("execute immediate 'CREATE SEQUENCE ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(text, operation.Schema)).AppendLine(" start with 1';");
                    builder.AppendLine();
                    string item = CreateTrigger(schema, name, name2, "insert", text);
                    list.Add(item);
                }
            }
            builder
                .Append("execute immediate 'CREATE TABLE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
                .AppendLine(" (");
            using (builder.Indent())
            {
                for (int j = 0; j < operation.Columns.Count; j++)
                {
                    AddColumnOperation operation2 = operation.Columns[j];
                    ColumnDefinition(operation2, model, builder);
                    if (j != operation.Columns.Count - 1)
                    {
                        builder.AppendLine(",");
                    }
                }
                if (operation.PrimaryKey != null)
                {
                    builder.AppendLine(",");
                    PrimaryKeyConstraint(operation.PrimaryKey, model, builder);
                }
                foreach (AddUniqueConstraintOperation uniqueConstraint in operation.UniqueConstraints)
                {
                    builder.AppendLine(",");
                    UniqueConstraint(uniqueConstraint, model, builder);
                }
                foreach (AddForeignKeyOperation foreignKey in operation.ForeignKeys)
                {
                    builder.AppendLine(",");
                    ForeignKeyConstraint(foreignKey, model, builder);
                }
                builder.AppendLine();
            }
            builder.Append(")';");
            if (!terminate)
            {
                return;
            }
            builder.AppendLine();
            foreach (string item2 in list)
            {
                builder.Append("execute immediate '");
                builder.Append(item2);
                builder.Append("';");
                builder.AppendLine("");
            }
            builder.AppendLine("END;");
            EndStatement(builder);
        }

        protected override void Generate([NotNull] CreateTableOperation operation, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder, bool terminate)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.Generate, "CreateTableOperation 2");
                }
                Check.NotNull(operation, nameof(operation));
                Check.NotNull(builder, nameof(builder));
                try
                {
                    m_bCreatingPlSqlBlock = true;
                    builder.AppendLine("BEGIN ");
                    if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                    {
                        Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Map, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.Generate, "CreateTableOperation 2: OracleSQLCompatibility: " + _oracleSQLCompatibility);
                    }
                    if (string.Compare(_oracleSQLCompatibility, "12") == 0)
                    {
                        if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                        {
                            Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Map, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.Generate, "CreateTableOperation 2: GenerateFor12cDB called");
                        }
                        GenerateFor12cDB(operation, model, builder, terminate);
                    }
                    else
                    {
                        if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                        {
                            Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Map, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.Generate, "CreateTableOperation 2: GenerateForLoweThan12cDB called");
                        }
                        GenerateForLoweThan12cDB(operation, model, builder, terminate);
                    }
                }
                finally
                {
                    m_bCreatingPlSqlBlock = false;
                }
            }
            catch (Exception ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.Generate, ex.ToString());
                }
                throw;
            }
            finally
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.Generate, "CreateTableOperation 2");
                }
            }
        }

        protected override void ColumnDefinition([CanBeNull] string schema, [NotNull] string table, [NotNull] string name, [NotNull] Type clrType, [CanBeNull] string type, [CanBeNull] bool? unicode, [CanBeNull] int? maxLength, [CanBeNull] bool? fixedLength, bool rowVersion, bool nullable, [CanBeNull] object defaultValue, [CanBeNull] string defaultValueSql, [CanBeNull] string computedColumnSql, bool identity, [NotNull] IAnnotatable annotatable, [CanBeNull] IModel model, [NotNull] MigrationCommandListBuilder builder, bool addNotNullKeyword = true)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.ColumnDefinition, "ColumnDefinition 2");
                }
                Check.NotEmpty(name, nameof(name));
                Check.NotNull(clrType, nameof(clrType));
                Check.NotNull(annotatable, nameof(annotatable));
                Check.NotNull(builder, nameof(builder));
                if (computedColumnSql != null)
                {
                    builder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(name)).Append(" ");
                    string columnType = GetColumnType(schema, table, name, clrType, unicode, maxLength, fixedLength, rowVersion, model);
                    if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                    {
                        Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Map, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.ColumnDefinition, $"Column '{name}' mapped to {columnType}");
                    }
                    if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                    {
                        Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Map, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.ColumnDefinition, $"Detailed column info: schema:{schema}, table:{table}, name:{name}, clrType:{clrType}, unicode:{unicode}, maxLength:{maxLength}, fixedLength:{fixedLength}, rowVersion:{rowVersion}");
                    }
                    builder.Append(type ?? columnType).Append(" AS (");
                    if (m_bCreatingPlSqlBlock)
                    {
                        builder.Append(computedColumnSql.Replace("'", "''"));
                    }
                    else
                    {
                        builder.Append(computedColumnSql);
                    }
                    builder.Append(")");
                    return;
                }
                builder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(name)).Append(" ");
                string columnType2 = GetColumnType(schema, table, name, clrType, unicode, maxLength, fixedLength, rowVersion, model);
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Map, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.ColumnDefinition, $"Column '{name}' mapped to {columnType2}");
                }
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Map, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.ColumnDefinition, $"Detailed column info: schema:{schema}, table:{table}, name:{name}, clrType:{clrType}, unicode:{unicode}, maxLength:{maxLength}, fixedLength:{fixedLength}, rowVersion:{rowVersion}");
                }
                builder.Append(type ?? columnType2);
                if (identity)
                {
                    if (string.Compare(_oracleSQLCompatibility, "12") == 0)
                    {
                        builder.Append(" GENERATED BY DEFAULT ON NULL AS IDENTITY");
                    }
                }
                else
                {
                    DefaultValue(defaultValue, defaultValueSql, builder);
                }
                if (!nullable && addNotNullKeyword)
                {
                    builder.Append(" NOT NULL");
                }
            }
            catch (Exception ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.ColumnDefinition, ex.ToString());
                }
                throw;
            }
            finally
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.ColumnDefinition, "ColumnDefinition 2");
                }
            }
        }

        protected override void DefaultValue([CanBeNull] object defaultValue, [CanBeNull] string defaultValueSql, [NotNull] MigrationCommandListBuilder builder)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.DefaultValue);
                }
                Check.NotNull(builder, nameof(builder));
                if (defaultValueSql != null)
                {
                    builder.Append(" DEFAULT (");
                    if (m_bCreatingPlSqlBlock)
                    {
                        builder.Append(defaultValueSql.Replace("'", "''"));
                    }
                    else
                    {
                        builder.Append(defaultValueSql);
                    }
                    builder.Append(")");
                }
                else if (defaultValue != null)
                {
                    object obj = Dependencies.TypeMappingSource.GetMappingForValue(defaultValue).GenerateSqlLiteral(defaultValue);
                    builder.Append(" DEFAULT ");
                    if (m_bCreatingPlSqlBlock)
                    {
                        builder.Append(obj.ToString().Replace("'", "''"));
                    }
                    else
                    {
                        builder.Append(obj.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.DefaultValue, ex.ToString());
                }
                throw;
            }
            finally
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Migrations>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleMigrationsSqlGenerator, OracleTraceFuncName.DefaultValue);
                }
            }
        }

        internal static string DeriveObjectName(string Prefix, string BaseName, int MaxLengthBytes = 30)
        {
            int num = 0;
            char[] array = null;
            if (Prefix != null)
            {
                num = Encoding.UTF8.GetByteCount(Prefix);
                array = Prefix.ToCharArray();
            }
            char[] array2 = null;
            string text = null;
            int num2 = 0;
            char[] array3 = null;
            if (BaseName != null)
            {
                BaseName = BaseName.Replace("\"", string.Empty).Replace('.', '_');
                Encoding.UTF8.GetByteCount(BaseName);
                array2 = BaseName.ToCharArray();
                text = Math.Abs(BaseName.GetHashCode()).ToString();
                num2 = Encoding.UTF8.GetByteCount(text);
                array3 = text.ToCharArray();
            }
            int byteCount = Encoding.UTF8.GetByteCount(NameSeparator);
            char[] array4 = NameSeparator.ToCharArray();
            int num3 = MaxLengthBytes - num - num2 - ((num > 0) ? (byteCount * 2) : byteCount);
            if (num3 < 0)
            {
                num3 = 0;
            }
            int num4 = 0;
            int num5 = 0;
            StringBuilder stringBuilder = new StringBuilder();
            if (num > 0)
            {
                char[] array5 = array;
                for (int i = 0; i < array5.Length; i++)
                {
                    char value = array5[i];
                    num5 = Encoding.UTF8.GetByteCount(value.ToString());
                    if (num4 + num5 > MaxLengthBytes)
                    {
                        break;
                    }
                    stringBuilder.Append(value);
                    num4 += num5;
                }
                if (num4 < MaxLengthBytes)
                {
                    array5 = array4;
                    for (int i = 0; i < array5.Length; i++)
                    {
                        char value2 = array5[i];
                        num5 = Encoding.UTF8.GetByteCount(value2.ToString());
                        if (num4 + num5 > MaxLengthBytes)
                        {
                            break;
                        }
                        stringBuilder.Append(value2);
                        num4 += num5;
                    }
                }
            }
            if (num3 > 0 && num4 < MaxLengthBytes)
            {
                int num6 = 0;
                char[] array5 = array2;
                for (int i = 0; i < array5.Length; i++)
                {
                    char value3 = array5[i];
                    num5 = Encoding.UTF8.GetByteCount(value3.ToString());
                    if (num6 + num5 > num3)
                    {
                        break;
                    }
                    stringBuilder.Append(value3);
                    num4 += num5;
                    num6 += num5;
                }
                if (num4 < MaxLengthBytes)
                {
                    array5 = array4;
                    for (int i = 0; i < array5.Length; i++)
                    {
                        char value4 = array5[i];
                        num5 = Encoding.UTF8.GetByteCount(value4.ToString());
                        if (num4 + num5 > MaxLengthBytes)
                        {
                            break;
                        }
                        stringBuilder.Append(value4);
                        num4 += num5;
                    }
                }
            }
            if (num2 > 0 && num4 < MaxLengthBytes)
            {
                char[] array5 = array3;
                for (int i = 0; i < array5.Length; i++)
                {
                    char value5 = array5[i];
                    num5 = Encoding.UTF8.GetByteCount(value5.ToString());
                    if (num4 + num5 > MaxLengthBytes)
                    {
                        break;
                    }
                    stringBuilder.Append(value5);
                    num4 += num5;
                }
            }
            return stringBuilder.ToString();
        }

    }
}
