using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MicroTest;
using System.Threading;

namespace SampleApp {
	public class FailingTests{
		[Test("This method is supposed to fail")]
		public static void FailedNotNull(Test test) { test.NotNull(null); }

		[Test("This method is supposed to fail")]
		public static void FailedNull(Test test) { test.Null(new object()); }

		[Test("This method is supposed to fail")]
		public static void FailedTrue(Test test) { test.Null(new object()); }

		[Test("This method is supposed to fail")]
		public static void FailedFalse(Test test) { test.False(true); }
		
		[Test("This method is supposed to fail")]
		public static void FailedEqual(Test test) { test.Equal(1,2); }

		[Test("This method is supposed to fail")]
		public static void FailedNotEqual(Test test) { test.NotEqual(true,true); }

		[Test("This method is supposed to fail")]
		public static void FailedSubstring(Test test) { test.HasSubstring("hello","world"); }

		[Test("This method is supposed to fail")]
		public static void FailedSame(Test test) { test.Same(new object(), new object()); }

		[Test("This method is supposed to fail")]
		public static void FailedNotSame(Test test) { var instance = new object(); test.NotSame(instance, instance); }

		[Test("This method is supposed to fail")]
		public static void FailedEmpty(Test test) { test.Empty(new string[]{"hello"}); }

		[Test("This method is supposed to fail")]
		public static void FailedNotEmpty(Test test) { test.NotEmpty(new string[]{}); }

		[Test("This method is supposed to fail")]
		public static void FailedThrows(Test test) { test.Throws(delegate { ; }); }

		[Test("This method is supposed to fail")]
		public static void FailedDoesNotThrow(Test test) { test.DoesNotThrow(delegate { throw new ApplicationException("..."); }); }

		[TestAsync("This method is supposed to fail", TimeoutSeconds=1 )]
		public void Async(Test test) {
			// never calling test.Success() or test.Failure() in an async test will cause the timeout to happen
		}

		[TestAsync("This method is supposed to fail", TimeoutSeconds=1 )]
		public void AsyncLongRunning(Test test) {
			Thread.Sleep(4000); // this test should print out the stacktrace where the test thread is at this time.
		}
	}
}
