using System;
using System.Collections.Generic;
using System.Linq;
using DbMigrations.Client.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbMigrations.UnitTests
{
    [TestClass]
    public class FullOuterJoinTests
    {
        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void FullOuterJoin_NullLeftListAndNullRightList_ThrowsArgumentException()
        {
            int[] left = null;
            IEnumerable<string> right = null;
            left.FullOuterJoin(right, i => i.ToString(), s => s).ToList();
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void FullOuterJoin_NullRightList_ThrowsArgumentException()
        {
            int[] left = null;
            var right = Enumerable.Empty<string>();
            left.FullOuterJoin(right, i => i.ToString(), s => s).ToList();
        }
        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void FullOuterJoin_NullLeftList_ThrowsArgumentException()
        {
            var migrations = Enumerable.Empty<int>();
            string[] scripts = null;
            migrations.FullOuterJoin(scripts, i => i.ToString(), s => s).ToList();
        }


        [TestMethod]
        public void FullOuterJoin_EquivalentLists_ZipsAllItems()
        {
            var left = new[] {1, 2, 3};
            var right = new[] {"1", "2", "3"};

            var expected = new []
            {
                Joined.Create("1", 1, "1"), 
                Joined.Create("2", 2, "2"), 
                Joined.Create("3", 3, "3") 
            };

            var result = left.FullOuterJoin(right, i => i.ToString(), s => s).ToList();

            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void FullOuterJoin_LeftListIsLonger_PadsRightList()
        {
            var left = new[] { 1, 2, 3 };
            var right = new[] { "1" };

            var expected = new[]
            {
                Joined.Create("1", 1, "1"), 
                Joined.Create("2", 2, (string)null), 
                Joined.Create("3", 3, (string)null) 
            };

            var result = left.FullOuterJoin(right, i => i.ToString(), s => s).ToList();

            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void FullOuterJoin_RightListIsLonger_PadsLeftList()
        {
            var left = new[] { 1 };
            var right = new[] { "1", "2", "3" };

            var expected = new[]
            {
                Joined.Create("1", 1, "1"), 
                Joined.Create("2", 0, "2"), 
                Joined.Create("3", 0, "3"), 
            };

            var result = left.FullOuterJoin(right, i => i.ToString(), s => s).ToList();

            CollectionAssert.AreEqual(expected, result);

        }

        [TestMethod]
        public void FullOuterJoin_MissingItemInRightList_ItemIsInserted()
        {
            var left = new[] { 1, 2, 3 };
            var right = new[] { "1", "3" };

            var expected = new[]
            {
                Joined.Create("1", 1, "1"), 
                Joined.Create("2", 2, (string)null), 
                Joined.Create("3", 3, "3"), 
            };

            var result = left.FullOuterJoin(right, i => i.ToString(), s => s).ToList();

            CollectionAssert.AreEqual(expected, result);
        }
        [TestMethod]
        public void FullOuterJoin_MissingItemInLeftList_ItemIsInserted()
        {
            var left = new int?[] { 1, 3 };
            var right = new[] { "1", "2","3" };

            var expected = new[]
            {
                Joined.Create("1", (int?)1, "1"), 
                Joined.Create("2", (int?)null, "2"), 
                Joined.Create("3", (int?)3, "3"), 
            };

            var result = left.FullOuterJoin(right, i => i.ToString(), s => s).OrderBy(j => j.Key).ToList();

            CollectionAssert.AreEqual(expected, result, string.Join(";", result.Select(x => x.ToString())));
        }
    }
}
