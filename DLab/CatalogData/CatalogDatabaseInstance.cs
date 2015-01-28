using System.Collections.Generic;
using DLab.Domain;
using DLab.ViewModels;
using Wintellect.Sterling.Database;

namespace DLab.CatalogData
{
    public class CatalogDatabaseInstance : BaseDatabaseInstance
    {
        public static string IdxFilename = "Filename";
        public static string IdxWebCommand = "WebCommand";

        public override string Name
        {
            get { return "CatalogDatabase"; }
        }

        protected override List<ITableDefinition> RegisterTables()
        {
            return new List<ITableDefinition>
                       {
                           CreateTableDefinition<CatalogEntry, int>(x => x.Id)
                                .WithIndex<CatalogEntry, string, int>(IdxFilename, x => x.Filename),

                           CreateTableDefinition<FolderSpec, int>(x => x.Id),

                           CreateTableDefinition<WebSpec, int>(x => x.Id)
                                .WithIndex<WebSpec, string, int>(IdxWebCommand, x => x.Command)
                       };
        }
    }


}
