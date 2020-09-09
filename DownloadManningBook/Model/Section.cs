using System.Collections.Generic;

namespace DownloadManningBook.Model
{
    public class Section
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public List<Subsection> SubSections { get; set; }
    }
}
