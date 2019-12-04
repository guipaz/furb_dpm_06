using System;
using System.Collections.Generic;
using Assets;
using Newtonsoft.Json;
using Proyecto26;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
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
    GameObject difficultyField;
    GameObject numberOfQuestionsField;
    GameObject questionNumber;
    GameObject winnerLabel;

    List<string> players = new List<string>();
    int nextPlayerId;

    float updateCooldown;
    float keepAliveCooldown;

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
        questionNumber = GameObject.Find("_QuestionNumber");
        tableInfoPanel = GameObject.Find("_TableInfoPanel");
        gamePanel = GameObject.Find("_GamePanel");
        difficultyField = GameObject.Find("_DifficultyField");
        numberOfQuestionsField = GameObject.Find("_QuestionsField");
        winnerLabel = GameObject.Find("_WinnerLabel");
    }
    
    public void Start()
    {
        Reset();
    }

    public void Reset()
    {
        idField.GetComponent<InputField>().text = "";
        gamePanel.SetActive(false);
        tableInfoPanel.SetActive(false);
        idText.SetActive(false);
        questionNumber.SetActive(false);
        winnerLabel.SetActive(false);
        registerPanel.SetActive(true);

        players.Clear();

        GameObject.FindGameObjectsWithTag("player")[0].name = "_Player1";
        GameObject.FindGameObjectsWithTag("player2")[0].name = "_Player2";
        GameObject.FindGameObjectsWithTag("player3")[0].name = "_Player3";
        GameObject.FindGameObjectsWithTag("player4")[0].name = "_Player4";

        GameObject.Find("_Player1Name").GetComponent<Text>().text = "";
        GameObject.Find("_Player2Name").GetComponent<Text>().text = "";
        GameObject.Find("_Player3Name").GetComponent<Text>().text = "";
        GameObject.Find("_Player4Name").GetComponent<Text>().text = "";
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
            tableInfoPanel.SetActive(true);

            updateCooldown = 1;
            keepAliveCooldown = 1;
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
        questionNumber.SetActive(true);
        winnerLabel.SetActive(true);

        var difficulty = difficultyField.GetComponent<Dropdown>().value;
        var numberOfQuestions = numberOfQuestionsField.GetComponent<InputField>().text ?? "1";
        GetComponent<GameFlowLogic>().Play(CurrentGame, (GameFlowLogic.Difficulty) difficulty, players,
            int.Parse(numberOfQuestions));
    }

    void Update()
    {
        if (keepAliveCooldown > 0)
        {
            keepAliveCooldown -= Time.deltaTime;
            if (keepAliveCooldown <= 0)
            {
                keepAliveCooldown = 1;
                KeepAlive();
            }
        }

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

    void KeepAlive()
    {
        RestClient.Post(ClientMaster.HOST + "keepAlive/" + CurrentGame.id, null);
    }
}
