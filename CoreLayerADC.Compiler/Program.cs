using System;
using CoreLayerADC.Compiler.Output;
using CoreLayerADC.Compiler.Processor;

namespace CoreLayerADC.Compiler
{
    class Program
    {
        private static ModuleProcessor _moduleProcessor;

        static void Main(string[] args)
        {
            var searchPath = args[0];
            Console.WriteLine("CoreLayerADC Framework Compiler");
            Console.WriteLine("-------------------------------");
            
            Console.WriteLine("Search path: {0}", searchPath);

            _moduleProcessor = ModuleProcessor.GetModuleProcessor(ModuleLoader.LoadModulesFromDirectory(searchPath));

            FileOutput.WriteAll(_moduleProcessor, searchPath);
        }
    }
}