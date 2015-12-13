using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Text.RegularExpressions;

namespace DapperExtension
{
    /// <summary>
    /// データベースサーバへのコネクションを管理するクラス
    /// </summary>
    public static class ConnectionManager
    {
        private static readonly MySqlConnectionStringBuilder builder;

        static ConnectionManager()
        {
            // 接続情報を格納
            builder = new MySqlConnectionStringBuilder()
            {
                Server = "192.168.33.10",
                Port = 3306,
                UserID = "root",
                Password = "",
                Database = "employees"
            };

            // TODO: ymt 2015/12/09 実際の実装では専用クラスを作って、起動時にReflectionでまとめて舐める
            SetSnakeToPascal<Employee>();
        }

        /// <summary>
        /// コネクションプールからOpenした状態のコネクションを取得します
        /// </summary>
        public static void Get(Action<MySqlConnection> actions)
        {
            using (var connection = new MySqlConnection(builder.ToString()))
            {
                connection.Open();
                actions.Invoke(connection);
            }
        }

        /// <summary>
        /// カスタムマッピング用のメソッド。ただしdynamicで取得した場合は変換されない
        /// 参考：http://neue.cc/2012/12/11_390.html
        /// </summary>
        static void SetSnakeToPascal<T>()
        {
            var mapper = new CustomPropertyTypeMap(typeof(T), (type, columnName) =>
            {
                //snake_caseをPascalCaseに変換
                var propName = Regex.Replace(columnName, @"^(.)|_(\w)", x => x.Groups[1].Value.ToUpper() + x.Groups[2].Value.ToUpper());
                return type.GetProperty(propName);
            });

            SqlMapper.SetTypeMap(typeof(T), mapper);
        }
    }
}
