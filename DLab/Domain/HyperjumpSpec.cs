using ProtoBuf;

namespace DLab.Domain
{
    [ProtoContract]
    public class HyperjumpSpec : IIdentityEntity
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string Path { get; set; }

        [ProtoMember(3)]
        public bool Exclude { get; set; }

        public virtual void SetId()
        {
            if (Id == default(int))
            {
                Id = Path.GetHashCode();
            }
        }
    }
}