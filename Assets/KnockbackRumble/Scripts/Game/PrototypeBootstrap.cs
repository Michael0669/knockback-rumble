using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>
    /// Entry point. On Play it finds (or builds) the course, starts Steam and the
    /// network session, sets up the camera, and shows the main menu. Races are started
    /// from the menu (offline vs bots, or online through the lobby).
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        [Range(0, 15)] public int botCount = 7;
        public string playerName = "Dummy 69";
        [Range(0f, 1f)] public float botSkillMin = 0.35f;
        [Range(0f, 1f)] public float botSkillMax = 0.85f;

        void Start()
        {
            Application.runInBackground = true; // keep the network running when the window isn't focused
            var course = CourseInfo.Current;
            if (course == null) course = ScrapSprintBuilder.Build(null);

            SteamService.Ensure();
            NetSession.Ensure();
            NetVoice.Ensure();

            if (GetComponent<RaceManager>() == null) gameObject.AddComponent<RaceManager>();
            if (GetComponent<HUD>() == null) gameObject.AddComponent<HUD>();
            if (GetComponent<MenuUI>() == null) gameObject.AddComponent<MenuUI>();

            // Camera
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var unityCam = camGo.GetComponent<Camera>();
            unityCam.fieldOfView = 70f;
            unityCam.farClipPlane = 600f;
            var cam = camGo.GetComponent<ThirdPersonCamera>();
            if (cam == null) cam = camGo.AddComponent<ThirdPersonCamera>();

            var match = GetComponent<MatchController>();
            if (match == null) match = gameObject.AddComponent<MatchController>();
            match.OfflineBots = botCount;
            match.OfflineName = playerName;
            match.BotSkillMin = botSkillMin;
            match.BotSkillMax = botSkillMax;
            match.Init(course, cam);
        }
    }
}
