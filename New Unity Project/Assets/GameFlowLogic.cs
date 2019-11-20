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
    
    bool waiting = false;
    float updateCooldown;
    float nextRoundCooldown;
    GameObject questionLabel;
    GameObject answerLabel;
    Question currentQuestion;

    void NextQuestion()
    {
        answerLabel.SetActive(false);
        questionLabel.SetActive(true);

        var random = new Random();
        currentQuestion = new Question(random.Next(1, 10), random.Next(1, 10), random.Next(0, 2) == 0 ? Question.Operation.Sum : Question.Operation.Subtraction);

        ShowQuestion(currentQuestion);

        RestClient.Post(ClientMaster.HOST + "sendQuestion/" + game.id, currentQuestion).Then((response) =>
        {
            waiting = true;
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

        if (waiting)
        {
            updateCooldown -= Time.deltaTime;
            if (updateCooldown <= 0)
            {
                updateCooldown = 1;

                RestClient.Get(ClientMaster.HOST + "state/" + game.id, (e, response) =>
                {
                    var r = JsonConvert.DeserializeObject<Response<GameState>>(response.Text);
                    if (r.data.finished)
                    {
                        questionLabel.SetActive(false);
                        answerLabel.SetActive(true);
                        answerLabel.GetComponent<Text>().text = "Resposta: " + currentQuestion.GetAnswer();

                        nextRoundCooldown = 5;
                    }
                });
            }
        }
    }

    void ShowQuestion(Question question)
    {
        questionLabel.GetComponent<Text>().text = question.operatorA + " " + (question.operation == Question.Operation.Sum ? "+" : "-") + " " + question.operatorB + " = ?";
    }
}
