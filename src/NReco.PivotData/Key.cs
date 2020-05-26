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
	/// Dimension key helper
	/// </summary>
	public static class Key {

		public static int GetHashCode(object key) {
			return key!=null ? key.GetHashCode() : 0;
		}

		public static bool Equals(object keyA, object keyB) {
			// quick checks
			if (Object.ReferenceEquals(keyA,keyB))
				return true;
			if (GetHashCode(keyA)!=GetHashCode(keyB))
				return false;
			return keyA!=null ? keyA.Equals(keyB) : (keyB==null);
		}

		public static bool IsEmpty(object key) {
			return _Empty == key;
		}

		public static string ToString(object key) {
			return String.Format("[{0}]", key );
		}

		internal static object _Empty = new object();
		public static object Empty {
			get { return _Empty; }
		}

	}

}
