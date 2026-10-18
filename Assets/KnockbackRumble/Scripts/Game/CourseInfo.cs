using UnityEngine;

namespace KnockbackRumble
{
    /// <summary>Data every course exposes to the race: spawns, the bots' path, and the fall-out height.</summary>
    public class CourseInfo : MonoBehaviour
    {
        public static CourseInfo Current { get; private set; }

        public string courseName = "Scrap Sprint";
        public Vector3[] spawnPoints;
        [Tooltip("The main route, in order. Bots run from point to point.")]
        public Vector3[] waypoints;
        public float killY = -12f;

        void Awake() => Current = this;
        void OnDestroy() { if (Current == this) Current = null; }

        void OnDrawGizmos()
        {
            if (waypoints == null) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Gizmos.DrawSphere(waypoints[i] + Vector3.up * 0.5f, 0.3f);
                if (i > 0) Gizmos.DrawLine(waypoints[i - 1] + Vector3.up * 0.5f, waypoints[i] + Vector3.up * 0.5f);
            }
            if (spawnPoints == null) return;
            Gizmos.color = Color.green;
            foreach (var s in spawnPoints) Gizmos.DrawWireSphere(s + Vector3.up, 0.5f);
        }
    }
}
