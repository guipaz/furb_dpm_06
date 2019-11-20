using System;
using System.Collections.Generic;
using Assets;
using Newtonsoft.Json;
using Proyecto26;
using UnityEngine;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
    public static Game CurrentGame;

    public GameObject pawnPrefab;
    public Vector2[] spawnPoints;
    int currentPlayers = 0;

    GameObject idField;
    GameObject registerPanel;
    GameObject idText;
    GameObject tableInfoPanel;
    GameObject gamePanel;

    List<string> players = new List<string>();
    int nextPlayerId;

    float updateCooldown;

    [Serializable]
    public class Game
    {
        public bool created;
        public string id;
        public string secret;
    }

    public void Awake()
    {
        idField = GameObject.Find("_IdField");
        registerPanel = GameObject.Find("_RegisterPanel");
        idText = GameObject.Find("_IdText");
        tableInfoPanel = GameObject.Find("_TableInfoPanel");
        gamePanel = GameObject.Find("_GamePanel");
    }
    
    public void Start()
    {
        gamePanel.SetActive(false);
        tableInfoPanel.SetActive(true);
        idText.SetActive(false);
    }

    public void RegisterGame()
    {
        CurrentGame = new Game {id = idField.GetComponent<InputField>().text };
        RestClient.Post(ClientMaster.HOST + "games", CurrentGame).Then(response =>
        {
            var r = JsonConvert.DeserializeObject<Response<RegisterGameData>>(response.Text);
            if (r.status != 200)
            {
                Debug.Log(r.error);
                return;
            }

            CurrentGame.created = true;
            CurrentGame.secret = r.data.secret;
            Debug.Log(CurrentGame.secret);

            idText.SetActive(true);
            idText.GetComponent<Text>().text = CurrentGame.id;

            registerPanel.SetActive(false);

            updateCooldown = 1;
        }).Catch(response =>
        {
            GameObject.Find("_Log").GetComponent<Text>().text = response.ToString();
            Debug.Log(response);
        });
    }

    public void StartGame()
    {
        tableInfoPanel.SetActive(false);
        gamePanel.SetActive(true);
        GetComponent<GameFlowLogic>().Play(CurrentGame);
    }

    public void AddPlayer()
    {
        if (currentPlayers >= spawnPoints.Length)
        {
            return;
        }

        var spawnPoint = spawnPoints[currentPlayers++];
        Instantiate(pawnPrefab, spawnPoint, Quaternion.identity);
    }

    void Update()
    {
        if (updateCooldown > 0)
        {
            updateCooldown -= Time.deltaTime;
            if (updateCooldown <= 0)
            {
                updateCooldown = 1;

                RestClient.Get(ClientMaster.HOST + "players/" + CurrentGame.id).Then(response =>
                {
                    var r = JsonConvert.DeserializeObject<Response<PlayersData>>(response.Text);
                    if (r.status != 200)
                    {
                        Debug.Log(r.error);
                        return;
                    }

                    foreach (var p in r.data.players)
                    {
                        if (!players.Contains(p.teamName))
                        {
                            players.Add(p.teamName);
                            var id = ++nextPlayerId;
                            if (id >= 4)
                            {
                                return;
                            }

                            var obj = GameObject.Find("_Player" + id + "Name");
                            obj.GetComponent<Text>().text = p.teamName;

                            obj = GameObject.Find("_Player" + id);
                            obj.name = "_Player_" + p.teamName;
                        }
                    }
                });
            }
        }
    }
}
