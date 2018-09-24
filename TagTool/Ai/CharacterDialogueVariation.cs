using TagTool.Cache;
using TagTool.Common;
using TagTool.Serialization;

namespace TagTool.Ai
{
    [TagStructure(Size = 0x18)]
    public class CharacterDialogueVariation : TagStructure
	{
        public CachedTagInstance Dialogue;
        [TagField(Label = true)]
        public StringId Name;
        public float Weight;
    }
}
