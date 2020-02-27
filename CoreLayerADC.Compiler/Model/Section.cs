using System.Collections.Generic;

namespace CoreLayerADC.Compiler.Model
{
    public class Section
    {
        public string Name { get; set; }
        public List<NitroElement> Elements { get; set; }
    }
}