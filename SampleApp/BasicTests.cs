using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MicroTest;
using System.Threading;

namespace SampleApp {
	public partial class BasicTests{ 
		public static void Asserts(Test test){
			var instance = new object();
			test.NotNull(new object());
			test.Null(null);
			test.True(true);
			test.False(false);
			test.Equal(1, 1);
			test.Equal("hello world", "hello world");
			test.Equal(true, true);
			test.NotEqual(true, false);
			test.NotEqual(1, 2);
			test.NotEqual("hello", "world");
			test.HasSubstring("ello", "hello");
			test.HasSubstring("eLLo", "HELLo", StringComparison.InvariantCultureIgnoreCase);
			test.Same(instance, instance);
			test.NotSame(instance, new object());
			test.Empty(new string[0]);
			test.Empty(new List<string>());
			test.NotEmpty(new string[]{"hello"});
			test.NotEmpty(new List<string> { "hello" });
			test.Throws(delegate { throw new ApplicationException("hi"); });
			test.Throws(delegate { throw new ApplicationException("hi"); }, (ex) => test.Equal("hi", ex.Message));
			test.Throws<ApplicationException>(delegate { throw new ApplicationException("hi"); }, (ex) => test.Equal("hi", ex.Message));
			test.Throws<ApplicationException>(delegate { throw new ApplicationException("hi"); });
			test.Throws(delegate { throw new ApplicationException("hi"); }, typeof(ApplicationException));
			test.DoesNotThrow(delegate {
				test.Equal(1, 1);
			}, typeof(Exception));
			test.DoesNotThrow(delegate {
				throw new Exception();
			}, typeof(ApplicationException));
		}


		public static void PublicStatic(Test test) { test.True(true);  }
		private static void privateStatic(Test test) { test.True(true);  }
		public void PublicInstance(Test test) { test.True(true);  }
		private void privateInstance(Test test) { test.True(true);  }

		public static void Slow40ms(Test test){ Thread.Sleep(40); }
		public static void Slow200ms(Test test){ Thread.Sleep(200); }
		public static void Slow400ms(Test test){ Thread.Sleep(400); }
		public static void Slow600ms(Test test){ Thread.Sleep(600); }
		public static void Slow800ms(Test test){ Thread.Sleep(800); }
		public static void Slow1000ms(Test test){ Thread.Sleep(1000); }
		public static void Slow1500ms(Test test){ Thread.Sleep(1500); }
		public static void Slow15000ms(Test test){ Thread.Sleep(15000); }

		[Test(Dependencies = "SampleApp.BasicTests.Assert,SampleApp.BasicTests.Slow1500ms,SampleApp.BasicTests.Slow15000ms")]
		public static void WithDependencies(Test test) {
			test.True(true);
		}

		[TestAsync]
		public void Async(Test test) {
			ThreadPool.QueueUserWorkItem(delegate {
				Thread.Sleep(200);
				test.Success();
			});
		}

		public void DoSomeLogging(Test test) {
			test.Info("I'm starting now");
			Thread.Sleep(300);
			test.Debug("Doing something else");
			Thread.Sleep(300);
			test.Info("moving forward");
			Thread.Sleep(300);
			test.Warn("Warning you!!");
			Thread.Sleep(300);
		}
	}
}
