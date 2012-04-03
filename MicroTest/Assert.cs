using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Diagnostics;

namespace MicroTest{
	public partial class Assert {
		[DebuggerNonUserCode] public static void NotNull(object value) { if(value == null) throw new Fail("NotNull", "Value was null"); }
		[DebuggerNonUserCode] public static void Null(object value) { if(value != null) throw new Fail("Null", "Value was not null"); }
		[DebuggerNonUserCode] public static void True(bool value) { if(value != true) throw new Fail("True", "Value was not true"); }
		[DebuggerNonUserCode] public static void False(bool value) { if(value != false) throw new Fail("False", "Value was not false"); }

		[DebuggerNonUserCode]
		public static void Error(string message) {
			throw new Fail("Error", message);
		}

		[DebuggerNonUserCode]
		public static void Equal<T>(T actual, T expected) {
			Equal(actual, expected, comparer<T>());
		}

		[DebuggerNonUserCode]
		public static void Equal<T>(T actual, T expected, IComparer<T> comparer) {
			if(comparer.Compare(expected, actual) != 0) throw new Fail("Equal", "Expected: ", expected, ", Got:", actual );
		}

		[DebuggerNonUserCode]
		public static void NotEqual<T>(T actual, T expected) {
			NotEqual(expected, actual, comparer<T>());
		}

		[DebuggerNonUserCode]
		public static void NotEqual<T>(T actual, T expected, IComparer<T> comparer) {
			if(comparer.Compare(expected, actual) == 0) throw new Fail("NotEqual", "The value: ", expected, " was supposed to be different then:", actual);
		}

		[DebuggerNonUserCode]
		public static void Contains(string substring, string container) {
			Contains(substring, container, StringComparison.CurrentCulture);
		}

		[DebuggerNonUserCode]
		public static void Contains(string substring, string container, StringComparison comparisonType) {
			if(container != null && substring != null && container.IndexOf(substring, comparisonType) < 0)
				throw new Fail("Contains", "The string ", container, " does not contain the expected substring ", substring);
		}

		[DebuggerNonUserCode]
		public static void Same(object expected, object actual) {
			if(!object.ReferenceEquals(expected, actual)) throw new Fail("Same", "The two objects did not reference the same instance");
		}

		[DebuggerNonUserCode]
		public static void NotSame(object expected, object actual) {
			if(object.ReferenceEquals(expected, actual)) throw new Fail("NotSame", "The two objects were not supposed to be the exact same instance");
		}

		[DebuggerNonUserCode]
		public static void NotEmpty(IEnumerable collection) {
			if(collection == null) throw new Fail("NotEmpty", "collection was null");
			foreach(object obj in collection) { return; }
			throw new Fail("NotEmpty", "The collection was empty");
		}

		[DebuggerNonUserCode]
		public static void Empty(IEnumerable collection) {
			if(collection == null) throw new Fail("Empty", "collection was null");
			foreach(object obj in collection) { throw new Fail("Empty", "The collection was not empty"); ; }
		}

		[DebuggerNonUserCode]
		public static void Succeds(string testString, Action code) {
			try {
				code();
			} catch(Fail e) {
				e.TestString = testString;
				throw;
			}
		}

		public static void Throws<T>(Action testCode, Action<T> processException) where T : Exception {
			T ex = Throws<T>(testCode);
			processException(ex);
		}
	
		public static T Throws<T>(Action testCode) where T:Exception {
			return (T)Throws(typeof(T), testCode);
		}

		public static Exception Throws(Type exceptionType, Action testCode) {
			try {
				testCode();
			} catch(Exception e) {
				if(! exceptionType.IsAssignableFrom(e.GetType()) )
					throw new Fail("Throws", "Expected exception type: ", exceptionType.FullName, ", but got: ", e.GetType().FullName);
				return e;
			}
			throw new Fail("Throws", "Code didn't throw any exception");
		}

		public static Exception DoesNotThrow<T>(Action testCode) where T : Exception {
			return DoesNotThrow(typeof(T), testCode);
		}

		public static Exception DoesNotThrow(Type exceptionType, Action testCode) {
			try {
				testCode();
			} catch(Exception e) {
				if(exceptionType.IsAssignableFrom(e.GetType()))
					throw new Fail("DoesNotThrow", "Code threw exception of unwanted type: ", e.GetType().FullName, ", Message=" + e.Message);
				return e;
			}
			return null;
		}

		public class Fail : ApplicationException {
			public string TestString { get; set; }
			public string AssertMethod { get; private set; }
			public Fail(string assertMethod, params object[] arguments) : base( String.Concat(Array.ConvertAll(arguments, (obj) => toString(obj)))) {
				this.AssertMethod = assertMethod;
			}
			private static string toString(object value) {
				if(value == null) {
					return "[null]";
				}else if(value is Exception) {
					return value.GetType().FullName;
				} else {
					return value.ToString().Replace("\n", "\\n");
				}
			}
		}

		private static IComparer<T> comparer<T>() { return new AssertComparer<T>(); }

		// Class copied from XUnit.net
		class AssertComparer<T> : IComparer<T> {
			public int Compare(T x,
							   T y) {
				Type type = typeof(T);

				// Null?
				if(!type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(Nullable<>)))) {
					if(Equals(x, default(T))) {
						if(Equals(y, default(T)))
							return 0;
						return -1;
					}

					if(Equals(y, default(T)))
						return -1;
				}

				// Same type?
				if(x.GetType() != y.GetType())
					return -1;

				// Implements IComparable<T>?
				IComparable<T> comparable1 = x as IComparable<T>;

				if(comparable1 != null)
					return comparable1.CompareTo(y);

				// Implements IComparable?
				IComparable comparable2 = x as IComparable;

				if(comparable2 != null)
					return comparable2.CompareTo(y);

				// Implements IEquatable<T>?
				IEquatable<T> equatable = x as IEquatable<T>;

				if(equatable != null)
					return equatable.Equals(y) ? 0 : -1;

				// Enumerable?
				IEnumerable enumerableX = x as IEnumerable;
				IEnumerable enumerableY = y as IEnumerable;

				if(enumerableX != null && enumerableY != null) {
					IEnumerator enumeratorX = enumerableX.GetEnumerator();
					IEnumerator enumeratorY = enumerableY.GetEnumerator();

					while(true) {
						bool hasNextX = enumeratorX.MoveNext();
						bool hasNextY = enumeratorY.MoveNext();
						
						if(!hasNextX || !hasNextY)
							return (hasNextX == hasNextY ? 0 : -1);

						if(!Equals(enumeratorX.Current, enumeratorY.Current))
							return -1;
					}
				}

				// Last case, rely on Object.Equals
				return Equals(x, y) ? 0 : -1;
			}
		}

		internal static void StartsWith(string value, string startsWith) {
			if(!value.StartsWith(startsWith)) {
				throw new Fail("StatsWith", "Value did not start with: " + startsWith + ". Value started with: " + value.Substring(0, Math.Min(50,value.Length)));
			}
		}
	}
}