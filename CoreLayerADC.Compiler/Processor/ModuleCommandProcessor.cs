using System.Collections.Generic;
using System.Linq;
using CoreLayerADC.Compiler.Model;
// ReSharper disable PossibleMultipleEnumeration

namespace CoreLayerADC.Compiler.Processor
{
    public static class ModuleCommandProcessor
    {
        private static readonly List<string> KnownDependencies = new List<string>();
        
        public static IEnumerable<string> GetCommandOrder(Dictionary<string, FrameworkModule> modules, string moduleName)
        {
            var expressionDependencyCount = CountCommandDependencies(modules, moduleName);
            var expressionDependencyOrder = expressionDependencyCount.OrderBy(counter => counter.Value).Reverse();

            var output = expressionDependencyOrder.Select(expression => expression.Key);
            
            KnownDependencies.AddRange(output);
            
            return output.ToList();
        }
        
        
        private static Dictionary<string, int> CountCommandDependencies(IReadOnlyDictionary<string, FrameworkModule> modules, string moduleName)
        {
            var sectionElements = modules[moduleName].Sections.SelectMany(section => section.Elements);
            var frameworkElements = sectionElements as FrameworkElement[] ?? sectionElements.ToArray();
            var elementOccurenceCounter = frameworkElements.ToDictionary(element => element.Name, element => 0);
            
            foreach (var dependency in frameworkElements)
            {
                if (KnownDependencies.Contains(dependency.Name)) continue;
                
                elementOccurenceCounter[dependency.Name]++;
                CountCommandNestedDependencies(modules[moduleName].Sections, dependency.Name,
                    elementOccurenceCounter);
            }
        
            return elementOccurenceCounter;
        }
        
        private static void CountCommandNestedDependencies(IEnumerable<FrameworkSection> sections, string dependency, Dictionary<string, int> elementOccurenceCounter)
        {
            foreach (var expressionDependency in sections.SelectMany(section => section.Elements)
                .Where(element => element.Name == dependency && element.Dependencies != null)
                .SelectMany(module => module.Dependencies))
            {
                if (KnownDependencies.Contains(expressionDependency)) continue;
                
                elementOccurenceCounter[expressionDependency]++;
                CountCommandNestedDependencies(sections, expressionDependency, elementOccurenceCounter);
            }
        }
    }
}