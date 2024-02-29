using TagTool.Cache;
using TagTool.Cache.HaloOnline;
using TagTool.Common;
using TagTool.Tags.Definitions;
using System.IO;
using System.Collections.Generic;

namespace TagTool.MtnDewIt.Commands.GenerateCache.Tags 
{
    public class ai_squad_templates_sq_camp_4_brute_squad_template : TagFile
    {
        public ai_squad_templates_sq_camp_4_brute_squad_template(GameCache cache, GameCacheHaloOnline cacheContext, Stream stream) : base
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
            var tag = GetCachedTag<SquadTemplate>($@"ai\squad_templates\sq_camp_4_brute");
            var sqtm = CacheContext.Deserialize<SquadTemplate>(Stream, tag);
            sqtm.Name = CacheContext.StringTable.GetStringId($@"sq_camp_brute_4");
            sqtm.CellTemplates = new List<SquadTemplate.CellTemplate>
            {
                new SquadTemplate.CellTemplate
                {
                    Name = CacheContext.StringTable.GetStringId($@"4_brute"),
                    NormalDiffCount = 4,
                    Characters = new List<SquadTemplate.CellTemplate.ObjectBlock>
                    {
                        new SquadTemplate.CellTemplate.ObjectBlock
                        {
                            Probability = 1,
                        },
                    },
                    InitialWeapons = new List<SquadTemplate.CellTemplate.ObjectBlock>
                    {
                        new SquadTemplate.CellTemplate.ObjectBlock
                        {
                            Object = GetCachedTag<Weapon>($@"objects\weapons\rifle\spike_rifle\spike_rifle"),
                            Probability = 10,
                        },
                        new SquadTemplate.CellTemplate.ObjectBlock
                        {
                            Probability = 10,
                        },
                        new SquadTemplate.CellTemplate.ObjectBlock
                        {
                            Object = GetCachedTag<Weapon>($@"objects\weapons\pistol\needler\needler"),
                            Probability = 3,
                        },
                        new SquadTemplate.CellTemplate.ObjectBlock
                        {
                            Object = GetCachedTag<Weapon>($@"objects\weapons\pistol\excavator\excavator"),
                            Probability = 1,
                        },
                    },
                },
            };
            CacheContext.Serialize(Stream, tag, sqtm);
        }
    }
}
