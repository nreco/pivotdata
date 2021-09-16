using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NReco.PivotData;

namespace NReco.PivotData.Examples.PivotTableMvc {
	public class PivotTableContext {

		public IPivotTable PivotTableData { get; set; }

		public IPivotData CubeData { get; set; }

	}
}
