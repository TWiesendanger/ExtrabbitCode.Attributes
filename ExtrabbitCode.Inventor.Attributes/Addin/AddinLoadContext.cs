using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace ExtrabbitCode.Inventor.Attributes.Addin
{
    internal sealed class AddinLoadContext : AssemblyLoadContext
    {
        private static readonly Dictionary<string, AddinLoadContext> DependenciesProviders = new(1);
        private readonly AssemblyDependencyResolver _resolver;
        private const BindingFlags MethodSearchFlags = BindingFlags.Public | BindingFlags.Instance;

        private AddinLoadContext(Type type, string addinName) : base(addinName)
        {
            string addinLocation = type.Assembly.Location;
            _resolver = new AssemblyDependencyResolver(addinLocation);
        }

        /// <summary>
        ///     Resolve dependency any time one is loaded if it exists in the isolated add-in dependency container.
        /// </summary>
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath is not null ? LoadFromAssemblyPath(assemblyPath) : null;
        }

        /// <summary>
        ///     Determine if the <see cref="AssemblyLoadContext" /> is custom or still the default context.
        /// </summary>
        public static bool CheckIfCustomContext(Type type)
        {
            AssemblyLoadContext? currentContext = GetLoadContext(type.Assembly);
            return currentContext != Default;
        }

        /// <summary>
        ///     Get or create a new isolated context for the type.
        /// </summary>
        public static AddinLoadContext GetDependenciesProvider(Type type)
        {
            // Assembly location used as context name and the unique provider key.
            string addinRoot = System.IO.Path.GetDirectoryName(type.Assembly.Location)!;
            if (DependenciesProviders.TryGetValue(addinRoot, out AddinLoadContext? provider))
            {
                return provider;
            }

            string addinName = System.IO.Path.GetFileName(addinRoot);
            provider = new AddinLoadContext(type, addinName);
            DependenciesProviders.Add(addinRoot, provider);
            return provider;
        }

        /// <summary>
        ///     Create new instance in the separated context.
        /// </summary>
        public object CreateInstance(Type type)
        {
            string assemblyLocation = type.Assembly.Location;
            Assembly assembly = LoadFromAssemblyPath(assemblyLocation);
            return assembly.CreateInstance(type.FullName!)!;
        }

        /// <summary>
        ///     Execute <see cref="ApplicationAddInSite" /> in the separated context.
        /// </summary>
        /// <remarks>
        ///     Matches parameter format of <see cref="ApplicationAddInServer.Activate" /> method.
        /// </remarks>
        public static void Invoke(object instance, string methodName, ApplicationAddInSite application, bool firstTime)
        {
            Type instanceType = instance.GetType();
            Type[] methodParameterTypes =
            [
                typeof(ApplicationAddInSite),
            typeof(bool)
            ];
            object[] methodParameters =
            [
                application,
            firstTime
            ];

            MethodInfo method = instanceType.GetMethod(methodName, MethodSearchFlags, null, methodParameterTypes, null)!;
            // Call the inheriting method which may contain custom logic if it exists.
            _ = method.Invoke(instance, methodParameters)!;
        }

        /// <remarks>
        ///     Matches parameter format of <see cref="ApplicationAddInServer.Deactivate" /> method.
        /// </remarks>
        public static void Invoke(object instance, string methodName)
        {
            Type instanceType = instance.GetType();

            MethodInfo method = instanceType.GetMethod(methodName, MethodSearchFlags, null, [], null)!;
            // Call the inheriting method which may contain custom logic if it exists.
            _ = method.Invoke(instance, [])!;
        }
    }
}