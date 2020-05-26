using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;

namespace NReco.PivotData.Examples.DynamicFormulaMeasure {

	/// <summary>
	/// Dynamic formula measure.
	/// User-defined expression evaluated at run-time with NReco.Lambda parser, https://github.com/nreco/lambdaparser/ .
	/// </summary>
	class Program {

		static void Main(string[] args) {

			// sample dataset
			var ordersTable = GetOrdersTable();

			// build data cube by DataTable
			var ordersPvtData = new PivotData(new[] {  "product", "year" },
				new CompositeAggregatorFactory(
					new SumAggregatorFactory("quantity"),
					new SumAggregatorFactory("total"),
					new AverageAggregatorFactory("total")
				));
			ordersPvtData.ProcessData( new DataTableReader(ordersTable) );

			// lets calculate simple expression-based formula measure
			var dynFormula = new DynamicFormulaMeasure("sumoftotal / sumofquantity", ordersPvtData);
			var resPvtData = new SliceQuery(ordersPvtData)
					.Measure("Weighted Total", dynFormula.GetFormulaValue, dynFormula.GetParentMeasureIndexes() )
					.Execute();

			// now resPvtData has only one measure calculated by the formula

			foreach (var k in resPvtData.GetDimensionKeys(new[] { "year" })[0]) {
				Console.WriteLine("Weighted total for [{0}]: {1:0.##}", k, resPvtData[Key.Empty, k].Value );
			}
			Console.WriteLine("Weighted Grand Total: {0:0.##}", resPvtData[Key.Empty, Key.Empty].Value);
			Console.WriteLine("\nPress any key to continue...");
			Console.ReadKey();
		}

		static DataTable GetOrdersTable() {
			// sample "orders" table that contains 1,000 rows
			var t = new DataTable("orders");
			t.Columns.Add("product", typeof(string));
			t.Columns.Add("country", typeof(string));
			t.Columns.Add("quantity", typeof(int));
			t.Columns.Add("total", typeof(decimal));
			t.Columns.Add("year", typeof(int));
			t.Columns.Add("month", typeof(int));
			t.Columns.Add("day", typeof(int));

			var countries = new [] { "USA", "United Kingdom", "Germany", "Italy", "France", "Canada", "Spain" };
			var products = new [] { "Product #1", "Product #2", "Product #3" };
			var productPrices = new decimal[] { 21, 33, 78 };

			for (int i = 1; i <= 1000; i++) {
				var q = 1+(i%6);
				var productIdx = (i+i%10)%products.Length;
				t.Rows.Add(new object[] {
					products[productIdx],
 					countries[i%countries.Length],
					q,
					q*productPrices[productIdx],
					2010 + (i%6),
					1+(i%12),
					i%29
				});
			}
			t.AcceptChanges();
			return t;

		}

	}
}
