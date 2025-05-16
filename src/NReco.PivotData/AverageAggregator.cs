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
	/// Implements an average value aggregator.
	/// </summary>
	/// <remarks>
	/// AverageAggregator can be used only with numeric values that may be coverted to System.Decimal.
	/// </remarks>
	public class AverageAggregator : IAggregator {

		decimal total = 0;
		ulong count = 0;
		string field;

		public AverageAggregator(string f) {
			field = f;
		}

		public AverageAggregator(string f, object state) : this(f) {
			var stateArr = state as object[];
			if (stateArr==null || stateArr.Length!=2)
				throw new InvalidOperationException("Invalid state, expected array [UInt64 count, decimal totalSum]");
			count = Convert.ToUInt64(stateArr[0]);
			if (stateArr[1]!=null) {
				total = Convert.ToDecimal(stateArr[1]);
			} else {
				count = 0; // if no avg value, number of rows should be zero to be consistent with "Push" logic
			}
		}

		public void Push(object r, Func<object,string,object> getValue) {
			var v = ConvertHelper.ConvertToDecimal(getValue(r,field), Decimal.MinValue);
			if (v != Decimal.MinValue) {
				total += v;
				count++;
			}
		}

		public object Value {
			get {
				if (count > 0)
					return total / count;
				return null;
			}
		}

		public ulong Count {
			get { return count; }
		}

		public void Merge(IAggregator aggr) {
			var avgAggr = aggr as AverageAggregator;
			if (avgAggr==null)
				throw new ArgumentException("aggr");
			count += avgAggr.count;
			if (avgAggr.count>0)
				total += avgAggr.total;
		}

		public object GetState() {
			return new object[]{count, total};
		}
	}

	/// <summary>
	/// <see cref="AverageAggregator"/> factory component
	/// </summary>
	public class AverageAggregatorFactory : IAggregatorFactory {

		public string Field { 
			get { return fld; }
		}

		string fld;

		public AverageAggregatorFactory(string field) {
			fld = field;
		}

		public IAggregator Create() {
			return new AverageAggregator(fld);
		}

		public IAggregator Create(object state) {
			return new AverageAggregator(fld, state);
		}

		public override bool Equals(object obj) {
			var avgFactory = obj as AverageAggregatorFactory;
			if (avgFactory==null)
				return false;
			return avgFactory.fld==fld;
		}		

		public override string ToString() {
			return String.Format("Average of {0}", Field);
		}

	}
}
