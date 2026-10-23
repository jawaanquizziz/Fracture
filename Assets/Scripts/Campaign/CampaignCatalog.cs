using System;
using UnityEngine;

namespace Valmon.Campaign
{
    [Serializable] public class MoveDefinition
    {
        public string name, type, effect;
        public int power = 40, accuracy = 100, pp = 20;
        public bool special;
    }

    [Serializable] public class SpeciesDefinition
    {
        public string id, displayName;
        public string[] types;
        public int hp = 60, attack = 60, defense = 60, specialAttack = 60, specialDefense = 60, speed = 60;
        public MoveDefinition[] moves;
        public GameObject model;
        public Texture2D albedoOverride;
    }

    [Serializable] public class CharacterDefinition
    {
        public string name;
        public GameObject model;
        public float yawOffset;
    }

    [CreateAssetMenu(menuName = "VALMON/Campaign Catalog")]
    public class CampaignCatalog : ScriptableObject
    {
        public SpeciesDefinition[] species;
        public CharacterDefinition[] characters;
        public GameObject[] maps;
        public GameObject captureBall;
        public Shader surfaceShader;
        public UnityEngine.AudioClip menuMusic, captureAudio, corruptionAudio, victoryAudio;
        public string introFile = "VALMON_Opening.mp4";
        public SpeciesDefinition Find(string id) => Array.Find(species, s => s.id == id);
    }
}
