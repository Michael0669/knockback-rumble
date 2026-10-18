using System.Collections.Generic;
using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Entry point for the offline prototype. On Play it finds (or builds) the course,
    /// spawns you plus bots, hooks up the camera and starts the race.
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        [Range(0, 15)] public int botCount = 7;
        public string playerName = "Dummy 69";
        [Range(0f, 1f)] public float botSkillMin = 0.35f;
        [Range(0f, 1f)] public float botSkillMax = 0.85f;

        void Start()
        {
            var course = CourseInfo.Current;
            if (course == null) course = ScrapSprintBuilder.Build(null);

            var race = GetComponent<RaceManager>();
            if (race == null) race = gameObject.AddComponent<RaceManager>();
            if (GetComponent<HUD>() == null) gameObject.AddComponent<HUD>();

            // Camera
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var unityCam = camGo.GetComponent<Camera>();
            unityCam.fieldOfView = 70f;
            unityCam.farClipPlane = 600f;
            var cam = camGo.GetComponent<ThirdPersonCamera>();
            if (cam == null) cam = camGo.AddComponent<ThirdPersonCamera>();

            // Contestants: you, plus a random selection of voice packs for the bots
            var list = new List<Contestant>();
            var player = Contestant.Create(playerName, Personas.Player.Color, true, Personas.Player, race.SpawnPointFor(course, 0), Quaternion.identity);
            player.gameObject.AddComponent<PlayerInputDriver>().Init(player, cam);
            cam.SetTarget(player.transform);
            list.Add(player);

            var personas = new List<Persona>(Personas.Bots);
            for (int i = 0; i < botCount; i++)
            {
                Persona persona;
                if (personas.Count > 0)
                {
                    int pick = Random.Range(0, personas.Count);
                    persona = personas[pick];
                    personas.RemoveAt(pick);
                }
                else
                {
                    persona = Personas.Bots[i % Personas.Bots.Length];
                }

                string name = list.Exists(c => c.DisplayName == persona.Name) ? persona.Name + " " + (i + 1) : persona.Name;
                var bot = Contestant.Create(name, persona.Color, false, persona, race.SpawnPointFor(course, i + 1), Quaternion.identity);
                bot.gameObject.AddComponent<BotBrain>().Init(bot, course, Random.Range(botSkillMin, botSkillMax));
                list.Add(bot);
            }

            race.Begin(course, list);
        }
    }
}
