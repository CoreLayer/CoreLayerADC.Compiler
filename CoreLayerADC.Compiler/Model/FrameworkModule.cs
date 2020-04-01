using System.Collections.Generic;

namespace CoreLayerADC.Compiler.Model
{
    public class FrameworkModule
    {
        public string Name { get; set; }
        public FrameworkVersion Version { get; set; }
        public IEnumerable<string> Dependencies { get; set; }
        public IEnumerable<FrameworkPlaceholder> Placeholders { get; set; }
        public IEnumerable<FrameworkSection> Sections { get; set; }
    }
}