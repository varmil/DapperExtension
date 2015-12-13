using System;
using Dapper;
using System.Linq;
using IDbConnection = System.Data.Common.DbConnection;
using IDbTransaction = System.Data.Common.DbTransaction;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;

namespace DapperExtension
{
    /// <summary>
    /// 生のSQLを書くことなくクエリ発行できるようにするユーティリティクラス
    /// </summary>
    public static class SqlExtension
    {
        static readonly ConcurrentDictionary<RuntimeTypeHandle, string> TypeTableName = new ConcurrentDictionary<RuntimeTypeHandle, string>();

        static ConcurrentDictionary<Type, List<string>> paramNameCache = new ConcurrentDictionary<Type, List<string>>();

        /// <summary>
        /// implementation from Dapper.Contrib
        /// キャッシュしていない状態だと、実行時間はローカルPCで50μs程度
        /// シャーディングなどでテーブル名を変更する必要がある場合は更に上の層でコントロールする
        /// </summary>
        static string GetTableName(Type type)
        {
            string name;

            // キャッシュを確認
            if (TypeTableName.TryGetValue(type.TypeHandle, out name)) return name;

            //NOTE: This as dynamic trick should be able to handle both our own Table-attribute as well as the one in EntityFramework
            var tableAttr = type
                .GetCustomAttributes(false)
                .SingleOrDefault(attr => attr.GetType().Name == "TableAttribute") as dynamic;

            // アトリビュートが存在する場合はそれを用いて、存在しない場合はデフォルトの補完
            if (tableAttr != null)
            {
                name = tableAttr.Name;
            }
            else
            {
                name = type.Name + "s";
            }

            TypeTableName[type.TypeHandle] = name;
            return name;
        }

        /// <summary>
        /// implementation from Dapper.Rainbow
        /// REVIEW: ymt 結局インテリセンス効かないし無駄にキャッシュするしなので、ラムダ式にして最低限だけqueryableにする
        /// </summary>
        static List<string> GetParamNames(object o)
        {
            var parameters = o as DynamicParameters;
            if (parameters != null)
            {
                return parameters.ParameterNames.ToList();
            }

            List<string> paramNames;
            if (!paramNameCache.TryGetValue(o.GetType(), out paramNames))
            {
                paramNames = new List<string>();
                foreach (var prop in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetGetMethod(false) != null))
                {
                    var attribs = prop.GetCustomAttributes(typeof(IgnorePropertyAttribute), true);
                    var attr = attribs.FirstOrDefault() as IgnorePropertyAttribute;
                    if (attr == null || (!attr.Value))
                    {
                        paramNames.Add(prop.Name);
                    }
                }
                paramNameCache[o.GetType()] = paramNames;
            }
            return paramNames;
        }

        //public static T Select<T>(this IDbConnection connection, dynamic id) where T : class
        //{
        //    return connection.Query<T>("SELECT * FROM `" + GetTableName(typeof(T)) + "` WHERE id = @id", new { id }).FirstOrDefault();
        //}

        // TODO: 2015/12/11 簡単なwhere条件だけの複数レコード取得。フィールドを限定してレコード取得。

        /// <summary>
        /// Grab a record with where clause from the DB 
        /// </summary>
        /// <param name="where"></param>
        public static T Select<T>(this IDbConnection connection, object where)
        {
            return (All<T>(connection, where)).FirstOrDefault();
        }

        /// <summary>
        /// Return All record
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static IEnumerable<T> All<T>(this IDbConnection connection, object where = null)
        {
            var sql = "SELECT * FROM " + GetTableName(typeof(T));
            if (where == null) return connection.Query<T>(sql);
            var paramNames = GetParamNames(where);
            var w = string.Join(" AND ", paramNames.Select(p => "`" + p + "` = @" + p));
            return connection.Query<T>(sql + " WHERE " + w, where);
        }
    }
}
