/*
 *  Copyright 2015-2022 Vitaliy Fedorchenko (nrecosite.com)
 *
 *  Licensed under PivotData Source Code Licence (see LICENSE file).
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS 
 *  OF ANY KIND, either express or implied.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NReco.PivotData {
	
	/// <summary>
	/// Implements generic dictionary-based in-memory high performance multidimensional dataset (OLAP cube).
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="PivotData"/> can be used for fast data aggregation from any input source that implements <see cref="IEnumerable"/> interface.
	/// <para>
	/// Totals may be pre-calculated during data processing or calulated on-the-fly on the first access (<see cref="PivotData.LazyTotals"/>).
	/// Lazy totals calculation mode is recommended for cases when cube has many dimensions and/or large number of unique dimension keys.
	/// </para>
	/// <para>Thread safety: a <see cref="PivotData"/> can support multiple readers concurrently, as long as the data is not modified. 
	/// To allow the collection to be accessed by multiple threads for reading and writing, you must implement your own synchronization.
	/// When data is processed with <see cref="PivotData.ProcessData"/> enumerating through 
	/// <see cref="AllValues"/> or <see cref="PivotData.GetEnumerator"/> is intrinsically not a thread-safe procedure.</para>
	/// <example>
	/// The following code illustrates how to aggregate data from DataTable:
	/// <code>DataTable dataTbl; // lets assume it has columns: 'Delivery Year', 'Delivery Month', 'Supplier Name'
	/// var pvtData = new PivotData(
	///		new []{"Delivery Year", "Delivery Month", "Supplier Name"},
	///		new CountAggregatorFactory(), true);
	///	pvtData.ProcessData( new DataTableReader(dataTbl) );  // real ADO.NET data reader can be used too
	/// </code>
	/// </example>
	/// </para>
	/// <para>
	/// Measure aggregation logic is controlled by <see cref="IAggregatorFactory"/> implementations (each measure is represented by <see cref="IAggregator"/> instance).
	/// Several measures may be calculated at once using <see cref="CompositeAggregatorFactory"/>:
	/// <code>var pvtData = new PivotData(
	///		new string[]{"country","product"},
	///		new CompositeAggregatorFactory(
	///			new CountAggregatorFactory(),
	///			new SumAggregatorFactory("amount")
	///		), true );</code>
	///	In this case individual measure values may be accessed in the following way:
	///	<code>var compositeAggr = pvtData["USA",null].AsComposite();
	///	var countValue = compositeAggr.Aggregators[0].Value; // refers to CountAggregator
	///	var sumValue = compositeAggr.Aggregators[1].Value; // refers to SumAggregator 
	///	</code>
	/// </para>
	/// </remarks>
	public class PivotData : IPivotData {

		Dictionary<object[],IAggregator> values;
		Dictionary<object[],IAggregator> totalValues;

		/// <summary>
		/// Gets dimension identifiers of the multidimensional dataset.
		/// </summary>
		public string[] Dimensions {
			get {
				return dimensions;
			}
		}
		string[] dimensions;

		/// <summary>
		/// Gets <see cref="IAggregatorFactory"/> instance used for creating measure value aggregators.
		/// </summary>
		public IAggregatorFactory AggregatorFactory {
			get {  return aggregatorFactory; }
		}

		/// <summary>
		/// Determines totals calculation mode. Lazy means that all totals/sub-totals are calculated on first use; otherwise totals/sub-totals are calculated on-the-fly. 
		/// </summary>
		public bool LazyTotals {
			get {  return lazyTotals; }
		}

		/// <summary>
		/// Determines behaviour when non-existing key is accessed (true by default).
		/// </summary>
		/// <remarks>If <see cref="LazyAdd"/> is <code>true</code> new aggregator is created and added to the values list in case of non-existing key access. If <code>false</code> <see cref="this[object[]]"/> will return <code>null</code>.</remarks>
		public bool LazyAdd {
			get { return lazyAdd; }
			set { lazyAdd = value; }
		}

		// internally lets use fields instead of getters for performance reasons
		IAggregatorFactory aggregatorFactory;
		bool lazyTotals = false;
		bool lazyAdd = true;

		/// <summary>
		/// Get all datapoints (dim keys -> aggregator pairs) contained in the <see cref="PivotData"/> (including totals).
		/// </summary>
		public ICollection<KeyValuePair<ValueKey, IAggregator>> AllValues {
			get { return new AllValuesCollection(this); }
		}

		/// <summary>
		/// Gets the number of unique data point of this multidimensional dataset (calculated data points like totals are not included).
		/// </summary>
		public int Count {
			get { return values.Count; }
		}

		/// <summary>
		/// Gets value by specified multidimensional key.
		/// </summary>
		/// <param name="vk">multidimensional key</param>
		/// <returns>matched measure <see cref="IAggregator"/> instance or empty aggregator</returns>
		public IAggregator this[ValueKey vk] {
			get {
				return this[ vk.DimKeys ];
			}
		}

		/// <summary>
		/// Gets aggegator by specified dimensions keys
		/// </summary>
		/// <param name="dimKeys">list of dimension keys</param>
		/// <returns>matched <see cref="IAggregator"/> instance or empty aggregator</returns>
		public IAggregator this[params object[] dimKeys] {
			get {
				if (dimKeys.Length!=Dimensions.Length)
					throw new ArgumentException("Specified keys don't match this PivotData dimensions");
				IAggregator aggr;
				if (values.TryGetValue(dimKeys, out aggr) || totalValues.TryGetValue(dimKeys, out aggr)) {
					return aggr;
				} else {
					if (ValueKey.HasEmptyKey(dimKeys)) {
						var totalAgg = aggregatorFactory.Create();
						if (lazyTotals)
							CalcLazyTotal(dimKeys, totalAgg);
						totalValues[dimKeys] = totalAgg;
						return totalAgg;
					} else {
						if (lazyAdd) {
							var newAgg = aggregatorFactory.Create();
							values[dimKeys] = newAgg;
							return newAgg;
						} else {
							return null;
						}
					}
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PivotData"/> with specified dimensions and aggregator.
		/// </summary>
		/// <param name="dimensions">dimensions configuration (array of fields)</param>
		/// <param name="aggregator">measure aggregators factory</param>
		public PivotData(string[] dimensions, IAggregatorFactory aggregator) : this(dimensions,aggregator,true) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PivotData"/> with specified dimensions, aggregator and lazy totals mode.
		/// </summary>
		/// <param name="dimensions">dimensions configuration (array of dimension elements)</param>
		/// <param name="aggregator">measure aggregators factory</param>
		/// <param name="lazyTotals">if true totals are calculated on-the-fly when accessed for the first time; otherwise they are calculated during data processing</param>
		public PivotData(string[] dimensions, IAggregatorFactory aggregator, bool lazyTotals) {
			this.dimensions = dimensions ?? new string[0];
			if (this.dimensions.Length>63 && !lazyTotals)
				throw new ArgumentOutOfRangeException("dimensions", "Too many dimensions for non-lazy totals: max=63");
			aggregatorFactory = aggregator;
			values = new Dictionary<object[],IAggregator>( new ValueKeyEqualityComparer() );
			totalValues = new Dictionary<object[],IAggregator>( new ValueKeyEqualityComparer() );

			this.lazyTotals = lazyTotals; 
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PivotData"/> with specified dimensions, aggregator and processes specified data.
		/// </summary>
		/// <param name="dimensions">dimensions configuration (array of dimension elements)</param>
		/// <param name="aggregator">measure aggregators factory</param>
		/// <param name="data">data represented by dictionary enumeration</param>
		public PivotData(string[] dimensions, IAggregatorFactory aggregator, IEnumerable<IDictionary<string,object>> data) :
			this (dimensions, aggregator, data, GetDictionaryValue) {
		}

		/// <summary>
		/// Initializes new instance of <see cref="PivotData"/> with specified dimensions, aggregator, lazy totals mode and processes specified data.
		/// </summary>
		/// <param name="dimensions">dimensions configuration (array of dimension elements)</param>
		/// <param name="aggregator">measure aggregators factory</param>
		/// <param name="data">data represented by dictionary enumeration</param>
		/// <param name="lazyTotals">if true totals are calculated on-the-fly when accessed for the first time</param>
		public PivotData(string[] dimensions, IAggregatorFactory aggregator, IEnumerable<IDictionary<string,object>> data, bool lazyTotals) :
			this (dimensions, aggregator, data, GetDictionaryValue, lazyTotals) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PivotData"/> with specified dimensions, aggregator and processes data from data reader.
		/// </summary>
		/// <param name="dimensions">dimensions configuration (array of dimension fields)</param>
		/// <param name="aggregator">aggregator factory component</param>
		/// <param name="dataReader">data represented by <see cref="IDataReader"/> instance</param>
		public PivotData(string[] dimensions, IAggregatorFactory aggregator, IDataReader dataReader) :
			this (dimensions, aggregator, GetDataReaderEnumeration(dataReader), GetDataRecordValue) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PivotData"/> with specified dimensions configuration, aggregator, lazy totals mode and  and processes data from data reader.
		/// </summary>
		/// <param name="dimensions">dimensions configuration (array of dimension fields)</param>
		/// <param name="aggregator">measure aggregators factory</param>
		/// <param name="dataReader">data represented by <see cref="IDataReader"/> instance</param>
		/// <param name="lazyTotals">if true totals are calculated on-the-fly when accessed for the first time</param>
		public PivotData(string[] dimensions, IAggregatorFactory aggregator, IDataReader dataReader, bool lazyTotals) :
			this (dimensions, aggregator, GetDataReaderEnumeration(dataReader), GetDataRecordValue, lazyTotals) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PivotData"/> with specified dimensions, aggregator, and processes data from enumeration.
		/// </summary>
		/// <param name="dimensions">dimensions configuration (array of dimension fields)</param>
		/// <param name="aggregator">measure aggregators factory</param>
		/// <param name="data">data enumeration</param>
		/// <param name="getValue">record field value accessor delegate</param>
		public PivotData(string[] dimensions, IAggregatorFactory aggregator, IEnumerable data, Func<object,string,object> getValue)
			: this(dimensions,aggregator) {
			ProcessData(data, getValue);
		}

		/// <summary>
		/// Initializes new instance of <see cref="PivotData"/> with specified dimensions configuration and calculates values for specified data
		/// </summary>
		/// <param name="dimensions">dimensions configuration (array of dimension fields)</param>
		/// <param name="aggregator">measure aggregators factory</param>
		/// <param name="data">data enumeration</param>
		/// <param name="getValue">record field value accessor delegate</param>
		/// <param name="lazyTotals">if true totals are calculated on-the-fly when accessed for the first time</param> 
		public PivotData(string[] dimensions, IAggregatorFactory aggregator, IEnumerable data, Func<object,string,object> getValue, bool lazyTotals)
			: this(dimensions,aggregator, lazyTotals) {
			ProcessData(data, getValue);
		}

		static IEnumerable GetDataReaderEnumeration(IDataReader dataReader) {
			while (dataReader.Read()) {
				yield return dataReader;
			}
		}

		static object GetDataRecordValue(object d, string f) {
			return ((IDataRecord)d)[f];
		}

		static object GetDictionaryValue(object d, string f) {
			return ((IDictionary<string,object>)d)[f];
		}

		static object GetDataRowValue(object d, string f) {
			return ((DataRow)d)[f];
		}

		void CalcTotals1D(object[] valKey, Func<object,string,object> getValue, object r) {
			this[ValueKey._Empty1D].Push(r, getValue);
		}

		void CalcTotals2D(object[] valKey, Func<object,string,object> getValue, object r) {
			if (!Key.IsEmpty(valKey[1])) {
				this[ new object[] {valKey[0], Key._Empty} ].Push(r, getValue);
			}
			if (!Key.IsEmpty(valKey[0])) {
				this[ new object[] {Key.Empty, valKey[1]} ].Push(r, getValue);
			}
			this[ValueKey._Empty2D.DimKeys].Push(r, getValue);
		}

		void CalcTotalsAny(object[] valKey, Func<object,string,object> getValue, object r) {
			var dimCnt = dimensions.Length;
			int i;
			IAggregator aggr;
			ulong d,e;
			ulong dMin = 1;
			ulong dMax = ( dMin << dimCnt )-1;
			for (d = dMin-1; d < dMax; d++) {
				var totalKeys = new object[dimCnt];
				for (i = 0; i < dimCnt; i++) { 
					e = dMin<<i;
					totalKeys[i] = (d&e) == 0 ? Key._Empty : valKey[i];
				}

				if (!totalValues.TryGetValue(totalKeys, out aggr)) {
					aggr = aggregatorFactory.Create();
					totalValues[totalKeys] = aggr;
				}
				aggr.Push(r,getValue);
			}
		}

		void BatchCalcTotals() {
			var dimCnt = dimensions.Length;
			int i;
			ulong d,e;
			ulong dMin = 1;
			ulong dMax = ( dMin << dimCnt )-1;
			IAggregator aggr;

			totalValues.Clear();
			foreach (var val in values) {
				// iterate dim totals combinations
				for (d = dMin-1; d < dMax; d++) {
					// compose totals key
					var totalsKey = new object[dimCnt];
					for (i = 0; i < dimCnt; i++) { 
						e = dMin<<i;
						totalsKey[i] = (d&e) == 0 ? Key._Empty : val.Key[i];
					}

					if (!totalValues.TryGetValue(totalsKey, out aggr)) {
						aggr = aggregatorFactory.Create();
						totalValues[totalsKey] = aggr;
					}						
					aggr.Merge( val.Value );
				}
			}
		}

		// lets cache values array b/c values.GetEnumerator is damn slow
		KeyValuePair<object[],IAggregator>[] valuesArr = null;

		void CalcLazyTotal(object[] k, IAggregator totalAggr) {
			int i;
			int dimLen = dimensions.Length;
			bool matched;
			var emptyKey = Key.Empty; // avoid getter for performance
			object[] dpKey;
			KeyValuePair<object[],IAggregator> dataPoint;
			if (valuesArr == null || valuesArr.Length!=values.Count) {
				valuesArr = new KeyValuePair<object[],IAggregator>[values.Count];
				int j = 0;
				foreach (var dp in values)
					valuesArr[j++] = dp;
			}

			for (int j=0; j<valuesArr.Length; j++) {
				dataPoint = valuesArr[j];
				dpKey = dataPoint.Key;
				matched = true;
				for (i = 0; i < dimLen; i++) {
					if ( emptyKey == k[i] )
						continue;
					if (!k[i].Equals(dpKey[i])) {
						matched = false;
						break;
					}
				}
				if (!matched) continue;
				totalAggr.Merge( dataPoint.Value );
			}
		}

		/// <summary>
		/// Returns keys of all <see cref="PivotData"/> dimensions
		/// </summary>
		/// <returns>array of keys for all <see cref="PivotData"/> dimensions</returns>
		public object[][] GetDimensionKeys() {
			return GetDimensionKeys(Dimensions);
		}

		/// <summary>
		/// Returns keys of specified dimensions
		/// </summary>
		/// <param name="dims">list of dimensions</param>
		/// <returns>array of keys for specified dimensions</returns>
		public object[][] GetDimensionKeys(string[] dims) {
			return GetDimensionKeys(dims, null);
		}

		/// <summary>
		/// Returns keys of specified dimensions
		/// </summary>
		/// <param name="dims">list of dimensions</param>
		/// <param name="dimSortComparers">list of comparers that should be used for sorting dimension keys</param>
		/// <returns>array of keys for specified dimensions</returns>
		public object[][] GetDimensionKeys(string[] dims, IComparer<object>[] dimSortComparers) {
			if (dims==null)
				throw new ArgumentNullException("dims");
			if (dimSortComparers == null) {
				// for backward compatibility, by default keys are sorted A-Z
				dimSortComparers = new IComparer<object>[dims.Length];
				for (int i = 0; i < dimSortComparers.Length; i++) {
					dimSortComparers[i] = NaturalSortKeyComparer.Instance;
				}
			}
			return PivotDataHelper.GetDimensionKeys(this, dims, dimSortComparers);		
		}


		/// <summary>
		/// Returns compacted state object that contains all values of this PivotData.
		/// </summary>
		/// <remarks>State object doesn't include any information about dimensions and aggregator factory. 
		/// Calculated values (totals) are not included into state object.</remarks>
		/// <returns>state object that can be serialized</returns>
		public PivotDataState GetState() {
			return new PivotDataState(this);
		}

		/// <summary>
		/// Restores PivotData from specified state object.
		/// </summary>
		/// <remarks>This method assumes that PivotData configuration (dimensions, aggregator factory) matches specified state object.</remarks>
		/// <param name="state">state object</param>
		public void SetState(PivotDataState state) {
			if (state.DimCount!=Dimensions.Length)
				throw new ArgumentException("Incompatible number of dimensions", "state");

			Clear();
			values = new Dictionary<object[],IAggregator>( state.Values.Length, new ValueKeyEqualityComparer() );

			var revKeyIdx = new object[state.KeyValues.Length];
			for (var i = 0; i < revKeyIdx.Length; i++)
				revKeyIdx[i] = state.KeyValues[i];

			var dimLen = Dimensions.Length;
			uint d;
			IAggregator aggr;
			for (var i = 0; i < state.ValueKeys.Length; i++) {
				var vkIdx = state.ValueKeys[i];
				var vkDimKeys = new object[dimLen];
				
				for (d = 0; d < dimLen; d++) {
					vkDimKeys[d] = revKeyIdx[ vkIdx[d] ];
				}
				if (values.TryGetValue(vkDimKeys, out aggr)) {
					aggr.Merge(AggregatorFactory.Create(state.Values[i]));
				} else {
					values[vkDimKeys] = AggregatorFactory.Create(state.Values[i]);
				}
			}
			if (!lazyTotals)
				BatchCalcTotals();
		}

		/// <summary>
		/// Removes all dimension keys and values from the <see cref="PivotData"/>.
		/// </summary>
		public void Clear() {
			values.Clear();
			totalValues.Clear();
			valuesArr = null;
		}

		/// <summary>
		/// Process data from specified list of dictionaries.
		/// </summary>
		public void ProcessData(IEnumerable<IDictionary<string,object>> data) {
			ProcessData(data, GetDictionaryValue);
		}

		/// <summary>
		/// Process data from specified sequence of DataRow objects.
		/// </summary>
		/// <remarks>
		/// This method is not supported in netstandard1.5 build.
		/// </remarks>		 
		public void ProcessData(IEnumerable<DataRow> data) {
			ProcessData(data, GetDataRowValue);
		}

		/// <summary>
		/// Processes data from the specified <see cref="IDataReader"/>.
		/// </summary>
		/// <param name="dataReader">data reader instance</param>
		/// <remarks>
		/// Dimension names and field names specified for aggregators should correspond reader's column names.
		/// <example>
		/// How to pivot a DataTable:
		/// <code>
		/// DataTable tbl;  // this is table with columns: "col1", "col2", "value"
		/// var pvtData = new PivotData( 
		///   new []{"col1","col2"},
		///   new SumAggregatorFactory("value") );
		/// pvtData.ProcessData(new DataTableReader(tbl));
		/// </code>
		/// </example>
		/// </remarks>
		public void ProcessData(IDataReader dataReader) {
			ProcessData(GetDataReaderEnumeration(dataReader), GetDataRecordValue);
		}

#if NET_STANDARD21
		/// <summary>
		/// Processes data a from the specified <see cref="System.Data.Common.DbDataReader"/> asynchronously.
		/// </summary>
		/// <param name="dbDataReader">data reader instance</param>
		public Task ProcessDataAsync(System.Data.Common.DbDataReader dbDataReader, CancellationToken cancellationToken = default(CancellationToken)) {
			return ProcessDataAsync(
				GetDataReaderEnumerationAsync(dbDataReader, cancellationToken), 
				GetDataRecordValue, cancellationToken);
		}

		static async IAsyncEnumerable<object> GetDataReaderEnumerationAsync(System.Data.Common.DbDataReader dbDataReader, CancellationToken cancellationToken) {
			while (await dbDataReader.ReadAsync(cancellationToken)) {
				yield return dbDataReader;
			}
		}

#endif

		/// <summary>
		/// Processes data from the specified <see cref="IPivotData"/> instance and calculates <see cref="PivotData"/> values
		/// </summary>
		/// <param name="data">input data represented by <see cref="IPivotData"/> instance</param>
		/// <param name="aggregatorNames">field names for accessing aggregator values</param>
		/// <remarks>
		/// This overload allows to use values of another data cube as input.
		/// <example>
		/// Lets assume that we need to calculate average over values that are calculated in another cube:
		/// <code>
		/// IPivotData sourcePvtData; // dimensions: "month", "store" and one sum measure
		/// var pvtData = new PivotData(new[] {"store"}, new AverageAggregatorFactory("value") );
		/// pvtData.ProcessData(sourcePvtData, "value");
		/// </code>
		/// </example>
		/// </remarks>
		public void ProcessData(IPivotData data, params string[] aggregatorNames) {
			var aggrCount = 1;
			if (data.AggregatorFactory is CompositeAggregatorFactory)
				aggrCount = ((CompositeAggregatorFactory)data.AggregatorFactory).Factories.Length;
			if (aggregatorNames.Length>aggrCount)
				throw new ArgumentOutOfRangeException("Number of aggregators is less than number of provided names.");
			var pvtDataMember = new PivotDataMember(data, aggregatorNames);
			ProcessData(data, pvtDataMember.GetValue);
		}


		/// <summary>
		/// Processes data from enumerable data.
		/// </summary>
		/// <param name="data"><see cref="IEnumerable"/> data stream</param>
		/// <param name="getValue">accessor used for getting field values from iterated objects</param>
		/// <remarks>
		/// When LazyTotals=False and <see cref="PivotData.ProcessData"/> is called for the first time 
		/// all dimension totals are calculated for a whole batch. For all next calls totals are updated incrementally.
		/// </remarks>
		public virtual void ProcessData(IEnumerable data, Func<object, string, object> getValue) {
			var processor = new DataProcessor(this, getValue);
			processor.Init();
			foreach (var r in data) {
				processor.ProcessEntry(r);
			}
			processor.Finish();
		}

#if NET_STANDARD21
		/// <summary>
		/// Processes data from asynchronous enumerable data.
		/// </summary>
		/// <param name="data"><see cref="IAsyncEnumerable"/> asynchronous data stream</param>
		/// <param name="getValue">accessor used for getting field values from iterated objects</param>
		/// <remarks>
		/// When LazyTotals=False and <see cref="PivotData.ProcessDataAsync"/> is called for the first time 
		/// all dimension totals are calculated for a whole batch. For all next calls totals are updated incrementally.
		/// </remarks>
		public async Task ProcessDataAsync(IAsyncEnumerable<object> data, Func<object, string, object> getValue, CancellationToken cancellationToken = default(CancellationToken)) {
			var processor = new DataProcessor(this, getValue);
			processor.Init();
			await foreach (var r in data.WithCancellation(cancellationToken).ConfigureAwait(false)) {
				processor.ProcessEntry(r);
			}
			processor.Finish();
		}	
#endif

		/// <summary>
		/// Modifies the current <see cref="PivotData"/> object to merge values from itself and specified <see cref="PivotData"/>.
		/// </summary>
		/// <remarks>
		/// Only compatible <see cref="PivotData"/> objects could be merged: they should have the same AggregatorFactory and Dimensions configuration.
		/// This method is also useful for organizing parallel data aggregation algorithm.
		/// </remarks>
		/// <param name="pvtData">multidimensional dataset to merge</param>
		public virtual void Merge(IPivotData pvtData) {
			if (!AggregatorFactory.Equals(pvtData.AggregatorFactory))
				throw new ArgumentException("AggregatorFactory mismatch");
			if (Dimensions.Length!=pvtData.Dimensions.Length)
				throw new ArgumentException("Dimensions mismatch");
			for (int i=0;i<Dimensions.Length;i++)
				if (!Dimensions[i].SequenceEqual(pvtData.Dimensions[i]))
					throw new ArgumentException(String.Format("Dimension {0} elements mismatch", i));

			// remove all totals
			totalValues.Clear();
			valuesArr = null;

			IAggregator aggr;
			foreach (var dp in pvtData) {
				if (!values.TryGetValue(dp.Key, out aggr)) {
					aggr = aggregatorFactory.Create();
					values[dp.Key] = aggr;
				}
				aggr.Merge(dp.Value);
			}
			if (!lazyTotals)
				BatchCalcTotals();
		}

		private bool matchAll(object[] key, IAggregator entry) {
			return true;
		}

		[Obsolete("Use SliceQuery class instead")]
		public PivotData Slice(string[] dimensions, bool lazyTotals) {
			return Slice(dimensions, lazyTotals, matchAll);
		}


		[Obsolete("Use SliceQuery class instead")]
		public PivotData Slice(string[] dimensions, bool lazyTotals, Func<KeyValuePair<ValueKey, IAggregator>,bool> predicate) {
			return Slice(dimensions, lazyTotals, (key, aggr) => {
				return predicate( new KeyValuePair<ValueKey,IAggregator>(new ValueKey(key),aggr) );
			});
		}

		[Obsolete("Use SliceQuery class instead")]
		public PivotData Slice(string[] dimensions, bool lazyTotals, Func<object[], IAggregator,bool> predicate) {
			var toPivotData = new PivotData(dimensions, AggregatorFactory, lazyTotals);
			int copyDimLen = toPivotData.Dimensions.Length;
			var dimIndexes = new int[copyDimLen];
			for (int i = 0; i < toPivotData.Dimensions.Length; i++) {
				var dimToCopyIdx = Array.IndexOf( Dimensions, toPivotData.Dimensions[i] );
				if (dimToCopyIdx<0)
					throw new ArgumentException(String.Format("Dimension {0} does not exist", toPivotData.Dimensions[i]));
				dimIndexes[i] = dimToCopyIdx;
			}
			
			int d;
			CopyTo(toPivotData, 
				(key,aggr) => {
					if (!predicate(key,aggr))
						return null;
					var valKeyDims = new object[copyDimLen];
					for (d = 0; d < copyDimLen; d++) {
						valKeyDims[d] = key[dimIndexes[d]];
					}
					return valKeyDims;
				},
				(aggr) => {
					return aggr;
				}
			);
			return toPivotData;
		}

		[Obsolete("Use SliceQuery class instead")]
		public void CopyTo(PivotData toPivotData, Func<object[], IAggregator,object[]> mapKey, Func<IAggregator,IAggregator> mapAggr) {
			// remove all totals
			toPivotData.totalValues.Clear();

			IAggregator aggr;
			foreach (var entry in values) {
				var toKey = mapKey(entry.Key, entry.Value);
				if (toKey!= null) {
					if (!toPivotData.values.TryGetValue(toKey, out aggr)) {
						aggr = toPivotData.aggregatorFactory.Create();
						toPivotData.values[toKey] = aggr;
					}
					aggr.Merge( mapAggr(entry.Value) );
				}
			}
			if (!toPivotData.lazyTotals)
				toPivotData.BatchCalcTotals();
		}

		public IEnumerator<KeyValuePair<object[],IAggregator>> GetEnumerator() {
			return values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}

		protected class AllValuesCollection : ICollection<KeyValuePair<ValueKey, IAggregator>> {

			PivotData PvtData;

			public AllValuesCollection(PivotData pvtData) {
				PvtData = pvtData;
			} 
			
			public void Add(KeyValuePair<ValueKey, IAggregator> item) {
				throw new NotSupportedException();
			}

			public void Clear() {
				throw new NotSupportedException();
			}

			public bool Contains(KeyValuePair<ValueKey, IAggregator> item) {
				throw new NotImplementedException();
			}

			public void CopyTo(KeyValuePair<ValueKey, IAggregator>[] array, int arrayIndex) {
				throw new NotImplementedException();
			}

			public int Count {
				get { return PvtData.values.Count + PvtData.totalValues.Count; }
			}

			public bool IsReadOnly {
				get { return true; }
			}

			public bool Remove(KeyValuePair<ValueKey, IAggregator> item) {
				throw new NotSupportedException();
			}

			public IEnumerator<KeyValuePair<ValueKey, IAggregator>> GetEnumerator() {
				foreach (var entry in PvtData.values)
					yield return new KeyValuePair<ValueKey,IAggregator>(new ValueKey(entry.Key),entry.Value);
				foreach (var entry in PvtData.totalValues)
					yield return new KeyValuePair<ValueKey,IAggregator>(new ValueKey(entry.Key),entry.Value);
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return this.GetEnumerator();
			}
		}

		protected sealed class DataProcessor {
			readonly PivotData pvtData;
			readonly Func<object, string, object> getValue;
			readonly string[] dimensions;
			Action<object[], Func<object, string, object>, object> calcTotals;

			bool incTotalsMode;
			bool notLazyTotals;
			readonly bool doCalcTotals;
			object[] valKey;

			internal DataProcessor(PivotData pvtData, Func<object, string, object> getValue) {
				this.pvtData = pvtData;
				this.getValue = getValue;
				this.dimensions = pvtData.dimensions;
				incTotalsMode = pvtData.values.Count > 0;
				notLazyTotals = !pvtData.lazyTotals;
				doCalcTotals = incTotalsMode && notLazyTotals;
			}

			internal void Init() {
				if (pvtData.lazyTotals && pvtData.totalValues.Count > 0)
					pvtData.totalValues.Clear(); // clear cached totals calculation
				pvtData.valuesArr = null; // flush values array cache
				switch (pvtData.dimensions.Length) {
					case 1: calcTotals = pvtData.CalcTotals1D; break;
					case 2: calcTotals = pvtData.CalcTotals2D; break;
					default: calcTotals = pvtData.CalcTotalsAny; break;
				}
				valKey = new object[dimensions.Length];
			}

			internal void ProcessEntry(object r) {
				var valKeyUsed = false;
				// compose key
				for (int d = 0; d < valKey.Length; d++) {
					valKey[d] = getValue(r, dimensions[d]) ?? DBNull.Value;
				}

				if (!pvtData.values.TryGetValue(valKey, out var aggr)) {
					aggr = pvtData.aggregatorFactory.Create();
					pvtData.values[valKey] = aggr;
					// ref to valKey is saved inside values, lets create new array
					valKeyUsed = true;
				}
				aggr.Push(r, getValue);

				if (doCalcTotals)
					calcTotals(valKey, getValue, r);

				if (valKeyUsed)
					valKey = new object[dimensions.Length];
			}

			internal void Finish() {
				if (!incTotalsMode && notLazyTotals) {
					pvtData.BatchCalcTotals();
				}
			}

		}

	}
}
