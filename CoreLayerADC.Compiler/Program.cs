using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoreLayerADC.Compiler.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CoreLayerADC.Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var searchPath = args[0];
            Console.WriteLine("CoreLayerADC Framework Compiler");
            Console.WriteLine("-------------------------------");
            
            Console.WriteLine("Search path: {0}", searchPath);
            
            GenerateOutputFile(searchPath, FrameworkOutputMode.Install);
            GenerateOutputFile(searchPath, FrameworkOutputMode.Uninstall);
        }

        private static void GenerateOutputFile(string searchPath, FrameworkOutputMode outputType)
        {
            var sortedModules = EnumerateFrameworkModules(searchPath);
            //var frameworkModules = sortedModules as FrameworkModule[];// ?? sortedModules.ToArray();
            var placeholders = LoadPlaceholders(sortedModules);
            var sortedExpressions = GetFrameworkExpressions(sortedModules, outputType);

            File.WriteAllLines(
                Path.Combine(searchPath, "output", outputType.ToString().ToLower() + ".conf"), 
                GetOutputLines(sortedExpressions, placeholders, sortedModules));
        }

        private static IEnumerable<string> GetOutputLines(IEnumerable<string> sortedExpressions, Dictionary<string, string> placeholders,
            IEnumerable<FrameworkModule> frameworkModules)
        {
            var outputExpressions = ReplacePlaceholdersInExpressions(sortedExpressions, placeholders);
            var coreVersion = frameworkModules.Single(module => module.Name.Equals("Core")).Version;
            outputExpressions = ReplaceVersion(
                outputExpressions,
                coreVersion.Major,
                coreVersion.Minor
            );
            return outputExpressions;
        }

        private static IEnumerable<string> ReplaceVersion(IEnumerable<string> expressions, int majorVersion, int minorVersion)
        {
            return expressions.Select(expression =>
                expression.Replace("_V_", "CL" + majorVersion.ToString().PadLeft(2, '0') + "" + minorVersion.ToString().PadLeft(2,'0')));
        } 

        private static IEnumerable<string> ReplacePlaceholdersInExpressions(IEnumerable<string> expressions,
            Dictionary<string, string> placeholders)
        {
            return expressions.Select(
                expression => placeholders.Aggregate(
                    expression, 
                    (result, s) => result.Replace(s.Key, s.Value)
                    )
                ).ToList();
        }

        private static Dictionary<string, string> LoadPlaceholders(IEnumerable<FrameworkModule> sortedModules)
        {
            var placeholders = new Dictionary<string, string>();
            try
            {
                placeholders = sortedModules.Where(module => module.Placeholders != null)
                    .SelectMany(module => module.Placeholders)
                    .OrderBy(placeholder => placeholder.Name)
                    .Reverse()
                    .ToDictionary(placeholder => placeholder.Name, placeholder => placeholder.Expression);
                
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("Duplicate key found in placeholders: {0}", ex.Message);
            }
            
            return placeholders;
        }

        private static IEnumerable<string> GetFrameworkExpressions(IEnumerable<FrameworkModule> modules, FrameworkOutputMode mode)
        {
            switch (mode)
            {
                case FrameworkOutputMode.Install: return GetFrameworkInstallExpressions(modules);
                case FrameworkOutputMode.Uninstall: return GetFrameworkUninstallExpressions(modules);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private static IEnumerable<string> GetFrameworkInstallExpressions(IEnumerable<FrameworkModule> modules)
        {
            var sortedExpressions = new List<string>();
            var tempDict = new Dictionary<string, string>();
            // Load expressions in Temporary dict
            foreach (var module in modules)
            {
                foreach (var element in module.Sections.SelectMany(section => section.Elements))
                {
                    tempDict.Add(element.Name, element.Expressions.Install);
                }

                // Sort expression names
                var sortedNames = GetFrameworkExpressionsOrder(module);

                // Fill sortedExpressions based on order
                sortedExpressions.AddRange(sortedNames.Select(name => tempDict[name]));
            }

            return sortedExpressions;
        }
        
        private static IEnumerable<string> GetFrameworkUninstallExpressions(IEnumerable<FrameworkModule> modules)
        {
            var sortedExpressions = new List<string>();
            var tempDict = new Dictionary<string, string>();
            // Load expressions in Temporary dict
            foreach (var module in modules.Reverse())
            {
                foreach (var element in module.Sections.SelectMany(section => section.Elements))
                {
                    tempDict.Add(element.Name, element.Expressions.Uninstall);
                }

                // Sort expression names
                var sortedNames = GetFrameworkExpressionsOrder(module).Reverse();

                // Fill sortedExpressions based on order
                sortedExpressions.AddRange(sortedNames.Select(name => tempDict[name]));
            }

            return sortedExpressions;
        }

        private static IEnumerable<string> GetFrameworkExpressionsOrder(FrameworkModule module)
        {
            var expressionDependencyCount = CountExpressionDependencies(module.Sections);
            var expressionDependencyOrder = expressionDependencyCount.OrderBy(counter => counter.Value).Reverse();
            
            return expressionDependencyOrder.Select(expression => expression.Key).ToList();
        }
        
        
        private static Dictionary<string, int> CountExpressionDependencies(IEnumerable<Section> sections)
        {
            var sectionElements = sections.SelectMany(section => section.Elements);
            var nitroElements = sectionElements as NitroElement[] ?? sectionElements.ToArray();
            var elementOccurenceCounter = nitroElements.ToDictionary(element => element.Name, element => 0);
            
            foreach (var dependency in nitroElements)
            {
                elementOccurenceCounter[dependency.Name]++;
                CountNestedExpressionDependencies(sections, dependency.Name, elementOccurenceCounter);
            }

            return elementOccurenceCounter;
        }
        
        private static void CountNestedExpressionDependencies(IEnumerable<Section> sections, string dependency, IDictionary<string, int> expressionOccurenceCounter)
        {
            foreach (var expressionDependency in sections.SelectMany(section => section.Elements)
                .Where(element => element.Name == dependency && element.Dependencies != null)
                .SelectMany(module => module.Dependencies))
            {
                expressionOccurenceCounter[expressionDependency]++;
                CountNestedExpressionDependencies(sections, expressionDependency, expressionOccurenceCounter);
            }
        }

        private static IEnumerable<FrameworkModule> EnumerateFrameworkModules(string searchPath)
        {
            var filePaths = Directory.EnumerateFiles(searchPath, "*.yaml", SearchOption.AllDirectories);
            var modules = filePaths.Select(filePath => ReadModuleFromYaml(ReadYamlFromFile(filePath)))
                .ToDictionary(module => module.Name);
            
            foreach (var module in modules)
            {
                Console.WriteLine(module.Key);
            }

            var moduleOrder = GetModuleDependencyOrder(modules);
            var sortedModules = moduleOrder.Select(moduleName => modules[moduleName]).ToList();
            return sortedModules;
        }

        private static IEnumerable<string> GetModuleDependencyOrder(Dictionary<string, FrameworkModule> modules)
        {
            var moduleDependencyCount = CountModuleDependencies(modules);
            var moduleDependencyOrder = moduleDependencyCount.OrderBy(counter => counter.Value).Reverse();

            return moduleDependencyOrder.Select(module => module.Key).ToList();
        }

        private static Dictionary<string, int> CountModuleDependencies(Dictionary<string, FrameworkModule> modules)
        {
            var moduleOccurenceCounter = modules.ToDictionary(module => module.Key, module => 0);
            
            foreach (var dependency in modules.Values.Where(module => module.Dependencies != null)
                .SelectMany(module => module.Dependencies))
            {
                moduleOccurenceCounter[dependency]++;
                CountNestedModuleDependencies(modules, dependency, moduleOccurenceCounter);
            }

            return moduleOccurenceCounter;
        }

        private static void CountNestedModuleDependencies(Dictionary<string, FrameworkModule> modules, string dependency, IDictionary<string, int> moduleOccurenceCounter)
        {
            foreach (var moduleDependency in modules.Values
                .Where(module => module.Name == dependency && module.Dependencies != null)
                .SelectMany(module => module.Dependencies))
            {
                moduleOccurenceCounter[moduleDependency]++;
                CountNestedModuleDependencies(modules, moduleDependency, moduleOccurenceCounter);
            }
        }


        private static string ReadYamlFromFile(string filePath)
        {
            try
            {
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