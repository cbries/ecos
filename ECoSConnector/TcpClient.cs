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

namespace ECoSConnector
{
	using System;
	using System.IO;
	using System.Net.Sockets;
	using System.Text;
	using System.Threading.Tasks;

	public delegate void LineReceivedDelegate(object sender, string line);
	public delegate void SendFailedDelegate(object sender, Exception ex);

	public class TcpClient
	{
		public event LineReceivedDelegate LineReceived;
		public event SendFailedDelegate SendFailed;

		public log4net.ILog Log { get; set; }

		private System.Net.Sockets.TcpClient _tcpClient;

		public int Port {  get; set; }
		public string Host { get; set; }

		public bool IsConnected
		{
			get
			{
				if (_tcpClient == null) return false;
				return _tcpClient.Connected;
			}
		}	

		public TcpClient()
		{
			_tcpClient = null;
		}

		public void Disconnect()
		{
			try
			{
				if (_tcpClient == null) return;

				_tcpClient.Close();
				_tcpClient.Dispose();
				_tcpClient = null;
			}
			catch
			{
				// ignore
			}
		}

		public void Connect(string hostname, int port)
		{
			Port = port;
			Host = hostname;

			try
			{
				_tcpClient = new System.Net.Sockets.TcpClient(Host, Port);
			}
			catch (SocketException ex)
			{
				Log?.Error("Connect failed", ex);
				throw new Exception(ex.Message);
			}
		}
		
		public bool SendMessage(string msg)
		{
			if (string.IsNullOrEmpty(msg)) return false;
			if (!_tcpClient.Connected) return false;

			var strm = _tcpClient.GetStream();
			var writer = new StreamWriter(strm) { AutoFlush = true };

			try
			{
				if (_tcpClient.Connected)
					writer.WriteLine(msg);

				return true;
			}
			catch (Exception ex)
			{
				SendFailed?.Invoke(this, ex);
				Log?.Error("SendMessage failed", ex);
				return false;
			}
		}

		public async Task HandleLines()
		{
			try
			{
				var strm = _tcpClient.GetStream();
				using (StreamReader reader = new StreamReader(strm, Encoding.UTF8))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						LineReceived?.Invoke(this, line);
					}
				}
			}
			catch (IOException ex)
			{
				Log?.Error("HandleLines::IOException", ex);
			}
			catch (InvalidOperationException ex)
			{
				Log?.Error("HandleLines::InvalidOperationException", ex);
			}

			await Task.Delay(1000);			
		}
	}
}
