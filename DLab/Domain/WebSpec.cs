namespace DLab.Domain
{

    public class WebSpec : EntityBase
    {
        public WebSpec()
        {
            Uri = "";
        }

        public WebSpec(string command, string uri)
        {
            Command = command;
            Uri = uri;
        }

        public int Id { get; set; }
        public string Uri { get; set; }
        public override string Target { get { return Uri; } }

        public void SetId()
        {
            if (Id == default(int))
            {
                Id = Command.GetHashCode();
            }
        }
    }

    public interface ISetPriority
    {
        int Priority { get; set; }
        string Command { get; }
        string Target { get; set; }
    }

    public class EntityBase : ISetPriority
    {
        public EntityBase()
        {
            Command = "";
            Target = "";
        }
        public virtual int Priority { get; set; }
        public virtual string Command { get; set; }
        public virtual string Target { get; set; }
    }
}