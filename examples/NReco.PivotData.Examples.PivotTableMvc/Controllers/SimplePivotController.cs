using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.IO;
using System.Net;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Data;

using System.Web.Security;

using NReco.PivotData;

namespace Controllers {
	
	/// <summary>
	/// Simple pivot table example. 
	/// HTML is rendered by custom code that iterates through <see cref="NReco.PivotData.PivotTable"/> model.
	/// (see Views\SimplePivot\PivotTable.cshtml)
	/// </summary>
	public class SimplePivotController : Controller {

		public ActionResult Index() {
			ViewBag.PageAlias = "simple";
			return View();
		}

		public ActionResult PivotTable() {
			var pvtData = GetDataCube();
			
			// slice data cube with SliceQuery class
			var filterByCategories = new [] {"web","software","hardware"};
			var slicedPvtData = new SliceQuery(pvtData)
					.Dimension("funded_year_quarter")
					.Dimension("category")
					.Where("category", filterByCategories).Measure(1).Execute();

			// illustrates how to build classic 2D pivot table
			var pvtTbl = new PivotTable(
					new [] {"funded_year_quarter"}, // rows
					new [] {"category"},
					slicedPvtData);
			
			return PartialView(pvtTbl);
		}

		public ActionResult GoogleChart() {
			var pvtData = GetDataCube();

			return PartialView(pvtData);
		}

		public PivotData GetDataCube() {
			// load serialized cube from sample file (aggregation result of 'TechCrunchcontinentalUSA.csv' from CsvDemo example) 
			var cubeFile = HttpContext.Server.MapPath("~/App_Data/TechCrunchCube.dat");
			
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
