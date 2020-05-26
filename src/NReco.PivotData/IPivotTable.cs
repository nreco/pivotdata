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

namespace NReco.PivotData {
	
	/// <summary>
	/// Represents an abstract pivot table model.
	/// </summary>
	public interface IPivotTable {

		/// <summary>
		/// Dimensions for pivot table columns.
		/// </summary>
		string[] Columns { get; }

		/// <summary>
		/// Dimensions for pivot table rows. 
		/// </summary>
		string[] Rows { get; }

		/// <summary>
		/// Gets the collection of column keys.
		/// </summary>
		ValueKey[] ColumnKeys { get; }

		/// <summary>
		/// Gets the collection of row keys.
		/// </summary>
		ValueKey[] RowKeys { get; }

		/// <summary>
		/// Gets the underlying <see cref="IPivotData"/> instance.
		/// </summary>
		IPivotData PivotData { get; }

		/// <summary>
		/// Gets value for specified row and column keys. 
		/// </summary>
		/// <param name="rowKey">row key (use partial key for sub-totals or null for column total)</param>
		/// <param name="colKey">column key (use partial key for sub-totals or null for row total)</param>
		/// <returns></returns>
		IAggregator GetValue(ValueKey rowKey, ValueKey colKey);
	}
}
