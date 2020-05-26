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
	/// Implements a composite aggregator that incapsulates list of aggregators.
	/// </summary>
	public class CompositeAggregator : IAggregator {

		public IAggregator[] Aggregators {
			get {
				return _Aggregators;
			}
		}
		IAggregator[] _Aggregators;
		uint count = 0;

		public CompositeAggregator(IAggregator[] aggregators) {
			_Aggregators = aggregators;
			for (int i = 0; i < aggregators.Length; i++) { 
				var c = aggregators[i].Count;
				if (c>count)
					count = c;
			}
		}

		public CompositeAggregator(IAggregatorFactory[] factories, object state) {
			var stateArr = state as object[];
			if (stateArr==null || stateArr.Length!=(factories.Length+1) )
				throw new InvalidOperationException("invalid state");
			_Aggregators = new IAggregator[factories.Length];
			for (var i = 0; i < factories.Length; i++) {
				_Aggregators[i] = factories[i].Create(stateArr[i]);
			}
			count = Convert.ToUInt32(stateArr[factories.Length]);
		}

		public virtual void Push(object r, Func<object,string,object> getValue) {
			count++;
			for (var i=0; i<_Aggregators.Length; i++)
				_Aggregators[i].Push(r, getValue);
		}

		public virtual object Value {
			get {
				var allValues = new object[_Aggregators.Length];
				for (var i=0;i<allValues.Length;i++)
					allValues[i] = _Aggregators[i].Value;
				return allValues;
			}
		}

		public uint Count {
			get { return count; }
		}

		public virtual void Merge(IAggregator aggr) {
			var compositeAggr = aggr as CompositeAggregator;
			if (compositeAggr==null || compositeAggr._Aggregators.Length!=_Aggregators.Length)
				throw new ArgumentException("aggr");
			count += compositeAggr.count;
			for (var i = 0; i < _Aggregators.Length; i++) {
				_Aggregators[i].Merge(compositeAggr._Aggregators[i]);
			}
		}

		public object GetState() {
			var stateArr = new object[_Aggregators.Length+1];
			for (var i = 0; i < _Aggregators.Length; i++) {
				stateArr[i] = _Aggregators[i].GetState();
			}
			stateArr[_Aggregators.Length] = count;
			return stateArr;
		}
	}

	/// <summary>
	/// <see cref="CompositeAggregator"/> factory component.
	/// </summary>
	public class CompositeAggregatorFactory : IAggregatorFactory {

		public IAggregatorFactory[] Factories {
			get { return _Factories; }
		}
		IAggregatorFactory[] _Factories;

		public CompositeAggregatorFactory(params IAggregatorFactory[] aggrFactories) {
			_Factories = aggrFactories;
		}

		public IAggregator Create() {
			var aggrs = new IAggregator[_Factories.Length];
			for (int i=0; i<_Factories.Length; i++)
				aggrs[i] = _Factories[i].Create();
			return new CompositeAggregator(aggrs);
		}

		public IAggregator Create(object state) {
			return new CompositeAggregator(_Factories, state);
		}

		public override bool Equals(object obj) {
			var compositeFactory = obj as CompositeAggregatorFactory;
			if (compositeFactory==null || compositeFactory._Factories.Length!=_Factories.Length)
				return false;
			for (int i = 0; i < _Factories.Length; i++) {
				if (!Factories[i].Equals(compositeFactory._Factories[i]))
					return false;
			}
			return true;
		}

		public override int GetHashCode() {
			int hashCode = _Factories.Length;
			for (int i = 0; i < _Factories.Length; i++) { 
				hashCode ^= _Factories[i].GetHashCode();
			}
			return hashCode;
		}

	}

    public static class AggregatorExtensions
    {
		/// <summary>
		/// Returns the <see cref="IAggregator"/> as <see cref="CompositeAggregator"/> instance. 
		/// </summary>
		/// <remarks>
		/// This method is useful for accessing atomic and composite aggregators in the same way.
		/// </remarks>
		public static CompositeAggregator AsComposite(this IAggregator aggr) {
			if (aggr is CompositeAggregator)
				return (CompositeAggregator)aggr;
			return new CompositeAggregator(new[] { aggr });
		}

	}


}
