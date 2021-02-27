using System;
using System.Linq;
using Xunit;

namespace CollectionChanges.Tests
{
    public class CollectionChangesTests
    {
        [Theory]
        [InlineData(new int[] { 1, 2, 3 }, new int[] { 2, 3, 4 }, new int[] { 4 }, new int[] { 1 }, new int[] { 2, 3 })]
        [InlineData(new int[] { 1, 2, 3, 4 }, new int[] { 2, 3 }, new int[] { }, new int[] { 1, 4 }, new int[] { 2, 3 })]
        [InlineData(new int[] { }, new int[] { 2, 3, 4 }, new int[] { 2, 3, 4 }, new int[] { }, new int[] { })]
        [InlineData(new int[] { 1, 2, 3 }, new int[] { }, new int[] { }, new int[] { 1, 2, 3 }, new int[] { })]
        [InlineData(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 }, new int[] { }, new int[] { }, new int[] { 1, 2, 3 })]
        public void Test1(int[] current, int[] source, int[] addedExpected, int[] deletedExpected, int[] intersectExpected)
        {
            var changes = CollectionChanges.GetChanges(current, source, (t, s) => t == s);

            Assert.Equal(addedExpected, changes.AddedSources);
            Assert.Equal(deletedExpected, changes.DeletedCurrents);
            Assert.Equal(intersectExpected, changes.Intersect.Select(x => x.Source));
            Assert.Equal(intersectExpected, changes.Intersect.Select(x => x.Current));
        }
    }
}
