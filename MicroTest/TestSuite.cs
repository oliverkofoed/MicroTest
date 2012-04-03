using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroTest {
	public class TestSuite :IEnumerable<Test>{
		private SortedDictionary<string, Test> tests = new SortedDictionary<string, Test>();
		public event Action<Test> TestUpdated;

		public void Run(){
			startEligable();
		}

		public void RunConsole() {
			var observer = new ConsoleVisualizer(this);
			Run();
			Console.ReadLine();
		}

		private void startEligable() {
			foreach(var test in tests.Values) {
				if( test.Status < TestStatus.Running ){
					if(Array.TrueForAll(test.Dependencies, (dep) => GetDependencyStatus(dep) == TestStatus.FinishedSuccessfully )) {
						startTest(test);
					} else {
						test.Status = TestStatus.WaitingForDependencies;
						TestUpdated(test);
					}
				}
			}
		}

		private void startTest(Test test) {
			test.Run(delegate {
				if(test.Status >= TestStatus.FinishedError) {
					startEligable();
				}
				TestUpdated(test);
			});
		}

		public TestStatus GetDependencyStatus(string dependency) {
			var status = TestStatus.FinishedSuccessfully;
			foreach(var v in tests.Values) {
				if(v.FullId.StartsWith(dependency)) {
					switch(v.Status){
						case TestStatus.FinishedSuccessfully: break;
						case TestStatus.FinishedError: status = TestStatus.FinishedError;break;
						default:
							if( status != TestStatus.FinishedError ){
								status = TestStatus.WaitingForDependencies;
							}
							break;
					}
				}
			}
			return status;
		}

		public TestSuite Register(Test test) {
			if(!tests.ContainsKey(test.FullId)) {
				tests[test.FullId] = test;
			}
			return this;
		}

		public TestSuite RegisterAll(Type type){
			foreach(var test in TestAttribute.FindAll(type)) {
				Register(test);
			}
			return this;
		}

		public TestSuite RegisterAll(){
			foreach(var test in TestAttribute.FindAll()) {
				Register(test);
			}
			return this;
		}

		public IEnumerator<Test> GetEnumerator() {
			return tests.Values.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return tests.Values.GetEnumerator();
		}
	}
}