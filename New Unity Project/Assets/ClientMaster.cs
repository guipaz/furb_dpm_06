using Assets;
using Newtonsoft.Json;
using Proyecto26;
using UnityEngine;
using UnityEngine.UI;

public class ClientMaster : MonoBehaviour
{
    public const string HOST = "http://localhost:3000/";

    public GameObject gamesListPanel;
    public GameObject choicesPanel;
    public GameObject nameChooserPanel;
    public GameObject waitingLabel;

    public GameObject cellPrefab;

    float updateCooldown;
    bool waiting;
    string gameId;
    bool updatedQuestions;

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
        waitingLabel.SetActive(false);

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
            
        RestClient.Get(HOST + "games/", (e, response) =>
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
                nameChooserPanel.SetActive(false);
                waitingLabel.SetActive(true);
                waiting = true;
                gameId = id;
            });
        });
    }

    void Update()
    {
        if (waiting)
        {
            updateCooldown -= Time.deltaTime;
            if (updateCooldown <= 0)
            {
                updateCooldown = 1;
                
                RestClient.Get(HOST + "state/" + gameId, (e, response) =>
                {
                    var r = JsonConvert.DeserializeObject<Response<GameState>>(response.Text);
                    if (r.data.started)
                    {
                        waitingLabel.SetActive(false);

                        if (r.data.finished)
                        {
                            choicesPanel.SetActive(false);
                            //TODO mostra outra coisa
                        }
                        else if (!updatedQuestions)
                        {
                            var question = r.data.currentQuestion;
                            choicesPanel.SetActive(true);

                            var answer1 = GameObject.Find("_Answer1");
                            var answer2 = GameObject.Find("_Answer2");
                            var answer3 = GameObject.Find("_Answer3");
                            var answer4 = GameObject.Find("_Answer4");

                            answer1.GetComponent<Text>().text = question.options[0].ToString();
                            answer2.GetComponent<Text>().text = question.options[1].ToString();
                            answer3.GetComponent<Text>().text = question.options[2].ToString();
                            answer4.GetComponent<Text>().text = question.options[3].ToString();

                            updatedQuestions = true;
                        }
                    }
                });
            }
        }
    }
}
