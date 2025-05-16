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
		string field = null;

		public CountAggregator() {
		}

		public CountAggregator(string field) {
			this.field = field;
		}

		public CountAggregator(object state) {
			if (state==null)
				throw new InvalidOperationException("Invalid state, expected: UInt64");
			count = Convert.ToUInt64(state);
		}

		public CountAggregator(string field, object state) : this(state) {
			this.field = field;
		}


		public void Push(object r, Func<object,string,object> getValue) {
			if (field!=null) {
				var v = getValue(r, field);
				if (v == null || DBNull.Value.Equals(v))
					return;
			}
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

		public string Field { get; private set; }

		public CountAggregatorFactory() {
			Field = null;
		}

		public CountAggregatorFactory(string field) {
			Field = field;
		}

		public IAggregator Create() {
			return new CountAggregator(Field);
		}

		public IAggregator Create(object state) {
			return new CountAggregator(Field, state);
		}

		public override bool Equals(object obj) {
			var aggrFactory = obj as CountAggregatorFactory;
			if (aggrFactory==null)
				return false;
			return aggrFactory.Field==Field;
		}

		public override string ToString() {
			var s = "Count";
			if (Field!=null)
				s += " of "+Field;
			return s;
		}

	}
}
