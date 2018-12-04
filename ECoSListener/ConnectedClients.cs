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
	using System.Collections.Generic;
	using SuperWebSocket;

	public class ConnectedClients : List<WsRouterSessions>
	{
		public WsRouterSessions GetSession(WebSocketSession wsSession)
		{
			foreach (var it in this)
			{
				if (it == null) continue;
				if (it.Session == wsSession)
					return it;
			}

			return null;
		}

		public bool AddIfNotExist(WsRouterSessions routerSession)
		{
			try
			{
				bool addItem = true;

				foreach (var it in this)
				{
					if (it == null) continue;
					if (it.Session == routerSession.Session)
					{
						addItem = false;
						it.EnableAccessories = routerSession.EnableAccessories;
						it.EnableLocomotives = routerSession.EnableLocomotives;
						it.EnableS88 = routerSession.EnableS88;
						break;
					}
				}

				if (addItem)
					Add(routerSession);

				return addItem;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
		}

		public bool Remove(WebSocketSession wsSession)
		{
			try
			{
				List<int> indecesToRemove = new List<int>();
				for (int idx = 0; idx < Count; ++idx)
				{
					var it = this[idx];
					if (it == null)
					{
						indecesToRemove.Add(idx);
						continue;
					}

					if (it.Session == wsSession)
						indecesToRemove.Add(idx);
				}

				indecesToRemove.Reverse();

				foreach (var idx in indecesToRemove)
					RemoveAt(idx);

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
		}
	}
}