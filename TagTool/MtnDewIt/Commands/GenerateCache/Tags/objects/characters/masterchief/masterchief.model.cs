using TagTool.Cache;
using TagTool.Cache.HaloOnline;
using TagTool.Common;
using TagTool.Tags.Definitions;
using System.IO;

namespace TagTool.MtnDewIt.Commands.GenerateCache.Tags 
{
    public class objects_characters_masterchief_masterchief_model : TagFile
    {
        public objects_characters_masterchief_masterchief_model(GameCache cache, GameCacheHaloOnline cacheContext, Stream stream) : base
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
            var tag = GetCachedTag<Model>($@"objects\characters\masterchief\masterchief");
            var hlmt = CacheContext.Deserialize<Model>(Stream, tag);
            hlmt.ShieldImpactThirdPerson = GetCachedTag<ShieldImpact>($@"globals\masterchief_3p_shield_impact");
            hlmt.ShieldImpactFirstPerson = GetCachedTag<ShieldImpact>($@"globals\masterchief_fp_shield_impact");
            hlmt.OvershieldFirstPerson = GetCachedTag<ShieldImpact>($@"fx\shield_impacts\overshield_1p");
            hlmt.OvershieldThirdPerson = GetCachedTag<ShieldImpact>($@"fx\shield_impacts\overshield_3p");
            CacheContext.Serialize(Stream, tag, hlmt);
        }
    }
}
