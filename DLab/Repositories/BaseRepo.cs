using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLab.Domain;
using ProtoBuf;

namespace DLab.Repositories
{
    public class BaseRepo<T> where T : EntityBase
    {
        private BaseCollection<T> _specs { get; set; }
        protected string StorageName { get; set; }

        public List<T> Specs
        {
            get
            {
                if (_specs != null) return _specs.Specs;
                if (!File.Exists(StorageName))
                {
                    _specs = new BaseCollection<T>();
                    return _specs.Specs;
                }

                using (var file = File.OpenRead(StorageName))
                {
                    _specs = Serializer.Deserialize<BaseCollection<T>>(file);
                    OnDataLoaded();
                }
                return _specs.Specs;
            }
        }

        protected virtual void OnDataLoaded()
        {
        }

        public void Save(T spec)
        {
            var existingSpec = _specs.Specs.SingleOrDefault(x => x.Id == spec.Id);
            if (existingSpec != null)
            {
                _specs.Specs.Remove(existingSpec);
            }
            _specs.Specs.Add(spec);
        }

        public void Flush()
        {
            using (var file = File.Create(StorageName))
            {
                Serializer.Serialize(file, _specs);
            }
        }

        public void Clear()
        {
            Specs.Clear();
        }

        public void Delete(T spec)
        {
            var existingSpec = _specs.Specs.SingleOrDefault(x => x.Id == spec.Id);
            if (existingSpec == null) return;
            _specs.Specs.Remove(existingSpec);
        }
    }
}