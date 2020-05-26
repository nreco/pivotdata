using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.ComponentModel;

namespace NReco.PivotData.Tests {
	
	public class PivotTableTests {

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
			/*foreach (var v in pvt.AllValues) {
				Console.WriteLine(v.Key.ToString() );
			}
			Console.WriteLine("*******");*/
			return pvt;
		}

		[Fact]
		public void PivotTable_Simple() {
			var pvtData = getSamplePivotData();
			var pvtTbl = new PivotTable(new string[] {"B"}, new string[] {"A"}, pvtData);
			Assert.Equal(3, pvtTbl.ColumnKeys.Length);
			Assert.Equal(3, pvtTbl.RowKeys.Length);
			Assert.Equal("A1", pvtTbl.ColumnKeys[0].DimKeys[0]);
			Assert.Equal("B1", pvtTbl.RowKeys[0].DimKeys[0]);

			Assert.Equal(54, Convert.ToInt32( pvtTbl[null,null].Value ) ); // global totals
			Assert.Equal(6, Convert.ToInt32( pvtTbl[0,0].Value ) );
			Assert.Equal(18, Convert.ToInt32( pvtTbl[0,null].Value ) );

			var pvtData2 = getSamplePivotData(true);
			var pvtTbl2 = new PivotTable(new string[] {"A", "B"}, new string[] {"C"}, pvtData2);
			Assert.Equal(6, pvtTbl2.RowKeys.Length);
			Assert.Equal("A1", pvtTbl2.RowKeys[0].DimKeys[0]);
			Assert.Equal("B1", pvtTbl2.RowKeys[0].DimKeys[1]);
			Assert.Equal(20, Convert.ToInt32( pvtTbl2[null,null].Value ) ); // global totals
			Assert.Equal(2, Convert.ToInt32( pvtTbl2[5,null].Value) );

			var pvtTbl3 = new PivotTable(new string[] { "A", "B", "C" }, null, pvtData);
			Assert.Equal(6, Convert.ToInt32(
				pvtTbl3.GetValue( new ValueKey(new object[] {"A1", "B1", Key.Empty }), null ).Value));
			Assert.Equal(18, Convert.ToInt32(
				pvtTbl3.GetValue(new ValueKey(new object[] { Key.Empty, "B1", Key.Empty }), null).Value));
			Assert.Equal(54, Convert.ToInt32(
				pvtTbl3[null, null].Value));

			var diagPvtTbl = new PivotTable(new []{"B"}, new []{"B"}, pvtData);
			for (int i = 0; i < diagPvtTbl.RowKeys.Length; i++) {
				for (int j = 0; j < diagPvtTbl.ColumnKeys.Length; j++) {
					if (i == j) {
						Assert.NotEqual(0, (int)diagPvtTbl[i,j].Count); // "Incorrect diagonal pivot table"
					} else {
						Assert.Equal(0, (int)diagPvtTbl[i,j].Count); // "Incorrect diagonal pivot table"
					}
				}
			}
		}

		[Fact]
		public void PivotTable_SortRowKeys() {
			var pvtData = getSamplePivotData(true);
			var pvtTbl = new PivotTable(new string[] {"B"}, new string[] {"A"}, pvtData);
			pvtTbl.SortRowKeys(0, ListSortDirection.Ascending );
			Assert.Equal("B3", pvtTbl.RowKeys[0].DimKeys[0]);
			Assert.Equal("B2", pvtTbl.RowKeys[1].DimKeys[0]);
			Assert.Equal("B1", pvtTbl.RowKeys[2].DimKeys[0]);

			pvtTbl.SortRowKeys(null, ListSortDirection.Descending );
			Assert.Equal("B2", pvtTbl.RowKeys[0].DimKeys[0]);
		}

		[Fact]
		public void PivotTable_SortColumnKeys() {
			var pvtData = getSamplePivotData(true);
			var pvtTbl = new PivotTable(new string[] {"C"}, new string[] {"B"}, pvtData);
			pvtTbl.SortColumnKeys(2, ListSortDirection.Descending );
			pvtTbl.SortColumnKeys(2, 0, ListSortDirection.Descending ); // the same
			Assert.Equal("B3", pvtTbl.ColumnKeys[0].DimKeys[0]);
			Assert.Equal("B2", pvtTbl.ColumnKeys[1].DimKeys[0]);
			Assert.Equal("B1", pvtTbl.ColumnKeys[2].DimKeys[0]);

			pvtTbl.SortColumnKeys(null, ListSortDirection.Ascending );
			Assert.Equal("B2", pvtTbl.ColumnKeys[2].DimKeys[0]);

			pvtTbl.SortColumnKeys(null, 0, ListSortDirection.Ascending );
			Assert.Equal("B2", pvtTbl.ColumnKeys[2].DimKeys[0]);
		}

		[Fact]
		public void PivotTable_SortPerformance() {
			var pvtData = new PivotData(new[] {"a","b"},
					new CompositeAggregatorFactory(
						new CountAggregatorFactory(), 
						new SumAggregatorFactory("d")
					),
					true);
			pvtData.ProcessData( DataUtils.getSampleData(50000), DataUtils.getProp);

			var pvtTbl = new PivotTable(new[] {"b"}, new []{"a"}, pvtData);
			pvtTbl.SortRowKeys(0, ListSortDirection.Descending);

			pvtTbl.SortRowKeys(null, ListSortDirection.Descending);
		}

		[Fact]
		public void PivotTable_LargeWithTotals() {
			// test for UlongCache impl
			var pvtData = new PivotData(Repeat(63, i => "a" + i.ToString()), new CountAggregatorFactory());
			pvtData.ProcessData(new int[] { 0, 1, 2, 3, 4, }, (row, field) => {
				var fldIdx = Int32.Parse(field.Substring(1));
				var rowIdx = (int)row;
				if (fldIdx < rowIdx)
					return 0;
				return rowIdx;
			});
			Check(new PivotTable(pvtData.Dimensions, null, pvtData));
			Check(new PivotTable(Repeat(62, i => "a" + i.ToString()), new string[] { "a62" }, pvtData));


			// test for BitArrayCache impl
			var pvtData2 = new PivotData(Repeat(128, i => "a" + i.ToString()), new CountAggregatorFactory());
			pvtData2.ProcessData(new int[] { 0, 1, 2, 3, 4, }, (row, field) => {
				var fldIdx = Int32.Parse(field.Substring(1));
				var rowIdx = (int)row;
				if (fldIdx < rowIdx)
					return 0;
				return rowIdx;
			});
			Check(new PivotTable(pvtData2.Dimensions, null, pvtData2));
			Check(new PivotTable(Repeat(126, i => "a" + i.ToString()), new string[] { "a126", "a127"}, pvtData2));

			void Check(IPivotTable pvtTbl) {
				Assert.Equal(3,
					Convert.ToInt32(pvtTbl.GetValue(
						new ValueKey(Repeat<object>( pvtTbl.Rows.Length, i => Key.Empty, new object[] { 0, 0, 0 })),
						new ValueKey(Repeat<object>( pvtTbl.Columns.Length, i => Key.Empty))
					).Value) );
				Assert.Equal(4,
					Convert.ToInt32( pvtTbl.GetValue(
						new ValueKey(Repeat<object>( pvtTbl.Rows.Length, i => Key.Empty, new object[] { 0, 0 })),
						new ValueKey(Repeat<object>( pvtTbl.Columns.Length, i => Key.Empty))
					).Value) );
				Assert.Equal(5,
					Convert.ToInt32( pvtTbl.GetValue(
						new ValueKey(Repeat<object>(pvtTbl.Rows.Length, i => Key.Empty)),
						new ValueKey(Repeat<object>(pvtTbl.Columns.Length, i => Key.Empty))
					).Value) );
			}

			T[] Repeat<T>(int n, Func<int,T> getVal, params T[] explicitVals) {
				var res = new T[n];
				for (int i = 0; i < res.Length; i++) {
					if (i < explicitVals.Length) {
						res[i] = explicitVals[i];
					} else {
						res[i] = getVal(i);
					}
				}
				return res;
			}
		}

	}
}
