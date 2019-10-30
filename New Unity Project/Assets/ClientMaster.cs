using System.Collections;
using System.Collections.Generic;
using Assets;
using Newtonsoft.Json;
using Proyecto26;
using UnityEngine;
using UnityEngine.UI;

public class ClientMaster : MonoBehaviour
{
    public GameObject cellPrefab;

    GameObject gameList;

    void Awake()
    {
        gameList = GameObject.Find("_GameList");
    }

    void Start()
    {
        RestClient.Get("http://localhost:3000/games/", (e, response) =>
        {
            var r = JsonConvert.DeserializeObject<Response<GetGamesData>>(response.Text);

            foreach (var g in r.data.games)
            {
                var go = Instantiate(cellPrefab, gameList.transform, false);
                go.transform.Find("_Text").gameObject.GetComponent<Text>().text = g.id;
            }
        });

        
    }
}
