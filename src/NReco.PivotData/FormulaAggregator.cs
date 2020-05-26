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
	/// Implements a formula aggregator calculated from other aggregators.
	/// </summary>
	public class FormulaAggregator : CompositeAggregator {

		Func<IAggregator[],object> FormulaValue;

		public FormulaAggregator(Func<IAggregator[],object> formulaValue, IAggregator[] aggregators) : base(aggregators) {
			FormulaValue = formulaValue;
		}

		public FormulaAggregator(Func<IAggregator[],object> formulaValue, IAggregatorFactory[] factories, object state)
			: base (factories, state) {
			FormulaValue = formulaValue;
		}

		public override object Value {
			get {
				return FormulaValue(Aggregators);
			}
		}

	}

	/// <summary>
	/// Factory for <see cref="FormulaAggregator"/>.
	/// </summary>
	public class FormulaAggregatorFactory : IAggregatorFactory {

		public IAggregatorFactory[] Factories { get; private set; }
		public Func<IAggregator[],object> FormulaValue;
		readonly string FormulaName;

		public FormulaAggregatorFactory(string name, Func<IAggregator[],object> formulaValue, IAggregatorFactory[] aggrFactories) {
			Factories = aggrFactories;
			FormulaValue = formulaValue;
			FormulaName = name;
		}

		public IAggregator Create() {
			return new FormulaAggregator(FormulaValue, Factories.Select(f=>f.Create()).ToArray());
		}

		public IAggregator Create(object state) {
			return new FormulaAggregator(FormulaValue, Factories, state);
		}

		public override string ToString() {
			return FormulaName;
		}

	}


}
