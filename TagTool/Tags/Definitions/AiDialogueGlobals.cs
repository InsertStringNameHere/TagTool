using TagTool.Ai;
using TagTool.Cache;
using TagTool.Common;
using System.Collections.Generic;
using static TagTool.Tags.TagFieldFlags;

namespace TagTool.Tags.Definitions
{
    [TagStructure(Name = "ai_dialogue_globals", Tag = "adlg", Size = 0x4C, MaxVersion = CacheVersion.Halo3Retail)]
    [TagStructure(Name = "ai_dialogue_globals", Tag = "adlg", Size = 0x50, MaxVersion = CacheVersion.Halo3ODST)]
    [TagStructure(Name = "ai_dialogue_globals", Tag = "adlg", Size = 0x5C, MinVersion = CacheVersion.HaloOnlineED)]
    public class AiDialogueGlobals : TagStructure
    {
        public Bounds<float> StrikeDelayBounds;
        public float RemindDelay;
        public float CoverCurseChance;

        [TagField(MinVersion = CacheVersion.Halo3ODST)]
        public float FaceFriendlyPlayerDistance;

        public List<AiVocalization> Vocalizations;

        public List<AiDialoguePattern> Patterns;

        [TagField(Flags = Padding, Length = 12)]
        public byte[] Unused1;

        public List<DialogDatum> DialogData;

        public List<InvoluntaryDatum> InvoluntaryData;

        [TagField(Flags = Padding, Length = 12, MinVersion = CacheVersion.HaloOnlineED)]
        public byte[] Unused2;
        
        [TagStructure(Size = 0x4)]
        public class DialogDatum : TagStructure
		{
            public short StartIndex;
            public short Length;
        }

        [TagStructure(Size = 0x4)]
        public class InvoluntaryDatum : TagStructure
		{
            public short InvoluntaryVocalizationIndex;

            [TagField(Flags = Padding, Length = 2)]
            public byte[] Unused;
        }
    }
}
