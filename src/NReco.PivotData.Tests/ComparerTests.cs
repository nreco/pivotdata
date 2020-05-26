using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using NReco.PivotData;

namespace NReco.PivotData.Tests
{
    public class ComparerTests
    {

		[Fact]
		public void SortAsComparerTest() {
			var ls = new object[] { "a", "b", "c"};
			var orderList = new[] {"b", "a", "c"};
			var sortAsCmp = new SortAsComparer(orderList);

			Array.Sort(ls, sortAsCmp);
			Assert.True( orderList.SequenceEqual(ls) );

			var ls2 = new object[] { 0, "a", "b", "c", "d"};
			Array.Sort(ls2, sortAsCmp);
			Assert.True( new object[]{"b","a","c", 0, "d"}.SequenceEqual(ls2) );
		}
	

    }
}
