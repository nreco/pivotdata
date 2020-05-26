using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NReco.Linq;

namespace NReco.PivotData.Examples.DynamicFormulaMeasure {
	
	/// <summary>
	/// Represents measure calculated by formula evaluated at run-time.
	/// </summary>
	/// <remarks>
	/// Formula expression may use index-based names for measure values (value0, value1 etc) 
	/// or human-friendly aggregator factory names like 'sumofa', 'averageofb' 
	/// (lower case ToString value returned by IAggregatorFactory where spaces are removed).
	/// </remarks>
	public class DynamicFormulaMeasure {

		LambdaParser Parser;
		string Formula;
		int[] ArgMeasureIndexes;
		Dictionary<string,int> ParamNameToArgIdx;

		public IDictionary<string, object> ExtraEvalContext { get; set; }

		public DynamicFormulaMeasure(string formula, IPivotData pvtData) {
			Parser = new LambdaParser();
			Formula = formula;
			var formulaExpr = Parser.Parse(formula);
			var formulaParams = LambdaParser.GetExpressionParameters(formulaExpr);
			
			var paramMeasureIndexes = new List<int>();
			var paramMeasureNames = new List<string>();

			foreach (var fParam in formulaParams) {
				if (paramMeasureNames.Contains(fParam.Name))
					continue; // avoid duplicates

				var paramMsrIdx = ResolveAggregatorIndex(fParam.Name, pvtData);
				if (paramMsrIdx >= 0) { 
					paramMeasureIndexes.Add(paramMsrIdx);
					paramMeasureNames.Add(fParam.Name);
				}
			}

			ArgMeasureIndexes = paramMeasureIndexes.ToArray();
			ParamNameToArgIdx = new Dictionary<string,int>();
			for (int i=0; i<paramMeasureNames.Count; i++)
				ParamNameToArgIdx[paramMeasureNames[i]] = i;
		}

		int ResolveAggregatorIndex(string name, IPivotData pvtData) {
			var aggrFactories = pvtData.AggregatorFactory is CompositeAggregatorFactory ?
				((CompositeAggregatorFactory)pvtData.AggregatorFactory).Factories :
				new [] { pvtData.AggregatorFactory };

			for (int i = 0; i < aggrFactories.Length; i++) {
				var indexBasedName = "value"+i.ToString();
				if (name == indexBasedName )
					return i;
				var aggrFactoryName = aggrFactories[i].ToString().ToLower().Replace(" ", "");
				if (name == aggrFactoryName)
					return i;
			}
			return -1;
		}

		public object GetFormulaValue(IAggregator[] args) {
			int argIdx;
			return Parser.Eval(Formula, (paramName) => {
				// resolve measure value
				if (ParamNameToArgIdx.TryGetValue(paramName, out argIdx))
					return args[argIdx].Value;

				// extra context
				if (ExtraEvalContext != null && ExtraEvalContext.ContainsKey(paramName)) {
					return ExtraEvalContext[paramName];
				}

				throw new ArgumentException("Unknown variable: "+paramName);
			});
		}

		public int[] GetParentMeasureIndexes() {
			return ArgMeasureIndexes;
		}

	}
}
