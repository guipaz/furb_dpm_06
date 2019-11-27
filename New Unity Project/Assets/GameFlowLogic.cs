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
            Sum = 1, Subtraction = 2, Multiplication = 3
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
                case Operation.Multiplication:
                    return operatorA * operatorB;
            }

            return 0;
        }

        List<int> GetOptions()
        {
            var options = new List<int>();
            var realAnswer = GetAnswer();
            if (realAnswer <= 1) 
            {
                options.Add(realAnswer + 3);
            } else 
            {
                options.Add(realAnswer - 1);
            }
            options.Add(realAnswer);
            options.Add(realAnswer + 1);            
            options.Add(realAnswer + 2);
            return options;
        }
    }
    
    public GameMaster.Game game;
    public void Play(GameMaster.Game game, Difficulty difficulty)
    {
        this.game = game;
        this.difficulty = difficulty;

        questionLabel = GameObject.Find("_QuestionLabel");
        answerLabel = GameObject.Find("_AnswerLabel");

        NextQuestion();
    }
    public enum Difficulty
    {
        Easy = 0, Medium = 1, Hard = 2
    }
    
    bool playing = false;
    float updateCooldown;
    float nextRoundCooldown;
    float timeUpCooldown;
    GameObject questionLabel;
    GameObject answerLabel;
    Question currentQuestion;
    List<GameObject> resetPlayers = new List<GameObject>();

    Difficulty difficulty;//precisa pegar da tela pra popular aqui

    void NextQuestion()
    {
        answerLabel.SetActive(false);
        questionLabel.SetActive(true);

        var random = new Random();
        switch (difficulty)
        {
            case Difficulty.Easy:
                currentQuestion = new Question(random.Next(1, 20), random.Next(1, 20), random.Next(0, 2) == 0 ? Question.Operation.Sum : Question.Operation.Subtraction);
                if (currentQuestion.operation == Question.Operation.Subtraction)
                {
                    while (currentQuestion.operatorA < currentQuestion.operatorB)
                    {
                        currentQuestion.operatorA = random.Next(1, 20);
                    }                    
                }
                break;
            case Difficulty.Medium:
                currentQuestion = new Question(235, 193, Question.Operation.Subtraction); 
                break;
            case Difficulty.Hard:
                currentQuestion = new Question(235, 193, Question.Operation.Multiplication);
                break;           
        }
        
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
        questionLabel.GetComponent<Text>().text = question.operatorA + " " + (question.operation == Question.Operation.Sum ? "+" : (question.operation == Question.Operation.Multiplication ? "*" : "-")) + " " + question.operatorB + " = ?";
    }
}

