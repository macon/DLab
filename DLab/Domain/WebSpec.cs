using System.Collections.Generic;
using System.Windows.Documents.DocumentStructures;
using ProtoBuf;

namespace DLab.Domain
{
    [ProtoContract]
    public class WebSpecs
    {
        [ProtoMember(1)]
        public List<WebSpec> Specs { get; set; }

        public WebSpecs()
        {
            Specs = new List<WebSpec>();
        }
    }

    [ProtoContract]
    public class WebSpec : EntityBase
    {
        public string Uri
        {
            get { return Target; }
            set { Target = value; }
        }

        public WebSpec()
        {
            Uri = "";
        }

        public WebSpec(string command, string uri)
        {
            Command = command;
            Uri = uri;
        }

        public void SetId()
        {
            if (Id == default(int))
            {
                Id = Command.GetHashCode();
            }
        }
    }

    public interface IWeightedCommand
    {
        int Priority { get; set; }
        string Command { get; }
        string Arguments { get; }
        string Target { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(100, typeof(CatalogEntry))]
    [ProtoInclude(101, typeof(WebSpec))]
    public class EntityBase : IWeightedCommand
    {
        public EntityBase()
        {
            Command = "";
            Target = "";
        }
        [ProtoMember(1)]
        public virtual int Id { get; set; }
        [ProtoMember(2)]
        public virtual int Priority { get; set; }
        [ProtoMember(3)]
        public virtual string Command { get; set; }
        [ProtoMember(4)]
        public virtual string Target { get; set; }
        [ProtoMember(5)]
        public string Arguments { get; set; }
    }
}