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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NReco.PivotData {
	
	/// <summary>
	/// Utility routines for <see cref="IPivotData"/> implementations.
	/// </summary>
	public static class PivotDataHelper {

		internal static bool IsConvertibleToDecimal(Type type) {
			switch (Type.GetTypeCode( Nullable.GetUnderlyingType(type) ?? type )) {
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
					return true;
			}
			return false;
		}
		internal static bool IsConvertibleToDouble(Type type) {
			switch (Type.GetTypeCode( Nullable.GetUnderlyingType(type) ?? type )) {
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
			}
			return false;
		}

		/// <summary>
		/// Suggest dimension type by key values.
		/// </summary>
		/// <param name="dimKeys">dimension keys</param>
		/// <returns>type compatible with all keys or string type</returns>
		public static Type GetDimensionType(IEnumerable dimKeys) {
			Type kType;
			Type t = null;
			foreach (var k in dimKeys) {
				if (k==null || DBNull.Value.Equals(k))
					continue;
				kType = k.GetType();
				if (t == null) {
					t = kType;
				} else if (t != kType) {
					if (t.IsAssignableFrom(kType))
						continue; // compatible type
					if (kType.IsAssignableFrom(t)) {
						t = kType;
					} else {
						if ( (t==typeof(decimal) || IsConvertibleToDecimal(t)) && IsConvertibleToDecimal(kType)) {
							t = typeof(decimal);
						} else if ( (t==typeof(double) || IsConvertibleToDouble(t)) && IsConvertibleToDouble(kType)) { 
							t = typeof(double);
						} else {
							t = null; // keys have no compatible type
							break;
						}
					}
				}
			}
			return t ?? typeof(string); // any object may be converted to string
		}

		/// <summary>
		/// Returns unique keys of specified dimensions for the <see cref="IPivotData"/> instance.
		/// </summary>
		/// <param name="pvtData"><see cref="IPivotData"/> instance</param>
		/// <param name="dims">list of dimensions</param>
		/// <param name="dimSortComparers">list of comparers that should be used for sorting dimension keys</param>
		/// <returns>array of keys for specified dimensions</returns>
		public static object[][] GetDimensionKeys(IPivotData pvtData, string[] dims, IComparer<object>[] dimSortComparers) {
			if (dims==null)
				dims = pvtData.Dimensions;
			var dimLen = dims.Length;
			var dimKeysArr = new object[dimLen][];
			var dimKeys = new HashSet<object>[dimLen];
			var dimIndexes = new int[dimLen];
			int d;
			for (d = 0; d < dimLen; d++) { 
				var dimIdx = Array.IndexOf(pvtData.Dimensions, dims[d]);
				if (dimIdx<0)
					throw new ArgumentOutOfRangeException("dims", String.Format("Unknown dimension: {0}",dims[d]));
				dimIndexes[d] = dimIdx;
				dimKeys[d] = new HashSet<object>();
			}
			foreach (var entry in pvtData) {
				for (d=0; d<dimLen; d++)
					dimKeys[d].Add(entry.Key[ dimIndexes[d] ]);
			}

			for (d = 0; d < dims.Length; d++) {
				dimKeysArr[d] = new object[dimKeys[d].Count];
				dimKeys[d].CopyTo(dimKeysArr[d]);
				if (dimSortComparers != null) { 
					// apply sort
					Array.Sort(dimKeysArr[d], 
						d<dimSortComparers.Length && dimSortComparers[d]!=null ? 
							dimSortComparers[d] : NaturalSortKeyComparer.Instance);
				}
			}
			return dimKeysArr;
		}
	}
}
