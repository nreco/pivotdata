using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Xunit;
using System.Diagnostics;
using System.IO;

using System.Runtime.Serialization.Formatters.Binary;

namespace NReco.PivotData.Tests {
	
	public class PivotDataStateTests {

		[Fact]
		public void Serialization() {
			// ensure that all primitive data types are serialized correctly
			var pivotDataState = new PivotDataState(2,
				new object[]{null, "AAA", (byte)5, (int)1024, (long)7777*1024, (decimal)1.5, (float)-0.01, new DateTime(2005,1,2)},
				new uint[2][] { 
					new uint[2] { 0, 1 },
					new uint[2] { 2, 3 }
				},
				new object[2] { (uint)5, (uint)10 }
			);

			var memStream = new MemoryStream();
			pivotDataState.Serialize(memStream);

			var deserializedState = PivotDataState.Deserialize( new MemoryStream(memStream.ToArray()) );
			Assert.Null(deserializedState.KeyValues[0]);
			Assert.Equal("AAA", deserializedState.KeyValues[1]);
			Assert.Equal(5, (byte)deserializedState.KeyValues[2]);
			Assert.Equal(1024, (int)deserializedState.KeyValues[3]);
			Assert.Equal(7777*1024, (long)deserializedState.KeyValues[4]);
			Assert.Equal(1.5M, (decimal)deserializedState.KeyValues[5]);
			Assert.Equal( (float)-0.01, (float)deserializedState.KeyValues[6]);
			Assert.Equal( new DateTime(2005,1,2), (DateTime)deserializedState.KeyValues[7]);

			Assert.Equal(2, deserializedState.ValueKeys.Length);
			Assert.Equal(3, (int) deserializedState.ValueKeys[1][1]);
		}

		[Fact]
		public void Serialization_KeyEmpty() {
			var pivotDataState = new PivotDataState(2,
				new object[] { Key.Empty, "AAA" },
				new uint[1][] {
					new uint[2] { 0, 1 },
				},
				new object[] { new object[] { (uint)1, 2M } }
			);

			var memStream = new MemoryStream();
			pivotDataState.Serialize(memStream);
			var deserializedState = PivotDataState.Deserialize(new MemoryStream(memStream.ToArray()));

			var pvtData = new PivotData(new[] { "a", "b" }, new SumAggregatorFactory("c"));
			pvtData.SetState(deserializedState);
			Assert.Equal(2M, pvtData[Key.Empty, "AAA"].Value);
		}

		[Fact]
		public void PivotDataState_SerializationPerf() {

			var pvtData = new PivotData( 
				new string[] { "year", "month", "a", "i" },
				new CountAggregatorFactory(), true);
			pvtData.ProcessData( 
				PivotDataTests.SampleGenerator(1000000), // 1 mln of unique aggregates
				PivotDataTests.GetRecordValue);

			/*var js = new JavaScriptSerializer();
			js.MaxJsonLength = Int32.MaxValue;
			CheckSerializerPerf(pvtData, "JSON", 
				(state) => {
					var jsonStr = js.Serialize(state);
					Console.WriteLine("JSON length: {0}", jsonStr.Length);
					return jsonStr;
				},
				(state) => {
					return js.Deserialize<PivotDataState>( (string)state);
				});*/

			/*var binFmt = new BinaryFormatter();
			
			CheckSerializerPerf(pvtData, "BinaryFormatter", 
				(state) => {
					var memStream = new MemoryStream();
					binFmt.Serialize(memStream, state);
					Console.WriteLine("Serialized bytes: {0}",memStream.Length);
					memStream.Position = 0;
					return memStream;
				},
				(state) => {
					return (PivotDataState)binFmt.Deserialize( (Stream)state );
				});*/


			CheckSerializerPerf(pvtData, "PivotDataState", 
				(state) => {
					var memStream = new MemoryStream();
					state.Serialize(memStream);
					var bytes = memStream.ToArray();
					Console.WriteLine("Serialized bytes: {0}",bytes.Length);
					return bytes;
				},
				(state) => {
					return PivotDataState.Deserialize( new MemoryStream( (byte[])state ) );
				});
		}

		protected void CheckSerializerPerf(PivotData pvtData, string serializerName, Func<PivotDataState, object> serialize, Func<object, PivotDataState> deserialize) {
			var sw = new Stopwatch();
			sw.Start();
			var pvtState = pvtData.GetState();
			sw.Stop();
			Console.WriteLine("PivotData GetState time: {0}", sw.Elapsed);


			sw.Restart();
			var serializedState = serialize(pvtState);
			sw.Stop();
			Console.WriteLine("PivotDataState {0} serialize time: {1}", serializerName, sw.Elapsed);

			sw.Restart();
			var pvtStateFromSerialized = deserialize(serializedState);
			sw.Stop();
			Console.WriteLine("PivotDataState {0} Deserialize time: {1}", serializerName, sw.Elapsed);

			var pvtData3 = new PivotData(new string[] { "year", "month", "a", "i" }, new CountAggregatorFactory(), true);
			sw.Restart();
			pvtData3.SetState(pvtStateFromSerialized);
			sw.Stop();
			Console.WriteLine("PivotData SetState time: {0}", sw.Elapsed);

			Assert.Equal( pvtState.KeyValues.Length, pvtStateFromSerialized.KeyValues.Length );
			Assert.Equal( pvtState.ValueKeys.Length, pvtStateFromSerialized.ValueKeys.Length );
			Assert.Equal( pvtState.Values.Length, pvtStateFromSerialized.Values.Length );
		}


	}
}
