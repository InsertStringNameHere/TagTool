using TagTool.Cache;
using TagTool.Cache.HaloOnline;
using TagTool.Common;
using TagTool.Tags.Definitions;
using System.IO;
using TagTool.Commands;

namespace TagTool.MtnDewIt.Commands.GenerateCache.Tags 
{
    public class ui_eldewrito_common_map_bitmaps_005_intro_bitmap : TagFile
    {
        public ui_eldewrito_common_map_bitmaps_005_intro_bitmap(GameCache cache, GameCacheHaloOnline cacheContext, Stream stream) : base
        (
            cache,
            cacheContext,
            stream
        )
        {
            Cache = cache;
            CacheContext = cacheContext;
            Stream = stream;
            TagData();
        }

        public override void TagData()
        {
            var tag = GetCachedTag<Bitmap>($@"ui\eldewrito\common\map_bitmaps\005_intro");
            var bitm = CacheContext.Deserialize<Bitmap>(Stream, tag);
            AddBitmap(bitm, 0, $@"{Program.TagToolDirectory}\Tools\BaseCache\Images\Maps\005_intro.dds");
            CacheContext.Serialize(Stream, tag, bitm);
        }
    }
}
