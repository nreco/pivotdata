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
	/// Implements a quantile aggregator: calculates median or specified quantile value.
	/// </summary>
	/// <remarks>
	/// Quantile is calculated as average of 2 elements.  <see cref="QuantileAggregator"/> can be used only with numeric values that may be coverted to System.Decimal.
	/// </remarks>
	public class QuantileAggregator : ListAggregator {
		decimal q;
		bool sorted = false;

		public QuantileAggregator(string f, decimal q) : base(f) {
			this.q = q;
		}

		public QuantileAggregator(string f, decimal q, object state) : base(f, state) {
			this.q = q;
		}

		public QuantileAggregator(string f, decimal q, ListAggregator listAggr) : base(f) {
			values = (List<object>)listAggr.Value;
			this.q = q;
		}

		public override void Push(object r, Func<object,string,object> getValue) {
			sorted = false;
			base.Push(r, getValue);
		}

		public override object Value {
			get {
				return GetQuantile(q);
			}
		}

		public override void Merge(IAggregator aggr) {
			sorted = false;
			base.Merge(aggr);
		}

		public object GetQuantile(decimal q) {
			if (values.Count == 0)
				return null;
			if (!sorted) {
				values.Sort();
				sorted = true;
			}
			decimal i = q*(values.Count - 1);
			var aVal = ConvertHelper.ConvertToDecimal( values[(int)Math.Floor(i)], Decimal.MinValue);
			var bVal = ConvertHelper.ConvertToDecimal( values[(int)Math.Ceiling(i)], Decimal.MinValue);
			if (aVal==Decimal.MinValue || bVal==Decimal.MinValue)
				return null;
			return (aVal + bVal) / 2.0M;
		}
	}

	/// <summary>
	/// <see cref="QuantileAggregator"/> factory component
	/// </summary>
	public class QuantileAggregatorFactory : IAggregatorFactory {

		string fld;
		decimal q;

		public string Field { 
			get { return fld; }
		}
		
		public decimal Quantile {
			get { return q;  }
		}

		public QuantileAggregatorFactory(string field, decimal q) {
			fld = field;
			this.q = q;
		}

		public IAggregator Create() {
			return new QuantileAggregator(fld, q);
		}

		public IAggregator Create(object state) {
			return new QuantileAggregator(fld, q, state);
		}

		public override bool Equals(object obj) {
			var aggrFactory = obj as QuantileAggregatorFactory;
			if (aggrFactory==null)
				return false;
			return aggrFactory.fld==fld && aggrFactory.q==q;
		}

		public override string ToString() {
			if (q==0.5M)
				return String.Format("Median of {0}", Field);
			return String.Format("Quantile {0} of {1}", q, Field);
		}

	}


}
