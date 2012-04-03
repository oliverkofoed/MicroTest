MicroTest is a very small unit testing framework for .NET.

It's small size and easy extensibility makes it easy to integrate and customize for your project. 

Features
========
* Easily define tests across your entire codebase
* Supports testing asynchronous code
* Multi-threaded and really fast: runs multiple tests at the same time.
* Dependencies between tests
* Grouping of tests
* Tests can be embedded close to code being tested as private static methods.
* Reports exact location of of failing unit test
* Measures how long it took to complete each test
* Console test runner can be embedded in project or in seperate .exe file

![](https://github.com/oliverkofoed/MicroTest/raw/master/ReadMe.ConsoleScreenshot.png)

Writing Tests
=============
To write a test anywhere in your codebase, simply create methods with a single parameter of type MicroTest.Test that are either static or inside a class with a parameterless constructor.

For example, this would be a simple way to test a simple Math class:

	using MicroTest;
	
	namespace MyApp{
		public class Math{
			public static int Add(int x, int y)(return x+y;)
			public static int Subtract(int x, int y)(return x-y;)
		
			private static void addTest(Test test){
				test.Equal(4, Add(2,2));
			}

			private static void subtractTest(Test test){
				test.Equal(2, Subtract(4,2));
			}
		}
	}

The MicroTest.Test instance passed to each test has methods for asserting conditions and logging messages specific to the given test.

These are the most commonly used methods:

	// basic comparisons
	test.NotNull(new object());
	test.Null(null);
	test.True(true);
	test.Equal(1, 1);
	test.NotEqual(1, 2);
	
	// exceptions
	test.Throws<ApplicationException>(delegate { throw new ApplicationException("hi"); });
	test.DoesNotThrow(delegate {
		test.Equal(1, 1);
	}, typeof(Exception));
	
	// logging messages
	test.Debug("Debugging information")
	test.Info("Informational message")
	test.Warn("Not an error, but not a good sign either")

You can use the [Test] attribute to add a description to each test, configure dependencies, or set specific timeout:

	using MicroTest;
	
	namespace MyApp{
		public class Math{
			public static int Add(int x, int y)(return x+y;)
			public static int Subtract(int x, int y)(return x-y;)
		
			[Test("Quick tests of the Math.Add(..) method")]
			private static void addTest(Test test){
				test.Equal(4, Add(2,2));
			}

			[Test("Quick tests of the Math.Subtract(..) method")]
			private static void subtractTest(Test test){
				test.Equal(2, Subtract(4,2));
			}
					
			[Test(TimeoutSeconds=4)]
			private static void neverEndingTest(Test test){
				while(true){
					;
				}
			}
			
			[Test(Dependencies="MyApp.Math.addTest,MyApp.Math.subtractTest")] 
			private static void dependentTest(Test test){
				
			}
		}
	}

MicroTest also features the ability to test asynchronous code by marking a 
test with the [TestAsync] attribute. 

When a test is marked as asynchronous, you have to explicitly call test.Success() or test.Failure(..) to complete the test:
	
	namespace MyApp{
		public class Math{
			public static int Add(int x, int y)(return x+y;)
			
			[TestAsync] 
			private static void asyncTest(Test test){
				ThreadPool.QueueUserWorkItem(delegate{
					Thread.Sleep(1000);
					test.Equal(4,Add(2,2));
					test.Success();
				})
			}
		}
	}
	
Running tests
=============
Tests are run by gathering them into a TestSuite, adding an visualiser, and then calling Run() on the test suite. Luckily, this is very easy to do:

	using MicroTest;
	
	namespace MyApp{
		public class TestRunnerProgram{
			public static void Main(string[] args){
				var suite = new TestSuite();// create new empty test suite
				suite.RegisterAll();		// add all tests created in loaded app domains
				suite.RunConsole();			// Add a console visualiser and call run
			}
		}
	}
	
For the times when you don't want to run all tests, just only register the specific tests you want to run:

	suite.RegisterAll(typeof(Math)); // register the tests in the Math class only.
	
If you're building a console app anyway, you can embed the main method directly into your program. Otherwise, you'll have to create a seperate console application to run your tests. You could also have the test program AND the tests defined in a seperate console application: the choice is yours.

License
=======
Copyright (c) 2012 Oliver Kofoed

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.