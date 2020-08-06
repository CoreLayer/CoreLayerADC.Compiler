using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using CoreLayerADC.Compiler.Model;

namespace CoreLayerADC.Compiler.Processor
{
    public class ModuleProcessor
    {
        private static ModuleProcessor _moduleProcessor;
        private static readonly object Lock = new object();

        private readonly Dictionary<string, FrameworkModule> _modules;
        private IEnumerable<string> _sortedModuleNames;
        private Dictionary<string, string> _placeholders = new Dictionary<string, string>();
        private readonly Dictionary<string, List<string>> _sortedModuleExpressions = new Dictionary<string, List<string>>();

        private ModuleProcessor(Dictionary<string, FrameworkModule> modules)
        {
            _modules = modules;
        }

        public static ModuleProcessor GetModuleProcessor(Dictionary<string, FrameworkModule> modules)
        {
            if (_moduleProcessor == null)
            {
                lock (Lock)
                {
                    if (_moduleProcessor == null)
                    {
                        _moduleProcessor = new ModuleProcessor(modules);
                        _moduleProcessor.Run();
                    }
                }
            }
            return _moduleProcessor;
        }

        private void Run()
        {
            SortModules();
            SortPlaceholders();
            SortExpressions();
        }

        private void SortModules()
        {
            _sortedModuleNames = ModuleOrderSubProcessor.SortModuleNames(_modules);

            Console.WriteLine("Sorted modules");
            foreach (var moduleName in _sortedModuleNames)
            {
                Console.WriteLine("\t{0}", moduleName);
            }
        }

        private void SortExpressions()
        {
            var moduleNames = _sortedModuleNames.ToList();

            for(var currentIndex = 0; currentIndex < _sortedModuleNames.Count(); currentIndex++)
            {
                var moduleName = moduleNames[currentIndex];
                
                _sortedModuleExpressions[moduleName] = 
                    ModuleCommandProcessor.GetCommandOrder(_modules, moduleName).ToList();
            }
            
            foreach (var module in _sortedModuleExpressions)
            {
                Console.WriteLine("Sorted expressions for {0}", module.Key);
                foreach (var expression in module.Value)
                {
                    Console.WriteLine("\t{0}", expression);
                }
            }
        }
        
        private void SortPlaceholders()
        {
            try
            {
                // Placeholders are reversed to avoid name conflicts during replace operation
                _placeholders = _modules.Values.Where(module => module.Placeholders != null)
                    .SelectMany(module => module.Placeholders)
                    .OrderBy(placeholder => placeholder.Name)
                    .Reverse()
                    .ToDictionary(placeholder => placeholder.Name, placeholder => placeholder.Expression);

                Console.WriteLine("Sorted placeholders");
                foreach (var placeholder in _placeholders)
                {
                    Console.WriteLine("\t{0}", placeholder.Key);
                }
                
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine();
                Console.WriteLine("ERROR - Duplicate key found in placeholders: {0}", ex.Message);
                Environment.Exit(0);
                
            }
        }


        public Dictionary<string, FrameworkModule> Modules => _modules;
        public FrameworkVersion Version => _modules["Core"].Version;
        public IEnumerable<string> InstallModuleNames => _sortedModuleNames;
        public IEnumerable<string> UninstallModuleNames => _sortedModuleNames.Reverse();
        public Dictionary<string, string> Placeholders => _placeholders;
        public Dictionary<string, List<string>> SortedModuleExpressions => _sortedModuleExpressions;
    }
}