using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.IO;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using NReco.PivotData;

namespace NReco.PivotData.Tests
{
    public class KeyTests
    {
		[Fact]
		public void TestHashCodeAndEquals() {
			var k1 = new ValueKey( "A", 5 );
			var k2 = new ValueKey( "A", 5 );

			Assert.Equal(k1.GetHashCode(), k2.GetHashCode());
			Assert.True(k1.Equals(k2));

			var k3 = new ValueKey( "row_1", "col_1" );
			var k4 = new ValueKey( "row_1", "col_1" );

			Assert.Equal(k3.GetHashCode(), k4.GetHashCode());
			Assert.NotEqual(k1.GetHashCode(), k3.GetHashCode());
			Assert.True(k3.Equals(k4));
			Assert.False(k4.Equals(k2));

			var k5 = new ValueKey( "row_2", "col_1" );
			Assert.NotEqual(k4.GetHashCode(), k5.GetHashCode());
			Assert.False(k5.Equals(k3));
			Assert.False(k5.Equals(null));

			Assert.True(Key.IsEmpty(Key.Empty));
			Assert.False(Key.IsEmpty(1));
			Assert.False(Key.IsEmpty(null));
			Assert.True( Key.Equals("A", "A") );
			Assert.False( Key.Equals("A", "B") );
			Assert.False( Key.Equals("A", null) );
			Assert.False( Key.Equals(null, "A") );
			Assert.True( Key.Equals(null, null) );
		}

		[Fact]
		public void NaturalSort() {
			var set1 = new object[] { "aab", "aaa", "aca" };
			var set2 = new object[] { 5, 4, DBNull.Value };
			var copy1 = new List<object>(set1);
			var copy2 = new List<object>(set2);
			copy1.Sort(NaturalSortKeyComparer.Instance);
			copy2.Sort(NaturalSortKeyComparer.Instance);

			Assert.Equal(set1[1], copy1[0]);
			Assert.Equal(set1[0], copy1[1]);
			
			Assert.Equal(set2[2], copy2[0]);
			Assert.Equal(set2[1], copy2[1]);


			var valKeySet = new ValueKey[] {
				new ValueKey( "aab", 5, new DateTime(2015, 1, 1) ),
				new ValueKey( "aaa", 5, new DateTime(2015, 1, 1) ),
				new ValueKey( "aab", 4, new DateTime(2015, 1, 1) ),
				new ValueKey( "aab", 5, new DateTime(2014, 1, 1) )
			};
			var valKeySetCopy = new List<ValueKey>(valKeySet);
			valKeySetCopy.Sort(NaturalSortKeyComparer.Instance);

			Assert.Equal(valKeySet[1], valKeySetCopy[0]);
			Assert.Equal(valKeySet[2], valKeySetCopy[1]);
			Assert.Equal(valKeySet[3], valKeySetCopy[2]);
		}

    }
}
