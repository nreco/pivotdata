using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.IO;

using NReco.PivotData;
using CsvHelper;
using CsvHelper.Configuration;

namespace NReco.PivotData.Examples.CsvDemo {
	
	/// <summary>
	/// This example illustrates how to use NReco PivotData library for CSV data aggregation.
	/// NOTE: you can use CsvSource class from BI Toolkit (NReco.PivotData.Extensions assembly) which can parse CSV file 3x times faster than CSVHelper library.
	/// </summary>	
	class Program {
		static void Main(string[] args) {

			Console.Write("CsvDemo: illustrates how to use CSV file as input for PivotData and use several aggregators at once\nInput data: TechCrunch funds raised facts\n\n");

			var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture) {
				Delimiter = ",",
			};
			var file = "TechCrunchcontinentalUSA.csv";
			using (var fileReader =  new StreamReader(file)) {
				var csvReader = new CsvReader(fileReader, csvConfig );

				var fldToIdx = new Dictionary<string, int>();
				csvReader.Read();
				csvReader.ReadHeader();
				if (csvReader.HeaderRecord != null && fldToIdx.Count == 0)
					for (int i = 0; i < csvReader.HeaderRecord.Length; i++) {
						fldToIdx[csvReader.HeaderRecord[i]] = i;
						//Console.WriteLine("Column #{0}: {1}", i, csvReader.FieldHeaders[i]);
					}
				
				// accessor for field values
				Func<object,string,object> getValue = (r, f) => {
					if (f == "fundedDate-year") {
						var foundedDate = ((CsvReader)r)[ fldToIdx["fundedDate"] ];
						DateTime dt;
						if (DateTime.TryParse(foundedDate, out dt))
							return dt.Year;
						else
							return null;
					}
					var csvColVal = ((CsvReader)r)[fldToIdx[f]];
					if (f == "raisedAmt")
						return Decimal.Parse(csvColVal);
					return csvColVal; // just return csv value
				};

				var pivotData = new PivotData(new [] {"category", "fundedDate-year", "round"},
						new CompositeAggregatorFactory(
							new IAggregatorFactory[] { 
								new SumAggregatorFactory("raisedAmt"),
								new AverageAggregatorFactory("raisedAmt"),
								new MaxAggregatorFactory("raisedAmt")
							}	
						), 
						true  // lazy totals
					);
				// calculate in-memory cube
				pivotData.ProcessData( readCsvRows(csvReader), getValue );

				// lets show total raised by round
				Console.WriteLine("Total raised $$ by round:");
				var byRoundPivotData = new SliceQuery(pivotData).Dimension("round").Execute(); // slice by specific dimension
				foreach (var round in byRoundPivotData.GetDimensionKeys()[0]) {
					Console.WriteLine("Round '{0}': ${1:0.#}M", round, 
						GetMln( ((object[])byRoundPivotData[round].Value)[0] /* 1st aggregator */ ) );
				}

				Console.WriteLine("\nMax/Avg raised by year:");
				var byYearPivotData = new SliceQuery(pivotData).Dimension("fundedDate-year").Execute();
				foreach (var year in byYearPivotData.GetDimensionKeys()[0]) {
					Console.WriteLine("Year {0}: max=${1:0.#}M   avg=${2:0.#}M", year, 
						GetMln( ((object[])byYearPivotData[year].Value)[2] /* 3rd aggregator */ ),
						GetMln( ((object[])byYearPivotData[year].Value)[1] /* 2nd aggregator */ )
						);
				}
				
				Console.WriteLine("\n\nPress any key to exit...");
				Console.ReadKey();
			}
		}

		static decimal GetMln(object o) {
			decimal d = Convert.ToDecimal( o );
			return d/(1000000);
		}

		static IEnumerable readCsvRows(CsvReader csvReader) {
			while (csvReader.Read()) {
				yield return csvReader;
			}
		}

	}
}
