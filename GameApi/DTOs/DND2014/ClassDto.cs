using System.Collections.Generic;

namespace GameApi.Dtos.DND2014
{
    public class ClassDto
    {
        public string Index { get; set; }
        public string Name { get; set; }
        public int HitDie { get; set; }

        public List<ReferenceItemDto> Proficiencies { get; set; } = new();
        public List<ReferenceItemDto> Subclasses { get; set; } = new();
        public List<StartingEquipmentDto> StartingEquipment { get; set; } = new();
        public List<ProficiencyChoiceDto> ProficiencyChoices { get; set; } = new();
        public MultiClassingDto MultiClassing { get; set; }
    }

    public class ReferenceItemDto
    {
        public string Index { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class StartingEquipmentDto
    {
        public string Index { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public int Quantity { get; set; }
    }

    public class ProficiencyChoiceDto
    {
        public string Desc { get; set; }
        public int Choose { get; set; }
        public string Type { get; set; }
    }

    public class MultiClassingDto
    {
        public List<PrerequisiteDto> Prerequisites { get; set; } = new();
        public List<ReferenceItemDto> Proficiencies { get; set; } = new();
    }

    public class PrerequisiteDto
    {
        public string AbilityScore { get; set; }
        public int MinimumScore { get; set; }
    }
}
