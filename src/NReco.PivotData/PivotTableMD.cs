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
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace NReco.PivotData
{

	/// <summary>
	/// Represents n-Dimensional Pivot Table view for multidimensional dataset.
	/// </summary>
	/// <remarks>
	/// <see cref="PivotTableMD"/> provides an API for building n-dimensional pivot table report (with >2 axies; for <=2 axies it is more natural to use <see cref="PivotTable"/>)
	/// from multidimensional dataset represented by <see cref="PivotData"/>.
	/// </remarks>
	public class PivotTableMD
	{
		/// <summary>
		/// Dimensions used for building axes of n-Dimensional Pivot Table view
		/// </summary>
		public string[][] Axes { get; private set; }

		/// <summary>
		/// Dimension keys that represent axes of n-Dimensional Pivot Table view
		/// </summary>
		public ValueKey[][] AxesKeys { get; private set; }

		/// <summary>
		/// Gets the <see cref="IPivotData"/> used for building pivot table.
		/// </summary>
		public IPivotData PivotData { 
			get { return PvtData; }
		}

		int[][] AxesIndexes;
		IPivotData PvtData;

		/// <summary>
		/// Initializes new instance of <see cref="PivotTable"/> instance by specified <see cref="PivotData"/>
		/// </summary>
		/// <param name="axes">list of axes determined by dimensions</param>
		/// <param name="pvtData">multidimensional dataset used for pivot table calculation</param>
		public PivotTableMD(string[][] axes, IPivotData pvtData) : this(axes,pvtData, null) {
		}

		/// <summary>
		/// Initializes new instance of <see cref="PivotTable"/> instance by specified <see cref="PivotData"/>
		/// </summary>
		/// <param name="axes">list of axes determined by dimensions</param>
		/// <param name="dataCube">multidimensional dataset used for calculating pivot table</param>
		/// <param name="axesComparers">list of custom comparers for sorting axes keys</param>
		public PivotTableMD(string[][] axes, IPivotData pvtData, IComparer<ValueKey>[] axesComparers) {
			Axes = axes;
			PvtData = pvtData;
			AxesIndexes = axes.Select(axis => GetDimIdx(axis)).ToArray();
			AxesKeys = new ValueKey[Axes.Length][];
			for (int i=0; i<Axes.Length; i++)	
				AxesKeys[i] = GetAxisKeys(i, 
					axesComparers!=null && i<axesComparers.Length && axesComparers[i]!=null ? axesComparers[i] : NaturalSortKeyComparer.Instance);
		}

		ValueKey[] GetAxisKeys(int axisIndex, IComparer<ValueKey> axisComparer) {
			var dimIndexes = AxesIndexes[axisIndex];
			var r = new HashSet<ValueKey>();
			var dimsLen = dimIndexes.Length;
			int i;
			bool hasEmpty;

			foreach (var val in PvtData)
			{
				var dimKeys = new object[dimsLen];
				hasEmpty = false;
				for (i = 0; i < dimsLen; i++)
				{
					if (Key.IsEmpty(dimKeys[i] = val.Key[dimIndexes[i]]))
					{
						hasEmpty = true;
						break;
					}
				}
				if (!hasEmpty)
				{
					r.Add(new ValueKey(dimKeys));
				}
			}

			var keys = new ValueKey[r.Count];
			r.CopyTo(keys);
			Array.Sort<ValueKey>(keys, axisComparer);
			return keys;
		}

		private string[] GetAxis(int idx) {
			return Axes.Length > idx ? Axes[idx] : new string[0];
		}
		private ValueKey[] GetKeys(int idx) {
			return AxesKeys.Length > idx ? AxesKeys[idx] : new ValueKey[0];
		}
		private int[] GetIndexes(int idx) {
			return AxesIndexes.Length > idx ? AxesIndexes[idx] : new int[0];
		}

		int[] GetDimIdx(string[] dims)
		{
			var r = new int[dims.Length];
			for (int i = 0; i < dims.Length; i++)
			{
				r[i] = Array.IndexOf(PvtData.Dimensions, dims[i]);
			}
			return r;
		}

		/// <summary>
		/// Returns sortable value from specified <see cref="IAggregator"/> instance
		/// </summary>
		/// <param name="aggr">aggregator</param>
		/// <returns>value used by <see cref="PivotTableMD.SortAxisKeys"/> for sorting</returns>
		protected virtual object GetSortValue(IAggregator aggr) {
			return aggr.Value;
		}

		/// <summary>
		/// Sort axis keys by datapoint values
		/// </summary>
		/// <param name="axisToSort">axis index of keys to sort</param>
		/// <param name="otherCoords">data point indexes used for determining sort values (null index means totals value by that axis)</param>
		/// <param name="sortDirection">sort direction (asc by default)</param>
		public void SortAxisKeys(int axisToSort, int?[] otherCoords, ListSortDirection sortDirection = ListSortDirection.Ascending)
		{
			var keys = GetKeys(axisToSort);
			var colValues = new object[keys.Length];
			var dtCoords = new int?[Axes.Length];
			for (int i = 0; i<dtCoords.Length; i++)
				dtCoords[i] = otherCoords!=null && i<otherCoords.Length ? otherCoords[i] : null;

			for (int i = 0; i < colValues.Length; i++) {
				dtCoords[axisToSort] = i;
				colValues[i] = GetSortValue(this[dtCoords]);
			}
			Array.Sort(colValues, keys);
			if (sortDirection == ListSortDirection.Descending)
				Array.Reverse(keys);
		}

		/// <summary>
		/// Gets aggregator for specified axes indexes
		/// </summary>
		/// <param name="coords">data point indexes</param>
		/// <returns>aggregator for requested data point</returns>
		public IAggregator this[params int?[] coords]
		{
			get
			{
				var pvtDataKey = new object[PvtData.Dimensions.Length];
				for (int i = 0; i < pvtDataKey.Length; i++)
				{
					pvtDataKey[i] = Key.Empty;
				}

				for (int ax = 0; ax < coords.Length; ax++)
				{
					var coord = coords[ax];
					if (coord.HasValue)
					{
						var keys = GetKeys(ax);
						var indexes = GetIndexes(ax);
						var key = keys[coord.Value];
						for (int i = 0; i < indexes.Length; i++)
						{
							var dimKey = key.DimKeys[i];
							var dimIdx = indexes[i];
							if (!Key.IsEmpty(pvtDataKey[dimIdx]) && !Key.Equals(pvtDataKey[dimIdx], dimKey)) {
								// lets handle special case when the same dimension is selected for both rows and columns
								// in this case lets return empty aggregator for different keys of the same dimension
								return PvtData.AggregatorFactory.Create();
							}
							pvtDataKey[dimIdx] = dimKey;
						}
					}
				}
				return PvtData[pvtDataKey];
			}
		}
	}
}
