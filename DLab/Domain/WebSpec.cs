using System.Windows.Documents.DocumentStructures;
using DLab.Repositories;
using ProtoBuf;

namespace DLab.Domain
{
    [ProtoContract]
    public class WebSpecs : BaseCollection<WebSpec>
    {
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
    }

    public interface IWeightedCommand
    {
        int Priority { get; set; }
        string Command { get; }
        string Arguments { get; }
        string Target { get; set; }
    }
}