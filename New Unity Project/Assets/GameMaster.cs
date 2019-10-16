using System;
using Assets;
using Newtonsoft.Json;
using Proyecto26;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    public static Game CurrentGame;

    public GameObject pawnPrefab;
    public Vector2[] spawnPoints;
    int currentPlayers = 0;

    [Serializable]
    public class Game
    {
        public bool created;
        public string id;
        public string secret;
    }
    
    public void Start()
    {
        CurrentGame = new Game {id = "mygame"};
        RegisterGame();
    }

    void RegisterGame()
    {
        RestClient.Post("http://localhost:3000/games", CurrentGame).Then(response =>
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
