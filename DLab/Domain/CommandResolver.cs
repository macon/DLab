using System;
using System.Collections.Generic;
using System.Linq;
using DLab.Infrastructure;
using DLab.Repositories;

namespace DLab.Domain
{
    public class CommandResolver
    {
        private readonly FileCommandsRepo _fileCommandsRepo;
        private readonly WebSpecRepo _webSpecRepo;
        private readonly RunnerSpecRepo _runnerSpecRepo;

        public CommandResolver(FileCommandsRepo fileCommandsRepo, WebSpecRepo webSpecRepo, RunnerSpecRepo runnerSpecRepo)
        {
            _fileCommandsRepo = fileCommandsRepo;
            _webSpecRepo = webSpecRepo;
            _runnerSpecRepo = runnerSpecRepo;
        }

        public List<MatchResult> GetMatches(string text, int pageSize = 5)
        {
            var webCommands = _webSpecRepo.Specs
                .Where(x => x.Command.StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
                .Take(pageSize);
//                .OrderByDescending(x => x.Priority);

            var fileCommands = _fileCommandsRepo.Specs
                .Where(x => x.Command.StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
                .Take(pageSize);
//                .OrderByDescending(x => x.Priority);

            var runnerCommands = _runnerSpecRepo.Specs
                .Where(x => x.Command.StartsWith(text, StringComparison.InvariantCultureIgnoreCase))
                .Take(pageSize);
//                .OrderByDescending(x => x.Priority);

            var r1 = webCommands.Select(x => new MatchResult(x) { CommandType = CommandType.Uri, Icon = DefaultSystemBrowser.IconImage });
            var r2 = runnerCommands.Select(x => new MatchResult(x) {CommandType = CommandType.File});
            var r3 = fileCommands.Select(x => new MatchResult(x) { CommandType = CommandType.File });
            var r4 = r1.Concat(r2).Concat(r3);

            return r4.OrderByDescending(x => x.Priority).Take(pageSize).ToList();
        }

        public void Save(EntityBase entity)
        {
            var spec = entity as WebSpec;
            if (spec != null)
            {
                _webSpecRepo.Save(spec);
            }
            else
            {
                var spec2 = entity as CatalogEntry;
                if (spec2 != null)
                {
                    _fileCommandsRepo.Save(spec2);
                }
                else
                {
                    _runnerSpecRepo.Save((RunnerSpec)entity);
                }
            }
        }

        public void Flush()
        {
            _webSpecRepo.Flush();
            _fileCommandsRepo.Flush();
        }
    }
}
