using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using NReco.PivotData;

namespace NReco.PivotData.Tests
{
    public class ObjectMemberTests
    {

		[Fact]
		public void GetProperty() {
			var objMember = new ObjectMember();
			var testObj = new TestObj();

			Assert.Equal(1, objMember.GetValue(testObj, "IntProp" ) );
			Assert.Equal(1, objMember.GetValue(testObj, "IntProp" ) ); // lets check cached accessor

			Assert.Equal("AAA", objMember.GetValue(testObj, "StrProp" ) );
			Assert.Equal("AAA", objMember.GetValue(testObj, "StrProp" ) ); // lets check cached accessor

			Assert.Throws<InvalidOperationException>( () => { objMember.GetValue(testObj, "ZZZ" ); } );
		}

		[Fact]
		public void GetField() {
			var objMember = new ObjectMember();
			var testObj = new TestObj();

			Assert.Equal(2, objMember.GetValue(testObj, "IntFld" ) );
			Assert.Equal(2, objMember.GetValue(testObj, "IntFld" ) ); // lets check cached accessor

			Assert.Equal("BBB", objMember.GetValue(testObj, "StrFld" ) );
			Assert.Equal("BBB", objMember.GetValue(testObj, "StrFld" ) ); // lets check cached accessor

			Assert.Equal(DBNull.Value, objMember.GetValue(testObj, "ObjFld" ) );

		}

		[Fact]
		public void GetIndexer() {
			var objMember = new ObjectMember();
			var testObj = new TestObj();

			Assert.Throws<InvalidOperationException>( () => { objMember.GetValue(testObj, "1" ); } );

			var testObjWithObjIdx = new TestObjWithObjIndexer();
			Assert.Equal("ColName", objMember.GetValue(testObjWithObjIdx, "ColName" ) );

			var testObjWithStrIdx = new TestObjWithStrIndexer();
			Assert.Equal(7, objMember.GetValue(testObjWithStrIdx, "ColName" ) );
		}


		public class TestObj {

			public int IntProp { get { return 1; } }

			public string StrProp { get { return "AAA"; } }


			public int IntFld = 2;
			public string StrFld = "BBB";

			public object ObjFld = DBNull.Value;

			public object this[int index] {
				get { return index; }
			}

		}

		public class TestObjWithObjIndexer {

			public object this[object key] {
				get { return key; }
			}			

		}

		public class TestObjWithStrIndexer {

			public int this[string key] {
				get { return key.Length; }
			}			

		}


    }
}
