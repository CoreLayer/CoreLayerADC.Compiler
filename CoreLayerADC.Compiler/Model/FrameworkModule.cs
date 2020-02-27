using System.Collections.Generic;

namespace CoreLayerADC.Compiler.Model
{
    public class FrameworkModule
    {
        public string Name { get; set; }
        public FrameworkVersion Version { get; set; }
        public List<string> Dependencies { get; set; }
        public List<Placeholder> Placeholders { get; set; }
        public List<Section> Sections { get; set; }
    }
}