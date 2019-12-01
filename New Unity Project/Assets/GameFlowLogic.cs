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
        public int operatorC;
        public int operatorD;
        public Operation operation;
        public List<int> options;
        public bool timeUp;
        
        public Question(int operatorA, int operatorB, int operatorC, int operatorD, Operation operation)
        {
            this.operatorA = operatorA;
            this.operatorB = operatorB;
            this.operatorC = operatorC;
            this.operatorD = operatorD;
            this.operation = operation;
            options = GetOptions();
        }

        public int GetAnswer()
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    switch (operation)
                    {
                        case Operation.Sum:
                            return operatorA + operatorB;
                        case Operation.Subtraction:
                            return operatorA - operatorB;
                    }
                case Difficulty.Medium:
                    switch (operation)
                    {
                        case Operation.Sum:
                            return operatorD == 0 ? operatorA + operatorB + operatorC : operatorA + operatorB + operatorC + operatorD;
                        case Operation.Subtraction:
                            return operatorD == 0 ? operatorA - operatorB - operatorC : operatorA - operatorB - operatorC - operatorD;
                    }
                case Difficulty.Hard:
                    switch (operation)
                    {
                        case Operation.Sum:
                            return operatorD == 0 ? operatorA + operatorB + operatorC : operatorA + operatorB + operatorC + operatorD;
                        case Operation.Subtraction:
                            return operatorD == 0 ? operatorA - operatorB - operatorC : operatorA - operatorB - operatorC - operatorD;
                        case Operation.Multiplication:
                            return operatorA * operatorB;
                    }
                }
            }
            return 0;
        }

        List<int> GetOptions()
        {
            var options = new List<int>();
            var realAnswer = GetAnswer();
            options.Add(realAnswer <= 1 ? realAnswer + 3 : realAnswer - 1);            
            options.Add(realAnswer);
            options.Add(realAnswer + 1);            
            options.Add(realAnswer + 2);
            return options;
        }
    }
    
    public GameMaster.Game game;
    public void Play(GameMaster.Game game, Difficulty difficulty, List<string> players)
    {
        this.game = game;
        this.difficulty = difficulty;

        foreach (var p in players)
        {
            points[p] = 0;
        }

        questionLabel = GameObject.Find("_QuestionLabel");
        answerLabel = GameObject.Find("_AnswerLabel");
        questionNumber = GameObject.Find("_QuestionNumber");
        winnerLabel = GameObject.Find("_WinnerLabel");
        winnerLabel.SetActive(false);

        NextQuestion();
    }
    public enum Difficulty
    {
        Easy = 0, Medium = 1, Hard = 2
    }
    
    bool playing;
    int currentQuestionNumber = 1;
    float updateCooldown;
    float nextRoundCooldown;
    float timeUpCooldown;
    GameObject questionLabel;
    GameObject answerLabel;
    GameObject questionNumber;
    GameObject winnerLabel;
    Question currentQuestion;
    List<GameObject> resetPlayers = new List<GameObject>();
    Dictionary<string, int> points = new Dictionary<string, int>();
    Difficulty difficulty;

    void NextQuestion()
    {
        answerLabel.SetActive(false);
        questionLabel.SetActive(true);

        var random = new Random();
        switch (difficulty)
        {
            case Difficulty.Easy:
                currentQuestion = new Question(random.Next(1, 20), random.Next(1, 20), 0, 0, random.Next(0, 2) == 0 ? Question.Operation.Sum : Question.Operation.Subtraction);
                if (currentQuestion.operation == Question.Operation.Subtraction)
                {
                    while (currentQuestion.operatorA < currentQuestion.operatorB)
                    {
                        currentQuestion.operatorA = random.Next(1, 20);
                    }
                }
                break;
            case Difficulty.Medium:
                currentQuestion = new Question(random.Next(1, 50), random.Next(1, 50), random.Next(1, 50), (random.Next(0, 2) == 0 ? random.Next(1, 50) : 0), (random.Next(0, 2) == 0 ? Question.Operation.Sum : Question.Operation.Subtraction));                
                break;
            case Difficulty.Hard:
                var op = (Question.Operation) random.Next(1, 3);
                if (op == Question.Operation.Multiplication)
                {
                    currentQuestion = new Question(random.Next(2, 10), random.Next(1, 10), 0, 0, op);
                } else 
                {
                    currentQuestion = new Question(random.Next(1, 100), random.Next(1, 100), random.Next(1, 100), (random.Next(0, 2) == 0 ? random.Next(1, 100) : 0), op);
                }                
                break;           
        }
        
        ShowQuestion(currentQuestion);

        foreach (var obj in resetPlayers)
        {
            obj.GetComponentInChildren<SpriteRenderer>().color = new Color(99/255f, 147/255f, 192/255f);
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
        if (!playing)
        {
            return;
        }

        if (nextRoundCooldown > 0)
        {
            nextRoundCooldown -= Time.deltaTime;
            if (nextRoundCooldown <= 0)
            {
                currentQuestionNumber++;
                if (currentQuestionNumber > 3)
                {
                    //TODO game finished
                    FinishGame();
                }
                else
                {
                    NextQuestion();
                }
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

    public void FinishGame()
    {
        string winnerName = "";
        int winnerPoints = 0;
        foreach (var player in points.Keys)
        {
            if (points[player] > winnerPoints)
            {
                winnerPoints = points[player];
                winnerName = player;
            }
        }

        playing = false;
        winnerLabel.SetActive(true);
        answerLabel.SetActive(false);
        questionLabel.SetActive(false);
        winnerLabel.GetComponent<Text>().text = "A equipe ganhadora é a '" + winnerName + "' com " + winnerPoints + " pontos!";
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
                var correctAnswer = currentQuestion.GetAnswer() == currentQuestion.options[int.Parse(a.answer) - 1];

                points[a.player] += correctAnswer ? 1 : 0;

                obj.GetComponentInChildren<SpriteRenderer>().color = correctAnswer ? Color.green : Color.red;
                resetPlayers.Add(obj);
            }

            answerLabel.GetComponent<Text>().text = "Resposta: " + currentQuestion.GetAnswer();// + "\n\n" + answersString;

            nextRoundCooldown = 5;
        });
    }

    void ShowQuestion(Question question)
    {
        var quest = "";
        if (question.operation == Question.Operation.Multiplication)
        {
            quest = question.operatorA + " * " + question.operatorB;
        } else
        {
            if (question.operatorC == 0)//2 op
            {
                quest = question.operatorA + (question.operation == Question.Operation.Sum ? " + " : " - ") + question.operatorB;
            } else if (question.operatorC != 0 && question.operatorD == 0)//3 op
            {
                var operation = question.operation == Question.Operation.Sum ? " + " : " - ";
                quest = question.operatorA + operation + question.operatorB + operation + question.operatorC;
            } else {//4 op
                var operation = question.operation == Question.Operation.Sum ? " + " : " - ";
                quest = question.operatorA + operation + question.operatorB + operation + question.operatorC + operation + question.operatorD;
            }
        }
        questionLabel.GetComponent<Text>().text = quest + " = ?";
    }
}

