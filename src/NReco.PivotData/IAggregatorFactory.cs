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
	/// Represents measure aggregator factory.
	/// </summary>
	public interface IAggregatorFactory {

		/// <summary>
		/// Creates a new instance of measure aggregator.
		/// </summary>
		/// <returns>new aggregator instance (empty)</returns>
		IAggregator Create();

		/// <summary>
		/// Creates a new instance of measure aggregator and initialize it with specified measure state object.
		/// </summary>
		/// <param name="state">state object</param>
		/// <returns>aggregator instance initialized with specified measure value</returns>
		IAggregator Create(object state);
	}

}
