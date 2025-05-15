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
	/// Implements aggregator that counts only unique values.
	/// </summary>
	/// <remarks>
	/// CountUniqueAggregator collects unique values of some field. It can be used with any types.
	/// </remarks>
	public class CountUniqueAggregator : IAggregator {

		ulong count = 0;
		string field;
		protected HashSet<object> uniqueValues;

		public CountUniqueAggregator(string f) {
			field = f;
			uniqueValues = new HashSet<object>();
		}

		public CountUniqueAggregator(string f, object state) : this(f) {
			var stateArr = state as object[];
			if (stateArr==null || stateArr.Length!=2)
				throw new InvalidOperationException("Invalid state, expected 2-elements array like [uint count, [object val1, object val2, ... ] ]");
			count = Convert.ToUInt64(stateArr[0]);
			var unqValsArr = stateArr[1] as object[];
			for (int i=0; i<unqValsArr.Length; i++)
				uniqueValues.Add(unqValsArr[i]);
		}

		public void Push(object r, Func<object,string,object> getValue) {
			var v = getValue(r,field);
			if (v != null && !DBNull.Value.Equals(v)) { 
				uniqueValues.Add(v);
				count++;
			}
		}

		public virtual object Value {
			get { return uniqueValues.Count; }
		}

		public ulong Count {
			get { return count; }
		}

		public virtual void Merge(IAggregator aggr) {
			var cntUnqAggr = aggr as CountUniqueAggregator;
			if (cntUnqAggr==null)
				throw new ArgumentException("aggr");
			count += cntUnqAggr.count;
			uniqueValues.UnionWith( cntUnqAggr.uniqueValues );
		}

		public object GetState() {
			var uniqValsArr = new object[uniqueValues.Count];
			uniqueValues.CopyTo(uniqValsArr);
			return new object[]{count, uniqValsArr};
		}

	}

	/// <summary>
	/// <see cref="CountUniqueAggregator"/> factory component
	/// </summary>
	public class CountUniqueAggregatorFactory : IAggregatorFactory {

		public string Field { 
			get { return fld; }
		}

		string fld;

		public CountUniqueAggregatorFactory(string field) {
			fld = field;
		}

		public IAggregator Create() {
			return new CountUniqueAggregator(fld);
		}

		public IAggregator Create(object state) {
			return new CountUniqueAggregator(fld, state);
		}

		public override bool Equals(object obj) {
			var cntUnqFactory = obj as CountUniqueAggregatorFactory;
			if (cntUnqFactory==null)
				return false;
			return cntUnqFactory.fld==fld;
		}	

		public override string ToString() {
			return String.Format("Count unique of {0}", Field);
		}

	}
}
