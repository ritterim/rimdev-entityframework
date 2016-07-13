using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace RimDev.EntityFramework
{
    public class MultipleResultSetWrapper
    {
        public MultipleResultSetWrapper(
            DbContext context,
            string commandText,
            CommandType commandType,
            List<SqlParameter> parameters = null)
        {
            this.context = context;
            this.commandText = commandText;
            this.commandType = commandType;
            this.parameters = parameters;
            resultSets = new List<Func<IObjectContextAdapter, DbDataReader, IEnumerable>>();
        }

        private readonly string commandText;
        private readonly CommandType commandType;
        private readonly DbContext context;
        private readonly List<SqlParameter> parameters;
        private readonly List<Func<IObjectContextAdapter, DbDataReader, IEnumerable>> resultSets;

        public List<IEnumerable> Execute()
        {
            var results = new List<IEnumerable>();
            var connection = context.Database.Connection;

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }

            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.CommandType = commandType;
                parameters?.ForEach(p => command.Parameters.Add(p));

                using (var reader = command.ExecuteReader())
                {
                    var adapter = ((IObjectContextAdapter)context);
                    foreach (var resultSet in resultSets)
                    {
                        results.Add(resultSet(adapter, reader));
                        reader.NextResult();
                    }
                }
            }

            return results;
        }

        public MultipleResultSetWrapper With<TResult>()
        {
            IEnumerable<TResult> _;

            return With(out _);
        }

        public MultipleResultSetWrapper With<TResult>(out IEnumerable<TResult> results)
        {
            IEnumerable<TResult> internalResults = new List<TResult>();

            resultSets.Add((adapter, reader) =>
            {
                ((List<TResult>)internalResults).AddRange(
                    adapter
                    .ObjectContext
                    .Translate<TResult>(reader)
                    .ToList());

                return internalResults as IEnumerable;
            });

            results = internalResults;

            return this;
        }
    }
}