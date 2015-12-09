using System.Collections.Generic;
using DLab.Domain;
using ProtoBuf;

namespace DLab.Repositories
{
    [ProtoContract]
    public class BaseCollection<T> where T: EntityBase
    {
        [ProtoMember(1)]
        public List<T> Specs { get; set; }

        public BaseCollection()
        {
            Specs = new List<T>();
        }
    }
}