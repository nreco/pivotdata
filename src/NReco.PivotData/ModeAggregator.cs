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
	/// Implements a mode aggregator: calculates value that appears most often.
	/// </summary>
	public class ModeAggregator : IAggregator {

		ulong count = 0;
		string field;
		bool multimodal;
		protected Dictionary<object,uint> uniqueValueCounts;
		object maxCountValue = null;

		public ModeAggregator(string f, bool multimodal) {
			field = f;
			uniqueValueCounts = new Dictionary<object,uint>();
			this.multimodal = multimodal;
		}

		public ModeAggregator(string f, bool multimodal, object state) : this(f, multimodal) {
			var stateArr = state as object[];
			if (stateArr == null || stateArr.Length != 3)
				throw new InvalidOperationException("Invalid state, expected 3-elements array like [uint count, [object val1, object val2, ... ], [uint val1_count, uint val2_count, ... ]");
			count = Convert.ToUInt64(stateArr[0]);
			var unqValsArr = stateArr[1] as object[];
			var unqValCountsArr = stateArr[2] as uint[];
			if (unqValsArr.Length != unqValCountsArr.Length)
				throw new InvalidOperationException("Invalid state, values array doesn't match counts array.");
			for (int i = 0; i < unqValsArr.Length; i++)
				uniqueValueCounts[ unqValsArr[i] ] = unqValCountsArr[i];
		}

		public void Push(object r, Func<object, string, object> getValue) {
			var v = getValue(r, field);
			if (v != null && !DBNull.Value.Equals(v)) {
				maxCountValue = null;
				if (uniqueValueCounts.TryGetValue(v, out var cnt))
					uniqueValueCounts[v] = cnt+1;
				else
					uniqueValueCounts[v] = 1;
				count++;
			}
		}

		object FindUnimodalValue() {
			object maxCountVal = null;
			uint maxCount = 1;
			bool canCompare = false;
			foreach (var entry in uniqueValueCounts)
				// max value is used in case of single-mode to guarantee determinate result
				if (entry.Value > maxCount || (canCompare && entry.Value==maxCount && ((IComparable)maxCountVal).CompareTo(entry.Key)<0 ) ) {
					maxCountVal = entry.Key;
					canCompare = maxCountVal is IComparable;
					maxCount = entry.Value;
				}
			return maxCountVal;
		}

		object FindMultimodalValue() {
			var res = new List<object>();
			uint maxCount = 1;
			foreach (var entry in uniqueValueCounts)
				if (entry.Value > maxCount) {
					maxCount = entry.Value;
				}
			foreach (var entry in uniqueValueCounts)
				if (entry.Value == maxCount)
					res.Add(entry.Key);
			var resArr = res.ToArray();
			if (resArr.Length > 1 && resArr[0] is IComparable)
				Array.Sort(resArr);
			return resArr;
		}

		public virtual object Value {
			get {
				if (maxCountValue==null) {
					maxCountValue = multimodal ? FindMultimodalValue() : FindUnimodalValue();
				}
				return maxCountValue;
			}
		}

		public ulong Count {
			get { return count; }
		}

		public virtual void Merge(IAggregator aggr) {
			var modeAggr = aggr as ModeAggregator;
			if (modeAggr == null)
				throw new ArgumentException("aggr");
			count += modeAggr.count;
			maxCountValue = null;
			foreach (var entry in modeAggr.uniqueValueCounts) {
				if (uniqueValueCounts.TryGetValue(entry.Key, out var cnt)) {
					uniqueValueCounts[entry.Key] = cnt + entry.Value;
				} else {
					uniqueValueCounts[entry.Key] = entry.Value;
				}
			}
		}

		public object GetState() {
			var uniqValsArr = new object[uniqueValueCounts.Count];
			var uniqValCountsArr = new uint[uniqueValueCounts.Count];
			int idx = 0;
			foreach (var entry in uniqueValueCounts) {
				uniqValsArr[idx] = entry.Key;
				uniqValCountsArr[idx] = entry.Value;
				idx++;
			}
			return new object[] { count, uniqValsArr, uniqValCountsArr };
		}

	}

	/// <summary>
	/// <see cref="ModeAggregator"/> factory component.
	/// </summary>
	/// <example>new ModeAggregatorFactory("employee_age")</example>
	public class ModeAggregatorFactory : IAggregatorFactory {

		string fld;

		public string Field { 
			get { return fld; }
		}
		
		public bool Multimodal { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ModeAggregatorFactory"/> for the specified field (single-value mode).
		/// </summary>
		/// <param name="field">field name</param>
		public ModeAggregatorFactory(string field) : this(field,false) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ModeAggregatorFactory"/> for the specified field and mode type (single or multi-value).
		/// </summary>
		/// <param name="field">field name</param>
		/// <param name="multimodal">if true mode value is an array that can contain several mode values.</param>
		public ModeAggregatorFactory(string field, bool multimodal) {
			fld = field;
			Multimodal = multimodal;
		}

		public IAggregator Create() {
			return new ModeAggregator(fld, Multimodal);
		}

		public IAggregator Create(object state) {
			return new ModeAggregator(fld, Multimodal, state);
		}

		public override bool Equals(object obj) {
			var aggrFactory = obj as ModeAggregatorFactory;
			if (aggrFactory==null)
				return false;
			return aggrFactory.fld==fld;
		}

		public override string ToString() {
			return String.Format("Mode of {0}", Field);
		}

	}


}
