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
	/// Implements a variance aggregator (calculates mean, variance, sample variance, standard deviation)
	/// </summary>
	/// <remarks>
	/// VarianceAggregator can be used only with numeric values that may be coverted to System.Double.
	/// </remarks>
	public class VarianceAggregator : IAggregator {
		string field;
		VarianceAggregatorValueType valType;

		ulong count = 0;
		double mean = 0;
		double M2 = 0;

		public VarianceAggregator(string f, VarianceAggregatorValueType aggrValueType) {
			field = f;
			valType = aggrValueType;
		}

		public VarianceAggregator(string f, VarianceAggregatorValueType aggrValueType, object state) : this(f, aggrValueType) {
			var stateArr = state as object[];
			if (stateArr==null || stateArr.Length!=3)
				throw new InvalidOperationException("Invalid state, expected array [uint count, double mean, double M2] where M2=variance*count");
			count = Convert.ToUInt64(stateArr[0]);
			mean = Convert.ToDouble(stateArr[1]);
			M2 = Convert.ToDouble(stateArr[2]);
		}

		public void Push(object r, Func<object,string,object> getValue) {
			var x = ConvertHelper.ConvertToDouble(getValue(r,field), Double.NaN);
			if (!Double.IsNaN(x)) {
				count++;
				double delta = x - mean;
				mean += delta/count;
				M2 += delta*(x - mean);
			}
		}

		public virtual object Value {
			get {
				switch (valType) {
					case VarianceAggregatorValueType.SampleStandardDeviation:
						return SampleStdDevValue;
					case VarianceAggregatorValueType.StandardDeviation:
						return StdDevValue;
					case VarianceAggregatorValueType.SampleVariance:
						return SampleVarianceValue;
					case VarianceAggregatorValueType.Variance: 
					default:
						return VarianceValue;
				}
			}
		}

		public ulong Count {
			get { return count; }
		}

		public double VarianceValue {
			get {
				return count<2 ? Double.NaN : M2 / count; 
			}
		}

		public double StdDevValue {
			get {
				return Math.Sqrt(VarianceValue); 
			}
		}

		public double SampleVarianceValue {
			get {
				return count<2 ? Double.NaN : M2 / (count-1); 
			}
		}

		public double SampleStdDevValue {
			get {
				return Math.Sqrt(SampleVarianceValue); 
			}
		}

		public double MeanValue {
			get {
				return mean; 
			}
		}

		public void Merge(IAggregator aggr) {
			var varAggr = aggr as VarianceAggregator;
			if (varAggr==null)
				throw new ArgumentException("aggr");

			var meanDiff = mean-varAggr.mean;
			var allCount = count+varAggr.count;
			mean = (mean*count + varAggr.mean*varAggr.count) / allCount;
			M2 += varAggr.M2 + meanDiff*meanDiff*( ((double) (count*varAggr.count))/allCount);

			count = allCount;
		}

		public object GetState() {
			return new object[]{count, mean, M2};
		}

	}

	/// <summary>
	/// <see cref="VarianceAggregator"/> factory component
	/// </summary>
	public class VarianceAggregatorFactory : IAggregatorFactory {

		public string Field { 
			get { return fld; }
		}

		public VarianceAggregatorValueType ValueType { get { return valueType; } }

		string fld;
		VarianceAggregatorValueType valueType;

		public VarianceAggregatorFactory(string field) : this(field, VarianceAggregatorValueType.Variance) {
		}

		public VarianceAggregatorFactory(string field, VarianceAggregatorValueType aggrValueType) {
			fld = field;
			valueType = aggrValueType;
		}

		public IAggregator Create() {
			return new VarianceAggregator(fld, valueType);
		}

		public IAggregator Create(object state) {
			return new VarianceAggregator(fld, valueType, state);
		}

		public override bool Equals(object obj) {
			var aggrFactory = obj as VarianceAggregatorFactory;
			if (aggrFactory==null)
				return false;
			return aggrFactory.fld==fld && aggrFactory.valueType==valueType;
		}

		public override string ToString() {
			return String.Format("Variance of {0}", Field);
		}

	}

	public enum VarianceAggregatorValueType {
		Variance = 1,
		SampleVariance = 2,
		StandardDeviation = 3,
		SampleStandardDeviation = 4
	}

}
