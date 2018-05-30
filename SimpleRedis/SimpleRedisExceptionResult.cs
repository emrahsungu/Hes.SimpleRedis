namespace Hes.SimpleRedis {

    using System;
    using System.Dynamic;

    internal class SimpleRedisExceptionResult : SimpleRedisResult {

        public SimpleRedisExceptionResult(byte[] value) : base(value) {
        }

        public override bool TryConvert(ConvertBinder binder, out object result) {
            var ex = GetException();
            if(binder.Type != typeof(Exception)) {
                throw ex;
            }
            result = ex;
            return true;
        }

        internal Exception GetException() {
            string s;
            if(!GetString(out s)) {
                s = "unknown error";
            }
            return new SimpleRedisException(s);
        }
    }
}