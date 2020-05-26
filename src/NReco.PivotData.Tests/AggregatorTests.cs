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
    public class AggregatorTests
    {

		[Fact]
		public void MinAggregator() {
			var minAggr = new MinAggregator("test");
			Func<object,string,object> getVal = (r,f) => {
				Assert.Equal("test", f);
				return r;
			};
			minAggr.Push(5, getVal);
			Assert.Equal(5, minAggr.Value);
			minAggr.Push(3, getVal);
			Assert.Equal(3, minAggr.Value);
			minAggr.Push(7, getVal);
			Assert.Equal(3, minAggr.Value);
		}

		[Fact]
		public void MinAggregator_Merge() {
			var minAggr1 = new MinAggregator("test");
			var minAggr2 = new MinAggregator("test");
			Func<object,string,object> getVal = (r,f) => {
				return r;
			};

			minAggr1.Push(5, getVal);
			minAggr2.Push(3, getVal);

			minAggr2.Merge(minAggr1);
			Assert.Equal(3, minAggr2.Value);
			minAggr1.Merge(minAggr2);
			Assert.Equal(3, minAggr1.Value);

			// datetime values
			var maxAggr3 = new MinAggregator("test");
			maxAggr3.Push( new DateTime(1983, 5, 6), getVal);
			maxAggr3.Push( new DateTime(2000, 1, 1), getVal);
			Assert.Equal(1983, ((DateTime)maxAggr3.Value).Year );

			var maxAggr4 = new MinAggregator("test");
			maxAggr4.Push( new DateTime(2010, 2, 2), getVal);
			maxAggr3.Merge(maxAggr4);
			Assert.Equal(1983, ((DateTime)maxAggr3.Value).Year );
		}

		[Fact]
		public void MaxAggregator() {
			var maxAggr = new MaxAggregator("test");
			Func<object,string,object> getVal = (r,f) => {
				Assert.Equal("test", f);
				return r;
			};
			maxAggr.Push(5, getVal);
			Assert.Equal(5, maxAggr.Value);
			maxAggr.Push(3, getVal);
			Assert.Equal(5, maxAggr.Value);
			maxAggr.Push(7, getVal);
			Assert.Equal(7, maxAggr.Value);
		}

		[Fact]
		public void MaxAggregator_Merge() {
			var maxAggr1 = new MaxAggregator("test");
			var maxAggr2 = new MaxAggregator("test");
			Func<object,string,object> getVal = (r,f) => {
				return r;
			};

			maxAggr1.Push(5, getVal);
			maxAggr1.Push(2, getVal);
			maxAggr2.Push(3, getVal);

			maxAggr1.Merge(maxAggr2);
			Assert.Equal(5, maxAggr1.Value);
			maxAggr2.Merge(maxAggr1);
			Assert.Equal(5, maxAggr2.Value);

			// datetime values
			var maxAggr3 = new MaxAggregator("test");
			maxAggr3.Push( new DateTime(1983, 5, 6), getVal);
			maxAggr3.Push( new DateTime(2000, 1, 1), getVal);
			Assert.Equal(2000, ((DateTime)maxAggr3.Value).Year );

			var maxAggr4 = new MaxAggregator("test");
			maxAggr4.Push( new DateTime(2010, 2, 2), getVal);
			maxAggr3.Merge(maxAggr4);
			Assert.Equal(2010, ((DateTime)maxAggr3.Value).Year );
		}

		[Fact]
		public void SumAggregator() {
			var sumAggr = new SumAggregator("test");
			Func<object,string,object> getVal = (r,f) => {
				Assert.Equal("test", f);
				return r;
			};
			sumAggr.Push(5, getVal);
			Assert.Equal(5M, sumAggr.Value);
			sumAggr.Push(3, getVal);
			Assert.Equal(8M, sumAggr.Value);

		}

		[Fact]
		public void SumAggregator_Merge() {
			var sumAggr1 = new SumAggregator("test");
			var sumAggr2 = new SumAggregator("test");
			Func<object,string,object> getVal = (r,f) => {
				return r;
			};

			for (int i=0; i<10; i++) {
				sumAggr1.Push(i, getVal);
				sumAggr2.Push(i%2, getVal);
			}

			sumAggr1.Merge(sumAggr2);
			Assert.Equal(50M, Convert.ToDecimal( sumAggr1.Value ) );
			Assert.Equal(20, (int)sumAggr1.Count );
		}


		[Fact]
		public void AverageAggregator() {
			var avg = new AverageAggregator("test");
			Assert.Equal(0M, Convert.ToDecimal( avg.Value ) );

			Func<object,string,object> getVal = (r,f) => {
				Assert.Equal("test", f);
				return r;
			};
			for (int i=0; i<=10; i++)
				avg.Push(i, getVal);

			Assert.Equal(5M, avg.Value);

			avg.Push(String.Empty, getVal);
			Assert.Equal(5M, avg.Value);
		}

		[Fact]
		public void AverageAggregator_Merge() {
			var avgAggr1 = new AverageAggregator("test");
			var avgAggr2 = new AverageAggregator("test");
			var avgAggr3 = new AverageAggregator("test");
			Func<object,string,object> getVal = (r,f) => {
				return r;
			};

			for (int i=0; i<10; i++) {
				avgAggr1.Push(i, getVal);
				avgAggr2.Push(i, getVal);
				avgAggr3.Push(i%2==0 ? null : (object)i, getVal);
			}

			Assert.Equal(4.5M, Convert.ToDecimal( avgAggr1.Value ) );
			avgAggr1.Merge(avgAggr2);
			Assert.Equal( 4.5M, Convert.ToDecimal( avgAggr1.Value) );
			Assert.Equal(20, Convert.ToInt32( avgAggr1.Count ) );

			avgAggr1.Merge(avgAggr3);
			Assert.Equal( 4.6M, Convert.ToDecimal( avgAggr1.Value ) );
			Assert.Equal(25, (int)avgAggr1.Count );
		}

		[Fact]
		public void CountAggregator() {
			var cnt = new CountAggregator();
			for (int i=0; i<10; i++)
				cnt.Push(5, null);
			Assert.Equal(10, Convert.ToInt32( cnt.Value) );
		}

		[Fact]
		public void CountUniqueAggregator() {
			var cntUnq = new CountUniqueAggregator("f");
			Func<object,string,object> getVal = (r,f) => {
				return r;
			};
			for (int i=0; i<10; i++)
				cntUnq.Push(i%5, getVal);
			Assert.Equal(5M, Convert.ToDecimal( cntUnq.Value ));
			Assert.Equal(10, (int)cntUnq.Count);
		}

		[Fact]
		public void ListUniqueAggregator() {
			var lstUnq = new ListUniqueAggregator("f");
			Func<object,string,object> getVal = (r,f) => {
				return r;
			};
			for (int i=10; i>=0; i--)
				lstUnq.Push(i%5, getVal);
			Assert.Equal("0,1,2,3,4", String.Join(",", ((IEnumerable)lstUnq.Value).Cast<object>().Select(v=>v.ToString()).ToArray() ) );
			Assert.Equal(11, (int)lstUnq.Count);
		}


		[Fact]
		public void ListUniqueAggregator_Merge() {
			var lstUnqAggr1 = new ListUniqueAggregator("test");
			var lstUnqAggr2 = new ListUniqueAggregator("test");
			Func<object,string,object> getVal = (r,f) => {
				return r;
			};
			for (int i = 10; i >= 0; i--) { 
				lstUnqAggr1.Push(i%5, getVal);
				lstUnqAggr2.Push(i%2==0 ? null : (object)i, getVal);
			}

			lstUnqAggr2.Merge(lstUnqAggr1);
			Assert.Equal("0,1,2,3,4,5,7,9", String.Join(",", ((IEnumerable)lstUnqAggr2.Value).Cast<object>().Select(v=>v.ToString()).ToArray() ) );
		}

		[Fact]
		public void CompositeAggregator() {
			var compositeAggrFactory = new CompositeAggregatorFactory( new IAggregatorFactory[] {
				new CountAggregatorFactory(),
				new SumAggregatorFactory("1")
			} );
			Assert.False( compositeAggrFactory.Equals( new CompositeAggregatorFactory(new IAggregatorFactory[] {new CountAggregatorFactory()}) ) );

			Func<object,string,object> getVal = (r,f) => {
				return r;
			};
			var compositeAggr = compositeAggrFactory.Create();
			for (int i = 0; i < 10; i++) {
				compositeAggr.Push(i, getVal);
			}
			Assert.Equal( 10, Convert.ToInt32(  ((object[])compositeAggr.Value)[0] ) );
			Assert.Equal( 45M, ((object[])compositeAggr.Value)[1] );

			var stateObj = compositeAggr.GetState();
			var compositeAggrCopy = compositeAggrFactory.Create(stateObj);
			Assert.Equal( 10, Convert.ToInt32( ((object[])compositeAggrCopy.Value)[0] ) );
			Assert.Equal( 45M, ((object[])compositeAggrCopy.Value)[1] );

			compositeAggr.Merge(compositeAggrCopy);
			Assert.Equal( 20, Convert.ToInt32( ((object[])compositeAggr.Value)[0] ) );
			Assert.Equal( 90M, ((object[])compositeAggr.Value)[1] );
		}


		[Fact]
		public void ListAggregator() {
			var lstAggr = new ListAggregator(null);
			for (int i=0; i<10; i++)
				lstAggr.Push(5, null);
			Assert.Equal(10, (int) lstAggr.Count);
			Assert.Equal(5, ((IList)lstAggr.Value)[0] );

			// list aggr with field
			Func<object,string,object> getVal = (r,f) => {
				Assert.Equal("test", f);
				return r;
			};
			var lstAggr2 = new ListAggregator("test");
			for (int i=0; i<20; i++)
				lstAggr2.Push(i, getVal);
			Assert.Equal(20, (int) lstAggr2.Count);
			Assert.Equal(0, ((IList)lstAggr2.Value)[0] );
			Assert.Equal(19, ((IList)lstAggr2.Value)[19] );
		}

		[Fact]
		public void ListAggregator_Merge() {
			var lstAggr1 = new ListAggregator(null, new[] { "a", "b", "c"} );
			var lstAggr2 = new ListAggregator(null, new[] { "c", "a", "b"} );

			lstAggr1.Merge(lstAggr2);
			Assert.Equal(6, (int) lstAggr1.Count);
		}

		[Fact]
		public void VarianceAggregator() {
			Func<object,string,object> getVal = (r,f) => {
				return r;
			};
			Func<double[],VarianceAggregator> calcAggr = (testArr) => {
				var varAggr = new VarianceAggregator("test", VarianceAggregatorValueType.Variance);
				foreach (var v in testArr)
					varAggr.Push(v, getVal);
				return varAggr;
			};

			var varAggr1 = calcAggr( new double[] { 2, 4, 4, 4, 5, 5, 7, 9 } );
			Assert.Equal(4.0D, varAggr1.Value );
			Assert.Equal(2.0D, varAggr1.StdDevValue );
			Assert.Equal(4.57D, Math.Round( varAggr1.SampleVarianceValue, 2) );
			Assert.Equal(2.14D, Math.Round( varAggr1.SampleStdDevValue, 2) );
			
			var varAggr2 = calcAggr( new double[] { 600, 470, 170 });
			var varAggr3 = calcAggr( new double[] { 430, 300 });
			varAggr2.Merge(varAggr3);
			Assert.Equal(21704, Math.Round( (double)varAggr2.Value, 4) );
			Assert.Equal(147, Math.Round( varAggr2.StdDevValue ) );
		}

		[Fact]
		public void QuantileAggregator() {
			Func<object, string, object> getVal = (r, f) => {
				return r;
			};
			var revOrderSeq = new List<int>();
			for (int i = 0; i <= 100; i++)
				revOrderSeq.Add(i);

			checkQuantiles(revOrderSeq, 25M, 50M, 75M);

			checkQuantiles( new[] { 20, 3, 6, 7, 8, 8, 9, 10, 13, 15, 16 }, 7.5M, 9M, 14M );

			void checkQuantiles(IEnumerable data, decimal firstQuantile, decimal secondQuantile, decimal thirdQuantile) {
				var qAggr = new QuantileAggregator("test", 0.5M);
				foreach (var o in data)
					qAggr.Push(o, getVal);
				Assert.Equal(firstQuantile, qAggr.GetQuantile(0.25M));
				Assert.Equal(secondQuantile, qAggr.GetQuantile(0.5M));
				Assert.Equal(secondQuantile, qAggr.Value);
				Assert.Equal(thirdQuantile, qAggr.GetQuantile(0.75M));
			}

		}

		[Fact]
		public void ModeAggregator() {
			Func<object, string, object> getVal = (r, f) => {
				return r;
			};
			var testInputs = new IEnumerable[] {
				new int[] { 1, 2, 2, 3, 4, 7, 9 },
				new int[] { 2, 7, 4, 7, 3, 5, 10, 3, 2, 7, 7, 3},
				new int[] { 2, 2, 1, 1},
				new int[] { 1, 2, 2, 1},  // in PivotData impl max value is used in case of single-mode to guarantee determinate result
				new int[] { 1, 2, 3},
				new string[] {"a", "b", "a", "c"}
			};
			var expectedOutputs = new object[] {
				2,
				7,
				2,
				2,
				null,
				"a"
			};
			for (int i=0; i<testInputs.Length; i++) {
				var input = testInputs[i];
				var expectedOutput = expectedOutputs[i];
				var mode = new ModeAggregator("f", false);
				foreach (var entry in input) 
					mode.Push(entry, getVal);

				Assert.Equal(expectedOutput, mode.Value);
			}

			// multi-mode test
			var multiMode = new ModeAggregator("f", true);
			foreach (var entry in new int[] { 1, 3, 2, 2, 3 })
				multiMode.Push(entry, getVal);
			var mmVal = multiMode.Value as object[];
			Assert.Equal(2, mmVal.Length);
			Assert.Equal(2, mmVal[0]);
			Assert.Equal(3, mmVal[1]);
		}

		[Fact]
		public void ModeAggregator_Merge() {
			Func<object, string, object> getVal = (r, f) => {
				return r;
			};
			var mode1 = new ModeAggregator("f", false);
			foreach (var entry in new int[] { 1, 3, 2, 2, 3 })
				mode1.Push(entry, getVal);
			var mode2 = new ModeAggregator("f", false);
			foreach (var entry in new int[] { 4, 2, 1})
				mode2.Push(entry, getVal);

			mode1.Merge(mode2);

			Assert.Equal(2, mode1.Value);
			Assert.Equal(8, (int)mode1.Count);
		}


	}
}
