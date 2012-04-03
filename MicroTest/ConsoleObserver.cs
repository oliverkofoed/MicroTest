using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace MicroTest {
	public class ConsoleVisualizer {
		private TestSuite suite;
		private Timer timer;
		private int printing = 0;

		public ConsoleVisualizer(TestSuite suite) {
			this.suite = suite;
			this.suite.TestUpdated += (t) => refresh();
		}

		private void refresh() {
			if(Interlocked.Exchange(ref printing, 1) == 0) {
				rewriteConsole();
				printing = 0;
			} else {
				if(timer == null) {
					timer = new Timer(delegate{
						timer = null;
						refresh();
					}, null, 500, Timeout.Infinite);
				}
			}
		}

		private void rewriteConsole() {
			Console.Clear();
			var first = true;
			foreach(var kv in suite.GroupBy((t) => t.Namespace).OrderBy(g => g.Key)) {
				var namespaceStatus = TestStatus.FinishedSuccessfully;
				var namespaceTime = TimeSpan.FromSeconds(0);
				foreach(var t in kv) {
					if(t.Status < namespaceStatus) {
						namespaceStatus = t.Status;
					}
					if(t.CompletionTime != TimeSpan.MinValue) {
						namespaceTime += t.CompletionTime;
					}
				}

				if(!first) {
					Console.WriteLine("");
				}
				first = false;
				Console.ForegroundColor = ConsoleColor.DarkGray;
				setColor(namespaceStatus);
				Console.WriteLine(checkbox(namespaceStatus) + " " + kv.Key);
				Console.WriteLine("==============================");

				foreach(var t in kv) {
					setColor(t.Status);
					Console.Write(" " +checkbox(t.Status) + " " + t.Id);
					if(t.Status == TestStatus.FinishedSuccessfully) {
						Console.Write(" - " + timestring(t.CompletionTime));
					}
					if(t.Status == TestStatus.FinishedError && !string.IsNullOrWhiteSpace(t.Description)) {
						Console.ForegroundColor = ConsoleColor.DarkRed;
						Console.WriteLine(" (" + t.Description + ")");
					} else { Console.WriteLine(); }

					if(t.Status == TestStatus.FinishedError) {
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("      [" + (!string.IsNullOrWhiteSpace(t.FailureType) ? t.FailureType + ": " : "") + t.FailureMessage + "]");

						foreach(var line in t.FailureStacktrace) {
							Console.WriteLine("     " + line);
							Console.ForegroundColor = ConsoleColor.DarkRed;
						}
						Console.WriteLine();
					}

					if(t.Status == TestStatus.WaitingForDependencies) {
						var addComma = false;
						Console.Write("     + Waiting for: ");
						foreach(var dependency in t.Dependencies) {
							var dependencyStatus = suite.GetDependencyStatus(dependency);
							if( dependencyStatus != TestStatus.FinishedSuccessfully){
								if(addComma){
									setColor(t.Status);
									Console.Write(", ");
								}
								Console.Write(dependency);
								if( dependencyStatus == TestStatus.FinishedError ){
									setColor(dependencyStatus);
									Console.Write("(E)");
								}
								addComma = true;
							}
						}
						Console.WriteLine();
					}

					var log = t.GetLogTail(10);
					if(log.Length > 0) {
						foreach(var msg in log) {
							switch(msg.Level) {
								case Test.LogLevel.Debug: Console.ForegroundColor = ConsoleColor.DarkGray; Console.Write("     [Debug] "); break;
								case Test.LogLevel.Info: Console.ForegroundColor = ConsoleColor.Gray; Console.Write("     [Info]  "); break;
								case Test.LogLevel.Warn: Console.ForegroundColor = ConsoleColor.White; Console.Write("     [Warn]  "); break;
							}
							Console.WriteLine(msg.Message);
						}
						Console.WriteLine();
					}
				}
			}
		}

		private string timestring(TimeSpan ts) {
			if(ts == TimeSpan.MinValue) {
				return "    ";
			}else if(ts.TotalMilliseconds < 100) {
				return ts.TotalMilliseconds.ToString("0").PadLeft(2) + "ms";
			} else if(ts.TotalSeconds < 10) {
				return ts.TotalSeconds.ToString("0.0") + "s";
			} else if(ts.TotalSeconds < 100) {
				return ts.TotalSeconds.ToString("0").PadLeft(3) + "s";
			} else {
				return ts.TotalMinutes.ToString() + "m";
			}
		}

		private string checkbox(TestStatus status) {
			switch(status) {
				case TestStatus.Idle:					return "[ ]";
				case TestStatus.WaitingForDependencies: return "[ ]";
				case TestStatus.Running:				return "[.]";
				case TestStatus.FinishedError:			return "[!]";
				case TestStatus.FinishedSuccessfully:	return "[X]";
				default: throw new ApplicationException("unknown status:" + status);
			}
		}

		private void setColor(TestStatus status) {
			switch(status) {
				case TestStatus.Idle:					Console.ForegroundColor = ConsoleColor.DarkGray; break;
				case TestStatus.WaitingForDependencies: Console.ForegroundColor = ConsoleColor.Gray; break;
				case TestStatus.Running:				Console.ForegroundColor = ConsoleColor.Yellow; break;
				case TestStatus.FinishedError:			Console.ForegroundColor = ConsoleColor.Red; break;
				case TestStatus.FinishedSuccessfully:	Console.ForegroundColor = ConsoleColor.Green; break;
			}
		}

		private static Regex fileAndLine = new Regex("[A-N]:\\\\[^:]+:[^0-9]+[0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static string getTopFilename(string[] stacktrace) {
			var match = fileAndLine.Match(stacktrace != null && stacktrace.Length>0 ? stacktrace[0] : "");
			return match.Success ? match.Value : null;
		}
	}
}