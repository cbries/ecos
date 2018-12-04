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
	using System.Linq;
	using ECoSCore;
	using ECoSEntities;
	using ECoSUtils;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using SuperWebSocket;

	public partial class ECoSListener
	{
		private bool StartWsRouter()
		{
			try
			{
				if (Router != null && Router.IsListening())
					return true;
				Router = new WsRouter();
				Router.NewConnection += RouterOnNewConnection;
				Router.DataReceived += RouterOnDataReceived;
				Router.Listen(WsRouter.StartPort);
				var r = Router.IsListening();
				
				Log?.Info(string.Format("Router {0}",
					r ? "started -> localhost Port: " + Router.Port
						: "failed"));
				return Router.IsListening();
			}
			catch (Exception ex)
			{
				ex.Show();

				return false;
			}
		}

		private void RouterOnNewConnection(object sender, WebSocketSession session)
		{
			var dp = GetDataProvider();
			var dps88 = GetDataProviderS88();
			if (dp == null || dps88 == null)
			{
				Log?.Warn($"data providers have no data");
				return;
			}

			var obj = dp.ToJson();
			var jsonObj = obj.ToString(Formatting.None);
			var res0 = Router.SendUnicast(session, jsonObj);
			if (!res0)
			{
				Log?.Warn($"sent of general data failed");
				return;
			}

			var objS88 = dps88.ToJson();
			var jsonObjS88 = objS88.ToString(Formatting.None);
			var res1 = Router.SendUnicast(session, jsonObjS88);
			if (!res1)
			{
				Log?.Warn($"sent of s88 data failed");
				return;
			}
		}

		private void RouterOnDataReceived(object sender, WebSocketSession session, JObject json)
		{
			if (json == null)
			{
				Log?.Error("Error: json data is null");
				return;
			}

			string mode = null;
			if (json["mode"] != null)
				mode = json["mode"].ToString();
			if (string.IsNullOrEmpty(mode))
				return;

			switch (mode)
			{
				case "register":
				{
					bool enableLocomotives = false;
					bool enableAccessories = false;
					bool enableS88 = false;

					if (json["enableLocomotives"] != null)
						enableLocomotives = (bool) json["enableLocomotives"];
					if (json["enableAccessories"] != null)
						enableAccessories = (bool) json["enableAccessories"];
					if (json["enableS88"] != null)
						enableS88 = (bool) json["enableS88"];

					var wsSession = new WsRouterSessions()
					{
						EnableAccessories = enableAccessories,
						EnableLocomotives = enableLocomotives,
						EnableS88 = enableS88,
						Session = session
					};

					Router.Clients.AddIfNotExist(wsSession);
				}
					break;

				case "relayToEcos":
				{
					var encodedCommands = json["encodedCommands"].ToString();
					if (string.IsNullOrEmpty(encodedCommands))
					{
						Log?.Warn($"command list for relay is empty");
						return;
					}

					var cmds = Item.GetCommands(encodedCommands);
					if (cmds == null || cmds.Count == 0)
					{
						Log?.Warn($"command list for relay is empty");
						return;
					}

					var cmdList = cmds.ToList();
					foreach (var cmd in cmdList)
					{
						var icmd = cmd as ICommand;
						if (icmd == null) continue;
						Log?.Debug(icmd.NativeCommand);
					}

					Connector?.SendCommands(cmds.ToList());
				}
					break;
			}
		}
	}
}
