using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MicroTest {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class TestAsync : TestAttribute {
		public TestAsync() { }
		public TestAsync(string description) : base(description) { }
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class TestAttribute : Attribute{
		private string[] dependencies = new string[0];
		public uint TimeoutSeconds { get; set; } // 
		public string Description { get; set; }
		public string Dependencies {
			get { return string.Join(",", dependencies); }
			set { dependencies = (value ?? "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries); }
		}

		public TestAttribute() {}

		public TestAttribute(string description) : base() {
			this.Description = description;
		}

		internal static IEnumerable<Test> FindAll() {
			foreach(var assembly in new Assembly[] { Assembly.GetCallingAssembly(), Assembly.GetEntryAssembly(), Assembly.GetExecutingAssembly() }) {
				foreach(var type in assembly.GetTypes()){
					foreach(var test in FindAll(type)) {
						yield return test;
					}
				}
			}
		}

		internal static IEnumerable<Test> FindAll(Type type) {
			foreach(var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
				var arguments = method.GetParameters();
				if(arguments.Length == 1 && arguments[0].ParameterType == typeof(Test) && type.Namespace != "MicroTest") {
					var attribute = (TestAttribute)Array.Find<object>(method.GetCustomAttributes(false), a=> a is TestAttribute);

					yield return new TestImpl(
						method,
						type.Namespace + "." + type.Name,
						method.Name.ToLower().EndsWith("Test") ? method.Name.Substring(0,method.Name.Length-4) : method.Name,	
						attribute != null ? attribute.Description : null,
						attribute != null ? attribute.dependencies : null,
						attribute != null && attribute.TimeoutSeconds>0? TimeSpan.FromSeconds( attribute.TimeoutSeconds ): (TimeSpan?) null,
						attribute != null && attribute is TestAsync
					);
				}
			}
		}

		private class TestImpl : Test {
			private static Dictionary<Type, object> instances = new Dictionary<Type, object>();
			private MethodInfo method;

			public TestImpl(MethodInfo method, string @namespace, string id, string description, string[] dependencies, TimeSpan? timeout, bool async) : base(@namespace, id, description, dependencies, timeout, async) {
				this.method = method;
			}

			protected override void Execute() {
				// get create an instance to call
				object instance = null;
				lock(instances) {
					if(!instances.TryGetValue(method.DeclaringType, out instance)) {
						instances[method.DeclaringType] = instance = Activator.CreateInstance(method.DeclaringType);
					}
				}

				// Using DynamicMethods to invoke the method instead of just calling method.Invoke(...)
				// because visual studio will otherwise treat any exceptions thrown by the method as
				// unhandled and jump straight to it, which is not really useful since MicroTest depends
				// on being able to throw exceptions to stop tests when they fail.
				if(!method.IsStatic) {
					var dm = new DynamicMethod("__dynamic_test_method__", null, new Type[] { typeof(object), typeof(Test) }, true);
					ILGenerator il = dm.GetILGenerator(256);
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Castclass, method.DeclaringType); 
					il.Emit(OpCodes.Ldarg_1); 
					il.Emit(OpCodes.Callvirt, method);
					il.Emit(OpCodes.Ret);
					var dynMethod = (Action<object, Test>)dm.CreateDelegate(typeof(Action<object, Test>));
					dynMethod(instance, this);
				} else {
					var dm = new DynamicMethod("__dynamic_test_method__", null, new Type[] { typeof(Test) }, true);
					ILGenerator il = dm.GetILGenerator(256);
					il.Emit(OpCodes.Ldarg_0); 
					il.Emit(OpCodes.Call, method);
					il.Emit(OpCodes.Ret);
					var dynMethod = (Action<Test>)dm.CreateDelegate(typeof(Action<Test>));
					dynMethod(this);
				}
			}

			protected override string[] FilterFailureStacktrace(string[] stacktrace) {
				for(var i=stacktrace.Length-1;i>0;i--){
					if(stacktrace[i].IndexOf("System.RuntimeMethodHandle.InvokeMethod(Object target, Object[] arguments, Signature sig, Boolean constructor)") > -1 || stacktrace[i].IndexOf("__dynamic_test_method__")>-1) {
						var newStackTrace = new string[i];
						Array.Copy(stacktrace, newStackTrace, newStackTrace.Length);
						return newStackTrace;
					}
				}
				return stacktrace;
			}
		}
	}
}