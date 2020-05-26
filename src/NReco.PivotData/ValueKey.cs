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
	
	/// <summary>
	/// Represents key for a multidimensional value.
	/// </summary>
	public sealed class ValueKey {

		public readonly object[] DimKeys;

		/// <summary>
		/// Check if at least one dimension key is empty
		/// </summary>
		public bool HasEmpty {
			get {
				return HasEmptyKey(DimKeys);
			}
		}

		public ValueKey(params object[] dimKeys) {
			DimKeys = dimKeys;
		}

		public override int GetHashCode() {
			return Comparer.GetHashCode(DimKeys);
		}

		public override bool Equals(object obj) {
			if ( obj==null || !(obj is ValueKey))
				return false;
			return Comparer.Equals( DimKeys, ((ValueKey)obj).DimKeys );
		}

		public static bool HasEmptyKey(object[] vk) {
			switch (vk.Length) {
				case 1: return Key._Empty == vk[0];
				case 2: return Key._Empty == vk[0] || Key._Empty == vk[1];
				case 3: return Key._Empty == vk[0] || Key._Empty == vk[1] || Key._Empty == vk[2];
				case 4: return Key._Empty == vk[0] || Key._Empty == vk[1] || Key._Empty == vk[2] || Key._Empty == vk[3];
				default:
					for (int i = 0; i < vk.Length; i++) {
						if (Key._Empty == vk[i])
							return true;
					}
					return false;
			}
		}

		public override string ToString() {
			return String.Format("[ {0} ]", String.Join(",", DimKeys.Select(k=>k.ToString()).ToArray() ) );
		}

		internal static ValueKey _Empty2D = new ValueKey(Key.Empty,Key.Empty);
		internal static ValueKey _Empty1D = new ValueKey(Key.Empty);

		public static ValueKey Empty2D {
			get { return _Empty2D; }
		}

		public static ValueKey Empty1D {
			get { return _Empty1D; }
		}

		private static ValueKeyEqualityComparer Comparer = new ValueKeyEqualityComparer();
	}


	internal sealed class ValueKeyEqualityComparer : IEqualityComparer<object[]> {

		public bool Equals(object[] x, object[] y) {
			if (ReferenceEquals(x,y))
				return true;
			if (x.Length!=y.Length)
				return false;
			switch (x.Length) {
				case 1: return eq(x[0], y[0]);
				case 2: return eq(x[0], y[0]) && eq(x[1], y[1]);
				case 3: return eq(x[0], y[0]) && eq(x[1], y[1]) && eq(x[2], y[2]);
				case 4: return eq(x[0], y[0]) && eq(x[1], y[1]) && eq(x[2], y[2]) && eq(x[3],y[3]);
				default:
					for (int i = 0; i < x.Length; i++) {
						if (!eq(x[i], y[i]))
							return false;
					}
					return true;
			}

			bool eq(object a, object b) {
				var sameType = a.GetType() == b.GetType();
				if (sameType) {
					// just Equals
					return a.Equals(b);
				}
				var sameHash = a.GetHashCode() == b.GetHashCode();
				if (sameHash) {
					// this could be the same fixed-point value but different types
					if (PivotDataHelper.IsConvertibleToDecimal(a.GetType()) &&
						PivotDataHelper.IsConvertibleToDecimal(b.GetType())) {
						return Convert.ToDecimal(a) == Convert.ToDecimal(b);
					}
				}

				return a.Equals(b);
			}
		}

		public int GetHashCode(object[] obj) {
			if (obj==null) return 0;
			switch (obj.Length) {
				case 1: return obj[0].GetHashCode();
				case 2: return unchecked( 31 * obj[0].GetHashCode() + obj[1].GetHashCode());
				case 3: 
					int hashCode3 =  unchecked(31 * obj[0].GetHashCode() + obj[1].GetHashCode());
					return unchecked(31*hashCode3 + obj[2].GetHashCode());
				case 4:
					int hashCode4 = unchecked(31 * obj[0].GetHashCode() + obj[1].GetHashCode());
					hashCode4 = unchecked(31 * hashCode4 + obj[2].GetHashCode());
					return unchecked(31*hashCode4 + obj[3].GetHashCode());
				default:
					int hashCode = 1;
					for (int i = 0; i < obj.Length; i++) {
						hashCode = unchecked( 31 * hashCode + obj[i].GetHashCode() );
					}
					return hashCode;
			}
		}
	}


}
