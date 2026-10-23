using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Valmon.Gameplay
{
    public enum GameRole
    {
        Tamer,
        Warden
    }

    public enum MatchState
    {
        Warmup,
        RoundActive,
        RoundEnded,
        RoleSwitching,
        MatchFinished
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Match Settings")]
        public float roundDuration = 75f;
        public int currentRound = 1;
        public int maxRounds = 2;

        [Header("Current State")]
        public MatchState state = MatchState.RoundActive;
        public GameRole localRole = GameRole.Tamer;
        public float timeRemaining;

        [Header("Scores")]
        public int tamerScore = 0;
        public int wardenScore = 0;
        public int round1TamerScore = 0;
        public int round1WardenScore = 0;

        [Header("References")]
        public GameObject tamerCameraRig;
        public GameObject wardenCameraRig;
        public GameObject tamerPlayer;

        [Header("Stage Select")]
        public int currentMapIndex = 0; // 0 = Map 1, 1 = Map 2, 2 = Map 3

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            timeRemaining = roundDuration;
            ApplyRoleCameras();
        }

        void Update()
        {
            // Toggle between Tamer and Warden for solo testing with [Tab]
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleRole();
            }

            // Quick Map change hotkeys: [F1] Map 1, [F2] Map 2, [F3] Map 3
            if (Input.GetKeyDown(KeyCode.F1)) SwitchMap(0);
            if (Input.GetKeyDown(KeyCode.F2)) SwitchMap(1);
            if (Input.GetKeyDown(KeyCode.F3)) SwitchMap(2);

            // Timer
            if (state == MatchState.RoundActive)
            {
                timeRemaining -= Time.deltaTime;
                if (timeRemaining <= 0f)
                {
                    timeRemaining = 0f;
                    EndRound();
                }
            }
        }

        public void ToggleRole()
        {
            localRole = (localRole == GameRole.Tamer) ? GameRole.Warden : GameRole.Tamer;
            ApplyRoleCameras();
            Debug.Log($"<color=#55ffff><b>[Valmon]</b> Switched active view to: <b>{localRole}</b> (Press [Tab] to switch back)</color>");
        }

        public void ApplyRoleCameras()
        {
            bool isTamer = (localRole == GameRole.Tamer);

            if (tamerCameraRig != null) tamerCameraRig.SetActive(isTamer);
            if (wardenCameraRig != null) wardenCameraRig.SetActive(!isTamer);

            if (isTamer)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void AddScore(bool isTamer, int points)
        {
            if (isTamer) tamerScore += points;
            else wardenScore += points;
        }

        public void EndRound()
        {
            state = MatchState.RoundEnded;

            if (currentRound < maxRounds)
            {
                StartCoroutine(RoleSwitchRoutine());
            }
            else
            {
                state = MatchState.MatchFinished;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Log("<color=#ffff55><b>[Valmon]</b> Match Finished! Check final scoreboard.</color>");
            }
        }

        private IEnumerator RoleSwitchRoutine()
        {
            state = MatchState.RoleSwitching;
            round1TamerScore = tamerScore;
            round1WardenScore = wardenScore;

            Debug.Log("<color=#ffff55><b>[Valmon]</b> Round 1 Complete! Switching Roles in 3 seconds...</color>");
            yield return new WaitForSeconds(3.5f);

            currentRound = 2;
            timeRemaining = roundDuration;
            tamerScore = 0;
            wardenScore = 0;

            ToggleRole();
            state = MatchState.RoundActive;
        }

        public void SwitchMap(int mapIndex)
        {
            currentMapIndex = Mathf.Clamp(mapIndex, 0, 2);
            ArenaManager arena = FindFirstObjectByType<ArenaManager>();
            if (arena != null)
            {
                arena.LoadArena(currentMapIndex);
            }
        }

        public void RestartMatch()
        {
            currentRound = 1;
            tamerScore = 0;
            wardenScore = 0;
            timeRemaining = roundDuration;
            state = MatchState.RoundActive;
            localRole = GameRole.Tamer;
            ApplyRoleCameras();
        }
    }
}
