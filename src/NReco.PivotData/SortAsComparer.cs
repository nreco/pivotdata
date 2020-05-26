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
	/// Comparer for custom keys order defined by explicit list.
	/// </summary>
	/// <example>
	/// var statusDimCmp = new SortAsComparer( new [] { "New", "Open", "Closed" } );
	/// </example>
	public class SortAsComparer : IComparer<object> {
		IDictionary<object,int> objToIndex;
		int maxIdx;

		public SortAsComparer(object[] list) {
			objToIndex = new Dictionary<object,int>();
			for (int i = 0; i < list.Length; i++) {
				objToIndex[list[i]] = i;
			}
			maxIdx = list.Length;
		}
		public int Compare(object x, object y) {
			int xIdx;
			int yIdx;
			if (!objToIndex.TryGetValue(x, out xIdx))
				xIdx = maxIdx;
			if (!objToIndex.TryGetValue(y, out yIdx))
				yIdx = maxIdx;
			return (xIdx==maxIdx && yIdx==maxIdx) ? NaturalSortKeyComparer.Instance.Compare(x,y) : xIdx.CompareTo(yIdx);
		}
	}

}
