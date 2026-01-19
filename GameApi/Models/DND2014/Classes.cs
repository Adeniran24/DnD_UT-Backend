using System.Collections.Generic;

namespace GameApi.Models.DND2014
{
    public class Class
    {
        public int Id { get; set; } // PK for EF Core
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int HitDie { get; set; }

        // Relationships
        public ICollection<Proficiency> Proficiencies { get; set; } = new List<Proficiency>();
        public ICollection<Subclass> Subclasses { get; set; } = new List<Subclass>();
        public ICollection<StartingEquipment> StartingEquipment { get; set; } = new List<StartingEquipment>();
        public MultiClassing MultiClassing { get; set; } = null!;
        public ICollection<ProficiencyChoice> ProficiencyChoices { get; set; } = new List<ProficiencyChoice>();
    }

    public class Proficiency
    {
        public int Id { get; set; }
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;
    }

    public class Subclass
    {
        public int Id { get; set; }
        public string Index { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;
    }

    public class StartingEquipment
    {
        public int Id { get; set; }
        public string Index { get; set; } = string.Empty; // equipment index
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int Quantity { get; set; }

        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;
    }

    public class ProficiencyChoice
    {
        public int Id { get; set; }
        public string Desc { get; set; } = string.Empty;
        public int Choose { get; set; }
        public string Type { get; set; } = string.Empty;

        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;
    }

    public class MultiClassing
    {
        public int Id { get; set; }
        public ICollection<Prerequisite> Prerequisites { get; set; } = new List<Prerequisite>();
        public ICollection<Proficiency> Proficiencies { get; set; } = new List<Proficiency>();

        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;
    }

    public class Prerequisite
    {
        public int Id { get; set; }
        public string AbilityScore { get; set; } = string.Empty; // STR, DEX, etc.
        public int MinimumScore { get; set; }

        public int MultiClassingId { get; set; }
        public MultiClassing MultiClassing { get; set; } = null!;
    }
}
