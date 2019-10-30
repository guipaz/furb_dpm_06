using System;
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
    }

    public void Start()
    {
        idText.SetActive(false);
    }

    public void RegisterGame()
    {
        CurrentGame = new Game {id = idField.GetComponent<InputField>().text };
        RestClient.Post("http://dpm-furb.herokuapp.com/games", CurrentGame).Then(response =>
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
        }).Catch(response =>
        {
            Debug.Log(response);
        });
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
}
