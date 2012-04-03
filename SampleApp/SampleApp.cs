using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MicroTest;
using System.Threading;

namespace SampleApp {
	public class SampleApp {
		public static void Main() {
			var suite = new TestSuite();// create new empty test suite
			suite.RegisterAll();		// add all tests created in loaded app domains
			suite.RunConsole();			// run in console
		}
	}
}
