using System.Collections.Generic;

namespace CoreLayerADC.Compiler.Model
{
    public class FrameworkSection
    {
        public string Name { get; set; }
        public IEnumerable<FrameworkElement> Elements { get; set; }
    }
}