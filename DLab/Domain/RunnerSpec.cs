using System.Collections.Generic;
using DLab.Repositories;
using ProtoBuf;

namespace DLab.Domain
{
    [ProtoContract]
    public class RunnerSpecs : BaseCollection<RunnerSpec>
    {
    }

    [ProtoContract]
    public class RunnerSpec : EntityBase
    {
        [ProtoMember(6)]
        public string Description { get; set; }
    }
}