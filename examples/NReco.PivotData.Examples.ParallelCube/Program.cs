using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

using NReco.PivotData;

namespace NReco.PivotData.Examples.ParallelCube {
	
	public class Program {
		static void Main(string[] args) {

			var fieldToIdx = new Dictionary<string,int>() {
				{"name", 0},
				{"age", 1},
				{"company", 2},
				{"date", 3},
				{"hours", 4},
			};
			Func<object,string,object> getValue = (r, f) => {
				return ((object[])r)[ fieldToIdx[f] ];
			};

			var dataChunks = GetDataChunks();
			Console.WriteLine("Generating 50 data chunks (each 100,000 records) on the fly = 5 mln in total");

			var dimensions = new string[] { 
				"company", "name", "age", "date"
			};
			var aggrFactory = new SumAggregatorFactory("hours");
			var allPvtData = new PivotData(dimensions,aggrFactory, true);
			Parallel.ForEach(dataChunks, (t)=> {
				var chunkPvtData = new PivotData(dimensions, aggrFactory, true);
				chunkPvtData.ProcessData(t, getValue);

				Console.WriteLine("Calculated pivot data chunk, aggregated values: {0}", chunkPvtData.Count);
				lock (allPvtData) {
					allPvtData.Merge(chunkPvtData);
				}
			});

			Console.WriteLine("Parallel calculation of cube finished.\nTotal dimensions: {0}\nTotal aggregated values: {1}\nTotal hours: {2}", 
				dimensions.Length, allPvtData.Count,
				allPvtData[Key.Empty,Key.Empty,Key.Empty,Key.Empty].Value);
			Console.ReadKey();
		}

		static IEnumerable<object[][]> GetDataChunks() {

			var names = new string[] {"John", "Mary", "Steve", "Bob", "Pit", "Peter"};
			var companies = new string[] {"Google", "Microsoft", "Facebook", "Yahoo"};

			for (int i = 0; i < 50; i++) {
				var t = new object[100000][];

				for (int j = 0; j < t.Length; j++) {
					t[j] = new object[] { 
						names[j%names.Length], 
						(15+(j+i))%40, 
						companies[j%companies.Length],
						new DateTime(2000+(i+j)%15, j%12+1, (i+j)%27+1 ),
						(i+j)%20
					};
				}

				yield return t;
			}
		}

		
	}
}
