using Assets;
using Newtonsoft.Json;
using Proyecto26;
using UnityEngine;
using UnityEngine.UI;

public class ClientMaster : MonoBehaviour
{
    public GameObject gamesListPanel;
    public GameObject choicesPanel;
    public GameObject nameChooserPanel;

    public GameObject cellPrefab;

    GameObject gameList;

    public class ClientJoin
    {
        public string id;
        public string teamName;
    }

    void Awake()
    {
        gamesListPanel.SetActive(true);
        choicesPanel.SetActive(false);
        nameChooserPanel.SetActive(false);

        gameList = GameObject.Find("_GameList");
    }

    void Start()
    {
        RefreshGames();
    }

    public void RefreshGames()
    {
        foreach (Transform t in gameList.transform)
            Destroy(t.gameObject);
            
        RestClient.Get("http://dpm-furb.herokuapp.com/games/", (e, response) =>
        {
            var r = JsonConvert.DeserializeObject<Response<GetGamesData>>(response.Text);

            foreach (var g in r.data.games)
            {
                var go = Instantiate(cellPrefab, gameList.transform, false);
                go.transform.Find("_Text").gameObject.GetComponent<Text>().text = g.id;
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    ShowNameChooser(g.id);
                });
            }
        });
    }

    void ShowNameChooser(string id)
    {
        gamesListPanel.SetActive(false);
        nameChooserPanel.SetActive(true);

        nameChooserPanel.GetComponentInChildren<Button>().onClick.AddListener(() =>
        {
            RestClient.Post("http://dpm-furb.herokuapp.com/games/", new ClientJoin { id = id, teamName = nameChooserPanel.GetComponentInChildren<InputField>().text }).Then((response) =>
            {
                Debug.Log("deu boa");
            });
        });
    }
}
