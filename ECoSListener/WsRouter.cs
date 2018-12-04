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

namespace ECoSListener
{
	using System;
	using System.Net;
	using System.Net.NetworkInformation;
	using System.Net.Sockets;
	using ECoSCore;
	using ECoSEntities;
	using log4net;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using SuperSocket.SocketBase;
	using SuperSocket.SocketBase.Config;
	using SuperSocket.SocketEngine;
	using SuperWebSocket;

	public delegate void ReceiveHandler(object sender, WebSocketSession session, JObject json);

	public delegate void NewConnectionHandler(object sender, WebSocketSession session);

	public static class WsRouterHelper
	{
		public static IPAddress LocalIp()
		{
			var hostname = Dns.GetHostName();
			IPHostEntry host = Dns.GetHostEntry(hostname);
			var listOfAddrs = host.AddressList;
			foreach (IPAddress ip in listOfAddrs)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					return ip;
				}
			}
			return IPAddress.Loopback;
		}

		public static UInt16 FindUnusedPort(IPAddress ipaddress, UInt16 startPort)
		{
			return FindUnusedPort(ipaddress, startPort, SocketType.Stream, ProtocolType.Tcp);
		}

		public static UInt16 FindUnusedPort(IPAddress localAddr, UInt16 desiredPort, SocketType socketType, ProtocolType protocol)
		{
			for (var p = desiredPort; p <= IPEndPoint.MaxPort; p++)
			{
				if (!IsPortUsed(localAddr, p, socketType, protocol))
				{
					return p;
				}
			}
			throw new SocketException(10048);
		}

		public static bool IsPortUsed(IPAddress localAddr, UInt16 desiredPort, SocketType socketType, ProtocolType protocol)
		{
			var s = new Socket(AddressFamily.InterNetwork, socketType, protocol);
			try
			{
				if (IsPortReady(desiredPort))
					return true;

				IPEndPoint endpoint = new IPEndPoint(localAddr, desiredPort);
				s.Bind(endpoint);
				s.Close();
				return false;
			}
			catch (SocketException ex)
			{
				// EADDRINUSE?
				if (ex.ErrorCode == 10048 || ex.ErrorCode == 10013)
					return true;
			}
			return true;
		}

		public static bool IsPortReady(UInt16 port)
		{
			try
			{
				IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
				TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
				foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
				{
					if (tcpi.LocalEndPoint.Port == port)
						return true;
					if (tcpi.RemoteEndPoint.Port == port)
						return true;
				}

				IPEndPoint[] tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
				foreach (IPEndPoint tcpl in tcpListeners)
				{
					if (tcpl.Port == port)
						return true;
				}

				return true;
			}
			catch
			{
				return false;
			}
		}
	}

	public class WsRouter : IDisposable
	{
		public event ReceiveHandler DataReceived;
		public event NewConnectionHandler NewConnection;

		public static UInt16 StartPort = 10050;
		private WebSocketServer _webSocketServer;

		public ILog Log { get; set; }

		public UInt16 Port { get; private set; }

		public ConnectedClients Clients { get; } = new ConnectedClients();

		public bool IsListening()
		{
			if (_webSocketServer == null)
				return false;

			return _webSocketServer.State == ServerState.Running;
		}

		public bool Listen(UInt16 port = 0)
		{
			if (port == 0)
			{
				IPAddress ipAddress = WsRouterHelper.LocalIp();
				if (ipAddress == null)
					ipAddress = IPAddress.Loopback;

				port = WsRouterHelper.FindUnusedPort(ipAddress, StartPort);
			}

			try
			{
				var r = new RootConfig();

				const int timeOutSeconds = 60;
				const int timeOutMinutes = 60;
				const int timeOutMultiplicator = 10;

				Port = port;

				// https://supersocket.codeplex.com/wikipage?title=SuperSocket%20Basic%20Configuration
				var s = new ServerConfig
				{
					Name = "RailwayEssentialWebSocket",
					Ip = IPAddress.Any.ToString(),
					Port = port,
					KeepAliveTime = 60,
					KeepAliveInterval = 1,
					ClearIdleSession = false,
					ClearIdleSessionInterval = 120,
					IdleSessionTimeOut = timeOutSeconds * timeOutMinutes * timeOutMultiplicator,
					Mode = SocketMode.Tcp,
					MaxRequestLength = 4096 * 1024
				};

				var f = new SocketServerFactory();

				if (_webSocketServer != null)
				{
					_webSocketServer.Stop();
					_webSocketServer = null;
				}

				_webSocketServer = new WebSocketServer();
				_webSocketServer.Setup(r, s, f);

				_webSocketServer.NewSessionConnected += mWebSocketServer_NewSessionConnected;
				_webSocketServer.NewMessageReceived += mWebSocketServer_NewMessageReceived;
				_webSocketServer.SessionClosed += MWebSocketServerOnSessionClosed;

				return _webSocketServer.Start();
			}
			catch (Exception ex)
			{
				ex.Show();

				return false;
			}
		}

		private void mWebSocketServer_NewSessionConnected(WebSocketSession session)
		{
			NewConnection?.Invoke(this, session);
			Log?.Info($"New client: {session.Host}");
		}

		private JObject ToJson(string cnt)
		{
			if (string.IsNullOrEmpty(cnt)) return null;

			try
			{
				return JObject.Parse(cnt);
			}
			catch (Exception ex)
			{
				ex.Show();
			}

			return null;
		}

		private void mWebSocketServer_NewMessageReceived(WebSocketSession session, string value)
		{
			if (DataReceived == null) return;

			try
			{
				var o = ToJson(value);
				if (o != null && DataReceived != null)
					DataReceived(this, session, o);
			}
			catch (Exception ex)
			{
				ex.Show();
			}
		}

		private void MWebSocketServerOnSessionClosed(WebSocketSession session, CloseReason value)
		{
			var res = Clients.Remove(session);
			Log?.Info($"Client left: {session.Host}");
			Log?.Info($"Memory freed '{res}'");
			Log?.Info($"Current connected clients: {Clients.Count}");
		}

		public bool SendBroadcast(JObject json, DataProvider.DataModeT mode /* do we need this? */)
		{
			var r0 = IsListening();
			if (r0 == false) return false;
			var r1 = Clients.Count <= 0;
			if (r1) return false;

			try
			{
				var cc = Clients;

				foreach (var c in cc)
				{
					if (c.Session.Connected)
					{
						var deepCopy = (JObject)json.DeepClone();

						if (deepCopy != null)
						{
							if (!c.EnableLocomotives && deepCopy["locomotives"] != null)
								deepCopy.Remove("locomotives");
							if (!c.EnableAccessories && deepCopy["accessories"] != null)
								deepCopy.Remove("accessories");
							if (!c.EnableS88 && deepCopy["feedbacks"] != null)
								deepCopy.Remove("feedbacks");

							if (deepCopy["feedbacks"] != null)
							{
								var ar = deepCopy["feedbacks"] as JArray;
								if(ar != null && ar.Count == 0)
									deepCopy.Remove("feedbacks");
							}
						}

						if(deepCopy != null && deepCopy.Count > 1)
							c.Session.Send(deepCopy.ToString(Formatting.Indented));
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				ex.Show();

				return false;
			}
		}

		public bool SendUnicast(WebSocketSession session, string json)
		{
			if (session.InClosing) return false;
			try
			{
				session.Send(json);

				return true;
			}
			catch (Exception ex)
			{
				ex.Show();
			}

			return false;
		}

		public bool Shutdown()
		{
			if (_webSocketServer == null)
				return true;

			try
			{
				_webSocketServer.Stop();

				return true;
			}
			catch (Exception ex)
			{
				ex.Show();
			}

			return false;
		}

		public void Dispose()
		{
			if (_webSocketServer != null)
			{
				Shutdown();
				_webSocketServer.Dispose();

				_webSocketServer.NewMessageReceived -= mWebSocketServer_NewMessageReceived;
				_webSocketServer.SessionClosed -= MWebSocketServerOnSessionClosed;
				_webSocketServer = null;
			}

			GC.SuppressFinalize(this);
		}
	}
}
