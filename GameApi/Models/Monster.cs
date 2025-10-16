using System.Text.Json.Serialization;

namespace GameApi.Models
{
    public class Monster
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        [JsonPropertyName("size")]
        public string Size { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("subtype")]
        public string Subtype { get; set; } = "";

        [JsonPropertyName("alignment")]
        public string Alignment { get; set; } = "";

        [JsonPropertyName("armor_class")]
        public List<ArmorClass> ArmorClass { get; set; } = new();

        [JsonPropertyName("hit_points")]
        public int? HitPoints { get; set; }

        [JsonPropertyName("hit_dice")]
        public string HitDice { get; set; } = "";

        [JsonPropertyName("hit_points_roll")]
        public string HitPointsRoll { get; set; } = "";

        [JsonPropertyName("speed")]
        public Speed Speed { get; set; } = new();

        [JsonPropertyName("strength")]
        public int Strength { get; set; }

        [JsonPropertyName("dexterity")]
        public int Dexterity { get; set; }

        [JsonPropertyName("constitution")]
        public int Constitution { get; set; }

        [JsonPropertyName("intelligence")]
        public int Intelligence { get; set; }

        [JsonPropertyName("wisdom")]
        public int Wisdom { get; set; }

        [JsonPropertyName("charisma")]
        public int Charisma { get; set; }

        [JsonPropertyName("proficiencies")]
        public List<Proficiency> Proficiencies { get; set; } = new();

        [JsonPropertyName("damage_vulnerabilities")]
        public List<string> DamageVulnerabilities { get; set; } = new();

        [JsonPropertyName("damage_resistances")]
        public List<string> DamageResistances { get; set; } = new();

        [JsonPropertyName("damage_immunities")]
        public List<string> DamageImmunities { get; set; } = new();

        [JsonPropertyName("condition_immunities")]
        public List<string> ConditionImmunities { get; set; } = new();

        [JsonPropertyName("senses")]
        public Senses Senses { get; set; } = new();

        [JsonPropertyName("languages")]
        public string Languages { get; set; } = "";

        [JsonPropertyName("challenge_rating")]
        public double? ChallengeRating { get; set; }

        [JsonPropertyName("proficiency_bonus")]
        public int? ProficiencyBonus { get; set; }

        [JsonPropertyName("xp")]
        public int? Xp { get; set; }

        [JsonPropertyName("special_abilities")]
        public List<SpecialAbility> SpecialAbilities { get; set; } = new();

        [JsonPropertyName("actions")]
        public List<Action> Actions { get; set; } = new();

        [JsonPropertyName("legendary_actions")]
        public List<LegendaryAction> LegendaryActions { get; set; } = new();

        [JsonPropertyName("reactions")]
        public List<Reaction> Reactions { get; set; } = new();

        [JsonPropertyName("forms")]
        public List<string> Forms { get; set; } = new();

        [JsonPropertyName("image")]
        public string Image { get; set; } = "";

        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        [JsonPropertyName("updatedAt")]
        public string UpdatedAt { get; set; } = "";
    }

    public class ArmorClass
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }

    public class Speed
    {
        [JsonPropertyName("walk")]
        public string Walk { get; set; } = "";

        [JsonPropertyName("fly")]
        public string Fly { get; set; } = "";

        [JsonPropertyName("swim")]
        public string Swim { get; set; } = "";

        [JsonPropertyName("climb")]
        public string Climb { get; set; } = "";

        [JsonPropertyName("burrow")]
        public string Burrow { get; set; } = "";

        [JsonPropertyName("hover")]
        public bool? Hover { get; set; }
    }

    public class Proficiency
    {
        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("proficiency")]
        public ProficiencyInfo ProficiencyInfo { get; set; } = new();
    }

    public class ProficiencyInfo
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("url")]
        public string Url { get; set; } = "";
    }

    public class Senses
    {
        [JsonPropertyName("darkvision")]
        public string Darkvision { get; set; } = "";

        [JsonPropertyName("blindsight")]
        public string Blindsight { get; set; } = "";

        [JsonPropertyName("tremorsense")]
        public string Tremorsense { get; set; } = "";

        [JsonPropertyName("truesight")]
        public string Truesight { get; set; } = "";

        [JsonPropertyName("passive_perception")]
        public int? PassivePerception { get; set; }
    }

    public class SpecialAbility
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        [JsonPropertyName("dc")]
        public DC? Dc { get; set; }
    }

    public class DC
    {
        [JsonPropertyName("dc_type")]
        public ProficiencyInfo DcType { get; set; } = new();

        [JsonPropertyName("dc_value")]
        public int? DcValue { get; set; }

        [JsonPropertyName("success_type")]
        public string SuccessType { get; set; } = "";
    }

    public class Action
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("multiattack_type")]
        public string MultiattackType { get; set; } = "";

        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        [JsonPropertyName("attack_bonus")]
        public int? AttackBonus { get; set; }

        [JsonPropertyName("dc")]
        public DC? Dc { get; set; }

        [JsonPropertyName("damage")]
        public List<Damage> Damage { get; set; } = new();

        [JsonPropertyName("actions")]
        public List<SubAction> Actions { get; set; } = new();

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    public class SubAction
    {
        [JsonPropertyName("action_name")]
        public string ActionName { get; set; } = "";

        [JsonPropertyName("count")]
        public int? Count { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
    }

    public class Damage
    {
        [JsonPropertyName("damage_type")]
        public ProficiencyInfo DamageType { get; set; } = new();

        [JsonPropertyName("damage_dice")]
        public string DamageDice { get; set; } = "";
    }

    public class Usage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("dice")]
        public string Dice { get; set; } = "";

        [JsonPropertyName("min_value")]
        public int? MinValue { get; set; }

        [JsonPropertyName("times")]
        public int? Times { get; set; }

        [JsonPropertyName("rest_types")]
        public List<string> RestTypes { get; set; } = new();
    }

    public class LegendaryAction
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";

        [JsonPropertyName("damage")]
        public List<Damage> Damage { get; set; } = new();
    }

    public class Reaction
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("desc")]
        public string Desc { get; set; } = "";
    }
}
