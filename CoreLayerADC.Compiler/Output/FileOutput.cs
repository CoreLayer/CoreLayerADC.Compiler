using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoreLayerADC.Compiler.Model;
using CoreLayerADC.Compiler.Processor;
// ReSharper disable PossibleMultipleEnumeration

namespace CoreLayerADC.Compiler.Output
{
    public static class FileOutput
    {
        public static void WriteAll(ModuleProcessor moduleProcessor, string path)
        {
            WriteInstall(moduleProcessor, path);
            WriteUninstall(moduleProcessor, path);
        }

        public static void WriteInstall(ModuleProcessor moduleProcessor, string path)
        {
            Console.WriteLine("Generating {0}", FrameworkOutputMode.Install);
            var commands = GetInstallCommands(moduleProcessor);
            var output = ReplaceParameters(commands, moduleProcessor);
            WriteToFile(output, path, FrameworkOutputMode.Install);
        }

        public static void WriteUninstall(ModuleProcessor moduleProcessor, string path)
        {
            Console.WriteLine("Generating {0}", FrameworkOutputMode.Uninstall);
            var commands = GetUninstallCommands(moduleProcessor);
            var output = ReplaceParameters(commands, moduleProcessor);
            WriteToFile(output, path, FrameworkOutputMode.Uninstall);
        }

        private static void WriteToFile(IEnumerable<string> commands, string path, FrameworkOutputMode outputType)
        {
            var outputFile = outputType.ToString().ToLower() + ".conf";

            File.WriteAllLines(
                Path.Combine(GetOutputPath(path), outputFile), 
                FilterEmptyLines(commands));
        }

        private static string GetOutputPath(string path)
        {
            var outputPath = Path.Combine(path, "output");
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            return outputPath;
        }

        private static IEnumerable<string> FilterEmptyLines(IEnumerable<string> commands)
        {
            var outputCommands = new List<string>();
            foreach (var command in commands)
            {
                var tempCommands = command.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                outputCommands.AddRange(tempCommands);
            }

            return outputCommands;
        }

        private static IEnumerable<string> GetInstallCommands(ModuleProcessor moduleProcessor)
        {
            var commands = new List<string>();
            var tempDict = new Dictionary<string, string>();
            // Load commands in Temporary dict
            foreach (var moduleName in moduleProcessor.InstallModuleNames)
            {
                var module = moduleProcessor.Modules[moduleName];
                foreach (var element in module.Sections.SelectMany(section => section.Elements))
                {
                    tempDict.Add(element.Name, element.Expressions.Install);
                }
        
                // Fill commands based on order
                commands.AddRange(GetModuleOutputPrefix(moduleName));
                commands.AddRange(moduleProcessor.SortedModuleExpressions[moduleName].Select(name => tempDict[name]));
                commands.AddRange(GetModuleOutputSuffix(moduleName));
            }
            return commands;
        }
        
        private static IEnumerable<string> GetUninstallCommands(ModuleProcessor moduleProcessor)
        {
            var commands = new List<string>();
            var tempDict = new Dictionary<string, string>();
            // Load commands in Temporary dict
            foreach (var moduleName in moduleProcessor.UninstallModuleNames)
            {
                var module = moduleProcessor.Modules[moduleName];
                foreach (var element in module.Sections.SelectMany(section => section.Elements))
                {
                    tempDict.Add(element.Name, element.Expressions.Uninstall);
                }

                // Fill commands based on order
                commands.AddRange(GetModuleOutputPrefix(moduleName));
                commands.AddRange(moduleProcessor.SortedModuleExpressions[moduleName].Select(name => tempDict[name]).Reverse());
                commands.AddRange(GetModuleOutputSuffix(moduleName));
            }
        
            return commands;
        }
        
        private static IEnumerable<string> ReplaceParameters(IEnumerable<string> commands, ModuleProcessor moduleProcessor)
        {
            int iteration = 0;
            var output = commands;
            do
            {
                iteration++;
                output = ReplacePlaceholders(output, moduleProcessor.Placeholders);
            }
            while (output.Contains("PLH"));
            
            Console.WriteLine("Iterations required for Placeholder replacement: {0}", iteration);
            output = ReplaceVersion(output, moduleProcessor.Version);
            
            return output;
        }

        private static IEnumerable<string> ReplaceVersion(IEnumerable<string> commands, FrameworkVersion version)
        {
            return commands.Select(expression =>
                expression.Replace("_V_",
                    "CL" + version.Major.ToString().PadLeft(2, '0') 
                         + "" 
                         + version.Minor.ToString().PadLeft(2, '0')));
        } 
        
        private static IEnumerable<string> ReplacePlaceholders(IEnumerable<string> commands,
            Dictionary<string, string> placeholders)
        {
            return commands.Select(
                expression => placeholders.Aggregate(
                    expression, 
                    (result, s) => result.Replace(s.Key, s.Value)
                )
            ).ToList();
        }

        private static IEnumerable<string> GetModuleOutputPrefix(string moduleName)
        {
            return new List<string>
            {
                "#",
                "##",
                "###",
                "############################################",
                "# MODULE " + moduleName + " - START",
            };
        }
        
        private static IEnumerable<string> GetModuleOutputSuffix(string moduleName)
        {
            return new List<string>
            {
                "# MODULE " + moduleName + " - END",
                "############################################",
                "###",
                "##",
                "#",
            };
        }
    }
}