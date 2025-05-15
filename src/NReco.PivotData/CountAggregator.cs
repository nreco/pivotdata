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
	/// Implements a counting aggregator.
	/// </summary>
	public class CountAggregator : IAggregator {

		ulong count = 0;

		public CountAggregator() {
		}

		public CountAggregator(object state) {
			if (state==null)
				throw new InvalidOperationException("Invalid state, expected: uint");
			count = Convert.ToUInt64(state);
		}

		public void Push(object r, Func<object,string,object> getValue) {
			count++;
		}

		public object Value {
			get { return count; }
		}

		public ulong Count {
			get { return count; }
		}

		public void Merge(IAggregator aggr) {
			var cntAggr = aggr as CountAggregator;
			if (cntAggr==null)
				throw new ArgumentException("aggr");
			count += cntAggr.count;
		}

		public object GetState() {
			return count;
		}
	}

	/// <summary>
	/// <see cref="CountAggregator"/> factory component
	/// </summary>
	public class CountAggregatorFactory : IAggregatorFactory {

		public CountAggregatorFactory() {
		}

		public IAggregator Create() {
			return new CountAggregator();
		}

		public IAggregator Create(object state) {
			return new CountAggregator(state);
		}

		public override bool Equals(object obj) {
			var aggrFactory = obj as CountAggregatorFactory;
			if (aggrFactory==null)
				return false;
			return true;
		}

		public override string ToString() {
			return "Count";
		}

	}
}
