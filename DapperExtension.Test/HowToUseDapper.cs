using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using MySql.Data.MySqlClient;
using Dapper;
using System.Text.RegularExpressions;
using DapperExtension;

namespace DapperExtension.Tests
{
    /// <summary>
    /// 基本的なDapperの使い方を示します
    /// 参考：https://gist.github.com/devlights/8779382
    ///
    /// 尚、テストに成功するためには以下のデータを予めDBに流し込んでおく必要があります。必要に応じて利用してください。
    /// 参考：https://github.com/datacharmer/test_db
    /// </summary>
    [TestFixture]
    public class HowToUseDapper
    {
        [Test]
        public void TypedMapping()
        {
            ConnectionManager.Get(connection =>
            {
                // Select文を発行 (マッピングクラスあり)
                //   通常のORマッピングライブラリと同様に、マッピング対象クラスを
                //   指定しての、結果取得も出来ます。
                //
                //   クエリの発行は、Query<T>メソッドで行います。
                //
                //var mappingObj = connection.Query<Employee>("SELECT * FROM employees LIMIT 1").FirstOrDefault();
                var mappingObj = connection.Select<Employee>(10001);

                Assert.AreEqual(10001, mappingObj.EmpNo);
                Assert.AreEqual(Employee.EGender.M, mappingObj.Gender);
                Assert.AreEqual(DateTime.Parse("1953-09-02 00:00:00.000"), mappingObj.BirthDate);
            });
        }

        [Test]
        public void DynamicMapping()
        {
            ConnectionManager.Get(connection =>
            {
                // Select文を発行 (マッピングクラスなし)
                //   通常のORマッピングライブラリでは、結果をマッピングするための
                //   クラスを作成して、そこに値を設定してくれるものが多いですが
                //   Dapperでは、.NETのdynamic型を利用して、動的オブジェクトで
                //   結果を返してくれる機能があります。これを利用すると、いちいち
                //   データクラスを作らなくても結果を取得できます。
                //
                //   クエリの発行は、Queryメソッドで行います。
                //
                dynamic dynamicObj = connection.Query("SELECT * FROM employees LIMIT 1").FirstOrDefault();

                // dynamicマッピングの場合はカスタムマッピングが適用されないので注意
                Assert.AreEqual(10001, dynamicObj.emp_no);
                Assert.AreEqual("Georgi", dynamicObj.first_name);
            });
        }

    }
}
