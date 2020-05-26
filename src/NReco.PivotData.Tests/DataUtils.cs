using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Xunit;

namespace NReco.PivotData.Tests {
	
	public class DataUtils {

		public static IEnumerable getSampleData(int records = 100000) {
			var aVals = new string[] {"val1", "val2", "val3"};
			var rnd = new Random();
			for (int i = 0; i < records; i++) {
				yield return new {
					a = aVals[i%aVals.Length],
					b = i,
					c = rnd.NextDouble()*10,
					d = i%100
				};
			}
		}

		public static object getProp(object r, string f) {
			return r.GetType().GetProperty(f).GetValue(r);
		}

	}
}
