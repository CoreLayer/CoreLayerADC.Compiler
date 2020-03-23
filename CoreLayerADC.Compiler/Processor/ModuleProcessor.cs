using System;
using System.Collections.Generic;
using System.Linq;
using CoreLayerADC.Compiler.Model;
using System.Threading.Tasks;

namespace CoreLayerADC.Compiler.Processor
{
    public class ModuleProcessor
    {
        private static ModuleProcessor _moduleProcessor;
        private static readonly object _lock = new object();

        private readonly Dictionary<string, FrameworkModule> _modules;
        private IEnumerable<string> _sortedModuleNames;
        private Dictionary<string, string> _placeholders = new Dictionary<string, string>();
        private readonly Dictionary<string, List<string>> _installExpressions = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, List<string>> _uninstallExpressions = new Dictionary<string, List<string>>();

        private ModuleProcessor(Dictionary<string, FrameworkModule> modules)
        {
            _modules = modules;
        }

        public static ModuleProcessor GetModuleProcessor(Dictionary<string, FrameworkModule> modules)
        {
            if (_moduleProcessor == null)
            {
                lock (_lock)
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
        }

        private void SortExpressions()
        {
            var modules = _sortedModuleNames.ToList();
            Parallel.For(0, _sortedModuleNames.Count(), (currentIndex) =>
            {
                var moduleName = modules[currentIndex];
                var module = _modules[moduleName];

                _installExpressions[moduleName] = 
                    ModuleExpressionSubProcessor.GetSortedExpressions(module, FrameworkOutputMode.Install).ToList();

                _uninstallExpressions[moduleName] = 
                    ModuleExpressionSubProcessor.GetSortedExpressions(module, FrameworkOutputMode.Uninstall).Reverse().ToList();
            });
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
                
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("Duplicate key found in placeholders: {0}", ex.Message);
            }
        }


        public Dictionary<string, FrameworkModule> Modules => _modules;
        public FrameworkVersion Version => _modules["Core"].Version;
        public IEnumerable<string> InstallModuleNames => _sortedModuleNames;
        public IEnumerable<string> UninstallModuleNames => _sortedModuleNames.Reverse();
        public Dictionary<string, string> Placeholders => _placeholders;
        public Dictionary<string, List<string>> InstallExpressions => _installExpressions;
        public Dictionary<string, List<string>> UninstallExpressions => _uninstallExpressions;
    }
}