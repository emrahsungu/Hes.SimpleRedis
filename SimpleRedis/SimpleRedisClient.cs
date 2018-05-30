namespace Hes.SimpleRedis {

    using System;
    using System.Dynamic;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    public sealed class SimpleRedisClient : DynamicObject, IDisposable {
        private MemoryStream _buffer;
        private NetworkStream _networkStream;
        private BufferedStream _outputStream;

        public SimpleRedisClient(string host = "127.0.0.1", int port = 6379) {
            Socket socket = null;
            try {
                // connect
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {NoDelay = true};
                socket.Connect(new DnsEndPoint(host, port));
                _networkStream = new NetworkStream(socket, true);

                // NetworkStream owns socket, no problem here
                socket = null;

                // Do not forget to buffer when writing to avoid excessive packet fragmentation
                _outputStream = new BufferedStream(_networkStream, 2048);
                _buffer = new MemoryStream();
            }
            catch {
                socket?.Dispose();
                Dispose();
                throw;
            }
        }

        public void Dispose() {
            Dispose(ref _outputStream);
            Dispose(ref _networkStream);
            Dispose(ref _buffer);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result) {
            WriteCommand(binder.Name, args);
            result = ReadResult();
            var err = result as SimpleRedisExceptionResult;
            if(err != null) {
                throw err.GetException();
            }
            return true;
        }

        private static void Dispose<T>(ref T field) where T : class, IDisposable {
            if(field == null) {
                return;
            }

            try {
                field.Dispose();
            }
            catch {
                //Do nothing, http://explosm.net/comics/2313/
            }
            finally {
                field = null;
            }
        }

        private void WriteRawData(Stream target, char value) {
            if(value < 128) {
                _outputStream.WriteByte((byte) value);
            }
            else {
                WriteRawData(target, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void WriteRawData(Stream target, string value) {
            var bytes = Encoding.UTF8.GetBytes(value);
            target.Write(bytes, 0, bytes.Length);
        }

        private static void WriteRawData(Stream target, int value) {
            if(value >= 0 && value < 10) {
                target.WriteByte((byte) ('0' + value));
            }
            else {
                WriteRawData(target, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void WriteRawData(Stream target, byte[] value) {
            target.Write(value, 0, value.Length);
        }

        private static void WriteRawData(Stream target, ArraySegment<byte> value) {
            target.Write(value.Array, value.Offset, value.Count);
        }

        private void WriteEndLine() {
            WriteRawData(_outputStream, '\r');
            WriteRawData(_outputStream, '\n');
        }

        private void WriteArg(object value) {
            // We need to know the length, We write to our memory-stream first
            _buffer.SetLength(0);
            SimpleRedisClient.WriteRawData(_buffer, (dynamic) value);

            // Then now write that to the (bufferred) output
            WriteRawData(_outputStream, '$');
            WriteRawData(_outputStream, (int) _buffer.Length);
            WriteEndLine();
            WriteRawData(_outputStream, new ArraySegment<byte>(_buffer.GetBuffer(), 0, (int) _buffer.Length));
            WriteEndLine();
        }

        private void WriteCommand(string name, object[] args) {
            WriteRawData(_outputStream, '*');
            WriteRawData(_outputStream, 1 + args.Length);
            WriteEndLine();
            WriteArg(name);
            foreach(var t in args) {
                WriteArg(t);
            }

            _outputStream.Flush();
            _networkStream.Flush();
        }

        private static Exception EndOfStream() {
            throw new EndOfStreamException("The server has disconnected");
        }

        private byte[] ReadToNewline() {
            var ms = new MemoryStream();
            do {
                var val = _networkStream.ReadByte();
                if(val < 0) {
                    throw EndOfStream();
                }

                if(val == '\r') {
                    val = _networkStream.ReadByte();
                    if(val == '\n') {
                        return ms.ToArray();
                    }
                    throw new InvalidOperationException("Expected end-of-line");
                }
                ms.WriteByte((byte) val);
            }
            while(true);
        }

        private int ReadLength() {
            var lenBlob = ReadToNewline();
            if(lenBlob.Length != 1){ return int.Parse(Encoding.ASCII.GetString(lenBlob), CultureInfo.InvariantCulture);}
            var len = lenBlob[0] - '0';
            if(len < 0 || len > 9) {
                throw new InvalidOperationException("Error reading bulk-reply");
            }
            return len;
        }

        private SimpleRedisResult ReadBulk() {
            int len = ReadLength(), offset = 0, read;
            if(len == -1) {
                return new SimpleRedisResult(null);
            }
            var data = new byte[len];
            while(len > 0 && (read = _networkStream.Read(data, offset, len)) > 0) {
                len -= read;
                offset += read;
            }
            if(len != 0) {
                throw EndOfStream();
            }
            ReadEndOfLine();
            return new SimpleRedisResult(data);
        }

        private void ReadEndOfLine() {
            if(_networkStream.ReadByte() != '\r' || _networkStream.ReadByte() != '\n')
                throw new InvalidOperationException("Expected end-of-line");
        }

        private object[] ReadMultiBulk() {
            var len = ReadLength();
            if(len == -1) {
                return null;
            }

            var results = new object[len];
            for(var i = 0; i < len; i++) {
                results[i] = ReadResult();
            }
            return results;
        }

        private object ReadResult() {
            var type = _networkStream.ReadByte();
            if(type < 0) {
                throw EndOfStream();
            }

            switch(type) {
                case (byte) '+': // status
                    return new SimpleRedisStatusResult(ReadToNewline());

                case (byte) '-': // error
                    return new SimpleRedisExceptionResult(ReadToNewline());

                case (byte) ':': // integer
                    return new SimpleRedisResult(ReadToNewline());

                case (byte) '$': // bulk
                    return ReadBulk();

                case (byte) '*': // multi-bulk
                    return ReadMultiBulk();

                default:
                    throw new NotSupportedException("Unexpected reply type: " + (char) type);
            }
        }
    }
}