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

namespace ECoSEntities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using ECoSCore;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using WebSocket4Net;

	public delegate void WsConnectStop(object sender);
	public delegate void WsConnectError(object sender);

	/// <param name="sender">always the instance which called the event</param>
	/// <param name="provider">on any event a new instance is created</param>
	public delegate void WsConnectData(object sender, DataProvider provider);

    public class WsConnect
    {
	    public WsConnectStop WsStop;
		public WsConnectError WsError;
		public WsConnectData WsData;

		private WebSocket _ws = null;
	    private ManualResetEvent _waitFor = null;
		public string LastError { get; private set; }

	    public bool IsConnected()
	    {
		    if (_ws == null) return false;
		    if (_ws.State == WebSocketState.Open) return true;
		    if (_ws.State == WebSocketState.Connecting) return true;
		    return false;
	    }

	    private void CleanUp()
	    {
		    if (_ws == null) return;
		    try
		    {
			    _ws.Dispose();
		    }
		    catch
		    {
				// ignore
		    }
		    _ws = null;
	    }

		public bool ConnectTo(string host, string [] dataFields)
	    {
		    if (string.IsNullOrEmpty(host)) return false;
		    if (_ws != null && IsConnected()) return true;

		    if (dataFields == null || dataFields.Length == 0)
		    {
			    LastError = "no data field set";
				return false;
		    }

		    CleanUp();

			try
			{
				_waitFor = new ManualResetEvent(false);

				_ws = new WebSocket(host);
				_ws.Error += WsOnError;
				_ws.Closed += WsOnClosed;
				_ws.MessageReceived += WsOnMessageReceived;
				_ws.Opened += WsOnOpened;

				_ws.Open();

				var res = _waitFor.WaitOne(5 * 1000);

				if (res)
				{
					if (!SendDataFields(dataFields))
					{
						LastError = "send of data fiel info failed";
						return false;
					}
				}

				if (!res)
					LastError = "Connection timeout";

				return res;
		    }
		    catch (Exception ex)
		    {
			    ex.Show();
			    return false;
		    }
	    }

	    public bool SendDataFields(string[] dataFields)
	    {
			if (dataFields == null || dataFields.Length == 0)
			{
				LastError = "no data field set";
				return false;
			}

		    var obj = new JObject
		    {
			    ["mode"] = "register",
			    ["enableLocomotives"] = dataFields.Contains("enableLocomotives"),
			    ["enableAccessories"] = dataFields.Contains("enableAccessories"),
			    ["enableS88"] = dataFields.Contains("enableS88")
			};

		    try
		    {
			    _ws?.Send(obj.ToString(Formatting.Indented));

			    return true;
		    }
		    catch (Exception ex)
		    {
			    LastError = ex.Message;
			    return false;
		    }
		}

		public bool SendCommands(Item item)
	    {
		    if (item == null) return false;
		    var base64 = item.ToBase64();
		    if (string.IsNullOrEmpty(base64)) return false;

		    var dataToSend = new JObject
		    {
			    ["mode"] = "relayToEcos",
			    ["encodedCommands"] = base64
		    };

		    try
		    {
			    _ws?.Send(dataToSend.ToString(Formatting.Indented));

			    return true;
		    }
		    catch (Exception ex)
		    {
			    LastError = ex.Message;
			    return false;
		    }
		}

	    public bool SendCommands(List<string> nativeCommands)
	    {
		    if (nativeCommands == null) return false;
		    var str = string.Join("\n", nativeCommands);
		    var base64 = Encoding.UTF8.ToBase64(str);
		    if (string.IsNullOrEmpty(base64)) return false;

		    var dataToSend = new JObject
		    {
			    ["mode"] = "relayToEcos",
			    ["encodedCommands"] = base64
		    };

		    try
		    {
			    _ws?.Send(dataToSend.ToString(Formatting.Indented));

			    return true;
		    }
		    catch (Exception ex)
		    {
			    LastError = ex.Message;
			    return false;
		    }
	    }

		private void WsOnOpened(object sender, EventArgs e)
	    {
		    _waitFor?.Set();
	    }

	    private void WsOnMessageReceived(object sender, MessageReceivedEventArgs e)
	    {
		    var msg = e.Message;

		    if (string.IsNullOrEmpty(msg)) return;

		    try
		    {
			    var tkn = JToken.Parse(msg);
			    var dp = new DataProvider(DataProvider.DataModeT.Any);
			    var res = dp.Parse(tkn);
			    if (res)
			    {
				    WsData?.Invoke(this, dp);
			    }
			    else
				{
					LastError = "DataProvider parse failed";
					WsError?.Invoke(this);
				}
		    }
		    catch (Exception ex)
		    {
			    LastError = ex.Message;
			    WsError?.Invoke(this);
			}
	    }

	    private void WsOnClosed(object sender, EventArgs e)
	    {
		    LastError = string.Empty;
	    }

	    private void WsOnError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
	    {
		    LastError = e.Exception.Message;
	    }
    }
}
