using System.Collections.Generic;

namespace GameApi.Dtos.DND2014
{
    public class ClassDto
    {
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int HitDie { get; set; }

        public List<ReferenceItemDto> Proficiencies { get; set; } = new();
        public List<ReferenceItemDto> Subclasses { get; set; } = new();
        public List<StartingEquipmentDto> StartingEquipment { get; set; } = new();
        public List<ProficiencyChoiceDto> ProficiencyChoices { get; set; } = new();
        public MultiClassingDto MultiClassing { get; set; } = new();
    }

    public class ReferenceItemDto
    {
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class StartingEquipmentDto
    {
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class ProficiencyChoiceDto
    {
        public string Desc { get; set; } = string.Empty;
        public int Choose { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    public class MultiClassingDto
    {
        public List<PrerequisiteDto> Prerequisites { get; set; } = new();
        public List<ReferenceItemDto> Proficiencies { get; set; } = new();
    }

    public class PrerequisiteDto
    {
        public string AbilityScore { get; set; } = string.Empty;
        public int MinimumScore { get; set; }
    }
}
