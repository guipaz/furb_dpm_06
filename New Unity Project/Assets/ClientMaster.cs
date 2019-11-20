using Assets;
using Newtonsoft.Json;
using Proyecto26;
using UnityEngine;
using UnityEngine.UI;

public class ClientMaster : MonoBehaviour
{
    //public const string HOST = "http://localhost:3000/";
    public const string HOST = "http://dpm-furb.herokuapp.com/";

    public GameObject gamesListPanel;
    public GameObject choicesPanel;
    public GameObject nameChooserPanel;
    public GameObject waitingLabel;

    public GameObject cellPrefab;

    float updateCooldown;
    bool playing;
    string gameId;
    string teamName;
    bool updatedQuestions;

    GameObject gameList;

    public class ClientJoin : IData
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
            var teamName = nameChooserPanel.GetComponentInChildren<InputField>().text;
            RestClient.Post(HOST + "enterGame/", new ClientJoin { id = id, teamName = teamName }).Then((response) =>
            {
                nameChooserPanel.SetActive(false);
                waitingLabel.SetActive(true);
                playing = true;
                gameId = id;
                this.teamName = teamName;
            });
        });
    }

    void Update()
    {
        if (playing)
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

                        var game = r.data;
                        var question = game.currentQuestion;

                        //TODO on finish game

                        if (question.timeUp)
                        {
                            choicesPanel.SetActive(false);
                            updatedQuestions = false;
                            //TODO mostra outra coisa
                        }
                        else if (!updatedQuestions)
                        {
                            choicesPanel.SetActive(true);

                            var answer1 = GameObject.Find("_Answer1");
                            var answer2 = GameObject.Find("_Answer2");
                            var answer3 = GameObject.Find("_Answer3");
                            var answer4 = GameObject.Find("_Answer4");

                            answer1.GetComponent<Text>().text = question.options[0].ToString();
                            answer2.GetComponent<Text>().text = question.options[1].ToString();
                            answer3.GetComponent<Text>().text = question.options[2].ToString();
                            answer4.GetComponent<Text>().text = question.options[3].ToString();

                            var answerButton1 = GameObject.Find("_AnswerButton1");
                            var answerButton2 = GameObject.Find("_AnswerButton2");
                            var answerButton3 = GameObject.Find("_AnswerButton3");
                            var answerButton4 = GameObject.Find("_AnswerButton4");

                            answerButton1.GetComponent<Image>().color = Color.white;
                            answerButton2.GetComponent<Image>().color = Color.white;
                            answerButton3.GetComponent<Image>().color = Color.white;
                            answerButton4.GetComponent<Image>().color = Color.white;

                            updatedQuestions = true;
                        }
                    }
                });
            }
        }
    }

    public class AnAnswer
    {
        public string player;
        public string answer;
    }

    public void Answer(string id)
    {
        GameObject obj = null;
        switch (id)
        {
            case "1":
                obj = GameObject.Find("_AnswerButton1");
                break;
            case "2":
                obj = GameObject.Find("_AnswerButton2");
                break;
            case "3":
                obj = GameObject.Find("_AnswerButton3");
                break;
            case "4":
                obj = GameObject.Find("_AnswerButton4");
                break;
        }

        obj.GetComponent<Image>().color = Color.green;

        RestClient.Post(ClientMaster.HOST + "sendAnswer/" + gameId, new AnAnswer { player = teamName, answer = id }).Then(response =>
        {
            
        }).Catch(response =>
        {
            GameObject.Find("_Log").GetComponent<Text>().text = response.ToString();
            Debug.Log(response);
        });
    }
}
