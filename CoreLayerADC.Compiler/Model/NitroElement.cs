using System.Collections.Generic;

namespace CoreLayerADC.Compiler.Model
{
    public class NitroElement
    {
        public string Name { get; set; }
        public List<string> References { get; set; }
        public List<string> Dependencies { get; set; }
        public NitroExpression Expressions { get; set; }
    }
}