using TagTool.Cache;
using TagTool.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace TagTool.Commands.Porting
{
    public class OpenCacheFileCommand : Command
    {
        private CommandContextStack ContextStack { get; }
        private GameCache Cache { get; }

        public OpenCacheFileCommand(CommandContextStack contextStack, GameCache cache)
            : base(false,

                  "OpenCacheFile",
                  "Opens a porting context with the specified cache file.",

                  "OpenCacheFile <Cache File>",

                  "Opens a porting context with the specified cache file.")
        {
            ContextStack = contextStack;
            Cache = cache;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count < 1)
                return false;

            var memory = false;

            while (args.Count > 1)
            {
                switch (args[0].ToLower())
                {
                    case "memory":
                        memory = true;
                        break;

                    default:
                        throw new FormatException(args[0]);
                }

                args.RemoveAt(0);
            }

            var fileName = new FileInfo(args[0]);

            if (!fileName.Exists)
            {
                Console.WriteLine($"Cache {fileName.FullName} does not exist");
                return true;
            }
                
            Console.Write("Loading cache...");

            GameCache cache = GameCache.Open(fileName);

            ContextStack.Push(PortingContextFactory.Create(ContextStack, Cache, cache));

            Console.WriteLine("done.");

            return true;
        }
    }
}

