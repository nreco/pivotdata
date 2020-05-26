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
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace NReco.PivotData {
	
	/// <summary>
	/// Represents 2D Pivot Table view for multidimensional array (<see cref="IPivotData"/>).
	/// </summary>
	/// <remarks>
	/// PivotTable provides an API for building pivot table report from multidimensional array represented by <see cref="IPivotData"/> instance.
	/// Configuration is rather simple: all it needs is a list of dimensions for building rows and columns 
	/// (it may use only subset of dimensions available in specified <see cref="PivotData"/> instance). Also it supports table sorting by
	/// column or row values (or by totals). 
	/// <example>
	/// The following code snippet illustrates how to use <see cref="PivotTable"/>:
	/// <code>
	/// PivotData pvtData; 
	/// var pvtTbl = new PivotTable(
	///		new [] {"country", "city" }, // rows are cities grouped by country
	///		new [] {"company"},  // columns are companines
	///		pvtData);
	///	for (var r=0; r&lt;pvtTbl.RowKeys.Length; r++) {
	///		for (var c=0; c&lt;pvtTbl.ColumnKeys.Length; c++) {
	///			Console.Write("{0}\t", pvtTbl[r,c].Value);
	///		}
	///		Console.WriteLine();
	/// }
	/// </code>
	/// </example>
	/// </remarks>
	public class PivotTable : IPivotTable {

		/// <summary>
		/// Dimensions used for building columns of the pivot table
		/// </summary>
		public string[] Columns { get; private set; }

		/// <summary>
		/// Dimensions used for building rows of the pivot table 
		/// </summary>
		public string[] Rows { get; private set; }

		/// <summary>
		/// Dimension keys that represent columns of the pivot table
		/// </summary>
		/// <remarks>These keys are used for iterating by pivot table columns indexes. 
		/// Order of table columns can be easily changed by reordering of <see cref="PivotTable.ColumnKeys"/> array.</remarks>
		public ValueKey[] ColumnKeys { get; private set; }

		/// <summary>
		/// Dimension keys that represent rows of the pivot table
		/// </summary>
		/// <remarks>These keys are used for iterating by pivot table rows indexes. 
		/// Order of table rows can be easily changed by reordering of <see cref="PivotTable.ColumnKeys"/> array.</remarks> 
		public ValueKey[] RowKeys { get; private set; }

		/// <summary>
		/// Gets or sets flag that preserves grouping order when <see cref="PivotTable.SortRowKeys"/> or <see cref="PivotTable.SortColumnKeys"/> is called (false by default).
		/// </summary>
		/// <remarks>When pivot table is sorted by values this may break labels grouping. 
		/// This option keeps labels grouping by sorting values only inside groups.</remarks>
		public bool PreserveGroupOrder { get; set; }

		/// <summary>
		/// Controls totals cache usage (true by default). 
		/// </summary>
		/// <remarks>If <see cref="TotalsCache"/> is <code>true</code> PivotTable will use <see cref="TotalsCachePivotData"/> wrapper for efficient totals calculation.</remarks>
		public bool TotalsCache { get; set; }

		/// <summary>
		/// This comparer is used when table is ordered by values. Null by default (in this case default comparer is used).
		/// </summary>
		public IComparer ValuesComparer { get; set; }

		int[] ColumnIndexes;
		int[] RowIndexes;
		IPivotData PvtData;
		IPivotData TotalsCachePvtData;
		bool AlwaysHasEmpty;
		bool IsMultiValue;

		/// <summary>
		/// Gets the <see cref="IPivotData"/> used for building pivot table.
		/// </summary>
		public IPivotData PivotData { 
			get { return PvtData; }
		}

		/// <summary>
		/// Initializes a new instance of <see cref="PivotTable"/> instance by specified <see cref="PivotData"/>
		/// </summary>
		/// <param name="rows">list of dimensions for determining table rows</param>
		/// <param name="columns">list of dimensions for determining table columns</param>
		/// <param name="pvtData">multidimensional dataset used for pivot table calculation</param>
		public PivotTable(string[] rows, string[] columns, IPivotData pvtData) : this(rows,columns,pvtData,null,null) {
		}

		/// <summary>
		/// Initializes a new instance of <see cref="PivotTable"/> instance by specified <see cref="PivotData"/>
		/// </summary>
		/// <param name="rows">list of dimensions for determining table rows</param>
		/// <param name="columns">list of dimensions for determining table columns</param>
		/// <param name="pvtData">multidimensional dataset used for pivot table calculation</param>
		/// <param name="rowKeysComparer">custom table row keys comparer (if null <see cref="NaturalSortKeyComparer"/> is used)</param>
		/// <param name="colKeysComparer">custom table row keys comparer (if null <see cref="NaturalSortKeyComparer"/> is used)</param>
		public PivotTable(string[] rows, string[] columns, IPivotData pvtData, IComparer<ValueKey> rowKeysComparer, IComparer<ValueKey> colKeysComparer) {
			PreserveGroupOrder = false;
			TotalsCache = true;
			Columns = columns ?? new string[0];
			Rows = rows ?? new string[0];
			PvtData = pvtData;
			TotalsCachePvtData = new TotalsCachePivotData(pvtData);
			ColumnIndexes = GetDimIdx(Columns);
			RowIndexes = GetDimIdx(Rows);
			AlwaysHasEmpty = ColumnIndexes.Union(RowIndexes).Count() < PvtData.Dimensions.Length;
			IsMultiValue = pvtData.AggregatorFactory is CompositeAggregatorFactory;

			GenerateAxesKeys();

			SortKeys(ColumnKeys, Columns, colKeysComparer ?? NaturalSortKeyComparer.Instance);
			SortKeys(RowKeys, Rows, rowKeysComparer ?? NaturalSortKeyComparer.Instance);
		}

		void GenerateAxesKeys() {
			var rowKeySet = RowIndexes.Length>0 ? new HashSet<ValueKey>() : null;
			var colKeySet = ColumnIndexes.Length>0 ? new HashSet<ValueKey>() : null;

			// try to reuse array and ValueKey instance to minimize number of allocations
			object[] rowKeyVals = new object[RowIndexes.Length];
			ValueKey rowKey = new ValueKey(rowKeyVals);

			object[] colKeyVals = new object[ColumnIndexes.Length];
			ValueKey colKey = new ValueKey(colKeyVals);

			foreach (var val in PvtData) {
				if (rowKeySet != null) {
					for (int i = 0; i < rowKeyVals.Length; i++) {
						rowKeyVals[i] = val.Key[RowIndexes[i]];
					}
					if (rowKeySet.Add(rowKey)) {
						rowKeyVals = new object[RowIndexes.Length];
						rowKey = new ValueKey(rowKeyVals);
					}
				}
				if (colKeySet != null) {
					for (int i = 0; i < colKeyVals.Length; i++) {
						colKeyVals[i] = val.Key[ColumnIndexes[i]];
					}
					if (colKeySet.Add(colKey)) {
						colKeyVals = new object[ColumnIndexes.Length];
						colKey = new ValueKey(colKeyVals);
					}
				}
			}
			RowKeys = getKeys(rowKeySet);
			ColumnKeys = getKeys(colKeySet);

			ValueKey[] getKeys(HashSet<ValueKey> set) {
				if (set != null) {
					var keys = new ValueKey[set.Count];
					set.CopyTo(keys);
					return keys;
				} else {
					return new ValueKey[0];
				}
			}
		}

		/// <summary>
		/// Performs inital ordering of pivot table rows/columns keys 
		/// </summary>
		/// <param name="keys">array of pivot table axis (rows or columns) keys</param> 
		/// <param name="dimensions">array of dimensions for given keys</param>
		/// <remarks>
		/// This method is called for initial ordering of pivot table rows and columns. 
		/// It may be overrided in inherited class if custom ordering logic should be applied 
		/// (note that custom sort also can be applied by ordering <see cref="PivotTable.ColumnKeys"/> and <see cref="PivotTable.RowKeys"/> properites.
		/// </remarks>
		protected virtual void SortKeys(ValueKey[] keys, string[] dimensions, IComparer<ValueKey> comparer) {
			Array.Sort<ValueKey>(keys, comparer);
		}

		int[] GetDimIdx(string[] dims) {
			var r = new int[dims.Length];
			for (int i = 0; i < dims.Length; i++) {
				r[i] = Array.IndexOf(PvtData.Dimensions, dims[i]);
				if (r[i]<0)
					throw new ArgumentException(String.Format("Unknown dimension: {0}",dims[i]));
			}
			return r;
		}

		private object GetSortValue(IAggregator aggr, int aggrIndex) {
			if (IsMultiValue) {
				var compositeAggr = aggr as CompositeAggregator;
				if (compositeAggr!=null && aggrIndex<compositeAggr.Aggregators.Length)
					return compositeAggr.Aggregators[aggrIndex].Value;
			}
			return aggr.Value;
		}

		private void ArraySortRange(object[] values, object[] keys, int start, int len, bool reverse) {
			if (len<=1)
				return;
			var comparer = ValuesComparer;
			if (comparer == null && ListValuesComparer.HasListValue(values))
				comparer = new ListValuesComparer();
			Array.Sort(values, keys, start, len, comparer );
			if (reverse)
				Array.Reverse(keys, start, len);
		}

		private void SortKeysByValues(string[] keyDims, ValueKey[] keys, object[] values, ListSortDirection sortDirection) {
			if (PreserveGroupOrder && keyDims.Length>1) {
				int grpIdx = keyDims.Length-2;
				int startIdx = 0;
				for (int i=1; i<values.Length; i++) {
					if (!keys[i].DimKeys[grpIdx].Equals(keys[i - 1].DimKeys[grpIdx])) {
						ArraySortRange(values, keys, startIdx, i-startIdx, sortDirection==ListSortDirection.Descending);
						startIdx = i;
					}
				}
				// last group
				ArraySortRange(values, keys, startIdx, values.Length-startIdx, sortDirection==ListSortDirection.Descending);
			} else {
				var comparer = ValuesComparer;
				if (comparer == null && ListValuesComparer.HasListValue(values))
					comparer = new ListValuesComparer();
				Array.Sort(values, keys, comparer);
				if (sortDirection==ListSortDirection.Descending)
					Array.Reverse(keys);
			}
		}

		/// <summary>
		/// Sort rows by specified column values.
		/// </summary>
		/// <param name="columnIndex">column index (use null to sort by totals)</param>
		/// <param name="sortDirection">sort direction (asc by default)</param>
		public void SortRowKeys(int? columnIndex, ListSortDirection sortDirection = ListSortDirection.Ascending) {
			SortRowKeys(columnIndex, 0, sortDirection);
		}

		/// <summary>
		/// Sort rows by specified column values.
		/// </summary>
		/// <param name="columnIndex">column index (use null to sort by totals)</param>
		/// <param name="measureIndex">measure index (applicable only for cubes with several measures)</param>
		/// <param name="sortDirection">sort direction (asc by default)</param>
		public void SortRowKeys(int? columnIndex, int measureIndex, ListSortDirection sortDirection = ListSortDirection.Ascending) {
			var colKey = columnIndex.HasValue ? ColumnKeys[columnIndex.Value] : null;
			SortRowKeysByColumnKey(colKey, measureIndex, sortDirection);
		}

		/// <summary>
		/// Sort rows by specified column key.
		/// </summary>
		/// <param name="colKey">column key (use null to sort by totals)</param>
		/// <param name="measureIndex">measure (aggregator) index</param>
		/// <param name="sortDirection">sort direction (asc by default)</param>
		public void SortRowKeysByColumnKey(ValueKey colKey, int measureIndex, ListSortDirection sortDirection = ListSortDirection.Ascending) {
			var rowValues = new object[RowKeys.Length];
			for (int i=0; i<RowKeys.Length; i++) {
				rowValues[i] = GetSortValue(GetValue(RowKeys[i],colKey), measureIndex);
			}
			SortKeysByValues(Rows, RowKeys, rowValues, sortDirection);
		}


		/// <summary>
		/// Sort columns by specified row values.
		/// </summary>
		/// <param name="rowIndex">row index (use null to sort by totals)</param>
		/// <param name="sortDirection">sort direction</param>
		public void SortColumnKeys(int? rowIndex, ListSortDirection sortDirection = ListSortDirection.Ascending) {
			SortColumnKeys(rowIndex, 0, sortDirection);
		}

		/// <summary>
		/// Sort columns by specified row values
		/// </summary>
		/// <param name="rowIndex">row index (use null to sort by totals)</param>
		/// <param name="measureIndex">measure index (applicable only for cubes with several measures)</param>	 
		/// <param name="sortDirection">sort direction</param>
		public void SortColumnKeys(int? rowIndex, int measureIndex, ListSortDirection sortDirection = ListSortDirection.Ascending) {
			var rowKey = rowIndex.HasValue ? RowKeys[rowIndex.Value] : null;
			SortColumnKeysByRowKey(rowKey, measureIndex, sortDirection);
		}

		/// <summary>
		/// Sort columns by specified row key.
		/// </summary>
		/// <param name="rowKey">row key (use null to sort by totals)</param>
		/// <param name="measureIndex">measure index (applicable only for cubes with several measures)</param>	 
		/// <param name="sortDirection">sort direction</param>
		public void SortColumnKeysByRowKey(ValueKey rowKey, int measureIndex, ListSortDirection sortDirection = ListSortDirection.Ascending) {
			var colValues = new object[ColumnKeys.Length];
			for (int i=0; i<ColumnKeys.Length; i++) {
				colValues[i] = GetSortValue( GetValue(rowKey, ColumnKeys[i]), measureIndex);
			}
			SortKeysByValues(Columns, ColumnKeys, colValues, sortDirection);
		}

		/// <summary>
		/// Gets aggregator for specified row and column indexes
		/// </summary>
		/// <param name="row">row index (can be null for totals)</param>
		/// <param name="col">column index (can be null for totals)</param>
		/// <returns>aggregator for row x column intersection</returns>
		public IAggregator this[int? row, int? col] {
			get {
				return GetValue( row.HasValue ? RowKeys[row.Value] : null, col.HasValue?ColumnKeys[col.Value] : null );
			}
		}

		/// <summary>
		/// Gets value for specified row and column keys. 
		/// </summary>
		/// <param name="rowKey">row key (use partial key for sub-totals)</param>
		/// <param name="colKey">column key (use partial key for sub-totals)</param>
		/// <returns>aggregator for row x column intersection</returns>
		public virtual IAggregator GetValue(ValueKey rowKey, ValueKey colKey) {
			var pvtDataKey = new object[PvtData.Dimensions.Length];
			for (int i=0; i<pvtDataKey.Length; i++)
				pvtDataKey[i] = Key.Empty;

			bool hasEmpty = AlwaysHasEmpty;
			if (rowKey!=null) {
				for (int i = 0; i < RowIndexes.Length; i++) {
					var rowKeyVal = rowKey.DimKeys[i];
					pvtDataKey[RowIndexes[i]] = rowKeyVal;
					if (Key._Empty == rowKeyVal)
						hasEmpty = true;
				}
			} else {
				if (RowIndexes.Length>0)
					hasEmpty = true;
			}

			if (colKey!=null) {
				for (int i = 0; i < ColumnIndexes.Length; i++) { 
					var colKeyVal = colKey.DimKeys[i];
					var dimIdx = ColumnIndexes[i];
					if (Key._Empty != pvtDataKey[dimIdx] && !Key.Equals(pvtDataKey[dimIdx], colKeyVal)) {
						// lets handle special case when the same dimension is selected for both rows and columns
						// in this case lets return empty aggregator for different keys of the same dimension
						return PvtData.AggregatorFactory.Create();
					}
					pvtDataKey[dimIdx] = colKeyVal;
					if (Key._Empty == colKeyVal)
						hasEmpty = true;
				}
			} else {
				if (ColumnIndexes.Length>0)
					hasEmpty = true;
			}
			return (TotalsCache && hasEmpty ? TotalsCachePvtData[pvtDataKey] : PvtData[pvtDataKey]) ?? PvtData.AggregatorFactory.Create();
		}

		internal class ListValuesComparer : IComparer {
			IComparer DefaultComparer;

			internal ListValuesComparer() {
				DefaultComparer = Comparer.Default;
			}

			public int Compare(object x, object y) {
				var xList = x as IList;
				var yList = y as IList;
				if (xList == null && yList == null)
					return DefaultComparer.Compare(x, y);
				if (xList == null && yList != null)
					return yList.Count > 0 ? -1 : 1;
				if (yList == null && xList != null)
					return xList.Count > 0 ? 1 : -1;
				if (xList.Count != yList.Count)
					return xList.Count.CompareTo(yList.Count);
				for (int i=0; i<xList.Count; i++) {
					var cmp = DefaultComparer.Compare(xList[i], yList[i]);
					if (cmp != 0)
						return cmp;
				}
				return 0;
			}

			internal static bool HasListValue(object[] arr) {
				for (int i = 0; i < arr.Length; i++)
					if (arr[i] is IList)
						return true;
				return false;
			}
		}

	}
}
