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
using System.IO;

namespace NReco.PivotData {
	
	/// <summary>
	/// Represents compacted "raw" pivot data values for serialization
	/// </summary>
	[Serializable]
	public class PivotDataState {

		/// <summary>
		/// Number of dimensions
		/// </summary>
		public uint DimCount;

		/// <summary>
		/// Ordered list of key values
		/// </summary>
		public object[] KeyValues;

		/// <summary>
		/// Ordered list of value keys (each key is represented by array of key indexes)
		/// </summary>
		public uint[][] ValueKeys;

		/// <summary>
		/// Ordered list of PivotData "values" represented by aggregator's state objects
		/// </summary>
		public object[] Values;

		public PivotDataState() { }

		public PivotDataState(IPivotData pvtData) {
			var allRawKeys = new List<object>();
			var keyIdx = new Dictionary<object,uint>(pvtData.Count);
			var allVkRefs = new uint[pvtData.Count][];
			var allRawVals = new object[pvtData.Count];
			uint i, idx;
			int dimCnt = pvtData.Dimensions.Length;
			object[] vk;

			int valIdx = 0;
			foreach (var entry in pvtData) {
				vk = entry.Key;
	
				var vkIdx = new uint[dimCnt];
				for (i = 0; i < dimCnt; i++) {
					if (!keyIdx.TryGetValue(vk[i], out idx)) {
						idx = (uint)allRawKeys.Count;
						allRawKeys.Add(vk[i]);
						keyIdx[vk[i]] = idx;
					}
					vkIdx[i] = idx;
				}
				allVkRefs[valIdx] = vkIdx;
				allRawVals[valIdx] = entry.Value.GetState();
				valIdx++;
			}

			DimCount = (uint)dimCnt;
			KeyValues = allRawKeys.ToArray();
			ValueKeys = allVkRefs;
			Values = allRawVals;
		}

		public PivotDataState(uint dimCount, object[] keys, uint[][] vk, object[] vals) {
			DimCount = dimCount;
			KeyValues = keys;
			ValueKeys = vk;
			Values = vals;
		}

		/// <summary>
		/// Deserializes <see cref="PivotDataState"/> from binary data contained by the specified stream
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> that contains serialized <see cref="PivotDataState"/> data</param>
		/// <returns>deserialized <see cref="PivotDataState"/> instance</returns>
		/// <remarks>
		/// This method is not supported in .NET Standards (.NET Core) PivotData build (use Json.NET or protobuf-net instead).
		/// </remarks>
		public static PivotDataState Deserialize(Stream stream) {
			using (var rdr = new PivotDataStateBinaryReader(stream)) {
				var typeName = rdr.ReadString();
				if (typeName!=typeof(PivotDataState).FullName)
					throw new InvalidDataException();
				// read version (reserved for future)
				var majorVer = rdr.ReadInt32();
				var minorVer = rdr.ReadInt32();

				var propsCount = rdr.ReadUInt16();
				var pvtDataState = new PivotDataState(0, null, null, null);
				for (short propIdx = 0; propIdx < propsCount; propIdx++) {
					var propName = rdr.ReadString();
					switch (propName) {
						case "KeyValues":
							var keyValues = ReadObject(rdr);
							if (keyValues is object[]) {
								pvtDataState.KeyValues = (object[])keyValues;
							} else {
								throw new InvalidDataException("Invalid value type: KeyValues");
							}
							break;
						case "Values":
							var values = ReadObject(rdr);
							if (values is object[]) {
								pvtDataState.Values = (object[])values;
							} else {
								throw new InvalidDataException("Invalid value type: Values");
							}
							break;
						case "DimCount":
							pvtDataState.DimCount = rdr.ReadUInt32();
							break;
						case "ValueKeys":
							if (pvtDataState.DimCount==0)
								throw new InvalidDataException("Missed DimCount property");
							pvtDataState.ValueKeys = ReadValueKeys(rdr, pvtDataState.DimCount);
							break;
						default:
							throw new InvalidDataException(String.Format("Unknown property: {0}", propName));
					}
				}

				return pvtDataState;
			}
		}

		/// <summary>
		/// Serializes <see cref="PivotDataState"/> into specified <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> used to write the data</param>
		/// <remarks>
		/// This method is not supported in .NET Standards (.NET Core) PivotData build (use Json.NET or protobuf-net instead).
		/// </remarks>		 
		public void Serialize(Stream stream) {
			using (var wr = new PivotDataStateBinaryWriter(stream)) {
				var t = typeof(PivotDataState);
				wr.Write(t.FullName);  // type marker
				var ver = t.Assembly.GetName().Version;
				wr.Write(ver.Major);
				wr.Write(ver.Minor);
				
				wr.Write( (ushort) 4); // write number of serialized properties
				wr.Write("DimCount");
				wr.Write( (uint)DimCount );
				wr.Write("KeyValues");
				WriteObject(wr, KeyValues);
				wr.Write("Values");
				WriteObject(wr, Values);
				wr.Write("ValueKeys");
				WriteValueKeys(wr, ValueKeys, DimCount);
			}
		}

		static object[] ReadObjectArray(PivotDataStateBinaryReader rdr) {
			var arrLen = rdr.Read7BitEncodedLong();
			var arr = new object[arrLen];
			for (ulong i = 0; i < arrLen; i++) {
				arr[i] = ReadObject(rdr);
			}
			return arr;
		}

		static object ReadObject(PivotDataStateBinaryReader rdr) {
			var typeCode = rdr.ReadByte();
			if (typeCode<TypeCodeReaders.Length)	
				return TypeCodeReaders[typeCode](rdr);
			switch (typeCode) {
				case (byte)ExtraTypeCode.ObjectArray:
					return ReadObjectArray(rdr);
				case (byte)ExtraTypeCode.KeyEmpty:
					return Key.Empty;
			}
			throw new InvalidDataException(String.Format("Unknown type code: {0}", typeCode));
		}

		static uint[][] ReadValueKeys(PivotDataStateBinaryReader rdr, uint dimCount) {
			var arrLen = rdr.Read7BitEncodedLong();
			var res = new uint[arrLen][];
			for (ulong i = 0; i < arrLen; i++) {
				var entry = new uint[dimCount];
				for (int j = 0; j < dimCount; j++) { 
					entry[j] = rdr.Read7BitEncodedInt();
				}
				res[i] = entry;
			}
			return res;
		}

		static void WriteObjectArray(PivotDataStateBinaryWriter wr, object[] arr) {
			wr.Write7BitEncodedLong( (ulong) arr.Length );
			for (long i = 0; i < arr.Length; i++) {
				WriteObject(wr, arr[i]);
			}
		}

		static void WriteValueKeys(PivotDataStateBinaryWriter wr, uint[][] valueKeys, uint dimCount) {
			wr.Write7BitEncodedLong( (ulong)valueKeys.Length );
			for (int i = 0; i < valueKeys.Length; i++) {
				for (int j=0; j<dimCount; j++)
					wr.Write7BitEncodedInt(valueKeys[i][j]);
			}
		}

		static void WriteObject(PivotDataStateBinaryWriter wr, object o) {
			var typeCode = o==null ? TypeCode.Empty : Type.GetTypeCode( o.GetType() );
			if (typeCode != TypeCode.Object) {
				var typeCodeIdx = (byte)typeCode;
				wr.Write(typeCodeIdx);
				TypeCodeWriters[typeCodeIdx](wr, o);
			} else if (o is object[]) {
				wr.Write((byte)ExtraTypeCode.ObjectArray);
				WriteObjectArray(wr, (object[])o);
			} else if (Key.IsEmpty(o)) {
				wr.Write((byte)ExtraTypeCode.KeyEmpty);
			} else {
				throw new InvalidDataException("Unsupported object type: " + o.GetType().ToString());
			}
		}

		internal static readonly Func<PivotDataStateBinaryReader,object>[] TypeCodeReaders = new Func<PivotDataStateBinaryReader,object>[] {
			(rdr) => { return null; }, // null
			(rdr) => { return null; }, // read object!! 
			(rdr) => { return DBNull.Value; }, // dbnull
			(rdr) => { return rdr.ReadBoolean(); },
			(rdr) => { return rdr.ReadChar(); },
			(rdr) => { return rdr.ReadSByte(); },
			(rdr) => { return rdr.ReadByte(); },
			(rdr) => { return rdr.ReadInt16(); },
			(rdr) => { return rdr.ReadUInt16(); },
			(rdr) => { return rdr.ReadInt32(); },
			(rdr) => { return rdr.Read7BitEncodedInt(); },
			(rdr) => { return rdr.ReadInt64(); },
			(rdr) => { return rdr.Read7BitEncodedLong(); },
			(rdr) => { return rdr.ReadSingle(); },
			(rdr) => { return rdr.ReadDouble(); },
			(rdr) => { return rdr.ReadDecimal(); },
			(rdr) => { return DateTime.FromBinary(rdr.ReadInt64()); },
			(rdr) => { return null; }, // 17 - not used typecode
			(rdr) => { return rdr.ReadString(); },
		};

		internal static readonly Action<PivotDataStateBinaryWriter,object>[] TypeCodeWriters = new Action<PivotDataStateBinaryWriter,object>[] {
			(wr,o) => { },
			(wr,o) => { },
			(wr,o) => { },
			(wr,o) => { wr.Write( (bool)o ); },
			(wr,o) => { wr.Write( (char)o ); },
			(wr,o) => { wr.Write( (sbyte)o ); },
			(wr,o) => { wr.Write( (byte)o ); },
			(wr,o) => { wr.Write( (short)o ); },
			(wr,o) => { wr.Write( (ushort)o ); },
			(wr,o) => { wr.Write( (int)o ); },
			(wr,o) => { wr.Write7BitEncodedInt( (uint)o ); },
			(wr,o) => { wr.Write( (long)o ); },
			(wr,o) => { wr.Write7BitEncodedLong( (ulong)o ); },
			(wr,o) => { wr.Write( (float)o ); },
			(wr,o) => { wr.Write( (double)o ); },
			(wr,o) => { wr.Write( (decimal)o ); },
			(wr,o) => { wr.Write( ((DateTime)o).ToBinary() ); },
			(wr,o) => { }, // 17 - not used typecode
			(wr,o) => { wr.Write(Convert.ToString(o)); }
		};

		internal enum ExtraTypeCode {
			ObjectArray = 128,
			KeyEmpty = 129
		}

		internal class PivotDataStateBinaryReader : BinaryReader {
			public PivotDataStateBinaryReader(Stream stream) : base(stream) {}
			
			public new uint Read7BitEncodedInt() {
				return (uint)base.Read7BitEncodedInt();
			}

			public ulong Read7BitEncodedLong() {
				ulong num = 0;
				int num2 = 0;
				while (num2 != 63)
				{
					byte b = this.ReadByte();
					num |= (ulong)(b & 127) << num2;
					num2 += 7;
					if ((b & 128) == 0)
					{
						return num;
					}
				}
				throw new FormatException("Invalid 7bit encoded value");
			}
		}

		internal class PivotDataStateBinaryWriter : BinaryWriter {
			public PivotDataStateBinaryWriter(Stream stream) : base(stream) {}
			
			public new void Write7BitEncodedInt(uint i) {
				base.Write7BitEncodedInt( (int)i );
			}

			public void Write7BitEncodedLong(ulong value) {
				ulong num;
				for (num = value; num >= 128u; num >>= 7) {
					this.Write((byte)(num | 128u));
				}
				this.Write((byte)num);
			}
		}


	}
}
