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
	/// Implements a minimum value aggregator.
	/// </summary>
	/// <remarks>
	/// MinAggregator can be used only with values that implement IComparable interface.
	/// </remarks>		 
	public class MinAggregator : IAggregator {

		IComparable minValue = null;
		uint count = 0;
		string field;

		public MinAggregator(string f) {
			field = f;
		}

		public MinAggregator(string f, object state) : this(f) {
			var stateArr = state as object[];
			if (stateArr==null || stateArr.Length!=2)
				throw new InvalidOperationException("Invalid state, expected array [uint count, IComparable value]");
			count = Convert.ToUInt32(stateArr[0]);
			minValue = (IComparable)stateArr[1];
		}

		public void Push(object r, Func<object,string,object> getValue) {
			var v = getValue(r,field);
			if (v != null && !DBNull.Value.Equals(v)) {
				var vv = v as IComparable;
				if (vv==null)
					throw new NotSupportedException("MinAggregator can be used only with IComparable values");
				if (minValue==null || vv.CompareTo(minValue)<0)
					minValue = vv;
				count++;
			}
		}

		public object Value {
			get { return minValue; }
		}

		public uint Count {
			get { return count; }
		}

		public void Merge(IAggregator aggr) {
			var minAggr = aggr as MinAggregator;
			if (minAggr==null)
				throw new ArgumentException("aggr");
			count += minAggr.count;
			if (minAggr.minValue!=null && (minValue==null || minAggr.minValue.CompareTo(minValue)<0) )
				minValue = minAggr.minValue;
		}

		public object GetState() {
			return new object[]{count, minValue};
		}
	}

	/// <summary>
	/// <see cref="MinAggregator"/> factory component
	/// </summary>
	public class MinAggregatorFactory : IAggregatorFactory {

		public string Field { 
			get { return fld; }
		}

		string fld;

		public MinAggregatorFactory(string field) {
			fld = field;
		}

		public IAggregator Create() {
			return new MinAggregator(fld);
		}

		public IAggregator Create(object state) {
			return new MinAggregator(fld, state);
		}

		public override bool Equals(object obj) {
			var minFactory = obj as MinAggregatorFactory;
			if (minFactory==null)
				return false;
			return minFactory.fld==fld;
		}

		public override string ToString() {
			return String.Format("Min of {0}", Field);
		}
	}
}
