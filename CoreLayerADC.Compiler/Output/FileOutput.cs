using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoreLayerADC.Compiler.Model;
using CoreLayerADC.Compiler.Processor;

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
            var commands = GetInstallCommands(moduleProcessor);
            var output = ReplaceParameters(commands, moduleProcessor);
            WriteToFile(output, path, FrameworkOutputMode.Install);
        }

        public static void WriteUninstall(ModuleProcessor moduleProcessor, string path)
        {
            var commands = GetUninstallCommands(moduleProcessor);
            var output = ReplaceParameters(commands, moduleProcessor);
            WriteToFile(output, path, FrameworkOutputMode.Uninstall);
        }

        private static void WriteToFile(IEnumerable<string> commands, string path, FrameworkOutputMode outputType)
        {
            var outputFile = outputType.ToString().ToLower() + ".conf";
            var outputPath = Path.Combine(path, "output");
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            File.WriteAllLines(
                Path.Combine(outputPath, outputFile), commands);
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
                commands.AddRange(
                    moduleProcessor.InstallExpressions[moduleName].Select(name => tempDict[name]));
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
                commands.AddRange(moduleProcessor.UninstallExpressions[moduleName]
                    .Select(name => tempDict[name]));
            }
        
            return commands;
        }
        
        private static IEnumerable<string> ReplaceParameters(IEnumerable<string> commands, ModuleProcessor moduleProcessor)
        {
            var output = ReplacePlaceholders(commands, moduleProcessor.Placeholders);
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
    }
}