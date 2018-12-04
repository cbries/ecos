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
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using ECoSUtils;

	public delegate void ConnectorFasterMessageReceived(object sender, MessageEventArgs eventArgs);
	public delegate void ConnectorFasterFailed(object sender, MessageEventArgs eventArgs);

	public class ConnectorFaster : IConnector
	{
		public event EventHandler Started;
		public event ConnectorFasterFailed Failed;
		public event EventHandler Stopped;
		public event ConnectorFasterMessageReceived MessageReceived;

		public log4net.ILog Log { get; set; }
		public string LastError { get; private set; }

		public string IpAddress { get; set; }
		public UInt16 Port { get; set; }

		#region IConnector

		public bool Start()
		{
			try
			{
				if (_thread != null && _thread.IsAlive)
					return true;

				_thread = new Thread(async () =>
				{
					Thread.CurrentThread.IsBackground = true;

					await StartHandler();
				});

				_thread.Start();

				_run = _thread.ThreadState == ThreadState.Running || _thread.ThreadState == ThreadState.Background;

				if (_run)
					return true;

				try
				{
					_thread.Abort(null);
					_thread = null;
				}
				catch
				{
					// ignore
				}

				return false;
			}
			catch (Exception ex)
			{
				LastError = ex.Message;
				return false;
			}
		}

		public bool Stop()
		{
			_run = false;
			_client?.Disconnect();
			_client = null;
			return true;
		}

		public bool SendMessage(string commandMessage)
		{
			if (string.IsNullOrEmpty(commandMessage)) return false;
			if (_client == null || !_client.IsConnected) return false;
			if (_client.IsConnected) return _client.SendMessage(commandMessage);
			return false;
		}

		public bool SendCommand(object command)
		{
			if (command is ICommand c)
				return SendMessage(c.ToString());
			return SendMessage(null);
		}

		public bool SendCommands(IReadOnlyList<object> commands)
		{
			var cmds = new List<ICommand>();
			foreach (var c in commands)
			{
				if (c is ICommand cc)
					cmds.Add(cc);
			}
			return SendMessage(string.Join("\r\n", cmds));
		}

		#endregion

		private bool _run;
		private Thread _thread;
		private TcpClient _client;

		private async Task StartHandler()
		{
			string ipaddr = IpAddress;
			int port = Port;

			try
			{
				_client = new TcpClient
				{
					Log = Log
				};
				_client.LineReceived += (sender, line) =>
				{
					if(string.IsNullOrEmpty(line)) return;
					Log?.Info($"<Connector> Message received, Length: {line.Length} -> {line}");
					MessageReceived?.Invoke(this, new MessageEventArgs(line));
				};
				_client.SendFailed += (sender, ex) =>
				{
					Log?.Info($"<Connector> Send failed: {ex.Message}");
					Failed?.Invoke(this, new MessageEventArgs($"Send of message to {ex.Message}"));
				};

				_client.Connect(ipaddr, port);

				Log?.Info($"<Connector> Connection established to {ipaddr}:{port}");
				Started?.Invoke(this, null);

				await _client.HandleLines();
			}
			catch (Exception ex)
			{
				Log?.Info($"<Connector> Connection failed to {ipaddr}:{port} with {ex.Message}");
				Failed?.Invoke(this, new MessageEventArgs($"Connection failed to {ipaddr}:{port} with {ex.Message}"));
			}
		}
	}
}
