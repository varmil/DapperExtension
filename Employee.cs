using System;
using System.Data.Linq.Mapping;

namespace DapperExtension
{
    /// <summary>
    /// MySQL (Dapper) テスト用 Employees テーブル
    /// プロパティ名はデフォルトで大文字小文字区別です
    /// </summary>
    [Table(Name = "employees")]
    public class Employee
    {
        public int EmpNo { get; set; }
        public DateTime BirthDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public EGender Gender { get; set; }
        public DateTime HireDate { get; set; }

        public enum EGender
        {
            M,
            F
        }
    }
}
