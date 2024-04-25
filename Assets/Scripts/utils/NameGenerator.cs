using System;
using System.Collections.Generic;

using UnityEngine;

namespace BaaroForce.Utils 
{
    public class NameGenerator : MonoBehaviour
    {

        private System.Random random = new System.Random();
        public enum RealmType
        {
            FIRE,
            WATER,
            EARTH,
            WIND,
            DARK,
            LIGHT,
        }
        // Start is called before the first frame update
        void Start()
        {

            for(int i=0; i < 100; i++) {
                RealmType realm = GetRandomRealm();
                string name = GetNameByRealm(realm);

                Debug.Log("Character Name: " + name);
            }
        }

        public RealmType GetRandomRealm()
        {
            Array values = Enum.GetValues(typeof(RealmType));
            int randomIndex = random.Next(0, values.Length);
            RealmType randomRealm = (RealmType)values.GetValue(randomIndex);
            return randomRealm;
        }

        public string GenerateRandomName(List<string> namePrefixes, List<string> nameSuffixes) 
        {
            string name = namePrefixes[random.Next(0, namePrefixes.Count)] + nameSuffixes[random.Next(0, nameSuffixes.Count)];
            return name;
        }

        public string GetNameByRealm(RealmType realm)
        {
            switch (realm)
            {
                case RealmType.FIRE:
                    return GetFireRealmName();
                case RealmType.WATER:
                    return GetWaterRealmName();
                case RealmType.EARTH:
                    return GetEarthRealmName();
                case RealmType.WIND:
                    return GetWindRealmName();
                case RealmType.DARK:
                    return GetDarkRealmName();
                case RealmType.LIGHT:
                    return GetLightRealmName();
                default:
                    return "Kirby";
            }
        }

        private string GetFireRealmName()
        {
            List<string> namePrefixes = new List<string>(){
                "Blaze",
                "Ember",
                "Ignis",
                "Char",
                "Pyre",
                "Bon",
                "Infern",
                "Furn",
                "Spark",
                "Vul",
                "Radi",
            };

            List<string> nameSuffixes = new List<string>() {
                "bee",
                "maw",
                "shin",
                "don",
                "shaun",
                "shim",
                "boot",
                "hook",
                "dog",
                "chief",
                "tim",
                "en",
            };

            string name = GenerateRandomName(namePrefixes, nameSuffixes);

            return name;
        }

        private string GetWaterRealmName()
        {
            List<string> namePrefixes = new List<string>(){
                "Dew",
                "Sea",
                "Naut",
                "Mist",
                "Shell",
                "Crab",
                "Fish",
                "Nose",
                "McGee",
                "Tide",
                "Aq",
            };

            List<string> nameSuffixes = new List<string>() {
                "bee",
                "maw",
                "shin",
                "don",
                "shaun",
                "shim",
                "boot",
                "hook",
                "dog",
                "chief",
                "tim",
                "en",
            };

            string name = GenerateRandomName(namePrefixes, nameSuffixes);

            return name;
        }

        private string GetEarthRealmName()
        {
            List<string> namePrefixes = new List<string>(){
                "Pebble",
                "Rock",
                "Root",
                "Moss",
                "Clif",
                "Geo",
                "Pa",
                "Bo",
                "Ca",
                "Qua",
                "Grove",
                "Worm",
                "Tree",
                "Mar"
            };

            List<string> nameSuffixes = new List<string>() {
                "bee",
                "maw",
                "shin",
                "don",
                "haun",
                "shim",
                "boot",
                "hook",
                "dog",
                "tim",
                "en",
                "fly",
                "rus",
            };

            string name = GenerateRandomName(namePrefixes, nameSuffixes);

            return name;
        }

        private string GetWindRealmName()
        {
            List<string> namePrefixes = new List<string>(){
                "Cy",
                "Scy",
                "Sto",
                "Tem",
                "Flutter",
                "Gale",
                "Zeph",
                "Whisp",
                "Bird",
                "Butter",
                "Moth"
            };

            List<string> nameSuffixes = new List<string>() {
                "bee",
                "maw",
                "shin",
                "don",
                "haun",
                "shim",
                "boot",
                "hook",
                "dog",
                "tim",
                "en",
                "fly",
                "rus",
            };


            string name = GenerateRandomName(namePrefixes, nameSuffixes);

            return name;
        }

        private string GetDarkRealmName()
        {
            List<string> namePrefixes = new List<string>(){
                "Ab",
                "Umb",
                "Grim",
                "Dusk",
                "Rav",
                "Sin",
                "Ebon",
                "Void",
                "Mor",
                "Sty",
                "Dam",
                "Dem",
                "Lun"
            };

            List<string> nameSuffixes = new List<string>() {
                "bee",
                "maw",
                "shin",
                "don",
                "haun",
                "shim",
                "boot",
                "hook",
                "dog",
                "tim",
                "en",
                "fly",
                "rus",
            };

            string name = GenerateRandomName(namePrefixes, nameSuffixes);

            return name;
        }

        private string GetLightRealmName()
        {
            List<string> namePrefixes = new List<string>(){
                "Lum",
                "Gle",
                "Shim",
                "Pris",
                "Ray",
                "Spark",
                "Day",
                "Sol",
                "Glo",
                "Daz",
            };

            List<string> nameSuffixes = new List<string>() {
                "bee",
                "maw",
                "shin",
                "don",
                "haun",
                "shim",
                "boot",
                "hook",
                "dog",
                "tim",
                "en",
                "fly",
                "rus",
            };

            string name = GenerateRandomName(namePrefixes, nameSuffixes);

            return name;
        }
    }


}