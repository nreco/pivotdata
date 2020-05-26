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
using System.Globalization;

namespace NReco.PivotData {
	
	public class ConvertHelper {

		public static decimal ConvertToDecimal(object o, decimal defaultVal) {
			if (o != null) { 
				var type = o.GetType();
				switch (Type.GetTypeCode( Nullable.GetUnderlyingType(type) ?? type )) {
					case TypeCode.Byte:
						return (byte)o;
					case TypeCode.SByte:
						return (sbyte)o;
					case TypeCode.UInt16:
						return (ushort)o;
					case TypeCode.UInt32:
						return (uint)o;
					case TypeCode.UInt64:
						return (ulong)o;
					case TypeCode.Int16:
						return (short)o;
					case TypeCode.Int32:
						return (int)o;
					case TypeCode.Int64:
						return (long)o;
					case TypeCode.Decimal:
						return (decimal)o;
					case TypeCode.Single:
						return (decimal) ((float)o);
					case TypeCode.Double:
						return (decimal) ((double)o);
					case TypeCode.String: { 
							decimal d;
							if (Decimal.TryParse((string)o, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
								return d;
							}
							return defaultVal;
						}
			}
			return defaultVal;
		}

		public static double ConvertToDouble(object o, double defaultVal) {
			if (o != null) { 
				var type = o.GetType();
				switch (Type.GetTypeCode( Nullable.GetUnderlyingType(type) ?? type )) {
					case TypeCode.Byte:
						return (byte)o;
					case TypeCode.SByte:
						return (sbyte)o;
					case TypeCode.UInt16:
						return (ushort)o;
					case TypeCode.UInt32:
						return (uint)o;
					case TypeCode.UInt64:
						return (ulong)o;
					case TypeCode.Int16:
						return (short)o;
					case TypeCode.Int32:
						return (int)o;
					case TypeCode.Int64:
						return (long)o;
					case TypeCode.Decimal:
						return (double) ((decimal)o);
					case TypeCode.Single:
						return (float)o;
					case TypeCode.Double:
						return (double)o;
					case TypeCode.String: { 
							double d;
							if (Double.TryParse((string)o, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
								return d;
							}
							return defaultVal;
						}
			}
			return defaultVal;
		}

	}

}
