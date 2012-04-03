using System;
using System.Diagnostics;
using System.Threading;

namespace MicroTest {
	public enum TestStatus : byte{
		Idle = 0,
		WaitingForDependencies =1,
		Running = 2,
		FinishedError = 3,
		FinishedSuccessfully = 4
	}

	public abstract partial class Test {
		private Action updatedCallback;
		private Timer timeoutTimer;
		private Thread executionThread;
		private Stopwatch stopwatch;
		public string Namespace{get;private set;}
		public string Id{get;private set;}
		public string FullId{get{return Namespace + "." + Id;}}
		public string Description { get; private set; }
		public string[] FailureStacktrace { get; private set; }
		public string FailureType { get; private set; }
		public string FailureMessage { get; private set; }
		public TestStatus Status { get; internal set; }
		public string[] Dependencies { get; private set; }
		public TimeSpan Timeout { get; private set; }
		public bool Async { get; private set; }
		public TimeSpan CompletionTime { get; private set; }

		public Test(string @namespace, string id, string description, string[] dependencies, TimeSpan? timeout, bool async) {
			this.Namespace = @namespace;
			this.Id = id;
			this.Description = description;
			this.Status = TestStatus.Idle;
			this.Dependencies = dependencies ?? new string[0];
			this.Timeout = timeout ?? TimeSpan.FromSeconds(30);
			this.CompletionTime = TimeSpan.MinValue;
			this.Async = async;
		}

		internal void Run(Action updated) {
			this.updatedCallback = updated;
			this.Status = TestStatus.Running;

			executionThread = new Thread((ThreadStart)delegate{
				stopwatch = Stopwatch.StartNew();

				updatedCallback();

				// setup a timer to timeout if the method never completes
				timeoutTimer = new Timer(delegate{
					if(Status == TestStatus.Running) {
						failure(null, "Timeout: The method executed longer than given timeout",false);
					}
				}, null, (int)Timeout.TotalMilliseconds, System.Threading.Timeout.Infinite);

				try{
					Execute();
					executionThread = null;
					if(!Async && Status == TestStatus.Running) {
						Success();
					}
				}catch(ThreadAbortException){
					Thread.ResetAbort();
				}catch(Exception ex){
					Failure("UnhandledException", ex.Message);
				}
			});
			executionThread.Start();
		}

		protected abstract void Execute();

		protected virtual string[] FilterFailureStacktrace(string[] stacktrace) { 
			return stacktrace; 
		}

		public void Success() {
			if(Status == TestStatus.Running) {
				stopwatch.Stop();
				CompletionTime = stopwatch.Elapsed;
				Status = TestStatus.FinishedSuccessfully;
				updatedCallback();
			}
		}

		public void Failure(string message) {
			failure(null, message, true);
		}

		public void Failure(string type, string message){
			failure(type, message, true);
		}

		private void failure(string type, string message, bool throwException){
			if( Status != TestStatus.FinishedError ){
				stopwatch.Stop();
				CompletionTime = stopwatch.Elapsed;
				if(executionThread != null) {
					if(Thread.CurrentThread != executionThread) { executionThread.Suspend(); }
					FailureStacktrace = new StackTrace(executionThread, true).ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
					if(Thread.CurrentThread != executionThread) { executionThread.Resume(); }
				} else {
					// no clue which thread is running the test
					FailureStacktrace = new string[0]; 
				}
				FailureStacktrace = Array.FindAll(FailureStacktrace, line => !hideStackFrame(line));
				FailureStacktrace = FilterFailureStacktrace(FailureStacktrace);
				FailureType = string.IsNullOrWhiteSpace( type ) ? null : "Test." + type + "()";
				FailureMessage = message;
				Status = TestStatus.FinishedError;
				updatedCallback();
				if( throwException ){
					throw new TestFailedException(message);
				}
			}
		}

		private bool hideStackFrame(string line) {
			return line.ToLower().IndexOf("system.environment.get") > -1 || line.IndexOf(this.GetType().Namespace+".Test") > -1;
		}

		public class TestFailedException : Exception {
			public TestFailedException(string message) : base(message) { }
		}
	}
}