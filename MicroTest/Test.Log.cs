using System;
using System.Collections.Generic;

namespace MicroTest {
	public abstract partial class Test {
		private LinkedList<LogMessage> messages = new LinkedList<LogMessage>();

		public LogMessage[] GetLogTail(int maxEntries) {
			var tail = new LogMessage[Math.Min(messages.Count, maxEntries)];

			var current = messages.Last;
			var index = 0;
			while(index < tail.Length) {
				tail[index++] = current.Value;
				current = current.Previous;
			}

			return tail;
		}

		public void Debug(string message) { log(LogLevel.Debug, message);} 
		public void Info(string message) { log(LogLevel.Info, message);} 
		public void Warn(string message) { log(LogLevel.Warn, message);} 

		private void log(LogLevel level, string message) {
			this.messages.AddLast(new LogMessage(level, message));
		}

		public enum LogLevel { Debug, Info, Warn }

		public class LogMessage {
			public readonly LogLevel Level;
			public readonly string Message;

			public LogMessage(LogLevel level, string message) {
				this.Level = level;
				this.Message = message;
			}
		}
	}
}