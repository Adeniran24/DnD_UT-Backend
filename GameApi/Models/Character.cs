using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GameApi.Models
{
    [Table("characters")]
    public class Character
    {
        [Key]
        public int id { get; set; }

        public int? userId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(userId))]
        public User? User { get; set; }

        public string characterName { get; set; } = "";
        public string classLevel { get; set; } = "";
        public string race { get; set; } = "";
        public string background { get; set; } = "";
        public string playerName { get; set; } = "";
        public string alignment { get; set; } = "";

        public int xp { get; set; }
        public int inspiration { get; set; }

        public int age { get; set; }
        public string height { get; set; } = "";
        public string weight { get; set; } = "";
        public string eyes { get; set; } = "";
        public string skin { get; set; } = "";
        public string hair { get; set; } = "";
        public string symbol { get; set; } = "";
        public string appearance { get; set; } = "";

        public int str { get; set; }
        public int dex { get; set; }
        public int con { get; set; }

        [Column("int_stat")]
        public int int_stat { get; set; }

        public int wis { get; set; }
        public int cha { get; set; }

        public int profBonus { get; set; }
        public int profBonusDuplicate { get; set; }

        public bool saveProf_str { get; set; }
        public bool saveProf_dex { get; set; }
        public bool saveProf_con { get; set; }
        public bool saveProf_int { get; set; }
        public bool saveProf_wis { get; set; }
        public bool saveProf_cha { get; set; }

        public bool skillProf_acrobatics { get; set; }
        public bool skillProf_animalHandling { get; set; }
        public bool skillProf_arcana { get; set; }
        public bool skillProf_athletics { get; set; }
        public bool skillProf_deception { get; set; }
        public bool skillProf_history { get; set; }
        public bool skillProf_insight { get; set; }
        public bool skillProf_intimidation { get; set; }
        public bool skillProf_investigation { get; set; }
        public bool skillProf_medicine { get; set; }
        public bool skillProf_nature { get; set; }
        public bool skillProf_perception { get; set; }
        public bool skillProf_performance { get; set; }
        public bool skillProf_persuasion { get; set; }
        public bool skillProf_religion { get; set; }
        public bool skillProf_sleightOfHand { get; set; }
        public bool skillProf_stealth { get; set; }
        public bool skillProf_survival { get; set; }

        public bool skillExp_acrobatics { get; set; }
        public bool skillExp_animalHandling { get; set; }
        public bool skillExp_arcana { get; set; }
        public bool skillExp_athletics { get; set; }
        public bool skillExp_deception { get; set; }
        public bool skillExp_history { get; set; }
        public bool skillExp_insight { get; set; }
        public bool skillExp_intimidation { get; set; }
        public bool skillExp_investigation { get; set; }
        public bool skillExp_medicine { get; set; }
        public bool skillExp_nature { get; set; }
        public bool skillExp_perception { get; set; }
        public bool skillExp_performance { get; set; }
        public bool skillExp_persuasion { get; set; }
        public bool skillExp_religion { get; set; }
        public bool skillExp_sleightOfHand { get; set; }
        public bool skillExp_stealth { get; set; }
        public bool skillExp_survival { get; set; }

        public int armor { get; set; }
        public int initiative { get; set; }
        public int speed { get; set; }

        public int hpMax { get; set; }
        public int hpCurrent { get; set; }
        public int hpTemp { get; set; }

        public int hitDiceTotal { get; set; }
        public int hitDiceCurrent { get; set; }

        public int deathSuccesses { get; set; }
        public int deathFailures { get; set; }

        public int passivePerception { get; set; }

        public int cp { get; set; }
        public int sp { get; set; }
        public int ep { get; set; }
        public int gp { get; set; }
        public int pp { get; set; }

        public string otherProfs { get; set; } = "";
        public string personalityTraits { get; set; } = "";
        public string ideals { get; set; } = "";
        public string bonds { get; set; } = "";
        public string flaws { get; set; } = "";
        public string allies { get; set; } = "";
        public string additionalFeatures { get; set; } = "";
        public string treasure { get; set; } = "";
        public string backstory { get; set; } = "";

        public string portraitDataUrl { get; set; } = "";

        public string equipment { get; set; } = "{}";
        public string attacks { get; set; } = "[]";
        public string spellbook { get; set; } = "[]";
        public string featuresFeats { get; set; } = "[]";

        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
