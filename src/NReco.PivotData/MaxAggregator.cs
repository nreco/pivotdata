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
	/// Implements a maximum value aggregator
	/// </summary>
	/// <remarks>
	/// MaxAggregator can be used only with values that implement IComparable interface.
	/// </remarks>		
	public class MaxAggregator : IAggregator {

		IComparable maxValue = null;
		ulong count = 0;
		string field;

		public MaxAggregator(string f) {
			field = f;
		}

		public MaxAggregator(string f, object state) : this(f) {
			var stateArr = state as object[];
			if (stateArr==null || stateArr.Length!=2)
				throw new InvalidOperationException("Invalid state, expected array [UInt64 count, IComparable value]");
			count = Convert.ToUInt64(stateArr[0]);
			maxValue = (IComparable)stateArr[1];
		}

		public void Push(object r, Func<object,string,object> getValue) {
			var v = getValue(r,field);
			if (v != null && !DBNull.Value.Equals(v)) {
				var vv = v as IComparable;
				if (vv==null)
					throw new NotSupportedException("MaxAggregator can be used only with IComparable values");
				if (maxValue==null || vv.CompareTo(maxValue)>0)
					maxValue = vv;
				count++;
			}
		}

		public object Value {
			get { return maxValue; }
		}

		public ulong Count {
			get { return count; }
		}

		public void Merge(IAggregator aggr) {
			var maxAggr = aggr as MaxAggregator;
			if (maxAggr==null)
				throw new ArgumentException("aggr");
			count += maxAggr.count;
			if (maxAggr.maxValue!=null && (maxValue==null || maxAggr.maxValue.CompareTo(maxValue)>0) )
				maxValue = maxAggr.maxValue;
		}

		public object GetState() {
			return new object[]{count, maxValue};
		}
	}

	/// <summary>
	/// <see cref="MaxAggregator"/> factory component
	/// </summary>
	public class MaxAggregatorFactory : IAggregatorFactory {

		public string Field { 
			get { return fld; }
		}

		string fld;

		public MaxAggregatorFactory(string field) {
			fld = field;
		}

		public IAggregator Create() {
			return new MaxAggregator(fld);
		}

		public IAggregator Create(object state) {
			return new MaxAggregator(fld, state);
		}

		public override bool Equals(object obj) {
			var maxFactory = obj as MaxAggregatorFactory;
			if (maxFactory==null)
				return false;
			return maxFactory.fld==fld;
		}		

		public override string ToString() {
			return String.Format("Max of {0}", Field);
		}

	}
}
