using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Data.Common;
using Dapper;

namespace DAO
{
    public static class DataBaseContext
    {
        
        public static IDbConnection DbService(string dbProvider, string dbConnectionString)
        {   
            DbProviderFactory  df = DbProviderFactories.GetFactory(dbProvider);
            
            var connection = df.CreateConnection();
            connection.ConnectionString = dbConnectionString;
            connection.Open();

            return connection;
        }

        /// <summary>
        /// 分页查询列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cnn">连接对象</param>
        /// <param name="sql">查询语句</param>
        /// <param name="param">动态参数</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页显示数量</param>
        /// <returns></returns>
        public static PagedList<dynamic> PagedList(this IDbConnection cnn, string sql, string orderString, int pageIndex, int pageSize)
        {   
            return PagedList(cnn, sql, null, orderString, pageIndex, pageSize);
        }

        /// <summary>
        /// 分页查询列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cnn">连接对象</param>
        /// <param name="sql">查询语句</param>
        /// <param name="param">动态参数</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页显示数量</param>
        /// <returns></returns>
        public static PagedList<dynamic> PagedList(this IDbConnection cnn, string sql, object param, string orderString, int pageIndex, int pageSize)
        {   
            if (!(cnn is System.Data.SqlClient.SqlConnection))
                return PagedList_Oracle(cnn, sql, param, orderString, pageIndex, pageSize);

            Regex reg = null;
            reg = new Regex(@"\s+from\s+.*?(\s+where\s+|\s+order\s+|\s+group\s+)|\s+from\s+.+", RegexOptions.IgnoreCase);
            string tableName = reg.Match(sql).Value;
            tableName = Regex.Replace(tableName.ToLower(), @"\s+from\s+|\s+where\s+|\[|\]|\s+order\s+|\s+group\s+", "");


            reg = new Regex(@"select\s+.*?\s+from\s+", RegexOptions.IgnoreCase);
            string fields = reg.Match(sql).Value;
            fields = Regex.Replace(fields.ToLower(), @"select\s+|\s+from\s+|\[|\]", "");

            List<string> fieldsList = new List<string>();
            fieldsList.AddRange(fields.Split(new string[] { "," }, System.StringSplitOptions.RemoveEmptyEntries));

            reg = new Regex(@"\s+where\s+.*?(\s+order\s+|\s+group\s+)|\s+where\s+.+", RegexOptions.IgnoreCase);
            string condition = reg.Match(sql).Value;
            condition = Regex.Replace(condition.ToLower(), @"\s+where\s+|\s+order\s+|\s+group\s+|\[|\]", "");


            reg = new Regex(@"\s+order\s+by\s+.*?\s(desc|asc)|\s+order\s+by\s+.*?\s", RegexOptions.IgnoreCase);
            string order = reg.Match(sql).Value;
            order = Regex.Replace(order.ToLower(), @"\s+order\s+by\s+|\[|\]", "");


            string totalSQL = "select count(*) from " + tableName;
            if (!string.IsNullOrEmpty(condition))
                totalSQL += " where " + condition;

            int totalItemCount = cnn.ExecuteScalar<int>(totalSQL, param);

            List<dynamic> items = new List<dynamic>();

            if (pageIndex == 0)
                pageIndex += 1;

            if (pageIndex > 0 && pageSize > 0)
            {
                int beginIndex = (pageIndex - 1) * pageSize;
                int endIndex = beginIndex + pageSize;

                if (fieldsList.Contains("*"))
                    fieldsList.Remove("*");

                string orderByFields = "";

                if (!string.IsNullOrWhiteSpace(orderString))
                {
                    orderByFields = orderString;
                }
                else if (fieldsList.Count > 0)
                {
                    orderByFields = fieldsList[0];
                }

                string pageSQL = "SELECT * ";
                pageSQL += " FROM (SELECT a.*, ";
                pageSQL += " ROW_NUMBER() OVER (ORDER BY " + orderByFields + ") AS RowNumber ";
                pageSQL += " FROM (" + sql + ") a) DataPage ";
                pageSQL += " WHERE RowNumber > " + beginIndex + " AND RowNumber <= " + endIndex;
                pageSQL += " ORDER BY " + orderByFields;

                var itemsData = cnn.Query(pageSQL, param);

                items.AddRange(itemsData);
            }
            else
            {
                var itemsData = cnn.Query(sql, param);
                items.AddRange(itemsData);
            }

            var result = new PagedList<dynamic>(items.ToArray(), pageIndex, pageSize, totalItemCount);
            return result;
        }


        /// <summary>
        /// 分页查询列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="cnn">连接对象</param>
        /// <param name="sql">查询语句</param>
        /// <param name="param">动态参数</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页显示数量</param>
        /// <returns></returns>
        private static PagedList<dynamic> PagedList_Oracle(this IDbConnection cnn, string sql, object param, string orderString, int pageIndex, int pageSize)
        {   
            Regex reg = null;
            reg = new Regex(@"\s+from\s+.*?(\s+where\s+|\s+order\s+|\s+group\s+)|\s+from\s+.+", RegexOptions.IgnoreCase);
            string tableName = reg.Match(sql).Value;
            tableName = Regex.Replace(tableName.ToLower(), @"\s+from\s+|\s+where\s+|\[|\]|\s+order\s+|\s+group\s+", "");


            reg = new Regex(@"select\s+.*?\s+from\s+", RegexOptions.IgnoreCase);
            string fields = reg.Match(sql).Value;
            fields = Regex.Replace(fields.ToLower(), @"select\s+|\s+from\s+|\[|\]", "");

            List<string> fieldsList = new List<string>();
            fieldsList.AddRange(fields.Split(new string[] { "," }, System.StringSplitOptions.RemoveEmptyEntries));

            reg = new Regex(@"\s+where\s+.*?(\s+order\s+|\s+group\s+)|\s+where\s+.+", RegexOptions.IgnoreCase);
            string condition = reg.Match(sql).Value;
            condition = Regex.Replace(condition.ToLower(), @"\s+where\s+|\s+order\s+|\s+group\s+|\[|\]", "");


            reg = new Regex(@"\s+order\s+by\s+.*?\s(desc|asc)|\s+order\s+by\s+.*?\s", RegexOptions.IgnoreCase);
            string order = reg.Match(sql).Value;
            order = Regex.Replace(order.ToLower(), @"\s+order\s+by\s+|\[|\]", "");


            string totalSQL = "select count(*) from " + tableName;
            if (!string.IsNullOrEmpty(condition))
                totalSQL += " where " + condition;

            int totalItemCount = cnn.ExecuteScalar<int>(totalSQL, param);

            List<dynamic> items = new List<dynamic>();

            if (pageIndex == 0)
                pageIndex += 1;

            if (pageIndex > 0 && pageSize > 0)
            {
                int beginIndex = (pageIndex - 1) * pageSize;
                int endIndex = beginIndex + pageSize;

                if (fieldsList.Contains("*"))
                    fieldsList.Remove("*");

                string orderByFields = "";

                if (!string.IsNullOrWhiteSpace(orderString))
                {
                    orderByFields = orderString;
                }
                else if (fieldsList.Count > 0)
                {
                    orderByFields = fieldsList[0];
                }

                string pageSQL = "select * from(select a.*,ROWNUM rn from(" + sql + ") a where ROWNUM <=(" + (beginIndex + pageSize) + ")) where rn > " + beginIndex;
                
                var itemsData = cnn.Query(pageSQL, param);

                items.AddRange(itemsData);
            }
            else
            {
                var itemsData = cnn.Query(sql, param);
                items.AddRange(itemsData);
            }

            var result = new PagedList<dynamic>(items.ToArray(), pageIndex, pageSize, totalItemCount);
            return result;
        }




        /// <summary>
        /// 执行脚本
        /// </summary>
        /// <param name="script">脚本</param>
        /// <returns>影响行数</returns>
        public static int Script(this IDbConnection cnn, string script)
        {
            
            Regex regex = new Regex("^GO", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Regex regexOracle = new Regex(@"\s*;\s*(?=(?:CREATE|ALTER|DROP|RENAME|TRUNCATE)\s|\s*$)", RegexOptions.IgnoreCase);

            string[] lines = null;
            
            if(cnn is System.Data.SqlClient.SqlConnection)
                lines = regex.Split(script);
            else
                lines = regexOracle.Split(script);
            
            int rows = 0;
            var transaction = cnn.BeginTransaction();
            using (var cmd = cnn.CreateCommand())
            {
                cmd.Transaction = transaction;

                foreach (string line in lines)
                {
                    if (line.Length > 0)
                    {
                        cmd.CommandText = line;
                        cmd.CommandType = CommandType.Text;

                        try
                        {   
                           rows = cmd.ExecuteNonQuery();
                        }
                        catch (SqlException)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }

            transaction.Commit();
            
            return rows;
        }
        
    }
}
