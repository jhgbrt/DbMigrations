using System;
using System.Collections.Generic;
using System.Linq;
using DbMigrations.Client.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbMigrations.UnitTests
{
    [TestClass]
    public class StringExtenstionsTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Words_Null_Throws()
        {
            string input = null;
            input.Words();
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Words_EmptyString_Throws()
        {
            string input = null;
            input.Words();
        }

        [TestMethod]
        public void Words_SingleWord_YieldsSingleWord()
        {
            string input = "String";
            var result = input.Words().ToArray();
            CollectionAssert.AreEqual(new[]{"String"}, result);
        }

        [TestMethod]
        public void Words_MultipleWords_YieldsWords()
        {
            string input = "SomeString";
            var result = input.Words().ToArray();
            var expected = new[] { "Some", "String" };
            CollectionAssert.AreEqual(expected, result, string.Format("expected '{0}' but was '{1}'", string.Join(",", expected), string.Join(",", result)));
        }

        [TestMethod]
        public void Words_Acronym_YieldsAcronym()
        {
            string input = "MD5";
            var result = input.Words().ToArray();
            var expected = new[] { "MD5" };
            CollectionAssert.AreEqual(expected, result, string.Format("expected '{0}' but was '{1}'", string.Join(",", expected), string.Join(",", result)));
        }

        [TestMethod]
        public void Words_StartsWithLowerCase_YieldsWords()
        {
            string input = "anotherProperty";
            var result = input.Words().ToArray();
            var expected = new[] { "another", "Property" };
            CollectionAssert.AreEqual(expected, result, string.Format("expected '{0}' but was '{1}'", string.Join(",", expected), string.Join(",", result)));
        }

        [TestMethod]
        public void Words_AcronymFollowedByWord_YieldsAsOneWord()
        {
            string input = "ABCAcronym";
            var result = input.Words().ToArray();
            var expected = new[] { "ABCAcronym" };
            CollectionAssert.AreEqual(expected, result, string.Format("expected '{0}' but was '{1}'", string.Join(",", expected), string.Join(",", result)));
        }

        [TestMethod]
        public void ToUpperCaseWithUnderscores()
        {
            var input = "SomeString";
            var expected = "SOME_STRING";
            var result = input.ToUpperCaseWithUnderscores();
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Eval_SimpleProperty()
        {
            var input = "Animal";
            var result = input.Eval(new {Animal = "fox"});
            Assert.AreEqual("fox", result);
        }

        [TestMethod]
        public void Eval_NestedProperty()
        {
            var input = "Animal.Name.Length";
            var result = input.Eval(new { Animal = new { Name = "fox" } });
            Assert.AreEqual("3", result);
        }

        [TestMethod]
        public void Eval_IntIndexedProperty()
        {
            var input = "Animal.Names[0]";
            var result = input.Eval(new { Animal = new { Names = new[] { "fox", "dog" } } });
            Assert.AreEqual("fox", result);
        }

        [TestMethod]
        public void Eval_FunctionCall()
        {
            var input = "string.Join(\",\", Animal.Names)";
            var result = input.Eval(new { Animal = new { Names = new[] { "fox", "dog" } } });
            Assert.AreEqual("fox,dog", result);
        }

        [TestMethod]
        public void Eval_ArithmeticExpression()
        {
            var input = "Quantity * Number";
            var result = input.Eval(
                new
                {
                    Quantity = 2,
                    Number = 3
                });
            Assert.AreEqual("6", result);
        }

        [TestMethod]
        public void Eval_ArithmeticExpression2()
        {
            var input = "A * B + C";
            var result = input.Eval(
                new
                {
                    A = 2,
                    B = 3,
                    C = 1
                });
            Assert.AreEqual("7", result);
        }


        [TestMethod]
        public void Eval_DictionaryIndexedProperty()
        {
            var input = @"Animal.Names[""first""]";
            var result = input.Eval(new { Animal = new { Names = new Dictionary<string,string>{{"first", "fox"}}} });
            Assert.AreEqual("fox", result);
        }

        [TestMethod]
        public void Eval_DictionaryIndexedProperty_SingleQuotes()
        {
            var input = @"Animal.Names['first']";
            var result = input.Eval(new { Animal = new { Names = new Dictionary<string, string> { { "first", "fox" } } } });
            Assert.AreEqual("fox", result);
        }

        [TestMethod]
        public void Eval_MemberFunction()
        {
            var input = @"Member.MyFunctionCall(5)";
            var result = input.Eval(new {Member = new MyClass()});
            Assert.AreEqual("5", result);
        }


        class MyClass
        {
            public int MyFunctionCall(int arg)
            {
                return arg;
            }
        }
    }
}