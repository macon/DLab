using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;

namespace DLab.Domain
{
    public interface IRepository<TEntity> where TEntity : EntityBase
    {
        List<TEntity> Items { get; }
        void Save(TEntity item);
        void ReplaceAll(IList<TEntity> items);
        void Flush();
        void Clear();
        void Delete(TEntity item);
    }

    public abstract class RepoBase<TDb, TEntity> : IRepository<TEntity>
        where TDb : IDb<TEntity>, new()
        where TEntity : EntityBase
    {
        private TDb _db;

        protected string Filename { get; set; }

        public RepoBase()
        {
            
        }

        public List<TEntity> Items
        {
            get
            {
                if (_db != null) return _db.Items;

                if (!File.Exists(Filename))
                {
                    _db = new TDb();
                    return _db.Items;
                }

                using (var file = File.OpenRead(Filename))
                {
                    _db = Serializer.Deserialize<TDb>(file);
                }
                return _db.Items;
            }
        }

        public void Save(TEntity item)
        {
            var existingItem = _db.Items.SingleOrDefault(x => x.Id == item.Id);
            if (existingItem != null)
            {
                _db.Items.Remove(existingItem);
            }
            _db.Items.Add(item);
        }

        public void ReplaceAll(IList<TEntity> items)
        {
            _db.Items.Clear();
            _db.Items.AddRange(items);
        }

        public void Flush()
        {
            using (var file = File.Create(Filename))
            {
                Serializer.Serialize(file, _db);
            }
        }

        public void Clear()
        {
            _db.Items.Clear();
        }

        public void Delete(TEntity item)
        {
            var existingSpec = _db.Items.SingleOrDefault(x => x.Id == item.Id);
            if (existingSpec == null) return;
            _db.Items.Remove(existingSpec);
        }
    }

    public class ConsoleRepo : RepoBase<ConsoleDb, Console>
    {
        public ConsoleRepo()
        {
            Filename = "Consoles.bin";
        }
    }

    public interface IDb<T> where T: EntityBase
    {
        List<T> Items { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(150, typeof(ConsoleDb))]
    public class Db<T> : IDb<T> where T : EntityBase
    {
        public Db()
        {
            Items = new List<T>();
        }

        [ProtoMember(1)]
        public List<T> Items { get; set; }
    }

    [ProtoContract]
    public class ConsoleDb : Db<Console>
    {
    }

    [ProtoContract]
    public class Console : EntityBase
    {
        [ProtoMember(10)]
        public char Hotkey { get; set; }
    }
}
