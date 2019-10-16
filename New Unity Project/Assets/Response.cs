using System;

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
}
