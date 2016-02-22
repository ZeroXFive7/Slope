﻿using UnityEngine;
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
    private SimpleSteering playerPrefab = null;
    [SerializeField]
    private ChaseCamera playerCameraPrefab = null;

    [SerializeField]
    private Transform[] spawnPoints = null;
    [SerializeField]
    private SimpleSteering.BoardTrailColors[] characterColors = null;
    [SerializeField]
    private SplitscreenViewport[] splitscreenViewports = null;

    [Header("Settings")]
    [SerializeField]
    private int maxPlayerCount = 2;

    private int spawnPointIndex = 0;
    private int colorIndex = 0;

    private HashSet<int> activePlayerInputIds = new HashSet<int>();

    [HideInInspector]
    public List<SimpleSteering> Players = new List<SimpleSteering>();

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

            if (Rewired.ReInput.players.Players[i].GetButton("Join Game") && !playerInputIsActive && Players.Count < maxPlayerCount)
            {
                activePlayerInputIds.Add(id);
                SpawnPlayerCharacter(id, "Player" + (Players.Count + 1));
                UpdateViewports();
            }
            else if (Rewired.ReInput.players.Players[i].GetButton("Join Game") && playerInputIsActive)
            {
                RespawnPlayerCharacter(id);
            }
            else if (Rewired.ReInput.players.Players[i].GetButton("Quit Game") && playerInputIsActive)
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

        SimpleSteering newPlayer = Instantiate(playerPrefab);
        ChaseCamera playerCamera = Instantiate(playerCameraPrefab);

        newPlayer.PlayerInputId = playerInputId;
        newPlayer.TrailColors = GetNextBoardColors();
        newPlayer.Camera = playerCamera;

        playerCamera.Steering = newPlayer;

        Players.Add(newPlayer);

        newPlayer.Reset(GetNextSpawnPoint());
    }

    private void DestroyPlayerCharacter(int playerInputId)
    {
        for (int i = 0; i < Players.Count; ++i)
        {
            if (Players[i].PlayerInputId == playerInputId)
            {
                SimpleSteering player = Players[i];
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
                SimpleSteering player = Players[i];
                player.Reset(GetNextSpawnPoint());
            }
        }
    }

    private void UpdateViewports()
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

    private SimpleSteering.BoardTrailColors GetNextBoardColors()
    {
        SimpleSteering.BoardTrailColors color = characterColors[colorIndex];
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