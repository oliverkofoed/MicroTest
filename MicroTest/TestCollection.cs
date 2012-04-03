using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;

namespace MicroTest {
	public class TestCollection {
		private Dictionary<string, Dictionary<string, RegisteredTest>> tests = new Dictionary<string, Dictionary<string, RegisteredTest>>();

		internal enum TestResult { 
			Unknown, 
			Success, 
			Failure 
		}

		internal class RegisteredTest {
			public string Group;
			public string Name;
			public string Caption;
			public TestResult Result = TestResult.Unknown;
			public Action Method;
			public string Status = null;
			public bool Started = false;
			public string[] Dependencies;
			public Exception Exception;
			public bool Debug;
		}

		private void reprintStatus() {
			lock (this) {
				Console.Clear();
				foreach (var kv in tests) {
					var allCompleted = Array.TrueForAll(kv.Value.Values.ToArray(), t => t.Result == TestResult.Success || t.Result == TestResult.Failure);
					var allSucceeded = Array.TrueForAll(kv.Value.Values.ToArray(), t => t.Result == TestResult.Success);

					setColor(allCompleted ? (allSucceeded ? ConsoleColor.Green : ConsoleColor.Red) : ConsoleColor.Yellow);
					Console.WriteLine(" [" + (allCompleted ? "x" : " ") + "] " + kv.Key);
					Console.WriteLine(" =============================== ");

					foreach (var test in kv.Value.Values) {
						setColor(test.Result == TestResult.Unknown ? ConsoleColor.Yellow : (test.Result == TestResult.Success ? ConsoleColor.Green : ConsoleColor.Red));
						Console.WriteLine("   [" + (test.Result == TestResult.Unknown ? (test.Started ? "." : " ") : "x") + "] " + test.Caption);
						if (test.Status != null) {
							Console.WriteLine("       Status: " + test.Status);
							Console.WriteLine();
						}
						if (test.Exception != null) {
							Console.WriteLine("       " + test.Exception.Message);
							Console.WriteLine(string.Join("\n", Array.FindAll( test.Exception.StackTrace.Replace("  ", "       ").Split('\n'), (l) => !l.Contains("Testing.Assert")  ) ) );
							Console.WriteLine();
						}
					}
					Console.WriteLine(" ");
				}
			}
		}

		public void Run() {
			while (true) {
				// check which tests can run
				foreach (var kv in tests) {
					foreach (var test in kv.Value.Values) {
						// start test if all it's dependences succeeded
						if (!test.Started) {
							// test all dependencies
							var dependencySucceded = true;
							foreach (var dep in test.Dependencies) {
								var parts = dep.Split('.');
								var g = tests[parts[0]];
								if (parts.Length == 2 && parts[1] != "") {
									RegisteredTest dTest;
									if (g.TryGetValue(parts[1], out dTest)) {
										dependencySucceded = dTest.Result == TestResult.Success;
									} else {
										throw new ApplicationException("Unknown dependency: " + dep);
									}
								} else {
									dependencySucceded = Array.TrueForAll(g.Values.ToArray(), t => t.Result == TestResult.Success);
								}

								if (!dependencySucceded) {
									break;
								}
							}

							// start it 
							if (dependencySucceded) {
								if (!test.Started) {
									test.Started = true;
									ThreadPool.QueueUserWorkItem(delegate(object state) {
										var t = (RegisteredTest)state;
										try {
											Test.current = t;
											Test.statusChangeNotification = () => reprintStatus();
											reprintStatus();
											t.Method();
											t.Result = TestResult.Success;
										} catch (Exception e) {
											t.Result = TestResult.Failure;
											t.Exception = e;
											if (t.Debug) {
												throw;
											}
										} finally {
											reprintStatus();
											Test.current = null;
										}
									}, test);
								}
							}
						}
					}
				}

				System.Threading.Thread.Sleep(5);
			}
		}

		private void setColor(ConsoleColor foreground) {
			Console.ForegroundColor = foreground;
		}

		public void DebugRegister(string group, string name, string caption, Action testMethod, params string[] dependencies) {
			register(group, name, caption, testMethod, true, dependencies);
		}
		public void Register(string group, string name, string caption, Action testMethod, params string[] dependencies) {
			register(group, name, caption, testMethod, false, dependencies);
		}

		private void register(string group, string name, string caption, Action testMethod, bool debug, params string[] dependencies) {
			Dictionary<string, RegisteredTest> groupDict = null;
			if( !tests.TryGetValue(group, out groupDict) ){
				tests[group] = groupDict = new Dictionary<string,RegisteredTest>();
			}		
			if (!groupDict.ContainsKey(name)) {
				groupDict[name] = new RegisteredTest {
					Group = group,
					Name = name,
					Caption = caption,
					Method = testMethod,
					Dependencies = dependencies,
					Debug = debug
				};
			} else {
				throw new ApplicationException("That test id (group,name) is already regged!");
			}
		}

		public static TestCollection FindAll() {
			var result = new TestCollection();
			var assembly = Assembly.GetCallingAssembly();
			foreach(var type in assembly.GetTypes()) {
				if(type.IsClass && type.IsSubclassOf(typeof(Test))) {
					foreach(var method in type.GetMethods()) {
						foreach(var attribute in method.GetCustomAttributes(true)) {
							if(attribute is Test) {
								if(method.GetParameters().Length != 0) {
									throw new ApplicationException("Method " + type.FullName + "." + method.Name + "(..) takes arguments. Methods marked with the [Test] attribute must not take any arguments.");
								}
								result.Register(type.Name, method.Name, (attribute as Test).Caption, getExecute(type,method));
							}
						}
					}
				}
			}
			return result;
		}

		private static Action getExecute(Type type, MethodInfo method){
			return delegate {
				var instance = (Test)Activator.CreateInstance(type);
				instance.Setup();
				method.Invoke(instance, new object[0]);
			};
		}
	}
}