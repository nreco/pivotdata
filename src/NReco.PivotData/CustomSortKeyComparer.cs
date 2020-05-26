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

namespace NReco.PivotData
{

	/// <summary>
	/// <see cref="ValueKey"/> comparer based on individual dimension comparers.
	/// </summary>
	public class CustomSortKeyComparer : IComparer<ValueKey>
	{
		IComparer<object>[] DimensionComparers;
		
		/// <summary>
		/// Initializes new instance of <see cref="CustomSortKeyComparer"/> with specified dimension comparers.
		/// </summary>
		/// <param name="dimComparers">array of dimension comparers</param>
		/// <remarks>If ValueKey has more dimensions than dimension comparers default <see cref="NaturalSortKeyComparer"/> is used.</remarks>
		public CustomSortKeyComparer(IComparer<object>[] dimComparers)
		{
			DimensionComparers = dimComparers;
		}

		public int Compare(ValueKey x, ValueKey y)
		{
			var xLen = x.DimKeys.Length;
			var yLen = y.DimKeys.Length;
			for (int i = 0; i < xLen && i < yLen; i++)
			{
				var xVal = x.DimKeys[i];
				var yVal = y.DimKeys[i];
				var comparer = i<DimensionComparers.Length && DimensionComparers[i]!=null ?
					DimensionComparers[i] : NaturalSortKeyComparer.Instance;
				var cmp = comparer.Compare(xVal, yVal);
				if (cmp != 0)
					return cmp;
			}
			return 0;
		}
	}
}