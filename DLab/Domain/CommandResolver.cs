using System;
using System.Collections.Generic;
using System.Linq;
using DLab.Infrastructure;

namespace DLab.Domain
{
    public class CommandResolver
    {
        private readonly FileCommandsRepo _fileCommandsRepo;
        private readonly WebSpecRepo _webSpecRepo;

        public CommandResolver(FileCommandsRepo fileCommandsRepo, WebSpecRepo webSpecRepo)
        {
            _fileCommandsRepo = fileCommandsRepo;
            _webSpecRepo = webSpecRepo;
        }

        public List<MatchResult> GetMatches(string text, int pageSize = 5)
        {
            var webCommands = _webSpecRepo.Specs
                .Where(x => x.Command.StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
                .Take(pageSize)
                .OrderByDescending(x => x.Priority);

            var fileCommands = _fileCommandsRepo.Files
                .Where(x => x.Command.StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
                .Take(pageSize)
                .OrderByDescending(x => x.Priority);

            var r1 = webCommands.Select(x => new MatchResult(x) { CommandType = CommandType.Uri, Icon = DefaultSystemBrowser.IconImage });
            var r2 = r1.Concat(fileCommands.Select(x => new MatchResult(x) { CommandType = CommandType.File }));

            return r2.Take(pageSize).ToList();
        }

        public void Save(EntityBase p0)
        {
            var spec = p0 as WebSpec;
            if (spec != null)
            {
                _webSpecRepo.Save(spec);
            }
            else
            {
                _fileCommandsRepo.Save((CatalogEntry) p0);
            }
        }

        public void Flush()
        {
            _webSpecRepo.Flush();
            _fileCommandsRepo.Flush();
        }
    }
}
