using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

using OfficeOpenXml;
using OfficeOpenXml.Style;

using NReco.PivotData;

namespace NReco.PivotData.Examples.ExcelPivotTable {
	
	/// <summary>
	/// Export PivotData pivot table to Excel Pivot Table.
	/// This example illustrates how to create Excel Pivot Table without writing whole dataset to Excel worksheet.
	/// More details: http://www.nrecosite.com/pivotdata/create-excel-pivot-table.aspx
	/// </summary>
	class Program {

		static void Main(string[] args) {

			// sample dataset
			var ordersTable = GetOrdersTable();

			// build data cube by large DataTable (you may use database data reader directly)
			var ordersPvtData = new PivotData(new[] {  "product", "country", "year", "month", "day"},
				new CompositeAggregatorFactory(
					new SumAggregatorFactory("quantity"),
					new SumAggregatorFactory("total"),
					new AverageAggregatorFactory("total")
				));
			ordersPvtData.ProcessData( new DataTableReader(ordersTable) );

			var pkg = new ExcelPackage();
			pkg.Compression = CompressionLevel.Default;
			var wsPvt = pkg.Workbook.Worksheets.Add( "Pivot Table" );
			var wsData = pkg.Workbook.Worksheets.Add( "Source Data" );

			var pvtTbl = new PivotTable(
					new[] {"country"}, //rows
					new[] { "year"}, // columns
					ordersPvtData
				);
			var excelPvtTblWr = new ExcelPivotTableWriter(wsPvt, wsData);
			excelPvtTblWr.Write(pvtTbl);

			//pkg.Workbook.Worksheets.Delete(wsData);

			using (var excelFs = new FileStream("result.xlsx", FileMode.Create, FileAccess.Write)) { 
				pkg.SaveAs(excelFs);
			}
			Console.WriteLine("Generated Excel file with PivotTable: result.xlsx");

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
