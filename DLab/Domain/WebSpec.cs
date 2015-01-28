namespace DLab.Domain
{

    public class WebSpec : ISetPriority
    {
        public WebSpec()
        {
        }

        public WebSpec(string command, string uri)
        {
            Command = command;
            Uri = uri;
        }

        public int Id { get; set; }
        public string Command { get; set; }
        public string Uri { get; set; }
        public int Priority { get; set; }

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
    }
}