using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Xunit;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace NReco.PivotData.Tests {
	
	public class PivotDataTests {

		ITestOutputHelper Output;
		
		public PivotDataTests(ITestOutputHelper output) {
			Output = output;
		}

		[Fact]
		public void PivotData_1D() {
			var pvtData = new PivotData(new string[]{"a"}, new SumAggregatorFactory("z"), true);
			Func<object,string,object> getVal = (o,f) => f=="a" ? ((int)o%2)==0 ? "A" : "B" : o;
			pvtData.ProcessData( new[] { 1 }, getVal );
			pvtData.ProcessData( new[] { 2, 3}, getVal );

			Assert.Equal(6M, pvtData[ValueKey.Empty1D].Value);
			Assert.Equal(4M, pvtData["B"].Value);

			// test lazy-totals = false for 1D
			var pvtData2 = new PivotData(new string[]{"a"}, new SumAggregatorFactory("z"), false);
			pvtData2.ProcessData( new[] { 1, 2}, getVal );
			pvtData2.ProcessData( new[] { 3}, getVal );
			Assert.Equal(6M, pvtData2[ValueKey.Empty1D].Value);
			Assert.Equal(4M, pvtData2["B"].Value);
		}


		[Fact]
		public void PivotData_2D() {
			var testData = generateData();

			var pvtData1 = new PivotData(new string[] { "name", "qty" }, new CountAggregatorFactory(), testData );
			Assert.Equal(2, pvtData1.GetDimensionKeys()[0].Length );
			Assert.Equal(10, pvtData1.GetDimensionKeys()[1].Length );
			foreach (var cKey in pvtData1.GetDimensionKeys()[0])
				foreach (var rKey in pvtData1.GetDimensionKeys()[1]) { 
					var v = pvtData1[cKey, rKey];
					if (v.Count > 0) { 
						Assert.Equal(100, Convert.ToInt32( v.Value ) );
					}
				}


			var pvtData = new PivotData(new string[] { "name", "date" }, new SumAggregatorFactory("i"), testData );

			Assert.Equal(2, pvtData.GetDimensionKeys()[0].Length );
			Assert.Equal(42, pvtData.GetDimensionKeys()[1].Length );

			var rowTest0Totals = new ValueKey( "Test0", Key.Empty );
			Assert.Equal( 1000M, pvtData[rowTest0Totals].Value );

			// calc test
			var calcData = new object[5][] {
				new object[] {"A", 10, 50},
				new object[] {"A", 15, 40},
				new object[] {"B", 20, 50},
				new object[] {"B", 25, 60},
				new object[] {"C", 10, 0}
			};
			Func<object,string,object> getVal = (r, f) => {
				return ((object[])r)[Convert.ToInt32(f)];
			};
			var countPvtData = new PivotData(new string[]{ "0", "1" }, new CountAggregatorFactory());
			countPvtData.ProcessData(calcData, getVal);
			Assert.Equal(2, Convert.ToInt32( countPvtData["A", Key.Empty].Value ) );
			Assert.Equal(1, Convert.ToInt32( countPvtData["C", Key.Empty].Value ) );

			var avgPvtData = new PivotData(new string[]{"0", "1"}, new AverageAggregatorFactory("2"));
			avgPvtData.ProcessData(calcData, getVal);
			Assert.Equal(45M, avgPvtData["A", Key.Empty].Value );
			Assert.Equal(0M, avgPvtData["C", Key.Empty].Value );
			Assert.Equal(25M, avgPvtData[Key.Empty, 10].Value );
		}

		[Fact]
		public void PivotData_3D() {
			var pvtData = new PivotData(new string[] {"a", "year", "month" }, 
					new CountAggregatorFactory());
			pvtData.ProcessData( SampleGenerator(100000), GetRecordValue );
			
			Assert.Equal(50000, Convert.ToInt32(
				pvtData[pvtData.GetDimensionKeys()[0][0], Key.Empty,Key.Empty ].Value ) );

			Assert.Equal(100000, Convert.ToInt32(
				pvtData[Key.Empty,pvtData.GetDimensionKeys()[1][0],Key.Empty ].Value ) );

			Assert.Equal(30240, Convert.ToInt32(
				pvtData[Key.Empty,Key.Empty,pvtData.GetDimensionKeys()[2][0] ].Value ) );
			
			Assert.Equal(100000, Convert.ToInt32(
				pvtData[Key.Empty,Key.Empty,Key.Empty ].Value ) );

			// incremental processing
			pvtData.ProcessData( SampleGenerator(10000), GetRecordValue );
			Assert.Equal(55000,	Convert.ToInt32( 
				pvtData[pvtData.GetDimensionKeys()[0][0], Key.Empty,Key.Empty ].Value ) );
			Assert.Equal(40240, Convert.ToInt32(
				pvtData[Key.Empty,Key.Empty,pvtData.GetDimensionKeys()[2][0] ].Value ) );

			Assert.Equal(110000, Convert.ToInt32(
				pvtData[Key.Empty,Key.Empty,Key.Empty ].Value ) );
		}

		[Fact]
		public void PivotData_Merge() {
			var pvtData = new PivotData(new string[] { "a", "year", "month" }, 
					new AverageAggregatorFactory("i"));

			var pvtData1 = new PivotData(new string[] { "a", "year", "month" }, 
					new AverageAggregatorFactory("i"));
			var pvtData2 = new PivotData(new string[] {  "a", "year", "month" }, 
					new AverageAggregatorFactory("i"));
			var pvtData3 = new PivotData(new string[] { "a", "b" }, new CountAggregatorFactory());

			Assert.Throws<ArgumentException>(() => {
				pvtData1.Merge(pvtData3);
			});

			pvtData.ProcessData( SampleGenerator(20000), GetRecordValue );
			pvtData1.ProcessData( SampleGenerator(10000), GetRecordValue );
			pvtData2.ProcessData( SampleGenerator(10000, 10000), GetRecordValue );

			pvtData1.Merge(pvtData2);

			foreach (var v in pvtData.AllValues) {
				var aggr = pvtData[v.Key];
				var aggrMerged = pvtData1[v.Key];
				Assert.Equal(aggr.Count, aggrMerged.Count);
				Assert.Equal(aggr.Value, aggrMerged.Value);
			}
			Assert.Equal( pvtData.GetDimensionKeys()[0].Length, pvtData1.GetDimensionKeys()[0].Length );
			Assert.Equal( pvtData.GetDimensionKeys()[1].Length, pvtData1.GetDimensionKeys()[1].Length );
			Assert.Equal( pvtData.GetDimensionKeys()[2].Length, pvtData1.GetDimensionKeys()[2].Length );
		}

		[Fact]
		public void PivotData_GetState() {
			var testData = generateData();
			var pvtData = new PivotData(new string[] { "name", "qty" }, new CountAggregatorFactory(), testData, false );
			var pvtState = pvtData.GetState();

			var pvtData2 = new PivotData(new string[] { "name", "qty" }, new CountAggregatorFactory(), false );
			pvtData2.SetState(pvtState);

			Action<PivotData,PivotData> check = (pvt1, pvt2) => {
				Assert.Equal(pvt1.AllValues.Count, pvt2.AllValues.Count);
				Assert.Equal(pvt1[ValueKey.Empty2D].Value, pvt2[ValueKey.Empty2D].Value);

				foreach (var vk in pvt1.AllValues) {
					Assert.Equal(pvt1[vk.Key].Value, pvt2[vk.Key].Value);
					Assert.Equal(pvt1[vk.Key].Count, pvt2[vk.Key].Count);
				}
			};
			check(pvtData,pvtData2);

			var jsonState = JsonConvert.SerializeObject(pvtState);
			//Output.WriteLine(jsonState);

			var pvtStateFromJson = JsonConvert.DeserializeObject<PivotDataState>(jsonState);
			var pvtData3 = new PivotData(new string[] { "name", "qty" }, new CountAggregatorFactory(), false);
			pvtData3.SetState(pvtStateFromJson);

			check(pvtData,pvtData3);

			// internal serialization
			var memStream = new MemoryStream();
			pvtState.Serialize(memStream);

			memStream = new MemoryStream( memStream.ToArray() );
			var pvtStateFromSerialized = PivotDataState.Deserialize(memStream);
			var pvtData4 = new PivotData(new string[] { "name", "qty" }, new CountAggregatorFactory(), false);
			pvtData4.SetState(pvtStateFromSerialized);

			check(pvtData,pvtData4);
		}

		[Fact]
		public void PivotData_SetState() {
			// check set state with duplicate keys for values
			var pvtState = new PivotDataState() {
				DimCount = 2,
				KeyValues = new object[] { "a1", "a2", "b1", "b2" },
				ValueKeys = new uint[][] {
					new uint[] { 0, 2 },
					new uint[] { 0, 3 },
					new uint[] { 1, 2 },
					new uint[] { 1, 3 },
					new uint[] { 0, 2 }  // duplicate key!
				},
				Values = new object[] { 1, 2, 3, 4, 5 }
			};
			var pvtData = new PivotData(new[] { "a", "b" }, new CountAggregatorFactory());
			pvtData.SetState(pvtState);
			Assert.Equal(6, Convert.ToInt32( pvtData["a1", "b1"].Value ) );
			Assert.Equal(15, Convert.ToInt32( pvtData[Key.Empty, Key.Empty].Value ) );
		}

		[Fact]
		public void PivotData_LazyTotals() {
			var nameToIdx = new Dictionary<string,int>() {
				{"A", 0},
				{"B", 1},
				{"C", 2},
				{"D", 3}
			};
			var data = new int[][] {
				new int[] { 1, 2, 3, 4},
				new int[] { 1, 2, 4, 8},
				new int[] { 2, 2, 4, 4},
				new int[] { 3, 3, 4, 16}
			};
			var pvt = new PivotData(
					new string[] {"A","B","C","D"}, new CountAggregatorFactory(), true );
			pvt.ProcessData( data, (r, f) => {
				return ((int[])r)[nameToIdx[f]];
			} );
			Assert.Equal( 3, Convert.ToInt32( pvt[Key.Empty, 2, Key.Empty, Key.Empty].Value ) );
			Assert.Equal( 2, Convert.ToInt32( pvt[Key.Empty, 2, 4, Key.Empty].Value ) );
			Assert.Equal( 1, Convert.ToInt32( pvt[1, Key.Empty, 4, Key.Empty].Value ) );
		}

		[Fact]
		public void PivotData_ByDataTable() {
			var t = new DataTable();
			t.Columns.Add("name", typeof(string));
			t.Columns.Add("age", typeof(int));
			t.Columns.Add("salary", typeof(decimal));
			
			t.Rows.Add(new object[] {"John", 25, 50000 });
			t.Rows.Add(new object[] {"Mary", 30, 60000 });
			t.Rows.Add(new object[] {"Bill", 30, 80000 });

			var pvtData = new PivotData(
					new string[] {"name","age"},
					new SumAggregatorFactory("salary"), new DataTableReader(t) );

			Assert.Equal(140000M, pvtData[Key.Empty, 30].Value );
		}

		[Fact]
		public void PivotData_ByAnotherPivotData() {
			var testData = generateData();
			var sourcePvtData = new PivotData(new string[] { "name", "qty", "i" }, 
				new CompositeAggregatorFactory(
					new AverageAggregatorFactory("total"),
					new CountAggregatorFactory()
 				),
				testData );

			var pvtData = new PivotData(new[]{ "name", "qty" }, new AverageAggregatorFactory("value") );
			pvtData.ProcessData(sourcePvtData, "value");

			Assert.Equal(225M, pvtData[Key.Empty,Key.Empty].Value );
			Assert.Equal(50M, pvtData["Test1",1].Value );

			// lets check also single-aggregator pivotdata source
			var pvtData2 = new PivotData(new[] {"name"}, new AverageAggregatorFactory("value"));
			pvtData2.ProcessData(pvtData, "value");
			Assert.Equal(225M, pvtData2[Key.Empty].Value );
		}

		[Fact]
		public void PivotData_Slice() {
			var testData = generateData();
			var pvtData = new PivotData(new string[] { "name", "date", "qty", "total" }, new CountAggregatorFactory(), testData );
			var slice1Data = pvtData.Slice(new string[] {"name", "qty"}, false);
			Assert.Equal(2, slice1Data.Dimensions.Length);

			var vk = new ValueKey("Test0", 0);
			Assert.Equal( pvtData["Test0", Key.Empty, 0, Key.Empty].Value, slice1Data[vk].Value );

			Assert.Equal( pvtData[Key.Empty, Key.Empty, Key.Empty, Key.Empty].Value, slice1Data[Key.Empty,Key.Empty].Value );

			var slice2Data = pvtData.Slice(new string[] {"name", "qty"}, false, (v) => {
				return v.Key.DimKeys[0].Equals("Test0");
			});
			Assert.Equal( ((uint)pvtData[Key.Empty, Key.Empty, Key.Empty, Key.Empty].Value)/2, slice2Data[Key.Empty,Key.Empty].Value );

			// check zero-dim slice
			var sliceZeroDimData = slice2Data.Slice(null, false);
			Assert.Equal( slice2Data[Key.Empty,Key.Empty].Value, sliceZeroDimData[new object[0]].Value );
		}

		[Fact]
		public void PivotData_GetDimensionKeys() {
			var testData = generateData();
			var pvtData = new PivotData(new string[] { "name", "qty", "i" }, new CountAggregatorFactory(), testData );

			var dimKeys = pvtData.GetDimensionKeys();
			Assert.Equal(3, dimKeys.Length);

			// check "i" dim keys
			Assert.Equal(1, dimKeys[2].Length);
			Assert.Equal(2, dimKeys[2][0]);

			// check "i" dim keys
			Assert.Equal(10, dimKeys[1].Length);
			
			var onlyOneDimKeys = pvtData.GetDimensionKeys(new [] {"i"});
			Assert.Equal(1, onlyOneDimKeys.Length);
			Assert.Equal(1, onlyOneDimKeys[0].Length);

			// check custom comparers
			var qtyDimKeys = pvtData.GetDimensionKeys(new []{ "i", "qty"}, 
				new IComparer<object> [] {
					null,  //use default for i
					NaturalSortKeyComparer.ReverseInstance
				});
			Assert.Equal(9, qtyDimKeys[1][0]);
		}

		public class RevComparer : IComparer<object> {
			IComparer<object> Cmpr;
			public RevComparer(IComparer<object> cmpr) {
				Cmpr = cmpr;
			}

			public int Compare(object x, object y) {
				return -Cmpr.Compare(x,y);
			}
		}

		List<Dictionary<string, object>> generateData() {
			var rs = new List<Dictionary<string,object>>();
			var dt = new DateTime(2015, 2, 08);
			for (int i = 0; i < 1000; i++) {
				var r = new Dictionary<string,object>();
				r["name"] = "Test"+(i%2).ToString();
				r["date"] = dt.Date.AddHours(i).Date;
				r["qty"] = i%10;
				r["total"] = (i%10)*50;
				r["i"] = 2;
				rs.Add(r);
			}
			return rs;
		}

		[Fact]
		public void PerfTest() {
			var sw = new Stopwatch();
			sw.Start();
			var pvtData = new PivotData( 
				new string[] { "year", "month", "a" },
				new CompositeAggregatorFactory( new CountAggregatorFactory(), new SumAggregatorFactory("i") ),
				SampleGenerator(5000000),
				GetRecordValue, true);
			sw.Stop();
			Console.WriteLine("ProcessData: {0}", sw.Elapsed);
			sw.Restart();
			var dimKeys = pvtData.GetDimensionKeys();
			sw.Stop();
			Console.WriteLine("GetDimensionKeys: {0}", sw.Elapsed);

			Assert.Equal(2500000, Convert.ToInt32( pvtData[ Key.Empty, Key.Empty, dimKeys[2][0] ].AsComposite().Aggregators[0].Value ) );
			Assert.Equal(470880, Convert.ToInt32( pvtData[2015, Key.Empty, Key.Empty].AsComposite().Aggregators[0].Value ) );
		}

		public static object GetRecordValue(object o, string f) {
			var r = (TestRecord)o;
			switch (f) {
				case "a": return r.a;
				case "year": return r.year;
				case "month": return r.month;
				case "i": return r.i;
			}
			return null;
		}

		public static IEnumerable<TestRecord> SampleGenerator(int cnt, int shift = 0) {
			var r = new TestRecord();
			var dtStart = new DateTime(2015, 2, 08);
			for (int i = 0; i < cnt; i++) {
				r.a = i%2;
				var dt = dtStart.AddMinutes(i);
				r.year = dt.Year;
				r.month = dt.Month;
				r.i = i+shift;
				if ((i%1000000)==0)
					Console.WriteLine("{0}mln processed", i/1000000);
				yield return r;
			}
		}

		public class TestRecord {
			public int a;
			public int year;
			public int month;
			public int i;
		}

	}
}
