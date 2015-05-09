using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;

namespace DLab.Domain
{
    public class WebSpecRepo
    {
        private WebSpecs _webSpecs { get; set; }

        public List<WebSpec> Specs
        {
            get
            {
                if (_webSpecs != null) return _webSpecs.Specs;
                if (!File.Exists("WebSpecs.bin"))
                {
                    _webSpecs = new WebSpecs();
                    return _webSpecs.Specs;
                }

                using (var file = File.OpenRead("WebSpecs.bin"))
                {
                    _webSpecs = Serializer.Deserialize<WebSpecs>(file);
                }
                return _webSpecs.Specs;
            }
        }

        public void Save(WebSpec webSpec)
        {
            var existingSpec = _webSpecs.Specs.SingleOrDefault(x => x.Id == webSpec.Id);
            if (existingSpec != null)
            {
                _webSpecs.Specs.Remove(existingSpec);
            }
            _webSpecs.Specs.Add(webSpec);
        }

        public void Flush()
        {
            using (var file = File.Create("WebSpecs.bin"))
            {
                Serializer.Serialize(file, _webSpecs);
            }
        }

        public void Clear()
        {
            Specs.Clear();
        }

        public void Delete(WebSpec webSpec)
        {
            var existingSpec = _webSpecs.Specs.SingleOrDefault(x => x.Id == webSpec.Id);
            if (existingSpec == null) return;
            _webSpecs.Specs.Remove(existingSpec);
        }
    }
}