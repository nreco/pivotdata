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
using System.Linq.Expressions;
using System.Text;
using System.Reflection;

namespace NReco.PivotData {
	
	/// <summary>
	/// Provides fast access to the object's public member (property, field or indexer) by name.
	/// </summary>
	/// <remarks>
	/// ObjectMember instance uses Expression-based approach for accessing any object properties in the fastest way:
	/// <code>
	/// object arr = new [] { "A", "B", "C" };
	/// var objMember = new ObjectMember();
	/// object arrLen = objMember.GetValue(arr, "Length"); // arrLen=3
	/// </code>
	/// ObjectMember is useful for grouping typed lists with <see cref="M:NReco.PivotData.PivotData.ProcessData(System.Collections.IEnumerable,System.Func{System.Object,System.String,System.Object})"/>.
	/// </remarks>
	public class ObjectMember {

		Dictionary<MemberKey,Func<object,object>> GetCache;

		public ObjectMember() {
			GetCache = new Dictionary<MemberKey,Func<object,object>>();
		}

		Func<object, object> GetPropAccessor(Type t, string name) {
			var param = Expression.Parameter(typeof(object));
			var getterExpr = Expression.Lambda<Func<object,object>>(
					Expression.Convert(
						Expression.PropertyOrField( Expression.Convert(param,t) , name),
						typeof(object)
					),
					param
				);
			return getterExpr.Compile();
		}

		Func<object, object> GetIndexerAccessor(Type t, PropertyInfo p, string name) {
			var param = Expression.Parameter(typeof(object));
			var getterExpr = Expression.Lambda<Func<object,object>>(
					Expression.Convert(
						Expression.Property( Expression.Convert(param,t), p, Expression.Constant(name) ),
						typeof(object)
					),
					param
				);
			return getterExpr.Compile();
		}

		/// <summary>
		/// Returns the member value (property, field or indexer) of the specified object.
		/// </summary>
		/// <param name="o">The object whose member value will be returned.</param>
		/// <param name="name">property name, field name or indexer parameter</param>
		/// <returns>The member value of the specified object.</returns>
		public object GetValue(object o, string name) {
			if (o==null)
				return null;
			var t = o.GetType();
			var key = new MemberKey(t, name);

			Func<object,object> getVal;
			if (!GetCache.TryGetValue(key, out getVal)) {

				var members = t.GetMembers();
				foreach (var m in members) {
					if (m.Name == name && (m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field)) {
						getVal = GetPropAccessor(t, name);
						break;
					}
					if (m is PropertyInfo) {
						var p = (PropertyInfo)m;
						var indexParams = p.GetIndexParameters();
						if (indexParams.Length == 1 && 
								(indexParams[0].ParameterType==typeof(object) || indexParams[0].ParameterType==typeof(string)) ) {
							getVal = GetIndexerAccessor(t, p, name);
						}
					}

				}
				if (getVal == null) {
					throw new InvalidOperationException(String.Format("Member '{0}' does not exists in {1}", name, t.FullName));
				}
				GetCache[key] = getVal;
			}

			return getVal(o);
		}

		internal struct MemberKey {
			Type T;
			string Member;
			internal MemberKey(Type t, string member) {
				T = t;
				Member = member;
			}
			public override int GetHashCode() {
				return T.GetHashCode() ^ Member.GetHashCode();
			}
			public override bool Equals(object obj) {
				if (Object.ReferenceEquals(this,obj))
					return true;
				var k = (MemberKey)obj;
				return T == k.T && Member == k.Member;
			}
		}

	}
}
