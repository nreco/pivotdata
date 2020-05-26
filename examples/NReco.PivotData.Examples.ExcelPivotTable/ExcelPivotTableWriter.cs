using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;

using OfficeOpenXml;
using OfficeOpenXml.Style;

using NReco.PivotData;

namespace NReco.PivotData.Examples.ExcelPivotTable {
	
	public class ExcelPivotTableWriter {

		ExcelWorksheet ws;
		ExcelWorksheet wsData;

		public ExcelPivotTableWriter(ExcelWorksheet wsPvt, ExcelWorksheet wsData) {
			this.ws = wsPvt;
			this.wsData = wsData;
		}

		DataTable getPivotDataAsTable(IPivotData pvtData) {
			var tbl = new DataTable();
			// create columns by pivot data
			foreach (var dim in pvtData.Dimensions)
				tbl.Columns.Add(dim, typeof(object));
			if (pvtData.AggregatorFactory is CompositeAggregatorFactory) {
				var aggrFactories = ((CompositeAggregatorFactory)pvtData.AggregatorFactory).Factories;
				for (int i=0; i<aggrFactories.Length; i++)
					tbl.Columns.Add(String.Format("value_{0}", i), typeof(object));
			} else {
				tbl.Columns.Add("value", typeof(object));
			}
			// add rows
			foreach (var entry in pvtData) {
				var vals = new object[tbl.Columns.Count];
				for (int i=0; i<entry.Key.Length; i++)
					vals[i] = entry.Key[i];
				var aggr = entry.Value.AsComposite();
				for (int i=0; i<aggr.Aggregators.Length; i++)
					vals[entry.Key.Length+i] = aggr.Aggregators[i].Value;
				tbl.Rows.Add(vals);
			}

			tbl.AcceptChanges();
			return tbl;
		}

		OfficeOpenXml.Table.PivotTable.DataFieldFunctions SuggestFunction(IAggregatorFactory aggrFactory) {
			if (aggrFactory is MinAggregatorFactory)
				return OfficeOpenXml.Table.PivotTable.DataFieldFunctions.Min;
			else if (aggrFactory is MaxAggregatorFactory)
				return OfficeOpenXml.Table.PivotTable.DataFieldFunctions.Max;
			else if (aggrFactory is AverageAggregatorFactory)
				return OfficeOpenXml.Table.PivotTable.DataFieldFunctions.Average;
			return OfficeOpenXml.Table.PivotTable.DataFieldFunctions.Sum; // by default
		}

		public void Write(PivotTable pvtTbl) {
			var tbl = getPivotDataAsTable(pvtTbl.PivotData);
			var rangePivotTable = wsData.Cells["A1"].LoadFromDataTable( tbl, true );

			var pivotTable = ws.PivotTables.Add(
					ws.Cells[1,1], 
					rangePivotTable, "pvtTable");

			foreach (var rowDim in pvtTbl.Rows)
				pivotTable.RowFields.Add(pivotTable.Fields[rowDim]);
			foreach (var colDim in pvtTbl.Columns)
				pivotTable.ColumnFields.Add(pivotTable.Fields[colDim]);

			if (pvtTbl.PivotData.AggregatorFactory is CompositeAggregatorFactory) {
				var aggrFactories = ((CompositeAggregatorFactory)pvtTbl.PivotData.AggregatorFactory).Factories;
				for (int i=0; i<aggrFactories.Length; i++) {
					var dt = pivotTable.DataFields.Add(pivotTable.Fields[String.Format("value_{0}", i)]);
					dt.Function = SuggestFunction(aggrFactories[i]);
					dt.Name = aggrFactories[i].ToString();
				}
			} else {
				pivotTable.DataFields.Add(pivotTable.Fields["value"]).Function = SuggestFunction(pvtTbl.PivotData.AggregatorFactory);
			}

		}
	}

}
