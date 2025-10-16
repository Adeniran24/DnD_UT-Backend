using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameApi.Models
{
    // ===== Common referenced types =====

    public class APIReference
    {
        [JsonPropertyName("index")]
        public string? Index { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("choose")]
        public int? Choose { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("from")]
        public List<object>? From { get; set; }
    }

    public class Damage
    {
        [JsonPropertyName("damage_type")]
        public APIReference? DamageType { get; set; }

        [JsonPropertyName("damage_dice")]
        public string? DamageDice { get; set; }

        [JsonPropertyName("damage_bonus")]
        public int? DamageBonus { get; set; }
    }

    public class DifficultyClass
    {
        [JsonPropertyName("dc_type")]
        public APIReference? DcType { get; set; }

        [JsonPropertyName("dc_value")]
        public int? DcValue { get; set; }

        [JsonPropertyName("success_type")]
        public string? SuccessType { get; set; }
    }

    // ===== Action-related classes =====

    public class ActionOption
    {
        [JsonPropertyName("action_name")]
        public string ActionName { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public string Count { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // melee, ranged, ability, magic
    }

    public class ActionUsage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("dice")]
        public string? Dice { get; set; }

        [JsonPropertyName("min_value")]
        public int? MinValue { get; set; }
    }

    public class MonsterAction
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("attack_bonus")]
        public int? AttackBonus { get; set; }

        [JsonPropertyName("damage")]
        public List<object>? Damage { get; set; }

        [JsonPropertyName("dc")]
        public DifficultyClass? DC { get; set; }

        [JsonPropertyName("options")]
        public Choice? Options { get; set; }

        [JsonPropertyName("usage")]
        public ActionUsage? Usage { get; set; }

        [JsonPropertyName("multiattack_type")]
        public string? MultiattackType { get; set; }

        [JsonPropertyName("actions")]
        public List<ActionOption>? Actions { get; set; }

        [JsonPropertyName("action_options")]
        public Choice? ActionOptions { get; set; }
    }

    // ===== Armor Class components =====

    public class ArmorClassDex
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "dex";

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("desc")]
        public string? Description { get; set; }
    }

    public class ArmorClassNatural
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "natural";

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("desc")]
        public string? Description { get; set; }
    }

    public class ArmorClassArmor
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "armor";

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("armor")]
        public List<APIReference>? Armor { get; set; }

        [JsonPropertyName("desc")]
        public string? Description { get; set; }
    }

    public class ArmorClassSpell
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "spell";

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("spell")]
        public APIReference? Spell { get; set; }

        [JsonPropertyName("desc")]
        public string? Description { get; set; }
    }

    public class ArmorClassCondition
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "condition";

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("condition")]
        public APIReference? Condition { get; set; }

        [JsonPropertyName("desc")]
        public string? Description { get; set; }
    }

    // ===== Legendary Action =====

    public class LegendaryAction
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("attack_bonus")]
        public int? AttackBonus { get; set; }

        [JsonPropertyName("damage")]
        public List<Damage>? Damage { get; set; }

        [JsonPropertyName("dc")]
        public DifficultyClass? DC { get; set; }
    }

    // ===== Monster Proficiency =====

    public class MonsterProficiency
    {
        [JsonPropertyName("proficiency")]
        public APIReference? Proficiency { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }

    // ===== Reaction =====

    public class Reaction
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("dc")]
        public DifficultyClass? DC { get; set; }
    }

    // ===== Sense =====

    public class Sense
    {
        [JsonPropertyName("blindsight")]
        public string? Blindsight { get; set; }

        [JsonPropertyName("darkvision")]
        public string? Darkvision { get; set; }

        [JsonPropertyName("passive_perception")]
        public int PassivePerception { get; set; }

        [JsonPropertyName("tremorsense")]
        public string? Tremorsense { get; set; }

        [JsonPropertyName("truesight")]
        public string? Truesight { get; set; }
    }

    // ===== Special Ability and Spellcasting =====

    public class SpecialAbilityUsage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("times")]
        public int? Times { get; set; }

        [JsonPropertyName("rest_types")]
        public List<string>? RestTypes { get; set; }
    }

    public class SpecialAbilitySpell
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("usage")]
        public SpecialAbilityUsage? Usage { get; set; }
    }

    public class SpecialAbilitySpellcasting
    {
        [JsonPropertyName("level")]
        public int? Level { get; set; }

        [JsonPropertyName("ability")]
        public APIReference? Ability { get; set; }

        [JsonPropertyName("dc")]
        public int? DC { get; set; }

        [JsonPropertyName("modifier")]
        public int? Modifier { get; set; }

        [JsonPropertyName("components_required")]
        public List<string> ComponentsRequired { get; set; } = new();

        [JsonPropertyName("school")]
        public string? School { get; set; }

        [JsonPropertyName("slots")]
        public Dictionary<string, int>? Slots { get; set; }

        [JsonPropertyName("spells")]
        public List<SpecialAbilitySpell> Spells { get; set; } = new();
    }

    public class SpecialAbility
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("attack_bonus")]
        public int? AttackBonus { get; set; }

        [JsonPropertyName("damage")]
        public List<Damage>? Damage { get; set; }

        [JsonPropertyName("dc")]
        public DifficultyClass? DC { get; set; }

        [JsonPropertyName("spellcasting")]
        public SpecialAbilitySpellcasting? Spellcasting { get; set; }

        [JsonPropertyName("usage")]
        public SpecialAbilityUsage? Usage { get; set; }
    }

    // ===== Monster Speed =====

    public class MonsterSpeed
    {
        [JsonPropertyName("burrow")]
        public string? Burrow { get; set; }

        [JsonPropertyName("climb")]
        public string? Climb { get; set; }

        [JsonPropertyName("fly")]
        public string? Fly { get; set; }

        [JsonPropertyName("hover")]
        public bool? Hover { get; set; }

        [JsonPropertyName("swim")]
        public string? Swim { get; set; }

        [JsonPropertyName("walk")]
        public string? Walk { get; set; }
    }

    // ===== Monster root model =====

    public class Monster
    {
        [JsonPropertyName("index")]
        public string Index { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public string Size { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("subtype")]
        public string? Subtype { get; set; }

        [JsonPropertyName("alignment")]
        public string Alignment { get; set; } = string.Empty;

        [JsonPropertyName("armor_class")]
        public List<object>? ArmorClass { get; set; }

        [JsonPropertyName("challenge_rating")]
        public double ChallengeRating { get; set; }

        [JsonPropertyName("charisma")]
        public int Charisma { get; set; }

        [JsonPropertyName("constitution")]
        public int Constitution { get; set; }

        [JsonPropertyName("dexterity")]
        public int Dexterity { get; set; }

        [JsonPropertyName("intelligence")]
        public int Intelligence { get; set; }

        [JsonPropertyName("wisdom")]
        public int Wisdom { get; set; }

        [JsonPropertyName("strength")]
        public int Strength { get; set; }

        [JsonPropertyName("hit_points")]
        public int HitPoints { get; set; }

        [JsonPropertyName("hit_dice")]
        public string HitDice { get; set; } = string.Empty;

        [JsonPropertyName("hit_points_roll")]
        public string HitPointsRoll { get; set; } = string.Empty;

        [JsonPropertyName("languages")]
        public string Languages { get; set; } = string.Empty;

        [JsonPropertyName("xp")]
        public int XP { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; } = string.Empty;

        [JsonPropertyName("speed")]
        public MonsterSpeed? Speed { get; set; }

        [JsonPropertyName("actions")]
        public List<MonsterAction>? Actions { get; set; }

        [JsonPropertyName("special_abilities")]
        public List<SpecialAbility>? SpecialAbilities { get; set; }

        [JsonPropertyName("legendary_actions")]
        public List<LegendaryAction>? LegendaryActions { get; set; }

        [JsonPropertyName("reactions")]
        public List<Reaction>? Reactions { get; set; }

        [JsonPropertyName("senses")]
        public Sense? Senses { get; set; }

        [JsonPropertyName("damage_immunities")]
        public List<string>? DamageImmunities { get; set; }

        [JsonPropertyName("damage_resistances")]
        public List<string>? DamageResistances { get; set; }

        [JsonPropertyName("damage_vulnerabilities")]
        public List<string>? DamageVulnerabilities { get; set; }

        [JsonPropertyName("condition_immunities")]
        public List<APIReference>? ConditionImmunities { get; set; }

        [JsonPropertyName("forms")]
        public List<APIReference>? Forms { get; set; }

        [JsonPropertyName("proficiencies")]
        public List<MonsterProficiency>? Proficiencies { get; set; }
    }
}
