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
	/// Represents an abstract mutlidimensional array.
	/// </summary>
	public interface IPivotData : IEnumerable<KeyValuePair<object[],IAggregator>> {

		/// <summary>
		/// List of dimensions.
		/// </summary>
		string[] Dimensions { get; }

		/// <summary>
		/// Aggregator factory used for creating measure values.
		/// </summary>
		IAggregatorFactory AggregatorFactory { get; }

		/// <summary>
		/// Returns aggregated value by specified keys.
		/// </summary>
		/// <param name="dimKeys">array of dimension keys</param>
		/// <returns>aggregated value</returns>
		IAggregator this[params object[] dimKeys] { get; }

		/// <summary>
		/// Gets the number of values (data points) in the <see cref="IPivotData"/>.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Returns an enumerator that can be used to iterate through <see cref="IPivotData"/> values (data points). 
		/// </summary>
		/// <returns></returns>
		IEnumerator<KeyValuePair<object[],IAggregator>> GetEnumerator();
	}
}
