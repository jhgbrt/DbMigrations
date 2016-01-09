using System;
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
    }
}