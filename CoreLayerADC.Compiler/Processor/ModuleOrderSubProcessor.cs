using System.Collections.Generic;
using System.Linq;
using CoreLayerADC.Compiler.Model;

namespace CoreLayerADC.Compiler.Processor
{
    public static class ModuleOrderSubProcessor
    {
        public static IEnumerable<string> SortModuleNames(Dictionary<string, FrameworkModule> modules)
        {
            // Count the amount of dependencies for each module
            var moduleDependencyCount = CountModuleDependencies(modules);
            
            // Sort moduleDependencyCount from high to low
            var moduleDependencyOrder = moduleDependencyCount.OrderBy(counter => counter.Value).Reverse();

            // Return the sorted list of modules
            return moduleDependencyOrder.Select(module => module.Key);
        }
        
        private static Dictionary<string, int> CountModuleDependencies(Dictionary<string, FrameworkModule> modules)
        {
            // Counter to hold how many times a module is referenced from another module
            var moduleOccurenceCounter = modules.ToDictionary(module => module.Key, module => 0);
            
            // Loop over all modules that have dependencies defined
            foreach (var dependency in modules.Values.Where(module => module.Dependencies != null)
                .SelectMany(module => module.Dependencies))
            {
                moduleOccurenceCounter[dependency]++;
                // Drill down nested dependencies
                CountNestedModuleDependencies(modules, dependency, moduleOccurenceCounter);
            }

            return moduleOccurenceCounter;
        }
        
        private static void CountNestedModuleDependencies(Dictionary<string, FrameworkModule> modules, string dependency, IDictionary<string, int> moduleOccurenceCounter)
        {
            foreach (
                var moduleDependency in modules.Values
                    .Where(module => module.Name == dependency && module.Dependencies != null)
                    .SelectMany(module => module.Dependencies)
            )
            {
                moduleOccurenceCounter[moduleDependency]++;
                CountNestedModuleDependencies(modules, moduleDependency, moduleOccurenceCounter);
            }
        }
    }
}