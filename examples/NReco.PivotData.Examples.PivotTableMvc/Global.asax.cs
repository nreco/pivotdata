using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace NReco.PivotData.Examples.PivotTableMvc
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
			RouteTable.Routes.Clear();
			RouteTable.Routes.MapRoute(
				"Default", // Route name
				"{controller}/{action}/{id}", // URL with parameters
				new { controller = "SimplePivot", action = "Index", id = UrlParameter.Optional } // Parameter defaults
			);

			// workaround for SQLite DLL load issue under IISExpress
			Environment.SetEnvironmentVariable("PreLoadSQLite_NoSearchForDirectory", "true");
			Environment.SetEnvironmentVariable("PreLoadSQLite_BaseDirectory", System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"bin") );
        }
    }
}
