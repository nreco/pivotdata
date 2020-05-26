using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NReco.PivotData;

namespace NReco.PivotData.Examples.DynamicListGrouping {

	/// <summary>
	/// Efficiently aggregate and group any .NET typed list dynamically.
	/// More details: http://www.nrecosite.com/pivotdata/dynamic-data-aggregation-and-grouping.aspx
	/// </summary>
	class Program {
		static void Main(string[] args) {

			Console.WriteLine("Generating customers list...");
			var customers = new List<Customer>();
			for (int i=0; i<1000000; i++)
				customers.Add( new Customer() {
					Category = "Category_"+(i%50).ToString(),  // lets generate 50 unique categories
					Status = "Status_"+( (i+i%17)%10).ToString()  // 10 unique statuses
				});
			Console.WriteLine("Generated {0:#,###} objects\n", customers.Count);

 			// variant 1: generic solution that uses ObjectMember library for getting object properties
			GroupByMultipleWithObjectMember(customers, new[] {"Category", "Status"} );

			// variant 2: object members are accessed with custom accessor delegate (faster)
			GroupByMultipleWithCustomAccessor(customers, new[] {"Category", "Status"} );


			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}

		static void GroupByMultipleWithObjectMember(IList<Customer> list, string[] groupByColumns) {
			Console.WriteLine("Aggregating by {0} with help of ObjectMember...", String.Join(", ", groupByColumns) );

			var sw = new Stopwatch();

			// configure pivot data
			var pvtData = new PivotData(groupByColumns, new CountAggregatorFactory() );

			// process the list
			sw.Start();
			pvtData.ProcessData( list, new ObjectMember().GetValue );
			sw.Stop();

			Console.WriteLine("Results: unique groups = {0}, processing time = {1} sec", pvtData.Count, sw.Elapsed.TotalSeconds);
		}


		static void GroupByMultipleWithCustomAccessor(IList<Customer> list, string[] groupByColumns) {
			Console.WriteLine("Aggregating by {0} with custom fields accessor...", String.Join(", ", groupByColumns) );

			var sw = new Stopwatch();

			// configure pivot data
			var pvtData = new PivotData(groupByColumns, new CountAggregatorFactory() );

			// process the list
			sw.Start();
			pvtData.ProcessData( list, (o, colName) => {
				// returned values are totally controlled by accessor delegates
				// this is fastest way but it is applicable only for typed collections
				var customer = (Customer)o;
				switch (colName) {
					case "Category": return customer.Category;  
					case "Status": return customer.Status;
				}
				return null;
			} );
			sw.Stop();

			Console.WriteLine("Results: unique groups = {0}, processing time = {1} sec", pvtData.Count, sw.Elapsed.TotalSeconds);
		}

	}

	// sample data model
	class Customer {
		public string Category { get; set; }
		public string Status { get; set; }

		// other properies might go here
	}
}
