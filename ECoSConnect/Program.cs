/*
 * MIT License
 *
 * Copyright (c) 2018 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace ECoSConnect
{
	using System;
	using System.IO;
	using System.Text;
	using System.Net.Sockets;
	using System.Threading;
    using C = System.Console;

	class Program
	{
		static void IsQuit(string msg)
		{
			if (string.IsNullOrEmpty(msg)) return;
			if (msg.Equals("exit", StringComparison.CurrentCultureIgnoreCase)
			    || msg.Equals("quit", StringComparison.OrdinalIgnoreCase))
				throw new Exception("Exit");
		}

		static void SendTests(ECoSConnect ecos)
		{
			if (ecos == null) return;
			for (int i = 0; i < 8; ++i)
			{
				for (int j = 257; j < 275; ++j)
				{
					var m = string.Format("get(1000, cv[{0}:{1}])\r\n", i, j);
					ecos.SendMessage(m);
					Thread.Sleep(100);
				}
			}
		}

		static void Main(string[] args)
		{
			var ecos = new ECoSConnect();
			var ip = args[0];
            new Thread(() => ecos.StartHandler(ip)).Start();

			Thread.Sleep(500);

			SendTests(ecos);

			try
            {
				for (;;)
				{
					var lineToSend = C.ReadLine();
					if (string.IsNullOrEmpty(lineToSend)) continue;
					lineToSend = lineToSend.Trim();
					if (lineToSend.Length <= 0) continue;
					IsQuit(lineToSend);
					ecos.SendMessage(lineToSend);
				}
			}
			catch
			{
				ecos.Disconnect();
				Environment.Exit(0);
			}
		}
	}

    public delegate void LineReceivedDelegate(object sender, string line);
    public delegate void SendFailedDelegate(object sender, Exception ex);

    public class ECoSConnect
    {
        public event LineReceivedDelegate LineReceived;
        public event SendFailedDelegate SendFailed;

        private TcpClient _client = null;

        public void Connect(string hostname, int port)
        {
            _client = new TcpClient(hostname, port);
        }

        public void Disconnect()
        {
            if (_client == null) return;
            _client.Close();
            _client.Dispose();
            _client = null;
        }

        public bool SendMessage(string msg)
        {
            try
            {
                if (string.IsNullOrEmpty(msg)) return false;
                if (_client == null) return false;
                if(!_client.Connected) return false;
	            Log("<Send> {0}", msg.Trim());
                var strm = _client.GetStream();
                var writer = new StreamWriter(strm) { AutoFlush = true };
                writer.WriteLine(msg);
                return true;
            }
            catch (Exception ex)
            {
                SendFailed?.Invoke(this, ex);
                return false;
            }
        }

        public void StartHandler(string ip, int port = 15471)
        {
            LineReceived += (sender, line) => {
                if (string.IsNullOrEmpty(line)) return;
                C.WriteLine($"<Empfangen> {line}");
                Log("<Recv> {0}", line.Trim());
            };
            SendFailed += (sender, ex) => C.WriteLine($"<Fehler> {ex.Message}");
            Connect(ip, port);
            var strm = _client.GetStream();
            using (var reader = new StreamReader(strm, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    LineReceived?.Invoke(this, line);
            }
        }

        private void Log(string fmt, params object [] data)
        {
            string dtNow =  DateTime.Now.ToString("HH:mm:ss.fff");
            string m = string.Format(fmt, data).Trim();
            File.AppendAllText("ECoSConnect.log", $"{dtNow}: {m}\r\n");
        }
    }
}