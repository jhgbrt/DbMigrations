using System;
using System.Linq;
using DbMigrations.Client.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbMigrations.UnitTests
{
    [TestClass]
    public class ExtractParametersFromQueryTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Parameters_Null_Throws()
        {
            string input = null;
            input.Parameters("@");
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Parameters_EmptyString_Throws()
        {
            string input = null;
            input.Parameters("@");
        }

        [TestMethod]
        public void Parameters_SingleWord_YieldsEmpty()
        {
            string input = "String";
            var result = input.Parameters("@").ToArray();
            CollectionAssert.AreEqual(new string[] { }, result);
        }
        [TestMethod]
        public void Parameters_SingleParam_YieldsParam()
        {
            string input = "@String";
            var result = input.Parameters("@").ToArray();
            CollectionAssert.AreEqual(new string[] { "String" }, result);
        }

        [TestMethod]
        public void Parameters_MultipleParams_YieldsAllParameters()
        {
            string input = "SELECT * FROM Table \r\nWHERE MyField = @MyParam\r\n AND MyOtherField = @MyOtherParam";
            var result = input.Parameters("@").ToArray();
            var expected = new[] { "MyParam", "MyOtherParam" };
            CollectionAssert.AreEqual(expected, result, string.Format("expected '{0}' but was '{1}'", string.Join(",", expected), string.Join(",", result)));
        }

        [TestMethod]
        public void Parameters_ParameterCanContainDigit()
        {
            string input = "@MD5";
            var result = input.Parameters("@").ToArray();
            var expected = new[] { "MD5" };
            CollectionAssert.AreEqual(expected, result, string.Format("expected '{0}' but was '{1}'", string.Join(",", expected), string.Join(",", result)));
        }

        [TestMethod]
        public void FormatWithTest()
        {
            var input = "{Number} quick brown {Animals} jumped (\r\n{{}} over the lazy {OtherAnimal} on {Day:d/MM/yyyy} at noon";
            var parameters = new
            {
                Number = 2,
                Animals = "Foxes", 
                OtherAnimal = "Dog", 
                Day = new DateTime(2010, 11, 1)
            };
            var result = input.FormatWith(parameters);
            Assert.AreEqual("2 quick brown Foxes jumped (\r\n{} over the lazy Dog on 1/11/2010 at noon", result);
        }
        [TestMethod]
        public void FormatWith_Multiline()
        {
            var input = "CREATE TABLE {TableName} (\r\n" +
                        "     ScriptName nvarchar2(255) not null, \r\n" +
                        "     MD5 nvarchar2(32) not null, \r\n" +
                        "     ExecutedOn date not null, \r\n" +
                        "     Content CLOB not null, \r\n" +
                        "     CONSTRAINT PK_Migrations PRIMARY KEY (ScriptName)\r\n" +
                        ")";

            var result = input.FormatWith(new {TableName = "Migrations"});
            Assert.AreEqual("CREATE TABLE Migrations (\r\n" +
                        "     ScriptName nvarchar2(255) not null, \r\n" +
                        "     MD5 nvarchar2(32) not null, \r\n" +
                        "     ExecutedOn date not null, \r\n" +
                        "     Content CLOB not null, \r\n" +
                        "     CONSTRAINT PK_Migrations PRIMARY KEY (ScriptName)\r\n" +
                        ")", result);

        }
    }
}