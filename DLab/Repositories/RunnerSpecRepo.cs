using DLab.Domain;

namespace DLab.Repositories
{
    public class RunnerSpecRepo : BaseRepo<RunnerSpec>
    {
        public RunnerSpecRepo()
        {
            StorageName = "RunnerSpecs.bin";
        }
    }
}