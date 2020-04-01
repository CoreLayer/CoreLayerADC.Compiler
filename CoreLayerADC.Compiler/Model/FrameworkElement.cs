using System.Collections.Generic;

namespace CoreLayerADC.Compiler.Model
{
    public class FrameworkElement
    {
        public string Name { get; set; }
        public IEnumerable<string> References { get; set; }
        public IEnumerable<string> Dependencies { get; set; }
        public FrameworkExpression Expressions { get; set; }
    }
}