using System.IO;
using TagTool.Cache.HaloOnline;
using TagTool.Cache;
using TagTool.Tags.Definitions;
using TagTool.MtnDewIt.BlamFiles;

namespace TagTool.MtnDewIt.Commands.ConvertCache.Maps 
{
    public class @mainmenu : MapVariantFile 
    {
        public @mainmenu(GameCache cache, GameCacheHaloOnline cacheContext, Stream stream) : base
        (
            cache,
            cacheContext,
            stream
        )
        {
            Cache = cache;
            CacheContext = cacheContext;
            Stream = stream;
            MapVariantData();
        }

        public override void MapVariantData() 
        {
            var tag = GetCachedTag<Scenario>($@"levels\ui\mainmenu\mainmenu");
            var scnr = CacheContext.Deserialize<Scenario>(Stream, tag);

            BlfData blfData = null;

            GenerateMapFile(tag, scnr, blfData);
        }
    }
}