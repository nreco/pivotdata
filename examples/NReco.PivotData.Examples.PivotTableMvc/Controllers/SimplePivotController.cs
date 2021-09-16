using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Xml;
using System.IO;
using System.Net;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using Microsoft.AspNetCore.Hosting;

using NReco.PivotData;

namespace NReco.PivotData.Examples.PivotTableMvc {
	
	/// <summary>
	/// Simple pivot table example. 
	/// HTML is rendered by custom code that iterates through <see cref="NReco.PivotData.PivotTable"/> model.
	/// (see Views\SimplePivot\PivotTable.cshtml)
	/// </summary>
	public class SimplePivotController : Controller {

		IWebHostEnvironment HostEnv;

		public SimplePivotController(IWebHostEnvironment hostEnv) {
			HostEnv = hostEnv;
		}

		public ActionResult Index() {
			ViewBag.PageAlias = "simple";

			var pvtData = GetDataCube();
			// slice data cube with SliceQuery class
			var filterByCategories = new[] { "web", "software", "hardware" };
			var slicedPvtData = new SliceQuery(pvtData)
					.Dimension("funded_year_quarter")
					.Dimension("category")
					.Where("category", filterByCategories).Measure(1).Execute();

			// illustrates how to build classic 2D pivot table
			var pvtTbl = new PivotTable(
					new[] { "funded_year_quarter" }, // rows
					new[] { "category" },
					slicedPvtData);

			return View(new PivotTableContext() {
				PivotTableData = pvtTbl,
				CubeData = pvtData
			} );
		}

		public PivotData GetDataCube() {
			// load serialized cube from sample file (aggregation result of 'TechCrunchcontinentalUSA.csv' from CsvDemo example) 
			var cubeFile = Path.Combine(HostEnv.ContentRootPath,"App_Data/TechCrunchCube.dat");
			
			// configuration of the serialized cube
			var pvtData = new PivotData(new[]{"company","category","fundedDate","funded_year_quarter","round"},
					new CompositeAggregatorFactory( 
						new CountAggregatorFactory(),
						new SumAggregatorFactory("raisedAmt"),
						new AverageAggregatorFactory("raisedAmt"),
						new MaxAggregatorFactory("raisedAmt")
					), false);
			using (var fs = new FileStream(cubeFile, FileMode.Open)) {
				var pvtState = PivotDataState.Deserialize(fs);
				pvtData.SetState(pvtState);
			}
			return pvtData;
		}



	}
}
