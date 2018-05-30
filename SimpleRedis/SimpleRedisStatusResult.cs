namespace Hes.SimpleRedis {

    using System;

    internal class SimpleRedisStatusResult : SimpleRedisResult {

        public SimpleRedisStatusResult(byte[] value) : base(value) {
        }

        protected override bool GetBoolean(out bool value) {
            string s;
            if(GetString(out s)) {
                value = string.Equals(s, "OK", StringComparison.InvariantCultureIgnoreCase);
                return true;
            }

            value = false;
            return false;
        }
    }
}