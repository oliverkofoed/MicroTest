using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace MicroTest{
	public abstract partial class Test {
		[DebuggerNonUserCode] public void NotNull(object value) { if(value == null) Failure("NotNull", "Value was null"); }
		[DebuggerNonUserCode] public void Null(object value) { if(value != null) Failure("Null", "Value was not null"); }
		[DebuggerNonUserCode] public void True(bool value) { if(value != true) Failure("True", "Value was not true"); }
		[DebuggerNonUserCode] public void False(bool value) { if(value != false) Failure("False", "Value was not false"); }
		[DebuggerNonUserCode] public void Equal<T>(T actual, T comparison) { if(!Compare(actual, comparison)) Failure("Equal", "The value " + ToString(actual) + " does not equal " + ToString(comparison)); }
		[DebuggerNonUserCode] public void Equal<T>(T actual, T comparison, IComparer<T> comparer) { if(comparer.Compare(comparison, actual) != 0) Failure("Equal", "The value " + ToString(actual) + " does not equal " + ToString(comparison)); }
		[DebuggerNonUserCode] public void NotEqual<T>(T actual, T comparison) { if(Compare(actual, comparison)) Failure("NotEqual", "The two values given should not be the same, they were. Values: " + ToString(actual)); }
		[DebuggerNonUserCode] public void NotEqual<T>(T actual, T comparison, IComparer<T> comparer) { if(comparer.Compare(comparison, actual) == 0) Failure("NotEqual", "The two values given should not be the same, they were. Values: " + ToString(actual)); }

		[DebuggerNonUserCode]
		public void HasSubstring(string substring, string container) {
			HasSubstring(substring, container, StringComparison.CurrentCulture);
		}

		[DebuggerNonUserCode]
		public void HasSubstring(string substring, string container, StringComparison comparisonType) {
			if(container == null) {
				Failure("HasSubstring", "The given container string was null.");
			} else if(substring == null) {
				Failure("HasSubstring", "The given substring was null.");
			}else if(container.IndexOf(substring, comparisonType) < 0){
				Failure("HasSubstring", "The string " + ToString(container) + " does not contain the substring " + ToString(substring));
			}
		}

		[DebuggerNonUserCode]
		public void StartsWith(string value, string startsWith) {
			if(!value.StartsWith(startsWith)) {
				Failure("StatsWith", "Value did not start with: " + ToString(startsWith) + ". Value started with: " + ToString(value.Substring(0, Math.Min(50, value.Length))));
			}
		}

		[DebuggerNonUserCode]
		public void Same(object actual, object comparison) {
			if(!object.ReferenceEquals(actual, comparison)) Failure("Same", "The two objects did not reference the same instance");
		}

		[DebuggerNonUserCode]
		public void NotSame(object actual, object comparison) {
			if(object.ReferenceEquals(actual, comparison)) Failure("NotSame", "The two objects were not supposed to be the exact same instance");
		}

		[DebuggerNonUserCode]
		public void Empty(IEnumerable collection) {
			if(collection != null) {
				foreach(object obj in collection) { Failure("Empty", "The collection was not empty"); }
			} else {
				Failure("Empty", "collection was null");
			} 
		}

		[DebuggerNonUserCode]
		public void NotEmpty(IEnumerable collection) {
			if(collection != null) {
				foreach(object obj in collection) { return; }
				Failure("NotEmpty", "The collection was empty");
			} else {
				Failure("NotEmpty", "collection was null");
			}
		}

		public Exception Throws(Action testCode) {
			return Throws<Exception>(testCode);
		}

		public void Throws<T>(Action testCode, Action<T> processException) where T : Exception {
			processException(Throws<T>(testCode));
		}
	
		public void Throws(Action testCode, Action<Exception> processException) {
			processException(Throws<Exception>(testCode));
		}

		public T Throws<T>(Action testCode) where T:Exception {
			return (T)Throws(testCode, typeof(T));
		}

		public Exception Throws(Action testCode, Type exceptionType) {
			try {
				testCode();
			} catch(Exception e) {
				if(!exceptionType.IsAssignableFrom(e.GetType())) {
					Failure("Throws", "Expected exception type: "+ exceptionType.FullName+ ", but got: "+ e.GetType().FullName);
				}
				return e;
			}
			Failure("Throws", "Code didn't throw any exception");
			return null;
		}

		public Exception DoesNotThrow(Action testCode){
			return DoesNotThrow(testCode, typeof(Exception));
		}

		public Exception DoesNotThrow<T>(Action testCode) where T : Exception {
			return DoesNotThrow(testCode, typeof(T));
		}

		public Exception DoesNotThrow(Action testCode, Type exceptionType) {
			try {
				testCode();
			} catch(Exception e) {
				if(exceptionType.IsAssignableFrom(e.GetType())) {
					Failure("DoesNotThrow", "Code threw exception of unwanted type: "+ e.GetType().FullName+ ", Message=" + e.Message);
				}
				return e;
			}
			return null;
		}

		public static string ToString(object value) {
			if(value == null) {
				return "[null]";
			}else if(value is Exception) {
				return value.GetType().FullName;
			}else if(value is string) {
				return "\"" + value.ToString().Replace("\n", "\\n") + "\"";
			} else {
				return value.ToString().Replace("\n", "\\n");
			}
		}

		public static bool Compare<T>(T x, T y) {
			return new Comparer<T>().Compare(x, y) == 0;
		}

		private class Comparer<T> : IComparer<T> {
			public int Compare(T a, T b) {
				var type = typeof(T);
				var nullable = !type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(Nullable<>)));
				var aIsNull = nullable && object.Equals(a, default(T));
				var bIsNull = nullable && object.Equals(b, default(T));
				var comparableGenericA = a as IComparable<T>;
				var comparableA = a as IComparable;
				var equatableGeneric = a as IEquatable<T>;
				var enumerableA = a as IEnumerable;
				var enumerableB = b as IEnumerable;

				if( aIsNull || bIsNull  ){
					// if one of them is null, they better both be null.
					return aIsNull == bIsNull ? 0 : -1;
				} else if(a.GetType() != b.GetType()) {
					// the objects should be of the same type and 
					return -1;
				} else if(comparableGenericA != null) {
					// using IComparable<T>
					return comparableGenericA.CompareTo(b);
				} if(comparableA != null) {
					// using IComparable
					return comparableA.CompareTo(b);
				}else if(equatableGeneric != null) {
					// using IEquatable<T>
					return equatableGeneric.Equals(b) ? 0 : -1;
				} else if(enumerableA != null && enumerableB != null) {
					// comparing entries 
					var enumeratorA = enumerableA.GetEnumerator();
					var enumeratorB = enumerableB.GetEnumerator();
					while(true) {
						var nextA = enumeratorA.MoveNext();
						var nextB = enumeratorB.MoveNext();

						if(!nextA || !nextB) {
							return (nextA == nextB ? 0 : -1);
						} else if(!object.Equals(enumeratorA.Current, enumeratorB.Current)) {
							return -1;
						}
					}
				} else {
					return object.Equals(a, b) ? 0 : -1;
				}
			}
		}
	}
}