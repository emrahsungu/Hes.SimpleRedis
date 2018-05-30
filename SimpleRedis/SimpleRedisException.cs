namespace Hes.SimpleRedis {

    using System;

    public class SimpleRedisException : Exception {

        public SimpleRedisException(string message) : base(message) {
        }
    }
}