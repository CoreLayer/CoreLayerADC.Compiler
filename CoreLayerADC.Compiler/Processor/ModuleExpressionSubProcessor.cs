using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using CoreLayerADC.Compiler.Model;

namespace CoreLayerADC.Compiler.Processor
{
    public static class ModuleExpressionSubProcessor
    {
        public static IEnumerable<string> GetSortedExpressions(FrameworkModule module, FrameworkOutputMode mode)
        {
            var expressionsOrder = GetFrameworkExpressionsOrder(module);
            return expressionsOrder;
        }
        
        private static IEnumerable<string> GetFrameworkExpressionsOrder(FrameworkModule module)
        {
            var expressionDependencyCount = CountExpressionDependencies(module.Sections);
            var expressionDependencyOrder = expressionDependencyCount.OrderBy(counter => counter.Value).Reverse();
            
            return expressionDependencyOrder.Select(expression => expression.Key).ToList();
        }
        
        
        public static Dictionary<string, int> CountExpressionDependencies(IEnumerable<Section> sections)
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
        
        private static void CountNestedExpressionDependencies(IEnumerable<Section> sections, string dependency, Dictionary<string, int> elementOccurenceCounter)
        {
            foreach (var expressionDependency in sections.SelectMany(section => section.Elements)
                .Where(element => element.Name == dependency && element.Dependencies != null)
                .SelectMany(module => module.Dependencies))
            {
                elementOccurenceCounter[expressionDependency]++;
                CountNestedExpressionDependencies(sections, expressionDependency, elementOccurenceCounter);
            }
        }
    }
}