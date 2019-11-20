using System;
using System.Collections.Generic;

namespace Assets
{
    [Serializable]
    public class Response<T> where T : IData
    {
        public int status;
        public string error;
        public T data;
    }

    public interface IData { }

    public class RegisterGameData : IData
    {
        public string secret;
    }

    public class GetGamesData : IData
    {
        public List<GameMaster.Game> games;
    }

    public class GameState : IData
    {
        public bool started;
        public bool finished;
        public GameFlowLogic.Question currentQuestion;
    }
}
