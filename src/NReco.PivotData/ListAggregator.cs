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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace NReco.PivotData {

	/// <summary>
	/// Implements aggregator that returns list of grouped source objects.
	/// </summary>
	/// <remarks>
	/// ListAggregator can be used with any type of objects. It is useful if you need to keep references to source objects (like LINQ GroupBy) or row IDs.
	/// </remarks>
	public class ListAggregator : IAggregator {

		protected List<object> values = new List<object>();
		string field = null;

		public ListAggregator(string f) {
			field = f;
		}

		public ListAggregator(string f, object state) : this(f) {
			var stateArr = state as IEnumerable;
			if (stateArr==null)
				throw new InvalidOperationException("Invalid state, expected IEnumerable");
			foreach (var o in stateArr)
				values.Add(o);
		}

		public virtual void Push(object r, Func<object,string,object> getValue) {
			if (field == null) { 
				// keep reference to source object
				if (r is IDataReader)
					throw new NotSupportedException("ListAggregator without field cannot be used with IDataReader data source");
				values.Add(r);
			} else {
				var v = getValue(r,field);
				if (v != null && !DBNull.Value.Equals(v)) {
					values.Add(v);
				}
			}
		}

		public virtual object Value {
			get { return values; }
		}

		public uint Count {
			get { return (uint)values.Count; }
		}

		public virtual void Merge(IAggregator aggr) {
			var lstAggr = aggr as ListAggregator;
			if (lstAggr==null)
				throw new ArgumentException("aggr");
			values.AddRange(lstAggr.values);
		}

		public object GetState() {
			return values.ToArray();
		}

	}

	/// <summary>
	/// <see cref="ListAggregator"/> factory component.
	/// </summary>
	public class ListAggregatorFactory : IAggregatorFactory {

		public string Field { 
			get { return fld; }
		}

		string fld = null;

		/// <summary>
		/// Initializes a list aggregator factory that collects references to source objects.
		/// </summary>
		public ListAggregatorFactory() {
		}

		/// <summary>
		/// Initializes a list aggregator factory that collects all values of specified field.
		/// </summary>
		public ListAggregatorFactory(string field) {
			fld = field;
		}

		public IAggregator Create() {
			return new ListAggregator(fld);
		}

		public IAggregator Create(object state) {
			return new ListAggregator(fld, state);
		}

		public override bool Equals(object obj) {
			var aggrFactory = obj as ListAggregatorFactory;
			if (aggrFactory==null || aggrFactory.Field!=this.Field)
				return false;
			return true;
		}

		public override string ToString() {
			return fld!=null ? "List of "+fld : "List";
		}

	}

}
