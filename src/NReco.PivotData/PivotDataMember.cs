/*
 *  Copyright 2015-2020 Vitaliy Fedorchenko (nrecosite.com)
 *
 *  Licensed under PivotData Source Code Licence (see LICENSE file).
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS 
 *  OF ANY KIND, either express or implied.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NReco.PivotData {

	internal class PivotDataMember {
		IPivotData PvtData;
		Dictionary<string,int> DimToIdx;
		Dictionary<string,int> AggrToIdx;

		internal PivotDataMember(IPivotData pvtData, string[] aggrNames) {
			PvtData = pvtData;
			DimToIdx = new Dictionary<string,int>(pvtData.Dimensions.Length);
			for (int i=0; i<pvtData.Dimensions.Length; i++) {
				DimToIdx[pvtData.Dimensions[i]] = i;
			}
			AggrToIdx = new Dictionary<string,int>(aggrNames.Length);
			for (int i=0; i<aggrNames.Length; i++)
				AggrToIdx[aggrNames[i]] = i;
		}

		internal object GetValue(object o, string name) {
			var entry = (KeyValuePair<object[],IAggregator>)o;
			int idx = -1;
			if (DimToIdx.TryGetValue(name, out idx))
				return entry.Key[idx];
			if (AggrToIdx.TryGetValue(name, out idx)) {
				var compositeAggr = entry.Value as CompositeAggregator;
				if (idx==0 && compositeAggr==null )
					return entry.Value.Value;
				return compositeAggr.Aggregators[idx].Value;
			}
			throw new ArgumentException("Unknown field name: "+name);
		}

	}

}
