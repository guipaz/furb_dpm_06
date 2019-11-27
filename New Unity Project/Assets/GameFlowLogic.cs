using System.Collections.Generic;
using Assets;
using Newtonsoft.Json;
using Proyecto26;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class GameFlowLogic : MonoBehaviour
{
    public class Question
    {
        public enum Operation
        {
            Sum = 1, Subtraction = 2
        }

        public int operatorA;
        public int operatorB;
        public Operation operation;
        public List<int> options;
        public bool timeUp;
        
        public Question(int operatorA, int operatorB, Operation operation)
        {
            this.operatorA = operatorA;
            this.operatorB = operatorB;
            this.operation = operation;
            options = GetOptions();
        }

        public int GetAnswer()
        {
            switch (operation)
            {
                case Operation.Sum:
                    return operatorA + operatorB;
                case Operation.Subtraction:
                    return operatorA - operatorB;
            }

            return 0;
        }

        List<int> GetOptions()
        {
            var options = new List<int>();
            var realAnswer = GetAnswer();
            options.Add(realAnswer);
            options.Add(realAnswer + 1);
            options.Add(realAnswer - 1);
            options.Add(realAnswer + 2);
            return options;
        }
    }
    
    public GameMaster.Game game;
    public void Play(GameMaster.Game game)
    {
        this.game = game;

        questionLabel = GameObject.Find("_QuestionLabel");
        answerLabel = GameObject.Find("_AnswerLabel");

        NextQuestion();
    }
    
    bool playing = false;
    float updateCooldown;
    float nextRoundCooldown;
    float timeUpCooldown;
    GameObject questionLabel;
    GameObject answerLabel;
    Question currentQuestion;
    List<GameObject> resetPlayers = new List<GameObject>();

    void NextQuestion()
    {
        answerLabel.SetActive(false);
        questionLabel.SetActive(true);

        var random = new Random();
        currentQuestion = new Question(random.Next(1, 10), random.Next(1, 10), random.Next(0, 2) == 0 ? Question.Operation.Sum : Question.Operation.Subtraction);

        ShowQuestion(currentQuestion);

        foreach (var obj in resetPlayers)
        {
            obj.GetComponent<SpriteRenderer>().color = Color.white;
        }

        RestClient.Post(ClientMaster.HOST + "sendQuestion/" + game.id, currentQuestion).Then((response) =>
        {
            playing = true;
            timeUpCooldown = 10;
        }).Catch((response) =>
        {
            Debug.Log(response);
        });
    }

    void Update()
    {
        if (nextRoundCooldown > 0)
        {
            nextRoundCooldown -= Time.deltaTime;
            if (nextRoundCooldown <= 0)
            {
                NextQuestion();
            }

            return;
        }

        if (timeUpCooldown > 0)
        {
            timeUpCooldown -= Time.deltaTime;
            if (timeUpCooldown <= 0)
            {
                FinishQuestion();
            }
        }
    }
    
    public void FinishQuestion()
    {
        timeUpCooldown = 0;

        RestClient.Post(ClientMaster.HOST + "finishQuestion/" + game.id, null);

        questionLabel.SetActive(false);
        answerLabel.SetActive(true);

        RestClient.Get(ClientMaster.HOST + "answers/" + game.id, (e, response) =>
        {
            var r = JsonConvert.DeserializeObject<Response<AnswersData>>(response.Text);
            var answers = r.data.answers;
            foreach (var a in answers)
            {
                var obj = GameObject.Find("_Player_" + a.player);
                obj.GetComponent<SpriteRenderer>().color = currentQuestion.GetAnswer() == currentQuestion.options[int.Parse(a.answer) - 1] ? Color.green : Color.red;
                resetPlayers.Add(obj);
            }

            answerLabel.GetComponent<Text>().text = "Resposta: " + currentQuestion.GetAnswer();// + "\n\n" + answersString;

            nextRoundCooldown = 5;
        });
    }

    void ShowQuestion(Question question)
    {
        questionLabel.GetComponent<Text>().text = question.operatorA + " " + (question.operation == Question.Operation.Sum ? "+" : "-") + " " + question.operatorB + " = ?";
    }
}

