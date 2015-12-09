using DLab.Domain;

namespace DLab.Repositories
{
    public class WebSpecRepo : BaseRepo<WebSpec>
    {
        public WebSpecRepo()
        {
            StorageName = "WebSpecs.bin";
        }
    }
}