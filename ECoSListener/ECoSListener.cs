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
	using System.Collections.Generic;
	using ECoSConnector;
	using ECoSEntities;
	using ECoSUtils;

	public delegate void SnifferChanged(object sender, IDataProvider dataProvider);

	public partial class ECoSListener
	{
		public event SnifferChanged Changed;

		public log4net.ILog Log { get; set; }

		private string _ipaddr;
		private ushort _port;

		internal ConnectorFaster Connector { get; set; }
		internal ConnectorFaster S88Connector { get; set; }
		internal IDataProvider DataProvider { get; private set; }
		internal IDataProvider DataProviderS88 { get; private set; }
		internal WsRouter Router { get; private set; }

		public ECoSListener(string ipaddr, ushort port)
		{
			_ipaddr = ipaddr;
			_port = port;
		}

		public ConnectorFaster GetConnector()
		{
			if (Connector == null)
				Connector = new ConnectorFaster
				{
					IpAddress = _ipaddr,
					Port = _port,
					Log = Log
				};
			return Connector;
		}

		public ConnectorFaster GetS88Connector()
		{
			if (S88Connector == null)
				S88Connector = new ConnectorFaster
				{
					IpAddress = _ipaddr,
					Port = _port,
					Log = Log
				};
			return S88Connector;
		}

		public IDataProvider GetDataProvider()
		{
			if (DataProvider == null)
			{
				DataProvider = new DataProvider(ECoSEntities.DataProvider.DataModeT.General)
				{
					Log = Log
				};
				var dp = (DataProvider)DataProvider;
				dp.Commands += DpOnCommands;
				dp.Modified += DpOnModified;
			}
			return DataProvider;
		}

		public IDataProvider GetDataProviderS88()
		{
			if (DataProviderS88 == null)
			{
				DataProviderS88 = new DataProvider(ECoSEntities.DataProvider.DataModeT.S88)
				{
					Log = Log
				};
				var dp = (DataProvider)DataProviderS88;
				dp.Commands += DpOnCommandsS88;
				dp.Modified += DpOnModified;
			}
			return DataProviderS88;
		}

		private void DpOnModified(object sender)
		{
			var dp = sender as DataProvider;
			if (dp == null) return;

			if (Router == null)
			{
				Log?.Error("Error: router is not set");
				return;
			}

			var obj = dp.ToJson();
			if (obj == null)
			{
				Log?.Error("Error: data object is null");
				return;
			}

			Router.SendBroadcast(obj, dp.Mode);
		}

		private void DpOnCommands(object sender, IReadOnlyList<ICommand> commands)
		{
			Connector?.SendCommands(commands);
		}

		private void DpOnCommandsS88(object sender, IReadOnlyList<ICommand> s88commands)
		{
			S88Connector?.SendCommands(s88commands);
		}

		public bool Start()
		{
			Log?.Info("Sniffer started");

			var c = GetConnector();
			c.Started += ConnectorOnStarted;
			c.Stopped += ConnectorOnStopped;
			c.Failed += ConnectorOnFailed;
			c.MessageReceived += ConnectorOnMessageReceived;

			var s88c = GetS88Connector();
			s88c.Started += S88COnStarted;
			s88c.Stopped += S88COnStopped;
			s88c.Failed += S88COnFailed;
			s88c.MessageReceived += S88COnMessageReceived;

			var r0 = c.Start();
			var r1 = s88c.Start();
			var r2 = StartWsRouter();

			return r0 && r1 && r2;
		}

		public void Stop()
		{
			Connector?.Stop();
			Connector = null;

			S88Connector?.Stop();
			S88Connector = null;
		}
	}
}
