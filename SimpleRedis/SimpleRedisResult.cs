namespace Hes.SimpleRedis {

    using System;
    using System.Dynamic;
    using System.Globalization;
    using System.Text;

    internal class SimpleRedisResult : DynamicObject {
        private readonly byte[] _value;

        public SimpleRedisResult(byte[] value) {
            _value = value;
        }

        public override string ToString() {
            string s;
            if(GetString(out s)) return s;

            return base.ToString();
        }

        public override bool TryConvert(ConvertBinder binder, out object result) {
            var type = binder.Type;

            // handle nulls
            if(type.IsClass) {
                if(_value == null) {
                    result = null;
                    return true;
                }
            }
            else {
                var underlyingType = Nullable.GetUnderlyingType(type);
                if(underlyingType != null) {
                    if(_value == null) {
                        result = null;
                        return true;
                    }

                    type = underlyingType;
                }
            }

            if(type == typeof(byte[])) {
                byte[] val;
                if(GetBytes(out val)) {
                    result = val;
                    return true;
                }
            }

            if(type == typeof(string)) {
                string val;
                if(GetString(out val)) {
                    result = val;
                    return true;
                }
            }

            if(type == typeof(int)) {
                int val;
                if(GetInt32(out val)) {
                    result = val;
                    return true;
                }
            }

            if(type == typeof(long)) {
                long val;
                if(GetInt64(out val)) {
                    result = val;
                    return true;
                }
            }

            if(type == typeof(bool)) {
                bool val;
                if(GetBoolean(out val)) {
                    result = val;
                    return true;
                }
            }

            return base.TryConvert(binder, out result);
        }

        protected virtual bool GetBytes(out byte[] value) {
            value = this._value;
            return true;
        }

        protected virtual bool GetString(out string value) {
            byte[] bytes;
            if(GetBytes(out bytes)) {
                value = bytes == null ? null : Encoding.UTF8.GetString(bytes);
                return true;
            }

            value = null;
            return false;
        }

        protected virtual bool GetBoolean(out bool value) {
            byte[] bytes;
            if(GetBytes(out bytes) && bytes.Length == 1)
                switch(bytes[0]) {
                    case (byte) '0':
                        value = false;
                        return true;

                    case (byte) '1':
                        value = true;
                        return true;
                }
            value = false;
            return false;
        }

        protected virtual bool GetInt64(out long value) {
            string s;
            if(GetString(out s)) {
                value = long.Parse(s, CultureInfo.InvariantCulture);
                return true;
            }

            value = 0;
            return false;
        }

        protected virtual bool GetInt32(out int value) {
            string s;
            if(GetString(out s)) {
                value = int.Parse(s, CultureInfo.InvariantCulture);
                return true;
            }

            value = 0;
            return false;
        }
    }
}