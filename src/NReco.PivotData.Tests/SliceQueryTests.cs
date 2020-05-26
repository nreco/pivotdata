using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Xunit;

namespace NReco.PivotData.Tests {
	
	public class SliceQueryTests {

		[Fact]
		public void QueryTest() {
			
			var pvtData = new PivotData(new[] {"a","b","d"},
					new CompositeAggregatorFactory(
						new CountAggregatorFactory(), 
						new SumAggregatorFactory("d")
					),
					false);
			pvtData.ProcessData( DataUtils.getSampleData(10000), DataUtils.getProp);

			var q = new SliceQuery(pvtData).Dimension("a").Where("a", "val1", "val2").Measure(0);
			var pvtDataRes1 = q.Execute(false);

			Assert.True( pvtDataRes1.AggregatorFactory is CountAggregatorFactory );
			Assert.Equal(1, pvtDataRes1.Dimensions.Length );
			Assert.Equal(2, pvtDataRes1.GetDimensionKeys()[0].Length );
			Assert.Equal( ((object[])pvtData["val1",Key.Empty,Key.Empty].Value)[0], pvtDataRes1["val1"].Value );

			var grandTotalCount = Convert.ToInt32( ((object[])pvtData[Key.Empty,Key.Empty,Key.Empty].Value)[0] );
			var q2 = new SliceQuery(pvtData)
				.Dimension("d")
				.Measure( 
					new SumAggregatorFactory("i"), // since this is derived measure field name actually may not match source data fields
					(sourceAggr) => {
						var cntAggr = sourceAggr.AsComposite().Aggregators[0];
						return new SumAggregator("i", new object[] { cntAggr.Count, Convert.ToDecimal(cntAggr.Value)/grandTotalCount*100 });
					}
				);
			var pvtDataRes2 = q2.Execute(true);
			Assert.Equal(100M, pvtDataRes2[ValueKey.Empty1D].Value );
			Assert.Equal(1M, pvtDataRes2[0].Value );

		}

		[Fact]
		public void WhereTest() {
			var pvtData = new PivotData(new[] {"a","d"}, new CountAggregatorFactory() );
			pvtData.ProcessData( DataUtils.getSampleData(10000), DataUtils.getProp);

			// empty filter = nothing is changed
			Assert.Equal(pvtData.Count,  new SliceQuery(pvtData).Where("a").Execute().Count);

			// 1/3 of 10k
			Assert.Equal(3334, Convert.ToInt32(
				new SliceQuery(pvtData).Where("a", "val1").Dimension("a").Execute()[Key.Empty].Value));

			// 2/3 of 10k
			Assert.Equal(6667, Convert.ToInt32(
				new SliceQuery(pvtData).Where("a", "val1", "val2").Dimension("a").Execute()[Key.Empty].Value));
		}

		[Fact]
		public void FormulaTest() {
			// simple test for 1-measure
			var pvtData1 = new PivotData(new[] {"a","d"},
					new SumAggregatorFactory("b"),
					false);
			pvtData1.ProcessData( DataUtils.getSampleData(1000), DataUtils.getProp);
			var q1 = new SliceQuery(pvtData1).Dimension("a").Measure("a*2", (paramAggrs) => {
				return Convert.ToDecimal( paramAggrs[0].Value ) * 2;
			}, new[]{0} );
			var pvt1Res = q1.Execute();

			Assert.Equal(
				((decimal) pvtData1["val1", Key.Empty].Value)*2, 
				pvt1Res["val1"].Value );

			// test for composite aggregator
			var pvtData = new PivotData(new[] {"a","d"},
					new CompositeAggregatorFactory(
						new CountAggregatorFactory(), 
						new SumAggregatorFactory("b")
					),
					false);
			pvtData.ProcessData( DataUtils.getSampleData(1000), DataUtils.getProp);

			var q = new SliceQuery(pvtData);
			q.Dimension("a");
			q.Measure(1);
			q.Measure("a*2", (paramAggrs) => {
				return Convert.ToDecimal( paramAggrs[0].Value ) * 2;
			}, new[]{1} );

			var pvtRes = q.Execute();
			Assert.Equal(
				((decimal) pvtData["val1", Key.Empty].AsComposite().Aggregators[1].Value)*2, 
				pvtRes["val1"].AsComposite().Aggregators[1].Value );

			// check that calculated measures are merged correctly
			pvtRes.ProcessData( DataUtils.getSampleData(10), DataUtils.getProp);

			Assert.Equal(
				((decimal) pvtRes["val1"].AsComposite().Aggregators[0].Value)*2, 
				pvtRes["val1"].AsComposite().Aggregators[1].Value );

		}

	}
}
