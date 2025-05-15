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
using System.Data;

namespace NReco.PivotData {

	/// <summary>
	/// Represents measure aggregator.
	/// </summary>
	/// <remarks>
	/// Aggregator implements common API for handling various types of measures:
	/// <list type="bullet">
	/// <item>
	///		<description>measure calculation (Push method)</description>
	///	</item>
	///	<item>
	///		<description>accessing current measure value (Value and Count properties)</description>
	/// </item>
	/// <item>
	///		<description>combining 2 measures of the same type (Merge method)</description>
	/// </item>
	/// <item>
	///		<description>provide compact measure data for serialization (GetState method)</description>
	/// </item>
	/// </list>
	/// </remarks>
	public interface IAggregator {

		/// <summary>
		/// Modifies aggregator value by processing specified fact (data record)
		/// </summary>
		/// <param name="v">data record</param>
		/// <param name="getValue">field value accessor delegate</param>
		void Push(object v, Func<object,string,object> getValue);

		/// <summary>
		/// Current aggregator value
		/// </summary>
		object Value { get; }

		/// <summary>
		/// Number of facts (data records) used for calculating aggregator value
		/// </summary>
		ulong Count { get; }

		/// <summary>
		/// Modifies current instance of aggregator by merging with specified compatible aggregator
		/// </summary>
		/// <param name="aggr">compatible aggregator to merge (should have the same type)</param>
		void Merge(IAggregator aggr);

		/// <summary>
		/// Returns an object that represents compacted "raw" aggregator state
		/// </summary>
		object GetState();
	}

}
