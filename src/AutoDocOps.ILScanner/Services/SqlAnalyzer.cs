using Microsoft.Extensions.Logging;
using AutoDocOps.ILScanner.Logging;
using System.Text.RegularExpressions;

namespace AutoDocOps.ILScanner.Services;

public class SqlAnalyzer
{
    private readonly ILogger<SqlAnalyzer> _logger;

    public SqlAnalyzer(ILogger<SqlAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<SqlMetadata> AnalyzeSqlAsync(
        string sqlContent,
        string databaseType,
        CancellationToken cancellationToken = default)
    {
    ArgumentNullException.ThrowIfNull(databaseType);
        var metadata = new SqlMetadata
        {
            AnalysisTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        try
        {
            // Parse SQL content based on database type
            switch (databaseType.ToLowerInvariant())
            {
                case "postgresql":
                case "postgres":
                    await AnalyzePostgreSqlAsync(sqlContent, metadata, cancellationToken).ConfigureAwait(false);
                    break;
                case "mysql":
                    await AnalyzeMySqlAsync(sqlContent, metadata, cancellationToken).ConfigureAwait(false);
                    break;
                case "sqlserver":
                case "mssql":
                    await AnalyzeSqlServerAsync(sqlContent, metadata, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    await AnalyzeGenericSqlAsync(sqlContent, metadata, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.SqlContentError(ex, databaseType);
            throw;
        }

        return metadata;
    }

    private async Task AnalyzePostgreSqlAsync(string sqlContent, SqlMetadata metadata, CancellationToken cancellationToken)
    {
    await AnalyzeGenericSqlAsync(sqlContent, metadata, cancellationToken).ConfigureAwait(false);
        
        // PostgreSQL-specific analysis
        AnalyzePostgreSqlSpecificFeatures(sqlContent, metadata);
    }

    private async Task AnalyzeMySqlAsync(string sqlContent, SqlMetadata metadata, CancellationToken cancellationToken)
    {
    await AnalyzeGenericSqlAsync(sqlContent, metadata, cancellationToken).ConfigureAwait(false);
        
        // MySQL-specific analysis
        AnalyzeMySqlSpecificFeatures(sqlContent, metadata);
    }

    private async Task AnalyzeSqlServerAsync(string sqlContent, SqlMetadata metadata, CancellationToken cancellationToken)
    {
    await AnalyzeGenericSqlAsync(sqlContent, metadata, cancellationToken).ConfigureAwait(false);
        
        // SQL Server-specific analysis
        AnalyzeSqlServerSpecificFeatures(sqlContent, metadata);
    }

    private async Task AnalyzeGenericSqlAsync(string sqlContent, SqlMetadata metadata, CancellationToken cancellationToken)
    {
        // Normalize SQL content
        var normalizedSql = NormalizeSql(sqlContent);
        
        // Extract CREATE TABLE statements
        var tableMatches = Regex.Matches(normalizedSql, 
            @"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:(\w+)\.)?(\w+)\s*\((.*?)\)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in tableMatches)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var schema = match.Groups[1].Value;
            var tableName = match.Groups[2].Value;
            var columnDefinitions = match.Groups[3].Value;

            var tableMetadata = ParseTableDefinition(tableName, schema, columnDefinitions);
            metadata.Tables.Add(tableMetadata);
        }

        // Extract CREATE VIEW statements
        var viewMatches = Regex.Matches(normalizedSql,
            @"CREATE\s+(?:OR\s+REPLACE\s+)?VIEW\s+(?:(\w+)\.)?(\w+)\s+AS\s+(.*?)(?=CREATE|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in viewMatches)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var schema = match.Groups[1].Value;
            var viewName = match.Groups[2].Value;
            var definition = match.Groups[3].Value.Trim();

            var viewMetadata = new ViewMetadata
            {
                Name = viewName,
                Schema = string.IsNullOrEmpty(schema) ? "public" : schema,
                Definition = definition
            };

            // Try to extract columns from SELECT statement
            ExtractViewColumns(definition, viewMetadata);
            metadata.Views.Add(viewMetadata);
        }

        // Extract stored procedures/functions
    await ExtractStoredProceduresAndFunctions(normalizedSql, metadata, cancellationToken).ConfigureAwait(false);
    }

    private TableMetadata ParseTableDefinition(string tableName, string schema, string columnDefinitions)
    {
        var tableMetadata = new TableMetadata
        {
            Name = tableName,
            Schema = string.IsNullOrEmpty(schema) ? "public" : schema
        };

        // Parse column definitions
        var columnMatches = Regex.Matches(columnDefinitions,
            @"(\w+)\s+([A-Za-z0-9_\(\),\s]+?)(?:\s+(NOT\s+NULL|NULL|PRIMARY\s+KEY|UNIQUE|DEFAULT\s+[^,\)]+))*(?=,|\)|$)",
            RegexOptions.IgnoreCase);

        foreach (Match match in columnMatches)
        {
            var columnName = match.Groups[1].Value.Trim();
            var dataType = match.Groups[2].Value.Trim();
            var constraints = match.Groups[3].Value;

            var columnMetadata = new ColumnMetadata
            {
                Name = columnName,
                DataType = dataType,
                IsNullable = !constraints.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase),
                IsPrimaryKey = constraints.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase),
                IsIdentity = constraints.Contains("IDENTITY", StringComparison.OrdinalIgnoreCase) ||
                           constraints.Contains("AUTO_INCREMENT", StringComparison.OrdinalIgnoreCase) ||
                           constraints.Contains("SERIAL", StringComparison.OrdinalIgnoreCase)
            };

            // Extract default value
            var defaultMatch = Regex.Match(constraints, @"DEFAULT\s+([^,\)]+)", RegexOptions.IgnoreCase);
            if (defaultMatch.Success)
            {
                columnMetadata.DefaultValue = defaultMatch.Groups[1].Value.Trim();
            }

            // Extract max length for string types
            var lengthMatch = Regex.Match(dataType, @"\((\d+)\)");
            if (lengthMatch.Success && int.TryParse(lengthMatch.Groups[1].Value, out int maxLength))
            {
                columnMetadata.MaxLength = maxLength;
            }

            tableMetadata.Columns.Add(columnMetadata);
        }

        return tableMetadata;
    }

    private void ExtractViewColumns(string selectStatement, ViewMetadata viewMetadata)
    {
        // Simple column extraction from SELECT statement
        var selectMatch = Regex.Match(selectStatement, @"SELECT\s+(.*?)\s+FROM", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (selectMatch.Success)
        {
            var columnList = selectMatch.Groups[1].Value;
            var columns = columnList.Split(',');

            foreach (var column in columns)
            {
                var cleanColumn = column.Trim();
                var columnName = cleanColumn;

                // Handle aliases (AS keyword)
                var aliasMatch = Regex.Match(cleanColumn, @"(.+?)\s+AS\s+(\w+)", RegexOptions.IgnoreCase);
                if (aliasMatch.Success)
                {
                    columnName = aliasMatch.Groups[2].Value;
                }
                else
                {
                    // Handle simple aliases (without AS)
                    var parts = cleanColumn.Split(' ');
                    if (parts.Length > 1)
                    {
                        columnName = parts.Last();
                    }
                }

                var columnMetadata = new ColumnMetadata
                {
                    Name = columnName,
                    DataType = "unknown", // Would need more sophisticated parsing
                    IsNullable = true // Default assumption for views
                };

                viewMetadata.Columns.Add(columnMetadata);
            }
        }
    }

    private Task ExtractStoredProceduresAndFunctions(string sqlContent, SqlMetadata metadata, CancellationToken cancellationToken)
    {
        // Extract stored procedures
        var procedureMatches = Regex.Matches(sqlContent,
            @"CREATE\s+(?:OR\s+REPLACE\s+)?PROCEDURE\s+(?:(\w+)\.)?(\w+)\s*\((.*?)\)\s+AS\s+(.*?)(?=CREATE|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in procedureMatches)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var schema = match.Groups[1].Value;
            var procedureName = match.Groups[2].Value;
            var parameters = match.Groups[3].Value;
            var definition = match.Groups[4].Value.Trim();

            var procedureMetadata = new StoredProcedureMetadata
            {
                Name = procedureName,
                Schema = string.IsNullOrEmpty(schema) ? "public" : schema,
                Definition = definition
            };

            // Parse parameters
            ParseParameters(parameters, procedureMetadata.Parameters);
            metadata.StoredProcedures.Add(procedureMetadata);
        }

        // Extract functions
        var functionMatches = Regex.Matches(sqlContent,
            @"CREATE\s+(?:OR\s+REPLACE\s+)?FUNCTION\s+(?:(\w+)\.)?(\w+)\s*\((.*?)\)\s+RETURNS\s+(\w+)\s+AS\s+(.*?)(?=CREATE|$)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in functionMatches)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var schema = match.Groups[1].Value;
            var functionName = match.Groups[2].Value;
            var parameters = match.Groups[3].Value;
            var returnType = match.Groups[4].Value;
            var definition = match.Groups[5].Value.Trim();

            var functionMetadata = new FunctionMetadata
            {
                Name = functionName,
                Schema = string.IsNullOrEmpty(schema) ? "public" : schema,
                ReturnType = returnType,
                Definition = definition
            };

            // Parse parameters
            ParseParameters(parameters, functionMetadata.Parameters);
            metadata.Functions.Add(functionMetadata);
        }
        
        return Task.CompletedTask;
    }

    private void ParseParameters(string parametersString, Google.Protobuf.Collections.RepeatedField<ParameterMetadata> parameters)
    {
        if (string.IsNullOrWhiteSpace(parametersString))
        {
            return;
        }

        var paramMatches = Regex.Matches(parametersString,
            @"(@?\w+)\s+([A-Za-z0-9_\(\),\s]+?)(?:\s*=\s*([^,]+))?(?=,|$)",
            RegexOptions.IgnoreCase);

        foreach (Match match in paramMatches)
        {
            var paramName = match.Groups[1].Value.Trim();
            var paramType = match.Groups[2].Value.Trim();
            var defaultValue = match.Groups[3].Value.Trim();

            var paramMetadata = new ParameterMetadata
            {
                Name = paramName,
                Type = paramType,
                IsOptional = !string.IsNullOrEmpty(defaultValue),
                DefaultValue = defaultValue
            };

            parameters.Add(paramMetadata);
        }
    }

    private string NormalizeSql(string sql)
    {
        // Remove comments
        sql = Regex.Replace(sql, @"--.*$", "", RegexOptions.Multiline);
        sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);
        
        // Normalize whitespace
        sql = Regex.Replace(sql, @"\s+", " ");
        
        return sql.Trim();
    }

    private void AnalyzePostgreSqlSpecificFeatures(string sqlContent, SqlMetadata metadata)
    {
        // PostgreSQL-specific features like ENUM types, arrays, etc.
        // This is a placeholder for more sophisticated PostgreSQL analysis
    }

    private void AnalyzeMySqlSpecificFeatures(string sqlContent, SqlMetadata metadata)
    {
        // MySQL-specific features
        // This is a placeholder for more sophisticated MySQL analysis
    }

    private void AnalyzeSqlServerSpecificFeatures(string sqlContent, SqlMetadata metadata)
    {
        // SQL Server-specific features
        // This is a placeholder for more sophisticated SQL Server analysis
    }
}

