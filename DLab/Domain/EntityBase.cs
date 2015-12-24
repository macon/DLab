using ProtoBuf;

namespace DLab.Domain
{

    public interface IIdentityEntity
    {
        int Id { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(100, typeof(CatalogEntry))]
    [ProtoInclude(101, typeof(WebSpec))]
    [ProtoInclude(102, typeof(Console))]
    [ProtoInclude(103, typeof(RunnerSpec))]
    public class EntityBase : IIdentityEntity, IWeightedCommand
    {
        public EntityBase()
        {
            Command = "";
            Target = "";
            Arguments = "";
        }

        [ProtoMember(2)]
        public virtual int Priority { get; set; }
        [ProtoMember(3)]
        public virtual string Command { get; set; }
        [ProtoMember(4)]
        public virtual string Target { get; set; }
        [ProtoMember(5)]
        public virtual string Arguments { get; set; }

        public virtual void SetId()
        {
            if (Id == default(int))
            {
                Id = Command.GetHashCode();
            }
        }

        [ProtoMember(1)]
        public int Id { get; set; }
    }
}