using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.ComponentModel;

namespace NReco.PivotData.Tests {
	
	public class PivotTableMDTests {

		PivotData getSamplePivotData(bool diag = false) {
			var vals = new List<object>();
			var keys = new List<uint[]>();
			for (uint a=0; a<3; a++)
				for (uint b=(diag?a:0); b<3; b++)
					for (uint c=(diag?b:0); c<3; c++) {
						keys.Add( new uint[] {a, b+3, c+6 });
						vals.Add(2);
					}

			var pvtState = new PivotDataState() {
				DimCount = 3,
				KeyValues = new object[] { "A1", "A2", "A3", "B1", "B2", "B3", "C1", "C2", "C3" },
				Values = vals.ToArray(),
				ValueKeys = keys.ToArray()
			};
			var pvt = new PivotData(new string[] {"A","B", "C"}, new CountAggregatorFactory(), true );
			pvt.SetState(pvtState);
			return pvt;
		}

		[Fact]
		public void PivotTableMD_2D() {
			var pvtData = getSamplePivotData();
			var pvtTbl = new PivotTableMD(
				new string[][] {
					new[] {"B"},
					new[] {"A"}
				}, pvtData);

			Assert.Equal(3, pvtTbl.AxesKeys[1].Length);
			Assert.Equal(3, pvtTbl.AxesKeys[0].Length);
			Assert.Equal("A1", pvtTbl.AxesKeys[1][0].DimKeys[0]);
			Assert.Equal("B1", pvtTbl.AxesKeys[0][0].DimKeys[0]);

			Assert.Equal(54, Convert.ToInt32( pvtTbl[null,null].Value) ); // global totals
			Assert.Equal(6, Convert.ToInt32( pvtTbl[0,0].Value) );
			Assert.Equal(18, Convert.ToInt32( pvtTbl[0,null].Value) );

			var pvtData2 = getSamplePivotData(true);
			var pvtTbl2 = new PivotTableMD(
				new string[][] {
					new [] {"A", "B"}, 
					new [] {"C"}
				}, pvtData2
			);
			Assert.Equal(6, pvtTbl2.AxesKeys[0].Length);
			Assert.Equal("A1", pvtTbl2.AxesKeys[0][0].DimKeys[0]);
			Assert.Equal("B1", pvtTbl2.AxesKeys[0][0].DimKeys[1]);
			Assert.Equal(20, Convert.ToInt32( pvtTbl2[null,null].Value) ); // global totals
			Assert.Equal(2, Convert.ToInt32( pvtTbl2[5,null].Value) );
		}

		[Fact]
		public void PivotTableMD_3D() {
			var pvtData = getSamplePivotData();
			var pvtTbl = new PivotTableMD(
				new string[][] {
					new[] {"B"},
					new[] {"A"},
					new[] {"C"}
				}, pvtData);

			Assert.Equal(54, Convert.ToInt32( pvtTbl[null,null].Value) ); // global totals
			Assert.Equal(18, Convert.ToInt32( pvtTbl[0,null,null].Value) );
			Assert.Equal(6, Convert.ToInt32( pvtTbl[0,0,null].Value) );
		}

		[Fact]
		public void PivotTableMD_SortAxisKeys() {
			var pvtData = getSamplePivotData(true);
			var pvtTbl = new PivotTableMD(
				new string[][] {
					new[] {"B"},
					new[] {"A"}
				}, pvtData);

			pvtTbl.SortAxisKeys(0, new int?[] {null, 0}, ListSortDirection.Ascending );
			Assert.Equal("B3", pvtTbl.AxesKeys[0][0].DimKeys[0]);
			Assert.Equal("B2", pvtTbl.AxesKeys[0][1].DimKeys[0]);
			Assert.Equal("B1", pvtTbl.AxesKeys[0][2].DimKeys[0]);

			pvtTbl.SortAxisKeys(0, null, ListSortDirection.Descending );
			Assert.Equal("B2", pvtTbl.AxesKeys[0][0].DimKeys[0]);

			pvtTbl = new PivotTableMD(
				new string[][] {
					new[] {"C"},
					new[] {"B"}
				}, pvtData);
			pvtTbl.SortAxisKeys(1, new int?[]{2,null}, ListSortDirection.Descending );
			Assert.Equal("B3", pvtTbl.AxesKeys[1][0].DimKeys[0]);
			Assert.Equal("B2", pvtTbl.AxesKeys[1][1].DimKeys[0]);
			Assert.Equal("B1", pvtTbl.AxesKeys[1][2].DimKeys[0]);

			pvtTbl.SortAxisKeys(1, null, ListSortDirection.Ascending );
			Assert.Equal("B2", pvtTbl.AxesKeys[1][2].DimKeys[0]);
		}

		[Fact]
		public void PivotTableMD_CustomKeysComparer() {
			var pvtData = getSamplePivotData();
			var pvtTbl = new PivotTableMD(
				new string[][] {
					new[] {"B"},
					new[] {"A"},
					new[] {"C"}
				}, pvtData, new IComparer<ValueKey>[] {
					null,
					NaturalSortKeyComparer.ReverseInstance,
					null
				});
			Assert.Equal("B1", pvtTbl.AxesKeys[0][0].DimKeys[0]);
			Assert.Equal("A3", pvtTbl.AxesKeys[1][0].DimKeys[0]);
			Assert.Equal("C1", pvtTbl.AxesKeys[2][0].DimKeys[0]);
		}


	}
}
