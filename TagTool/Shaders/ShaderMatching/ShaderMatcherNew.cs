﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagTool.Cache;
using TagTool.Tags.Definitions;
using static TagTool.Tags.Definitions.RenderMethodTemplate;

namespace TagTool.Shaders.ShaderMatching
{
    public class ShaderMatcherNew
    {
        private GameCache BaseCache;
        private GameCache PortingCache;
        private Stream BaseCacheStream;
        private Stream PortingCacheStream;
        private Dictionary<CachedTag, RenderMethodTemplate> _rmt2Cache;

        public static string DefaultTemplate => @"shaders\shader_templates\_0_0_0_0_0_0_0_0_0_0_0.rmt2";
        public bool IsInitialized { get; private set; } = false;
        public bool UseMs30 { get; set; } = false;
        public bool PerfectMatchesOnly { get; set; } = false;

        static Dictionary<string, int[]> MethodWeights = new Dictionary<string, int[]>()
        {
           ["default"] = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
           ["shader"] = new int[] { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 }
        };

        

        public ShaderMatcherNew()
        {
        }

        public void Init(GameCache baseCache, GameCache portingCache, Stream baseCacheStream, Stream portingCacheStream, bool useMS30 = false, bool perfectMatchesOnly = false)
        {
            UseMs30 = useMS30;
            PerfectMatchesOnly = perfectMatchesOnly;
            BaseCache = baseCache;
            PortingCache = portingCache;
            BaseCacheStream = baseCacheStream;
            PortingCacheStream = portingCacheStream;
            _rmt2Cache = new Dictionary<CachedTag, RenderMethodTemplate>();
            IsInitialized = true;
        }

        /// <summary>
        /// Find the closest template in the base cache to the input template.
        /// </summary>
        public CachedTag FindClosestTemplate(CachedTag sourceRmt2Tag, RenderMethodTemplate sourceRmt2)
        {
            Debug.Assert(IsInitialized);

            Rmt2Descriptor sourceRmt2Desc;
            if (!Rmt2Descriptor.TryParse(sourceRmt2Tag.Name, out sourceRmt2Desc))
                throw new ArgumentException($"Invalid rmt2 name '{sourceRmt2Tag.Name}'", nameof(sourceRmt2Tag));

            var relevantRmt2s = new List<Rmt2Pairing>();

            Dictionary<CachedTag, long> ShaderTemplateValues = new Dictionary<CachedTag, long>();

            ShaderSorter shaderTemplateSorter = new ShaderSorter();
            HalogramSorter halogramTemplateSorter = new HalogramSorter();
            TerrainSorter terrainTemplateSorter = new TerrainSorter();
            FoliageSorter foliageTemplateSorter = new FoliageSorter();

            foreach (var rmt2Tag in BaseCache.TagCache.NonNull().Where(tag => tag.IsInGroup("rmt2")))
            {
                Rmt2Descriptor destRmt2Desc;
                if (!Rmt2Descriptor.TryParse(rmt2Tag.Name, out destRmt2Desc))
                    continue;

                // ignore ms30 templates if desired
                if (!UseMs30 && destRmt2Desc.IsMs30)
                    continue;

                // ignore templates that are not of the same type
                if (destRmt2Desc.Type != sourceRmt2Desc.Type)
                    continue;

                int[] weights;
                if (!MethodWeights.TryGetValue(sourceRmt2Desc.Type, out weights))
                    weights = MethodWeights["default"];

                // match the options from the rmt2 tag names
                int commonOptions = 0;
                int score = 0;
                for (int i = 0; i < sourceRmt2Desc.Options.Length; i++)
                {
                    if (sourceRmt2Desc.Options[i] == destRmt2Desc.Options[i])
                    {
                        score += 1 + weights[i];
                        commonOptions++;
                    }
                }

                // if we found an exact match, return it
                if (commonOptions == sourceRmt2Desc.Options.Length)
                {
                    Console.WriteLine("Found perfect rmt2 match:");
                    Console.WriteLine(sourceRmt2Tag.Name);
                    Console.WriteLine(rmt2Tag.Name);
                    return rmt2Tag;
                }
                    

                // add it to the list to be considered
                relevantRmt2s.Add(new Rmt2Pairing()
                {
                    Score = score,
                    CommonOptions = commonOptions,
                    DestTag = rmt2Tag,
                    SourceTag = sourceRmt2Tag
                });

                switch (sourceRmt2Desc.Type)
                {
                    case "shader":
                        ShaderTemplateValues.Add(rmt2Tag, Sorter.GetValue(shaderTemplateSorter, Sorter.GetTemplateOptions(rmt2Tag.Name)));
                        break;
                    case "halogram":
                        ShaderTemplateValues.Add(rmt2Tag, Sorter.GetValue(halogramTemplateSorter, Sorter.GetTemplateOptions(rmt2Tag.Name)));
                        break;
                    case "terrain":
                        ShaderTemplateValues.Add(rmt2Tag, Sorter.GetValue(terrainTemplateSorter, Sorter.GetTemplateOptions(rmt2Tag.Name)));
                        break;
                    case "foliage":
                        ShaderTemplateValues.Add(rmt2Tag, Sorter.GetValue(foliageTemplateSorter, Sorter.GetTemplateOptions(rmt2Tag.Name)));
                        break;
                }
            }

            // if we've reached here, we haven't found an extract match.
            // now we need to consider other factors such as which options they have, which parameters are missing etc..
            // whatever can be used to narrow it down.
            Console.WriteLine($"No rmt2 match found for {sourceRmt2Tag.Name}");

            if (PerfectMatchesOnly)
                return null;

            // find closest rmt2

            switch (sourceRmt2Desc.Type)
            {
                case "shader":      return GetBestTag(shaderTemplateSorter, ShaderTemplateValues, sourceRmt2Tag);
                case "halogram":    return GetBestTag(halogramTemplateSorter, ShaderTemplateValues, sourceRmt2Tag);
                case "terrain":     return GetBestTag(terrainTemplateSorter, ShaderTemplateValues, sourceRmt2Tag);
                case "foliage":     return GetBestTag(foliageTemplateSorter, ShaderTemplateValues, sourceRmt2Tag);
                default:            return null;
            }
        }


        /// <summary>
        /// Returns the best render_method_template tag match using the given dictionary and source rmt2
        /// </summary>
        private CachedTag GetBestTag(SortingInterface sortingInterface, Dictionary<CachedTag, long> shaderTemplateValues, CachedTag sourceRmt2Tag)
        {
            long targetValue = Sorter.GetValue(sortingInterface, Sorter.GetTemplateOptions(sourceRmt2Tag.Name));
            long bestValue = long.MaxValue;
            CachedTag bestTag = null;

            foreach (var pair in shaderTemplateValues)
            {
                if (Math.Abs(pair.Value - targetValue) < bestValue)
                {
                    bestValue = Math.Abs(pair.Value - targetValue);
                    bestTag = pair.Key;
                }
            }

            Console.WriteLine($"Closest tag to {sourceRmt2Tag.Name} with options and value {targetValue}");
            sortingInterface.PrintOptions(Sorter.GetTemplateOptions(sourceRmt2Tag.Name));
            Console.WriteLine($"is tag {bestTag.Name} with options and value {bestValue + targetValue}");
            sortingInterface.PrintOptions(Sorter.GetTemplateOptions(bestTag.Name));

            return bestTag;
        }

        /// <summary>
        /// Modifies the input render method to make it work using the matchedTemplate
        /// </summary>
        private RenderMethod MatchRenderMethods(RenderMethod renderMethod, RenderMethodTemplate matchedTemplate, RenderMethodTemplate originalTemplate)
        {

            return renderMethod;
        }

        private Rmt2ParameterMatch MatchParameterBlocks(List<ShaderArgument> sourceBlock, List<ShaderArgument> destBlock)
        {
            var result = new Rmt2ParameterMatch();

            var destNames = destBlock.Select(x => BaseCache.StringTable.GetString(x.Name));
            var sourceNames = sourceBlock.Select(x => PortingCache.StringTable.GetString(x.Name));

            result.SourceCount = sourceNames.Count();
            result.DestCount = destNames.Count();
            result.MissingFromDest = sourceNames.Except(destNames).Count();
            result.MissingFromSource = destNames.Except(sourceNames).Count();
            result.Common = destNames.Intersect(sourceNames).Count();

            return result;
        }

        private RenderMethodTemplate GetTemplate(CachedTag tag)
        {
            RenderMethodTemplate template;
            if (!_rmt2Cache.TryGetValue(tag, out template))
                template = _rmt2Cache[tag] = BaseCache.Deserialize<RenderMethodTemplate>(BaseCacheStream, tag);

            return template;
        }

        class Rmt2Pairing
        {
            public Rmt2ParameterMatch RealParams;
            public Rmt2ParameterMatch IntParams;
            public Rmt2ParameterMatch BoolParams;
            public Rmt2ParameterMatch TextureParams;
            public int CommonOptions;
            public int Score;
            public CachedTag DestTag;
            public CachedTag SourceTag;

            public int CommonParameters =>
                  RealParams.Common
                + TextureParams.Common
                + IntParams.Common
                + BoolParams.Common;

            public int MissingFromSource =>
                  RealParams.MissingFromSource
                + TextureParams.MissingFromSource
                + IntParams.MissingFromSource
                + BoolParams.MissingFromSource;

            public int MissingFromDest =>
                  RealParams.MissingFromDest
                + TextureParams.MissingFromDest
                + IntParams.MissingFromDest
                + BoolParams.MissingFromDest;
        }

        struct Rmt2ParameterMatch
        {
            public int MissingFromSource;
            public int MissingFromDest;
            public int Common;
            public int SourceCount;
            public int DestCount;
        }

        public struct Rmt2Descriptor
        {
            public DescriptorFlags Flags;
            public string Type;
            public byte[] Options;

            [Flags]
            public enum DescriptorFlags
            {
                Ms30 = (1 << 0)
            }

            public bool IsMs30 => Flags.HasFlag(DescriptorFlags.Ms30);

            public static bool TryParse(string name, out Rmt2Descriptor descriptor)
            {
                descriptor = new Rmt2Descriptor();

                var parts = name.Split(new string[] { "shaders\\" }, StringSplitOptions.None);

                var prefixParts = parts[0].Split('\\');
                if (prefixParts.Length > 0 && prefixParts[0] == "ms30")
                    descriptor.Flags |= DescriptorFlags.Ms30;

                var nameParts = parts[1].Split('\\');
                if (nameParts.Length < 2)
                    return false;

                descriptor.Type = nameParts[0].Substring(0, nameParts[0].Length-10);
                descriptor.Options = nameParts[1].Split('_').Skip(1).Select(x => byte.Parse(x)).ToArray();

                return true;
            }
        }

        public CachedTag FindRmdf(CachedTag matchedRmt2Tag)
        {
            Rmt2Descriptor rmt2Description;
            if (!Rmt2Descriptor.TryParse(matchedRmt2Tag.Name, out rmt2Description))
                throw new ArgumentException($"Invalid rmt2 name '{matchedRmt2Tag.Name}'", nameof(matchedRmt2Tag));

            string prefix = matchedRmt2Tag.Name.StartsWith("ms30") ? "ms30\\" : "";
            string type = rmt2Description.Type; // remove _templates
            string rmdfName = $"{prefix}shaders\\{type}";

            return BaseCache.TagCache.GetTag(rmdfName, "rmdf");
        }


        private List<string> GetRenderMethodDefinitionMethods(RenderMethodDefinition rmdf, GameCache cache)
        {
            var result = new List<string>();
            foreach(var method in rmdf.Methods)
            {
                var str = cache.StringTable.GetString(method.Type);
                result.Add(str);
            }
            return result;
        }

        private List<string> GetMethodOptions(RenderMethodDefinition rmdf, int methodIndex, GameCache cache)
        {
            var result = new List<string>();
            var method = rmdf.Methods[methodIndex];
            foreach(var option in method.ShaderOptions)
            {
                result.Add(cache.StringTable.GetString(option.Type));
            }
            return result;
        }

        private void FindClosestShaderTemplate(CachedTag sourceRmt2)
        {
            // somehow build a list of rmt2 of the same type
            List<CachedTag> candidateTemplates = new List<CachedTag>();

            // defined method search order, ignore last method from ms30
            List<int> methodOrder = new List<int> {0, 2, 3, 1, 6, 4, 5, 7, 8, 9, 10};

            Dictionary<CachedTag, int> matchLevelDictionary = new Dictionary<CachedTag, int>();
            Rmt2Descriptor sourceRmt2Desc;
            if (!Rmt2Descriptor.TryParse(sourceRmt2.Name, out sourceRmt2Desc))
                return;

            while (candidateTemplates.Count != 0)
            {
                var template = candidateTemplates.Last();
                Rmt2Descriptor destRmt2Desc;
                if (!Rmt2Descriptor.TryParse(template.Name, out destRmt2Desc))
                {
                    candidateTemplates.Remove(template);
                    matchLevelDictionary[template] = 0;
                }
                else
                {
                    var matchLevel = 0;
                    for(int i = 0; i < methodOrder.Count; i++)
                    {
                        var methodIndex = methodOrder[i];
                        // we need to define a ordering on the method options, so that there is a single best rmt2
                        if (sourceRmt2Desc.Options[methodIndex] == destRmt2Desc.Options[methodIndex])
                            matchLevel++;
                        else
                            break;
                    }
                    matchLevelDictionary[template] = matchLevel;
                } 
            }

            CachedTag bestRmt2 = null;
            var bestScore = -1;

        }
    }
}
