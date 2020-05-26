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
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace NReco.PivotData {

	internal class TotalsCachePivotData : IPivotData {
		IPivotData PvtData;
		Func<object[], IAggregator> GetValueHandler;

		public string[] Dimensions => PvtData.Dimensions;
		public IAggregatorFactory AggregatorFactory => PvtData.AggregatorFactory;
		public int Count => PvtData.Count;
		public IEnumerator<KeyValuePair<object[], IAggregator>> GetEnumerator() => PvtData.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)PvtData).GetEnumerator();
		public IAggregator this[object[] key] => GetValueHandler(key);

		internal TotalsCachePivotData(IPivotData pvtData) {
			PvtData = pvtData;
			if (pvtData.Dimensions.Length <= 64)
				GetValueHandler = new UlongMaskCache(this).Get;
			else 
				GetValueHandler = new BitArrayMaskCache(this).Get;
		}

		PivotData GetReducedPivotData(IPivotData basePvtData, object[] key, int emptyDims) {
			if (emptyDims == key.Length) {
				// this is grand-total
				return GetGrandTotalPvtData(basePvtData);
			}
			var sliceQuery = new SliceQuery(basePvtData);
			for (int i = 0; i < key.Length; i++)
				if (Key._Empty != key[i]) {
					sliceQuery.Dimension(PvtData.Dimensions[i]);
				}
			var reducedPvtData = sliceQuery.Execute();
			reducedPvtData.LazyAdd = false;
			return reducedPvtData;
		}

		static readonly string[] emptyStringArray = new string[0];
		static readonly object[] emptyObjectArray = new object[0];

		PivotData GetGrandTotalPvtData(IPivotData basePvtData) {
			// this is grand-total
			var grandTotalPvtData = new PivotData(emptyStringArray, PvtData.AggregatorFactory);
			var grandTotalAggr = grandTotalPvtData[emptyObjectArray];
			foreach (var entry in basePvtData)
				grandTotalAggr.Merge(entry.Value);
			return grandTotalPvtData;
		}

		object[] GetReducedKey(object[] key, int emptyDims) {
			object[] reducedKey = new object[key.Length - emptyDims];
			int j = 0;
			for (int i = 0; i < key.Length; i++)
				if (Key._Empty != key[i]) {
					reducedKey[j++] = key[i];
				}
			return reducedKey;
		}

		internal struct UlongMask {
			internal ulong mask;
			internal int emptyDims;

			public override bool Equals(object obj) {
				var m = (UlongMask)obj;
				return mask == m.mask;
			}

			public override int GetHashCode() {
				return mask.GetHashCode();
			}
		}

		internal class UlongMaskCache {
			TotalsCachePivotData PvtDataWrapper;
			Dictionary<UlongMask, PivotData> Cache;

			internal UlongMaskCache(TotalsCachePivotData pvtDataWr) {
				PvtDataWrapper = pvtDataWr;
				Cache = new Dictionary<UlongMask, PivotData>();
			}

			IPivotData GetBasePivotData(ulong findDimMask) {
				PivotData minPvtData = null;
				foreach (var cacheEntry in Cache)
					if ((cacheEntry.Key.mask & findDimMask) == findDimMask) {
						if (minPvtData == null || cacheEntry.Value.Count < minPvtData.Count)
							minPvtData = cacheEntry.Value;
					}
				return minPvtData ?? PvtDataWrapper.PvtData;
			}

			UlongMask GetMask(object[] key) {
				var m = new UlongMask();
				for (int i = 0; i < key.Length; i++) {
					if (Key._Empty == key[i]) {
						m.emptyDims++;
					} else {
						m.mask |= (1ul << (i));
					}
				}
				return m;
			}

			internal IAggregator Get(object[] key) {
				var m = GetMask(key);
				if (m.emptyDims > 0) {
					PivotData reducedPvtData;
					if (!Cache.TryGetValue(m, out reducedPvtData)) {
						reducedPvtData = PvtDataWrapper.GetReducedPivotData(GetBasePivotData(m.mask), key, m.emptyDims);
						Cache[m] = reducedPvtData;
					}
					return reducedPvtData[PvtDataWrapper.GetReducedKey(key, m.emptyDims)];
				} else {
					return PvtDataWrapper.PvtData[key];
				}
			}
		}

		internal struct BitArrayMask {
			internal int[] bitArray;
			internal int emptyDims;

			internal BitArrayMask(int size) {
				bitArray = new int[size];
				emptyDims = 0;
			}

			internal void Set(int index, bool value) {
				if (value) {
					this.bitArray[index / 32] |= 1 << index % 32;
				} else {
					this.bitArray[index / 32] &= ~(1 << index % 32);
				}
			}

			internal bool Match(BitArrayMask m) {
				int maskPart = 0;
				for (int i = 0; i < bitArray.Length; i++) {
					maskPart = m.bitArray[i];
					if ((bitArray[i] & maskPart) != maskPart)
						return false;
				}
				return true;
			}

			public override bool Equals(object obj) {
				return Equals((BitArrayMask)obj);
			}

			internal bool Equals(BitArrayMask m) {
				for (int i = 0; i < bitArray.Length; i++)
					if (bitArray[i] != m.bitArray[i])
						return false;
				return true;
			}
		}

		internal class BitArrayMaskCache {
			TotalsCachePivotData PvtDataWrapper;
			Dictionary<BitArrayMask, PivotData> Cache;
			int Size;

			internal BitArrayMaskCache(TotalsCachePivotData pvtDataWr) {
				PvtDataWrapper = pvtDataWr;
				Size = (pvtDataWr.Dimensions.Length - 1) / sizeof(int) + 1;
				Cache = new Dictionary<BitArrayMask, PivotData>();
			}

			IPivotData GetBasePivotData(BitArrayMask findMask) {
				PivotData minPvtData = null;
				foreach (var cacheEntry in Cache)
					if (cacheEntry.Key.Match(findMask))
						if (minPvtData == null || cacheEntry.Value.Count < minPvtData.Count)
							minPvtData = cacheEntry.Value;
				return minPvtData ?? PvtDataWrapper.PvtData;
			}

			BitArrayMask GetMask(object[] key) {
				var m = new BitArrayMask(Size);
				for (int i = 0; i < key.Length; i++) {
					if (Key._Empty == key[i]) {
						m.emptyDims++;
					} else {
						m.Set(i, true);
					}
				}
				return m;
			}

			internal IAggregator Get(object[] key) {
				var m = GetMask(key);
				if (m.emptyDims > 0) {
					PivotData reducedPvtData;
					if (!Cache.TryGetValue(m, out reducedPvtData)) {
						reducedPvtData = PvtDataWrapper.GetReducedPivotData(GetBasePivotData(m), key, m.emptyDims);
						Cache[m] = reducedPvtData;
					}
					return reducedPvtData[PvtDataWrapper.GetReducedKey(key, m.emptyDims)];
				} else {
					return PvtDataWrapper.PvtData[key];
				}
			}
		}
		

	}

}
