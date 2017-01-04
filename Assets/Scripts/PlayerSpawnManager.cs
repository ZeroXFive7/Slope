using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawnManager : MonoBehaviour
{
    [System.Serializable]
    private struct SplitscreenViewport
    {
        public string Name;
        public Rect[] PlayerViewports;
    };

    [Header("Prefab References")]
    [SerializeField]
    private PlayerController playerPrefab = null;
    [SerializeField]
    private ChaseCamera playerCameraPrefab = null;

    [SerializeField]
    private Transform[] spawnPoints = null;
    [SerializeField]
    private BoardTrailRenderers.TrailColors[] characterColors = null;
    [SerializeField]
    private SplitscreenViewport[] splitscreenViewports = null;

    [Header("Settings")]
    [SerializeField]
    private int maxPlayerCount = 2;

    private int spawnPointIndex = 0;
    private int colorIndex = 0;

    private HashSet<int> activePlayerInputIds = new HashSet<int>();

    [HideInInspector]
    public List<PlayerController> Players = new List<PlayerController>();

    private void Awake()
    {
        spawnPointIndex = Random.Range(0, spawnPoints.Length - 1);
        colorIndex = Random.Range(0, characterColors.Length - 1);
    }

    private void Update()
    {
        for (int i = 0; i < Rewired.ReInput.players.Players.Count; ++i)
        {
            int id = Rewired.ReInput.players.Players[i].id;

            bool playerInputIsActive = activePlayerInputIds.Contains(id);

            if (Rewired.ReInput.players.Players[i].GetButtonDown("Join Game") && !playerInputIsActive && Players.Count < maxPlayerCount)
            {
                activePlayerInputIds.Add(id);
                SpawnPlayerCharacter(id, "Player" + (Players.Count + 1));
                UpdateViewports();
            }
            else if (Rewired.ReInput.players.Players[i].GetButtonDown("Join Game") && playerInputIsActive)
            {
                RespawnPlayerCharacter(id);
            }
            else if (Rewired.ReInput.players.Players[i].GetButtonDown("Quit Game") && playerInputIsActive)
            {
                activePlayerInputIds.Remove(id);
                DestroyPlayerCharacter(id);
                UpdateViewports();
            }
        }
    }

    private void SpawnPlayerCharacter(int playerInputId, string layerName)
    {
        int playerLayerId = LayerMask.NameToLayer(layerName);

        PlayerController newPlayer = Instantiate(playerPrefab);
        ChaseCamera playerCamera = Instantiate(playerCameraPrefab);

        newPlayer.PlayerInputId = playerInputId;
        newPlayer.Camera = playerCamera;
        newPlayer.GetComponent<BoardTrailRenderers>().Colors = GetNextBoardColors();

        playerCamera.Player = newPlayer.transform;

        Players.Add(newPlayer);

        newPlayer.Reset(GetNextSpawnPoint());
    }

    private void DestroyPlayerCharacter(int playerInputId)
    {
        for (int i = 0; i < Players.Count; ++i)
        {
            if (Players[i].PlayerInputId == playerInputId)
            {
                PlayerController player = Players[i];
                Players.RemoveAt(i);
                Destroy(player.Camera.gameObject);
                Destroy(player.gameObject);
            }
        }
    }

    private void RespawnPlayerCharacter(int playerInputId)
    {
        for (int i = 0; i < Players.Count; ++i)
        {
            if (Players[i].PlayerInputId == playerInputId)
            {
                PlayerController player = Players[i];
                player.Reset(GetNextSpawnPoint());
            }
        }
    }

    private void UpdateViewports()
    {
        if (Players.Count > 0)
        {
            SplitscreenViewport viewports = splitscreenViewports[Players.Count - 1];
            for (int i = 0; i < viewports.PlayerViewports.Length; ++i)
            {
                if (i < Players.Count)
                {
                    Players[i].Camera.Viewport = viewports.PlayerViewports[i];
                }
            }
        }
    }

    private BoardTrailRenderers.TrailColors GetNextBoardColors()
    {
        BoardTrailRenderers.TrailColors color = characterColors[colorIndex];
        colorIndex = (colorIndex + 1) % characterColors.Length;
        return color;
    }

    private Transform GetNextSpawnPoint()
    {
        Transform spawnPoint = spawnPoints[spawnPointIndex];
        spawnPointIndex = (spawnPointIndex + 1) % spawnPoints.Length;
        return spawnPoint;
    }
}
