using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoreLayerADC.Compiler.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CoreLayerADC.Compiler.Processor
{
    public static class ModuleLoader
    {
        public static Dictionary<string, FrameworkModule> LoadModulesFromDirectory(string searchPath)
        {
            Console.WriteLine("Looking for modules in {0}", searchPath);
            var filePaths = Directory.EnumerateFiles(searchPath, "*.yaml", SearchOption.AllDirectories);
            var modules = filePaths.Select(filePath => ReadModuleFromYaml(ReadYamlFromFile(filePath)))
                .ToDictionary(module => module.Name);

            return modules;
        }

        private static string ReadYamlFromFile(string filePath)
        {
            try
            {
                Console.WriteLine("Loading yaml-file: {0}", filePath);
                using var stream = new StreamReader(filePath);
                return stream.ReadToEnd();
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        private static FrameworkModule ReadModuleFromYaml(string frameworkModuleYaml)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<FrameworkModule>(frameworkModuleYaml);
        }
    }
}