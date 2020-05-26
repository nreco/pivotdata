/*
 *  Copyright 2015-2020 Vitaliy Fedorchenko (nrecosite.com)
 *
 *  Licensed under PivotData Source Code Licence (see LICENSE file).
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS 
 *  OF ANY KIND, either express or implied.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NReco.PivotData
{
	/// <summary>
	/// Represents a query operation over specified <see cref="IPivotData"/> instance.
	/// </summary>
	/// <remarks>
	/// SliceQuery can be used for querying data cube (slice, dice, roll-up). 
	/// <example>
	/// Lets assume that we have some cube that represents sales data with the following dimensions: 
	/// "year", "month", "day", "country", "product" and 2 measures collected with <see cref="CountAggregatorFactory"/> (=count of orders) and
	/// <see cref="SumAggregatorFactory"/> (= sum of sales amount):
	/// <code>var salesCube = new PivotData(
	///		new [] {"year","month","day","country","product"},
	///		new CompositeAggregatorFactory(
	///			new CountAggregatorFactory(),
	///			new SumAggregatorFactory("amount")
	///		), true );</code>
	/// The following query illustrates how to reduce number of dimensions, filter by specific dimension values 
	/// and get resulting <see cref="PivotData"/> with single measure:
	/// <code>var q = new SliceQuery(salesCube)
	///		.Dimension("year")
	///		.Dimension("country")
	///		.Dimension("product")
	///		.Where("country", new[]{"USA","Canada"})
	///		.Measure(1);
	///	var salesAmountForUsaAndCanada = q.Execute(true);
	/// </code>
	/// Resulting data cube will contain only specified dimensions, "country" dimension will contain only "USA" and "Canada" keys
	/// and only one measure (index=1 refers to SumAggregatorFactory("amount")).
	/// </example>
	/// <example>
	/// SliceQuery can be used in the more complex filtering cases; lets calculate derived dimension and skip all days with &lt;10 orders:
	/// <code>var q = new SliceQuery(salesCube)
	///		.Dimension("year")
	///		.Dimension("quarter", (dimKeys) => {  
	///			var month = Convert.ToInt32( dimKeys[1] ); // "month" dimension index
	///			return (int)Math.Ceiling((float)(month)/3);
	///		}).Where( (dataPoint) => {
	///			var compositeAggr = dataPoint.Value.AsComposite(); // CompositeAggregator is used if cube has >1 measure
	///			var countOrders = Convert.ToInt32( compositeAggr.Aggregators[0].Value );
	///			return countOrders&gt;=10; // include data point by custom condition
	///		});
	///	var bigSalesByYearAndQuarter = q.Execute(true);
	/// </code>
	/// </example>
	/// </remarks>
    public class SliceQuery
    {
		IPivotData PvtData;
		List<DimensionSelector> DimSelectors;

		List<AggregatorSelector> AggrSelectors;
		List<Func<KeyValuePair<object[],IAggregator>,bool>> FilterHandlers;

		/// <summary>
		/// Initializes new slicing query to specified <see cref="PivotData"/> instance.
		/// </summary>
		/// <param name="pvtData">multidimensional dataset to query</param>
		public SliceQuery(IPivotData pvtData) {
			PvtData = pvtData;
			DimSelectors = new List<DimensionSelector>();
			AggrSelectors = new List<AggregatorSelector>();
			FilterHandlers = new List<Func<KeyValuePair<object[],IAggregator>,bool>>();
		}

		/// <summary>
		/// Execute the query and return operation result as new <see cref="PivotData"/> instance.
		/// </summary>
		public PivotData Execute() {
			return Execute(true);
		}

		/// <summary>
		/// Execute the query and return operation result as new <see cref="PivotData"/> instance.
		/// </summary>
		/// <param name="lazyTotals"></param>
		/// <returns>data cube that represents query results.</returns>
		public PivotData Execute(bool lazyTotals) {
			// compose aggregator handler
			var aggrFactory = PvtData.AggregatorFactory;
			Func<KeyValuePair<object[],IAggregator>,IAggregator> aggrHandler = (dp) => { 
				// default handler that returns umodified measure
				return dp.Value; 
			};
			if (AggrSelectors.Count == 1) {
				aggrFactory = AggrSelectors[0].GetFactory(aggrFactory);
				aggrHandler = AggrSelectors[0].GetAggregator;
			} else if (AggrSelectors.Count > 1) {
				aggrFactory = new CompositeAggregatorFactory(
					AggrSelectors.Select( s=>s.GetFactory(aggrFactory) ).ToArray()
				);
				var resAggrHandlers = AggrSelectors.Select( s=>s.GetAggregator ).ToArray();
				var resAggrCnt = AggrSelectors.Count;
				int i;
				aggrHandler = (dp) => {
					var resAggregators = new IAggregator[resAggrCnt];
					for (i=0; i<resAggrCnt; i++)
						resAggregators[i] = resAggrHandlers[i](dp);
					return new CompositeAggregator(resAggregators);
				};
			}

			// compose filter handler
			Func<KeyValuePair<object[],IAggregator>,bool> filterHandler = (entry) => { return true; };
			if (FilterHandlers.Count > 0) {
				int i;
				var resFilterCnt = FilterHandlers.Count;
				var resFilterHandlers = FilterHandlers.ToArray();
				filterHandler = (entry) => {
					for (i = 0; i < resFilterCnt; i++) {
						if (!resFilterHandlers[i](entry)) return false;
					}
					return true;
				};
			}

			// compose default value key handler
			Func<KeyValuePair<object[],IAggregator>,object[]> valKeyHandler = (dp) => {
				// default handler for unmodified dimensions
				return dp.Key;
			};
			var selectDims = PvtData.Dimensions;
			if (DimSelectors.Count > 0) {
				int copyDimLen = DimSelectors.Count;
				var resDimKeyHandlers = DimSelectors.Select(s=>s.GetDimensionKey).ToArray();
				int d;
				valKeyHandler = (dp) => {
					var valKeyDims = new object[copyDimLen];
					for (d = 0; d < copyDimLen; d++) {
						valKeyDims[d] = resDimKeyHandlers[d](dp.Key);
					}
					return valKeyDims;
				};
				selectDims = DimSelectors.Select(ds=>ds.Dimension).ToArray();
			}

			var toPivotData = new PivotData(selectDims, aggrFactory, lazyTotals);
			toPivotData.Merge( new FilterPivotData(selectDims, aggrFactory, PvtData,
				filterHandler, valKeyHandler, aggrHandler
			));
			return toPivotData;
		}

		/// <summary>
		/// Define dimension to select in result of this query.
		/// </summary>
		/// <param name="dimension">Dimension from context <see cref="PivotData"/> dimensions</param>
		public SliceQuery Dimension(string dimension) {
			var dimToCopyIdx = Array.IndexOf( PvtData.Dimensions, dimension );
			if (dimToCopyIdx<0)
				throw new ArgumentException(String.Format("Dimension {0} does not exist", dimension));
			DimSelectors.Add(
				new DimensionSelector() {
					Dimension = dimension,
					GetDimensionKey = (vk) => {
						return vk[dimToCopyIdx];
					}
				}	
			);
			return this;
		}

		/// <summary>
		/// Define dimension constructed from keys of existing dimensions. 
		/// </summary>
		/// <param name="dimension">new dimension to construct</param>
		/// <param name="getDimensionKey">a function that returns dimension key by data point key</param>
		public SliceQuery Dimension(string dimension, Func<object[], object> getDimensionKey) {
			DimSelectors.Add(
				new DimensionSelector() {
					Dimension = dimension,
					GetDimensionKey = getDimensionKey
				}	
			);
			return this;
		}

		/// <summary>
		/// Define measure aggregator at specified index to select in result of this query.
		/// </summary>
		/// <param name="index">index of measure in the composite aggregator</param>
		/// <remarks>This selector is applicable only for cubes configured with <see cref="CompositeAggregatorFactory"/>.</remarks>
		public SliceQuery Measure(int index) {
			if (PvtData.AggregatorFactory is CompositeAggregatorFactory) {
				var compositeFactory = PvtData.AggregatorFactory as CompositeAggregatorFactory;
				if (index<0 || index>=compositeFactory.Factories.Length)
					throw new ArgumentException(String.Format("Invalid aggregator index: {0}", index));
				AggrSelectors.Add(
					new AggregatorSelector() {
						GetFactory = (factory) => {
							return ((CompositeAggregatorFactory)factory).Factories[index];
						},
						GetAggregator = (dp) => {
							return ((CompositeAggregator)dp.Value).Aggregators[index];
						}	

					}
				);
			} else {
				if (index!=0)
					throw new ArgumentException(String.Format("Invalid aggregator index: {0}", index));
				AggrSelectors.Add(new AggregatorSelector() {
					GetFactory = (factory) => { return factory; },
					GetAggregator = (dp) => { return dp.Value; }
				});
			}
			return this;
		}

		/// <summary>
		/// Define new measure aggregator calculated from existing cube measure(s).
		/// </summary>
		/// <param name="aggrFactory">new measure aggregator factory</param>
		/// <param name="createMeasure">handler that creates new measure aggregator.</param>
		public SliceQuery Measure(IAggregatorFactory aggrFactory, Func<IAggregator,IAggregator> createMeasure) {
			AggrSelectors.Add(new AggregatorSelector() {
				GetFactory = (factory) => aggrFactory,
				GetAggregator = (dp) => createMeasure(dp.Value)
			});
			return this;
		}

		/// <summary>
		/// Define new measure aggregator calculated from existing cube measure(s).
		/// </summary>
		/// <param name="aggrFactory">new measure aggregator factory</param>
		/// <param name="createMeasure">handler that creates new measure aggregator by the data point (key and value).</param>
		public SliceQuery Measure(IAggregatorFactory aggrFactory, Func<KeyValuePair<object[],IAggregator>,IAggregator> createMeasure) {
			AggrSelectors.Add(new AggregatorSelector() {
				GetFactory = (factory) => aggrFactory,
				GetAggregator = createMeasure
			});
			return this;
		}

		/// <summary>
		/// Define formula measure.
		/// </summary>
		/// <param name="measureName">name of the measure that describes formula value meaning</param>
		/// <param name="formulaValue">delegate that calculates formula value</param>
		/// <param name="parentMeasureIndexes">indexes of existing measures used as formula parameters</param>
		public SliceQuery Measure(string measureName, Func<IAggregator[], object> formulaValue, int[] parentMeasureIndexes) {
			var parentAggrFactories = parentMeasureIndexes.Select(idx => GetAggregatorFactory(idx)).ToArray();
			AggrSelectors.Add(new AggregatorSelector() {
				GetFactory = (factory) => new FormulaAggregatorFactory(measureName, formulaValue, parentAggrFactories),
				GetAggregator = (dp) => {
					var aggrs = new IAggregator[parentMeasureIndexes.Length];
					var compositeAggr = dp.Value.AsComposite();
					for (int i = 0; i < aggrs.Length; i++) { 
						aggrs[i] = parentAggrFactories[i].Create( compositeAggr.Aggregators[ parentMeasureIndexes[i] ].GetState() );
					}
					return new FormulaAggregator(formulaValue, aggrs);
				}
			});
			return this;
		}

		IAggregatorFactory GetAggregatorFactory(int index) {
			if (PvtData.AggregatorFactory is CompositeAggregatorFactory)
				return ((CompositeAggregatorFactory)PvtData.AggregatorFactory).Factories[index];
			if (index!=0)
				throw new ArgumentException(String.Format("Invalid aggregator index: {0}", index));
			return PvtData.AggregatorFactory;
		}


		/// <summary>
		/// Filters dimension keys by explicit list of values.
		/// </summary>
		/// <param name="dimension">dimension to filter</param>
		/// <param name="filterKeys">allowed list of dimension keys</param>
		public SliceQuery Where(string dimension, params object[] filterKeys) {
			if (filterKeys.Length==0)
				return this; // nothing to filter
			if (filterKeys.Length == 1) {
				var fltKey = filterKeys[0];
				return Where(dimension, (key) => fltKey.Equals(key) );
			}
			// for other cases lets use hashset
			var filterKeysSet = new HashSet<object>(filterKeys);
			return Where(dimension, (key) => filterKeysSet.Contains(key) );
		}

		/// <summary>
		/// Filters dimension keys based on a predicate.
		/// </summary>
		/// <param name="dimension">dimension to filter</param>
		/// <param name="predicate">a function to test dimension key for a condition.</param>
		public SliceQuery Where(string dimension, Func<object,bool> predicate) {
			var dimIdx = Array.IndexOf( PvtData.Dimensions, dimension );
			if (dimIdx<0)
				throw new ArgumentException("Unknown dimension: "+dimension);
			FilterHandlers.Add( (entry) => {
				return predicate(entry.Key[dimIdx]);
			});
			return this;
		}

		/// <summary>
		/// Filters data points based on a predicate.
		/// </summary>
		/// <param name="predicate">a function to test data point for a condition.</param>
		public SliceQuery Where(Func<KeyValuePair<object[],IAggregator>,bool> predicate) {
			FilterHandlers.Add(predicate);
			return this;
		}

		internal class DimensionSelector {
			public string Dimension;
			public Func<object[],object> GetDimensionKey;
		}

		internal class AggregatorSelector {
			public Func<IAggregatorFactory,IAggregatorFactory> GetFactory;
			public Func<KeyValuePair<object[],IAggregator>,IAggregator> GetAggregator;
		}

		private sealed class FilterPivotData : IPivotData {
			string[] Dims;
			IAggregatorFactory Aggr;
			IPivotData PvtData;
			Func<KeyValuePair<object[],IAggregator>,bool> FilterHandler;
			Func<KeyValuePair<object[],IAggregator>,object[]> KeyHandler;
			Func<KeyValuePair<object[],IAggregator>,IAggregator> AggrHandler;

			public FilterPivotData(string[] dims, IAggregatorFactory aggr, IPivotData origPvtData,
				Func<KeyValuePair<object[],IAggregator>,bool> filterHandler,
				Func<KeyValuePair<object[],IAggregator>,object[]> keyHandler,
				Func<KeyValuePair<object[],IAggregator>,IAggregator> aggrHandler) {
				Dims = dims;
				Aggr = aggr;
				PvtData = origPvtData;
				FilterHandler = filterHandler;
				KeyHandler = keyHandler;
				AggrHandler = aggrHandler;
			}

			public string[] Dimensions {
				get { return Dims; }
			}

			public IAggregatorFactory AggregatorFactory {
				get { return Aggr; }
			}

			public IAggregator this[params object[] dimKeys] {
				get { throw new NotSupportedException(); }
			}

			public int Count {
				get { 
					int cnt = 0;
					foreach (var dp in PvtData) {
						if (!FilterHandler(dp))
							continue;
						cnt++;
					}
					return cnt; 
				}
			}

			public IEnumerator<KeyValuePair<object[],IAggregator>> GetEnumerator() {
				foreach (var dp in PvtData) {
					if (!FilterHandler(dp))
						continue;
					yield return new KeyValuePair<object[],IAggregator>( KeyHandler(dp),AggrHandler(dp));
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
				return this.GetEnumerator();
			}
		}

    }

}
