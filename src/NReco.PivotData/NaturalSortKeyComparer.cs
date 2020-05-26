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

namespace NReco.PivotData {
	
	/// <summary>
	/// Implements lexicographical comparison for any 2 objects (A-Z or Z-A).
	/// </summary>
	public class NaturalSortKeyComparer : IComparer<object>, IComparer<ValueKey> {

		/// <summary>
		/// Represents an instance of <see cref="NaturalSortKeyComparer"/> for lexicographical comparison (A-Z).
		/// </summary>
		public static readonly NaturalSortKeyComparer Instance = new NaturalSortKeyComparer();

		/// <summary>
		/// Represents an instance of <see cref="NaturalSortKeyComparer"/> for reverse lexicographical comparison (Z-A).
		/// </summary>
		public static readonly NaturalSortKeyComparer ReverseInstance = new NaturalSortKeyComparer(true);

		private int Direction = 1;

		/// <summary>
		/// Initializes a new instance of <see cref="NaturalSortKeyComparer"/> instance for lexicographical comparison (A-Z).
		/// </summary>
		public NaturalSortKeyComparer() {
		}

		/// <summary>
		/// Initializes a new instance of <see cref="NaturalSortKeyComparer"/> instance for reverse lexicographical comparison (Z-A).
		/// </summary>
		public NaturalSortKeyComparer(bool reverse) {
			if (reverse)
				Direction = -1;
		}

		public int Compare(object xVal, object yVal) {
			if (DBNull.Value.Equals(xVal))
				xVal = null;
			if (DBNull.Value.Equals(yVal))
				yVal = null;
			if (xVal is IComparable) {
				if (yVal != null && yVal.GetType() != xVal.GetType()) {
					xVal = xVal.ToString();
					yVal = yVal.ToString();
				}
				return ((IComparable)xVal).CompareTo(yVal)*Direction;
			}
			if (yVal is IComparable) { 
				if (xVal != null && yVal.GetType() != xVal.GetType()) {
					xVal = xVal.ToString();
					yVal = yVal.ToString();
				}				
				return (-((IComparable)yVal).CompareTo(xVal))*Direction;
			}
			return 0;
		}

		public int Compare(ValueKey x, ValueKey y) {
			var xLen = x.DimKeys.Length;
			var yLen = y.DimKeys.Length;
			for (int i = 0; i < xLen && i < yLen; i++) {
				var xVal = x.DimKeys[i];
				var yVal = y.DimKeys[i];
				var cmp = Compare(xVal, yVal);
				if (cmp!=0)
					return cmp;
			}
			return 0;
		}

	}
}
