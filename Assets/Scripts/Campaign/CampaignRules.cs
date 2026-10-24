using System;
using System.Collections.Generic;
using System.Linq;

namespace Valmon.Campaign
{
    public enum PlayerRole { Tamer, Corruptor }
    public enum StageOutcome { Playing, Cleared, PartyLost, TimeExpired, NoBalls }

    [Serializable] public sealed class OwnedCreature
    {
        public SpeciesDefinition species;
        public int level, health;
        public int[] pp;
        public int attackStage, defenseStage, accuracyStage, sleepTurns;
        public string status = "";
        public int MaxHealth => ((2 * species.hp + 20) * level / 100) + level + 10;
        public int Attack => Stat(species.attack);
        public int Defense => Stat(species.defense);
        public int SpecialAttack => Stat(species.specialAttack);
        public int SpecialDefense => Stat(species.specialDefense);
        public int Speed => Stat(species.speed);
        private int Stat(int value) => ((2 * value + 20) * level / 100) + 5;

        public OwnedCreature(SpeciesDefinition definition, int creatureLevel)
        {
            species = definition;
            level = Math.Max(1, Math.Min(50, creatureLevel));
            Heal();
        }
        public void Heal()
        {
            health = MaxHealth;
            pp = species.moves.Select(m => m.pp).ToArray();
            ResetBattleEffects();
        }
        public void ResetBattleEffects()
        {
            attackStage = defenseStage = accuracyStage = sleepTurns = 0;
            status = "";
        }
    }

    public sealed class CampaignRun
    {
        public const int RequiredCatches = 5, RequiredCorruptions = 6, BallsPerStage = 10;
        public const float SecondsPerStage = 480f;
        public int stage, balls, catches;
        public float seconds;
        public readonly List<OwnedCreature> party = new List<OwnedCreature>();
        public int MinLevel => stage == 0 ? 1 : stage == 1 ? 16 : 31;
        public int MaxLevel => stage == 0 ? 15 : stage == 1 ? 30 : 50;
        public string Habitat => stage == 0 ? "Grass" : stage == 1 ? "Fire" : "Water";

        public void StartStage(int index)
        {
            stage = Math.Max(0, Math.Min(2, index));
            balls = BallsPerStage;
            catches = 0;
            seconds = SecondsPerStage;
            foreach (OwnedCreature creature in party)
            {
                creature.level = Math.Max(creature.level, stage == 0 ? 10 : MinLevel + 4);
                creature.Heal();
            }
        }
        public bool SpendBall()
        {
            if (balls <= 0 || Outcome(false) != StageOutcome.Playing) return false;
            balls--;
            return true;
        }
        public void Catch(OwnedCreature creature)
        {
            creature.Heal();
            party.Add(creature);
            catches++;
        }
        public StageOutcome Outcome(bool encounterPending)
        {
            if (party.Count == 0) return StageOutcome.PartyLost;
            if (seconds <= 0) return catches >= RequiredCatches ? StageOutcome.Cleared : StageOutcome.TimeExpired;
            if (!encounterPending && balls == 0 && catches >= RequiredCatches) return StageOutcome.Cleared;
            // The last ball can still complete a capture or a battle.
            if (!encounterPending && balls < RequiredCatches - catches) return StageOutcome.NoBalls;
            return StageOutcome.Playing;
        }
    }

    public static class BattleRules
    {
        private static readonly Dictionary<string, float> Chart = BuildChart();
        private static Dictionary<string, float> BuildChart()
        {
            var result = new Dictionary<string, float>();
            Add(result, "Normal", "Rock,Steel", .5f); Add(result, "Normal", "Ghost", 0);
            Add(result, "Fire", "Grass,Ice,Bug,Steel", 2); Add(result, "Fire", "Fire,Water,Rock,Dragon", .5f);
            Add(result, "Water", "Fire,Ground,Rock", 2); Add(result, "Water", "Water,Grass,Dragon", .5f);
            Add(result, "Grass", "Water,Ground,Rock", 2); Add(result, "Grass", "Fire,Grass,Poison,Flying,Bug,Dragon,Steel", .5f);
            Add(result, "Electric", "Water,Flying", 2); Add(result, "Electric", "Electric,Grass,Dragon", .5f); Add(result, "Electric", "Ground", 0);
            Add(result, "Ice", "Grass,Ground,Flying,Dragon", 2); Add(result, "Ice", "Fire,Water,Ice,Steel", .5f);
            Add(result, "Fighting", "Normal,Ice,Rock,Dark,Steel", 2); Add(result, "Fighting", "Poison,Flying,Psychic,Bug,Fairy", .5f); Add(result, "Fighting", "Ghost", 0);
            Add(result, "Poison", "Grass,Fairy", 2); Add(result, "Poison", "Poison,Ground,Rock,Ghost", .5f); Add(result, "Poison", "Steel", 0);
            Add(result, "Ground", "Fire,Electric,Poison,Rock,Steel", 2); Add(result, "Ground", "Grass,Bug", .5f); Add(result, "Ground", "Flying", 0);
            Add(result, "Flying", "Grass,Fighting,Bug", 2); Add(result, "Flying", "Electric,Rock,Steel", .5f);
            Add(result, "Psychic", "Fighting,Poison", 2); Add(result, "Psychic", "Psychic,Steel", .5f); Add(result, "Psychic", "Dark", 0);
            Add(result, "Bug", "Grass,Psychic,Dark", 2); Add(result, "Bug", "Fire,Fighting,Poison,Flying,Ghost,Steel,Fairy", .5f);
            Add(result, "Rock", "Fire,Ice,Flying,Bug", 2); Add(result, "Rock", "Fighting,Ground,Steel", .5f);
            Add(result, "Ghost", "Psychic,Ghost", 2); Add(result, "Ghost", "Dark", .5f); Add(result, "Ghost", "Normal", 0);
            Add(result, "Dragon", "Dragon", 2); Add(result, "Dragon", "Steel", .5f); Add(result, "Dragon", "Fairy", 0);
            Add(result, "Dark", "Psychic,Ghost", 2); Add(result, "Dark", "Fighting,Dark,Fairy", .5f);
            Add(result, "Steel", "Ice,Rock,Fairy", 2); Add(result, "Steel", "Fire,Water,Electric,Steel", .5f);
            Add(result, "Fairy", "Fighting,Dragon,Dark", 2); Add(result, "Fairy", "Fire,Poison,Steel", .5f);
            return result;
        }
        private static void Add(Dictionary<string, float> chart, string attack, string targets, float value)
        { foreach (string target in targets.Split(',')) chart[attack + "/" + target] = value; }

        public static float Effectiveness(string type, SpeciesDefinition target)
        {
            float value = 1;
            foreach (string defenderType in target.types)
                if (Chart.TryGetValue(type + "/" + defenderType, out float scale)) value *= scale;
            return value;
        }
        private static double Stage(int stage) => stage >= 0 ? (2d + stage) / 2d : 2d / (2d - stage);
        public static bool HasMoves(OwnedCreature actor) => actor.pp.Any(p => p > 0);
        public static int BestMove(OwnedCreature actor, OwnedCreature target, Random random)
        {
            int best = -1;
            double score = -1;
            for (int i = 0; i < actor.species.moves.Length; i++)
            {
                if (actor.pp[i] <= 0) continue;
                MoveDefinition move = actor.species.moves[i];
                double value = move.power * Effectiveness(move.type, target.species) * move.accuracy / 100d;
                if (actor.species.types.Contains(move.type)) value *= 1.5;
                if (move.effect == "heal" && actor.health < actor.MaxHealth / 2) value = 100;
                value += random.NextDouble() * 12;
                if (value > score) { score = value; best = i; }
            }
            return best;
        }

        public static string Attack(OwnedCreature actor, OwnedCreature target, int moveIndex, Random random)
        {
            if (actor.health <= 0 || target.health <= 0) return "The battle is already decided.";
            bool struggle = !HasMoves(actor);
            if (!struggle && (moveIndex < 0 || moveIndex >= actor.pp.Length || actor.pp[moveIndex] <= 0)) return "That move has no PP.";
            if (actor.sleepTurns > 0)
            {
                actor.sleepTurns--;
                if (actor.sleepTurns == 0) actor.status = "";
                return actor.species.displayName + " is asleep.";
            }
            MoveDefinition move = struggle ? new MoveDefinition { name = "Struggle", type = "Normal", power = 50, effect = "recoil" } : actor.species.moves[moveIndex];
            if (!struggle) actor.pp[moveIndex]--;
            string text = actor.species.displayName + " used " + move.name + ". ";
            if (random.NextDouble() * 100 > move.accuracy * Stage(actor.accuracyStage)) return text + "It missed!";
            float effectiveness = struggle ? 1f : Effectiveness(move.type, target.species);
            if (move.power > 0)
            {
                double attack = move.special ? actor.SpecialAttack : actor.Attack * Stage(actor.attackStage);
                if (!move.special && actor.status == "Burn") attack *= .5;
                double defense = move.special ? target.SpecialDefense : target.Defense * Stage(target.defenseStage);
                double stab = !struggle && actor.species.types.Contains(move.type) ? 1.5 : 1;
                bool critical = random.NextDouble() < (move.effect == "critical" ? .125 : 1d / 24);
                int damage = effectiveness == 0 ? 0 : Math.Max(1, (int)(((2d * actor.level / 5 + 2) * move.power * attack / Math.Max(1, defense) / 50 + 2) * effectiveness * stab * (.85 + random.NextDouble() * .15) * (critical ? 1.5 : 1)));
                target.health = Math.Max(0, target.health - damage);
                text += damage + " damage. ";
                if (effectiveness == 0) text += "No effect. ";
                else if (effectiveness > 1) text += "Super effective! ";
                else if (effectiveness < 1) text += "Not very effective. ";
                if (critical && damage > 0) text += "Critical hit! ";
                if (move.effect == "drain") actor.health = Math.Min(actor.MaxHealth, actor.health + damage / 2);
                if (move.effect == "recoil") actor.health = Math.Max(0, actor.health - Math.Max(1, actor.MaxHealth / 4));
            }
            switch (move.effect)
            {
                case "heal": actor.health = Math.Min(actor.MaxHealth, actor.health + actor.MaxHealth / 2); text += "Restored health."; break;
                case "guard": actor.defenseStage = Math.Min(6, actor.defenseStage + 1); text += "Defense rose."; break;
                case "weaken": target.attackStage = Math.Max(-6, target.attackStage - 1); text += "Opponent's Attack fell."; break;
                case "blind": target.accuracyStage = Math.Max(-6, target.accuracyStage - 1); text += "Opponent's accuracy fell."; break;
                case "burn":
                    if (effectiveness > 0 && target.health > 0 && target.status == "" && !target.species.types.Contains("Fire") && random.NextDouble() < .2) { target.status = "Burn"; text += "Burned!"; }
                    break;
                case "poison":
                    if (target.health > 0 && target.status == "" && !target.species.types.Contains("Poison") && !target.species.types.Contains("Steel")) { target.status = "Poison"; text += "Poisoned!"; }
                    break;
                case "sleep":
                    if (target.health > 0 && target.status == "") { target.sleepTurns = random.Next(1, 3); target.status = "Sleep"; text += "Fell asleep!"; }
                    break;
            }
            if (actor.health > 0 && (actor.status == "Poison" || actor.status == "Burn"))
            {
                actor.health = Math.Max(0, actor.health - Math.Max(1, actor.MaxHealth / 12));
                text += " " + actor.status + " dealt damage.";
            }
            return text;
        }
    }
}
