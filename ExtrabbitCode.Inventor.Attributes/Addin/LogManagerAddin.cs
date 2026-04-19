using log4net;
using log4net.Config;
using log4net.Repository;
using System;
using System.IO;
using System.Reflection;

namespace ExtrabbitCode.Inventor.Attributes.Addin;

internal static class LogManagerAddin
{
    private static readonly object SyncObj = new();
    private static ILog? _logger;
    private static ILoggerRepository? _repository;

    public static ILog GetLogger(Type type)
    {
        EnsureConfigured(type);
        return _logger ?? LogManager.GetLogger(_repository?.Name ?? "DefaultRepo", type);
    }

    private static void EnsureConfigured(Type type)
    {
        if (_repository != null)
        {
            return;
        }

        lock (SyncObj)
        {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
            string basePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string configPath = System.IO.Path.Combine(basePath, "log4net.config");

            // Create a unique repository per add-in DLL name
            _repository = LogManager.CreateRepository(assemblyName);

            if (System.IO.File.Exists(configPath))
            {
                XmlConfigurator.ConfigureAndWatch(_repository, new FileInfo(configPath));
            }
            else
            {
                BasicConfigurator.Configure(_repository);
            }

            _logger = LogManager.GetLogger(_repository.Name, type);
        }
    }
}