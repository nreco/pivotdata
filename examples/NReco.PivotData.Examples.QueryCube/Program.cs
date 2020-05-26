using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

using NReco.PivotData;

namespace NReco.PivotData.Examples.QueryCube {
	
	/// <summary>
	/// Create data cube by DataTable (or DB data reader) and perform analytical cube queries.
	/// More details: http://www.nrecosite.com/pivotdata/analytical-cube-queries.aspx
	/// </summary>
	class Program {
		
		static void Main(string[] args) {
			// sample dataset
			var ordersTable = GetOrdersTable();

			// build data cube by DataTable
			var ordersPvtData = new PivotData(new[] {  "product", "country", "year", "month", "day"},
				new CompositeAggregatorFactory(
					new SumAggregatorFactory("quantity"),
					new SumAggregatorFactory("total"),
					new AverageAggregatorFactory("total")
				));
			ordersPvtData.ProcessData( new DataTableReader(ordersTable) );

			// query 1: select products from USA and Canada greater than $50
			var northAmericaBigOrdersQuery = new SliceQuery(ordersPvtData)
					.Dimension("product")
					.Dimension("country")
					.Where("country", "USA", "Canada")
					.Where( (dp) => { 
						// filter by measure value (index #1 => sum of total)
						return ConvertHelper.ConvertToDecimal( dp.Value.AsComposite().Aggregators[1].Value, 0M) > 50; 
					} )
					.Measure(1);  // include only "sum of total" measure

			var northAmericaPvtData = northAmericaBigOrdersQuery.Execute();
			// resulting data cube:
			// dimensions = {"product", "country"}, country dimension keys = {"USA", "Canada"}
			// aggregator = sum of total

			Console.WriteLine("North america big orders grand total: ${0:0.00}", northAmericaPvtData[Key.Empty,Key.Empty].Value );
			Console.WriteLine("\tUSA grand total: ${0:0.00}", northAmericaPvtData[Key.Empty,"USA"].Value );
			Console.WriteLine("\tCanada grand total: ${0:0.00}", northAmericaPvtData[Key.Empty,"Canada"].Value );
			Console.WriteLine();

			// query 2: calculated (formula) measure
			// average item price = sum of total / sum of quantity
			var avgItemPriceByYearQuery = new SliceQuery(ordersPvtData)
				.Dimension("year")
				.Measure("Avg item price",
					(aggrArgs) => {
						var sumOfTotal = ConvertHelper.ConvertToDecimal( aggrArgs[0].Value, 0M);  // value of first argument (from measure #1)
						var sumOfQuantity = ConvertHelper.ConvertToDecimal( aggrArgs[1].Value, 0M); // value of second argument (from measure #0)
						if (sumOfQuantity==0)
							return 0M; // prevent div by zero
						return sumOfTotal/sumOfQuantity;
					},
					new int[] { 1, 0 } // indexes of measures for formula arguments
				);
			var avgItemPriceByYearPvtData = avgItemPriceByYearQuery.Execute();
			Console.WriteLine("Average item price by years:");
			foreach (var year in avgItemPriceByYearPvtData.GetDimensionKeys()[0]) {
				Console.WriteLine("\t {0}: ${1:0.00}", year, avgItemPriceByYearPvtData[year].Value );
			}
			Console.WriteLine("\tTotal: ${0:0.00}", avgItemPriceByYearPvtData[Key.Empty].Value);

			// query 3: calculated dimension
			// lets introduce 'region' (calculated by 'country') with 2 possible values: North America, Europe
			var regionQuery = new SliceQuery(ordersPvtData)
					.Dimension("year")
					.Dimension("region",
						(dimKeys) => {
							var country = dimKeys[1];  // depends on ordersPvtData configuration: index #1 is 'country'
							if (country.Equals("USA") || country.Equals("Canada"))
								return "North America";
							return "Europe";
						}
					);  // note that if measure selectors are not defined, measures remains unchanged
			var regionPvtData = regionQuery.Execute();
			var regionPvtTbl = new PivotTable(
					new[]{"year"}, // row dimension
					new[]{"region"}, // column dimension
					regionPvtData);
			Console.WriteLine("\nTotals by region:");
			Console.Write("\t\t\t");
			foreach (var colKey in regionPvtTbl.ColumnKeys) {
				Console.Write("{0}\t", colKey);
			}
			Console.WriteLine();
			for (int r = 0; r < regionPvtTbl.RowKeys.Length; r++) {
				Console.Write("\t{0}:", regionPvtTbl.RowKeys[r]);
				for (int c = 0; c < regionPvtTbl.ColumnKeys.Length; c++) {
					Console.Write("\t${0:######.00}", regionPvtTbl[r,c].AsComposite().Aggregators[1].Value );
				}
				Console.WriteLine();
			}
				

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
