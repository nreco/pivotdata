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
	/// Implements aggregator that returns sorted list of unique field values
	/// </summary>
	public class ListUniqueAggregator : CountUniqueAggregator {

		public ListUniqueAggregator(string f) : base(f) {
		}

		public ListUniqueAggregator(string f, object state) : base(f, state) { }

		public override object Value {
			get {
				var arr = new object[uniqueValues.Count];
				uniqueValues.CopyTo(arr);
				Array.Sort(arr);
				return arr; 
			}
		}

	}

	/// <summary>
	/// Factory class for <see cref="ListUniqueAggregator"/>
	/// </summary>
	public class ListUniqueAggregatorFactory : IAggregatorFactory {

		public string Field { 
			get { return fld; }
		}

		string fld;

		/// <summary>
		/// Initializes new instance of ListUniqueAggregatorFactory
		/// </summary>
		/// <param name="field">field name</param>
		public ListUniqueAggregatorFactory(string field) {
			fld = field;
		}

		public IAggregator Create() {
			return new ListUniqueAggregator(fld);
		}

		public IAggregator Create(object state) {
			return new ListUniqueAggregator(fld, state);
		}

		public override bool Equals(object obj) {
			var lstUnqFactory = obj as ListUniqueAggregatorFactory;
			if (lstUnqFactory==null)
				return false;
			return lstUnqFactory.fld==fld;
		}	

		public override string ToString() {
			return String.Format("List unique values of {0}", Field);
		}
	}
}
