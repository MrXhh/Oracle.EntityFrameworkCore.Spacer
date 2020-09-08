using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Oracle.EntityFrameworkCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Oracle.EntityFrameworkCore.Query.Sql.Internal
{
    public class OracleQuerySqlGeneratorSpacer : OracleQuerySqlGenerator
    {
        internal string _oracleSQLCompatibility = "12";

        private bool firstwhereclauseappended;

        private int Count;

        private bool is112SqlCompatibility;

        private bool outerSelectRequired;

        private int generateProjectionCallCount;

        private int generateProjectionCallCountCounter;

        private IDiagnosticsLogger<DbLoggerCategory.Query> m_oracleLogger;

        public OracleQuerySqlGeneratorSpacer([NotNull] QuerySqlGeneratorDependencies dependencies, [NotNull] SelectExpression selectExpression, string oracleSQLCompatibility, IDiagnosticsLogger<DbLoggerCategory.Query> logger = null)
            : base(dependencies, selectExpression, oracleSQLCompatibility, logger)
        {
            if (!string.IsNullOrEmpty(oracleSQLCompatibility))
            {
                _oracleSQLCompatibility = oracleSQLCompatibility;
            }
            if (_oracleSQLCompatibility.StartsWith("11"))
            {
                is112SqlCompatibility = true;
            }
            m_oracleLogger = logger;
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            Check.NotNull(binaryExpression, nameof(binaryExpression));
            if (is112SqlCompatibility)
            {
                Sql.Append("(");
            }
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.And:
                    Sql.Append("BITAND(");
                    Visit(binaryExpression.Left);
                    Sql.Append(", ");
                    Visit(binaryExpression.Right);
                    Sql.Append(")");
                    if (is112SqlCompatibility)
                    {
                        Sql.Append(")");
                    }
                    return binaryExpression;
                case ExpressionType.Or:
                    Visit(binaryExpression.Left);
                    Sql.Append(" - BITAND(");
                    Visit(binaryExpression.Left);
                    Sql.Append(", ");
                    Visit(binaryExpression.Right);
                    Sql.Append(") + ");
                    Visit(binaryExpression.Right);
                    if (is112SqlCompatibility)
                    {
                        Sql.Append(")");
                    }
                    return binaryExpression;
                case ExpressionType.Modulo:
                    Sql.Append("MOD(");
                    Visit(binaryExpression.Left);
                    Sql.Append(", ");
                    Visit(binaryExpression.Right);
                    Sql.Append(")");
                    if (is112SqlCompatibility)
                    {
                        Sql.Append(")");
                    }
                    return binaryExpression;
                default:
                    {
                        if (binaryExpression.Right is ConstantExpression && (binaryExpression.Right as ConstantExpression).Value is string && string.IsNullOrEmpty((binaryExpression.Right as ConstantExpression).Value as string) && (binaryExpression.NodeType == ExpressionType.Equal || binaryExpression.NodeType == ExpressionType.NotEqual))
                        {
                            Visit(binaryExpression.Left);
                            if (binaryExpression.NodeType == ExpressionType.Equal)
                            {
                                Sql.Append(" IS NULL ");
                            }
                            else if (binaryExpression.NodeType == ExpressionType.NotEqual)
                            {
                                Sql.Append(" IS NOT NULL ");
                            }
                            if (is112SqlCompatibility)
                            {
                                Sql.Append(")");
                            }
                            return binaryExpression;
                        }
                        Expression result = base.VisitBinary(binaryExpression);
                        if (is112SqlCompatibility)
                        {
                            Sql.Append(")");
                        }
                        return result;
                    }
            }
        }

        protected override void GenerateProjection(Expression projection)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleQuerySqlGenerator, OracleTraceFuncName.GenerateProjection);
                }
                AliasExpression aliasExpression = projection as AliasExpression;
                Expression expression = aliasExpression?.Expression ?? projection;
                Expression expression2 = ExplicitCastToBool(expression);
                expression = ((aliasExpression != null) ? new AliasExpression(aliasExpression.Alias, expression2) : expression2);
                base.GenerateProjection(expression);
                if (is112SqlCompatibility && outerSelectRequired)
                {
                    if (!(projection is AliasExpression))
                    {
                        Sql.Append(" K" + generateProjectionCallCountCounter);
                    }
                    generateProjectionCallCountCounter++;
                }
            }
            catch (Exception ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleQuerySqlGenerator, OracleTraceFuncName.GenerateProjection, ex.ToString());
                }
                throw;
            }
            finally
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleQuerySqlGenerator, OracleTraceFuncName.GenerateProjection);
                }
            }
        }

        public override Expression VisitSelect(SelectExpression selectExpression)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleQuerySqlGenerator, OracleTraceFuncName.VisitSelect);
                }
                if (is112SqlCompatibility)
                {
                    VisitSelectForDB112OrLess(selectExpression);
                }
                else
                {
                    VisitSelectForDB121OrMore(selectExpression);
                }
                return selectExpression;
            }
            catch (Exception ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleQuerySqlGenerator, OracleTraceFuncName.VisitSelect, ex.ToString());
                }
                throw;
            }
            finally
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleQuerySqlGenerator, OracleTraceFuncName.VisitSelect);
                }
            }
        }

        public new Expression VisitSelectForDB112OrLess(SelectExpression selectExpression)
        {
            try
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Entry, OracleTraceClassName.OracleQuerySqlGenerator, OracleTraceFuncName.VisitSelectForDB112OrLess);
                }
                Check.NotNull(selectExpression, nameof(selectExpression));
                IDisposable disposable = null;
                int num = 0;
                int num2 = 0;
                if (selectExpression.Alias != null)
                {
                    Sql.AppendLine("(");
                    disposable = Sql.Indent();
                }
                if ((selectExpression.OrderBy.Count > 0 && selectExpression.Limit != null) || selectExpression.Offset != null)
                {
                    outerSelectRequired = true;
                    string alias = null;
                    new List<string>();
                    Count++;
                    num2 = Count;
                    if (selectExpression != null && selectExpression.ProjectStarTable != null)
                    {
                        alias = selectExpression.ProjectStarTable.Alias;
                        selectExpression.ProjectStarTable.Alias = "m" + num2;
                    }
                    Sql.AppendLine("Select ");
                    if (selectExpression.IsDistinct)
                    {
                        Sql.Append("DISTINCT ");
                    }
                    GenerateTop(selectExpression);
                    bool flag = false;
                    if (selectExpression.IsProjectStar)
                    {
                        string alias2 = selectExpression.ProjectStarTable.Alias;
                        Sql.Append(SqlGenerator.DelimitIdentifier(alias2)).Append(".*");
                        flag = true;
                    }
                    if (selectExpression.Projection.Count > 0)
                    {
                        if (selectExpression.IsProjectStar)
                        {
                            Sql.Append(", ");
                        }
                        generateProjectionCallCount = selectExpression.Projection.Count;
                        for (int i = 0; i < generateProjectionCallCount - 1; i++)
                        {
                            if (selectExpression.Projection[i] is ColumnExpression)
                            {
                                Sql.Append(" K" + i);
                                Sql.Append(" " + SqlGenerator.DelimitIdentifier(((ColumnExpression)selectExpression.Projection[i]).Name));
                                Sql.Append(",");
                            }
                            else if (selectExpression.Projection[i] is ColumnReferenceExpression)
                            {
                                Sql.Append(" K" + i);
                                Sql.Append(" " + SqlGenerator.DelimitIdentifier(((ColumnReferenceExpression)selectExpression.Projection[i]).Name));
                                Sql.Append(",");
                            }
                            else if (selectExpression.Projection[i] is AliasExpression)
                            {
                                Sql.Append(" " + SqlGenerator.DelimitIdentifier(((AliasExpression)selectExpression.Projection[i]).Alias));
                                Sql.Append(",");
                            }
                            else
                            {
                                Sql.Append(" K" + i);
                                Sql.Append(",");
                            }
                        }
                        if (selectExpression.Projection[generateProjectionCallCount - 1] is ColumnExpression)
                        {
                            Sql.Append(" K" + (generateProjectionCallCount - 1));
                            Sql.Append(" " + SqlGenerator.DelimitIdentifier(((ColumnExpression)selectExpression.Projection[generateProjectionCallCount - 1]).Name));
                        }
                        else if (selectExpression.Projection[generateProjectionCallCount - 1] is ColumnReferenceExpression)
                        {
                            Sql.Append(" K" + (generateProjectionCallCount - 1));
                            Sql.Append(" " + SqlGenerator.DelimitIdentifier(((ColumnReferenceExpression)selectExpression.Projection[generateProjectionCallCount - 1]).Name));
                        }
                        else if (selectExpression.Projection[generateProjectionCallCount - 1] is AliasExpression)
                        {
                            Sql.Append(" " + SqlGenerator.DelimitIdentifier(((AliasExpression)selectExpression.Projection[generateProjectionCallCount - 1]).Alias));
                        }
                        else
                        {
                            Sql.Append(" K" + (generateProjectionCallCount - 1));
                        }
                        flag = true;
                    }
                    if (!flag)
                    {
                        Sql.Append("1");
                    }
                    Sql.Append(" from");
                    Sql.AppendLine("(");
                    if (selectExpression != null && selectExpression.ProjectStarTable != null)
                    {
                        selectExpression.ProjectStarTable.Alias = alias;
                    }
                }
                if (selectExpression.Offset != null)
                {
                    Count++;
                    num = Count;
                    Sql.AppendLine($"select {SqlGenerator.DelimitIdentifier("m" + num)}.*, rownum r" + num + " from");
                    Sql.AppendLine("(");
                }
                Sql.Append("SELECT ");
                if (selectExpression.IsDistinct)
                {
                    Sql.Append("DISTINCT ");
                }
                GenerateTop(selectExpression);
                bool flag2 = false;
                if (selectExpression.IsProjectStar)
                {
                    string alias3 = selectExpression.ProjectStarTable.Alias;
                    Sql.Append(SqlGenerator.DelimitIdentifier(alias3)).Append(".*");
                    flag2 = true;
                }
                if (selectExpression.Projection.Count > 0)
                {
                    if (selectExpression.IsProjectStar)
                    {
                        Sql.Append(", ");
                    }
                    GenerateList(selectExpression.Projection, GenerateProjection);
                    flag2 = true;
                }
                if (!flag2)
                {
                    Sql.Append("1");
                }
                outerSelectRequired = false;
                generateProjectionCallCount = 0;
                generateProjectionCallCountCounter = 0;
                if (selectExpression.Tables.Count > 0)
                {
                    Sql.AppendLine().Append("FROM ");
                    GenerateList(selectExpression.Tables, delegate (IRelationalCommandBuilder sql)
                    {
                        sql.AppendLine();
                    });
                }
                else
                {
                    GeneratePseudoFromClause();
                }
                if (selectExpression.Predicate != null)
                {
                    GeneratePredicate(selectExpression.Predicate);
                    firstwhereclauseappended = true;
                }
                if (selectExpression.OrderBy.Count == 0 && selectExpression.Offset == null && selectExpression.Limit != null)
                {
                    if (firstwhereclauseappended)
                    {
                        Sql.AppendLine().Append("and rownum <= ");
                        Visit(selectExpression.Limit);
                    }
                    else
                    {
                        Sql.AppendLine().Append("where rownum <= ");
                        Visit(selectExpression.Limit);
                    }
                }
                firstwhereclauseappended = false;
                if (selectExpression.GroupBy.Count > 0)
                {
                    Sql.AppendLine();
                    Sql.Append("GROUP BY ");
                    GenerateList(selectExpression.GroupBy);
                }
                if (selectExpression.Having != null)
                {
                    GenerateHaving(selectExpression.Having);
                }
                if (selectExpression.OrderBy.Count > 0)
                {
                    Sql.AppendLine();
                    GenerateOrderBy(selectExpression.OrderBy);
                }
                if (selectExpression.Offset != null)
                {
                    Sql.AppendLine().Append($") {SqlGenerator.DelimitIdentifier("m" + num)}");
                }
                if ((selectExpression.OrderBy.Count > 0 && selectExpression.Limit != null) || selectExpression.Offset != null)
                {
                    Sql.AppendLine().Append($") {SqlGenerator.DelimitIdentifier("m" + num2)}");
                    if (selectExpression.Limit != null && selectExpression.Offset == null)
                    {
                        Sql.AppendLine().Append("where rownum <= ");
                        Visit(selectExpression.Limit);
                    }
                    if (selectExpression.Limit == null && selectExpression.Offset != null)
                    {
                        Sql.AppendLine().Append("where r" + num + " > ");
                        Visit(selectExpression.Offset);
                    }
                    if (selectExpression.Limit != null && selectExpression.Offset != null)
                    {
                        Sql.AppendLine().Append("where r" + num + " > ");
                        Visit(selectExpression.Offset);
                        Sql.AppendLine().Append("and r" + num + " <= (");
                        Visit(selectExpression.Offset);
                        Sql.Append(" + ");
                        Visit(selectExpression.Limit);
                        Sql.Append(")");
                    }
                }
                if (disposable != null)
                {
                    disposable.Dispose();
                    Sql.AppendLine().Append(")");
                    if (selectExpression.Alias.Length > 0)
                    {
                        Sql.Append(AliasSeparator).Append(SqlGenerator.DelimitIdentifier(selectExpression.Alias));
                    }
                }
                return selectExpression;
            }
            catch (Exception ex)
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Error))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Error, OracleTraceTag.Error, OracleTraceClassName.OracleQuerySqlGenerator, OracleTraceFuncName.VisitSelectForDB112OrLess, ex.ToString());
                }
                throw;
            }
            finally
            {
                if (m_oracleLogger != null && m_oracleLogger.Logger != null && m_oracleLogger.Logger.IsEnabled(LogLevel.Trace))
                {
                    Trace<DbLoggerCategory.Query>.Write(m_oracleLogger, LogLevel.Trace, OracleTraceTag.Exit, OracleTraceClassName.OracleQuerySqlGenerator, OracleTraceFuncName.VisitSelectForDB112OrLess);
                }
            }
        }

        private static Expression ExplicitCastToBool(Expression expression)
        {
            BinaryExpression obj = expression as BinaryExpression;
            if (obj == null || obj.NodeType != ExpressionType.Coalesce || !(expression.Type.UnwrapNullableType() == typeof(bool)))
            {
                return expression;
            }
            return new ExplicitCastExpression(expression, expression.Type);
        }
    }
}
