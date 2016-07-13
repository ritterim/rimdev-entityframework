using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;

namespace RimDev.EntityFramework
{
    public static class DbContextExtensions
    {
        public static MultipleResultSetWrapper MultipleResults(
        this DbContext context,
        string commandText,
        CommandType commandType,
        List<SqlParameter> parameters = null)
        {
            return new MultipleResultSetWrapper(context, commandText, commandType, parameters);
        }

        public static MultipleResultSetWrapper MultipleResultsUsingSql(
            this DbContext context,
            string sql,
            List<SqlParameter> parameters = null)
        {
            return MultipleResults(context, sql, CommandType.Text, parameters);
        }

        public static MultipleResultSetWrapper MultipleResultsUsingStoredProcedure(
            this DbContext context,
            string storedProcedure,
            List<SqlParameter> parameters = null)
        {
            return MultipleResults(context, storedProcedure, CommandType.StoredProcedure, parameters);
        }
    }
}
