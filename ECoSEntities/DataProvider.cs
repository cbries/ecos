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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ECoSCore;
using ECoSUtils;
using ECoSUtils.Replies;
using Newtonsoft.Json.Linq;

namespace ECoSEntities
{
	public delegate void DataProviderCommands(object sender, IReadOnlyList<ICommand> commands);
	public delegate void DataProviderModified(object sender);

	public sealed class DataProvider : IDataProvider
	{
		public event DataProviderCommands Commands;
		public event DataProviderModified Modified;

		public enum DataModeT { Any, General, S88 }

		public log4net.ILog Log { get; set; }

		public DataModeT Mode { get; private set; }

		private readonly List<IItem> _objects = new List<IItem>();

		private bool _modified = false;

		public IReadOnlyList<IItem> Objects => _objects;

		public DataProvider(DataModeT type)
		{
			Mode = type;
		}

		public void Append(IItem item)
		{
			if (item == null) return;
			if (item.ObjectId <= 0) return;
			if (_objects.All(o => o.ObjectId != item.ObjectId))
			{
				_objects.Add(item);

				return;
			}

			int idx = -1;
			for (int i = 0; i < _objects.Count; ++i)
			{
				if (_objects[i].ObjectId == item.ObjectId)
				{
					idx = i;
					break;
				}
			}
			if(idx != -1)
				_objects.RemoveAt(idx);

			_objects.Add(item);
		}

		public IItem GetObjectBy(int objectid)
		{
			if (objectid <= -1) return null;

			lock (_objects)
			{
				foreach (var obj in Objects)
				{
					if (obj?.ObjectId == objectid)
						return obj;
				}
			}

			return null;
		}

		public bool HandleData(IBlock block)
		{
			if(block!=null)
				Log?.Debug(block.NativeBlock);

			try
			{
				_modified = false;
				if (block == null) return false;
				if (HandleEvent(block)) return true;
				if (HandleBase(block)) return true;
				if (HandleGet(block)) return true;
				if (HandleRequest(block)) return true;
				if (HandleQuery(block)) return true;
				return false;
			}
			catch(Exception ex)
			{
				ex.Show();
				return false;
			}
			finally
			{
				if(_modified && Modified != null)
					Modified(this);
			}
		}

		private bool HandleBase(IBlock block)
		{
			if (block is EventBlock)
				return false;

			if (block.Command != null && block.Command.Type != CommandT.Get)
				return false;

			// <REPLY get(1, info, status)>
			// 1 status[GO]
			// 1 ECoS2
			// 1 ProtocolVersion[0.5]
			// 1 ApplicationVersion[4.2.2]
			// 1 HardwareVersion[2.1]
			// <END 0(OK) >

			foreach (var e in block.ListEntries)
			{
				if (e == null) return false;
				if (e.ObjectId != Globals.ID_EV_BASEOBJECT) return false;

				switch (e.ObjectId)
				{
					case Globals.ID_EV_BASEOBJECT:
						{
							var item = GetObjectBy(Globals.ID_EV_BASEOBJECT);
							if (item == null)
							{
								lock (_objects)
								{
									item = new Ecos2();
									if (item.Parse(e.Arguments))
									{
										item.ObjectId = Globals.ID_EV_BASEOBJECT;
										_objects.Add(item);
									}
								}
							}
							else
							{
								item.Parse(e.Arguments);
							}
						}
						break;
				}
			}

			_modified = true;

			return true;
		}

		private bool HandleGet(IBlock block)
		{
			if (block is EventBlock) return false;
			if (block.Command != null && block.Command.Type != CommandT.Get) return false;

			foreach (var e in block.ListEntries)
			{
				if (e == null) continue;
				int objId = e.ObjectId;
				if (objId == -1) continue;
				var item = GetObjectBy(objId);
				if (item == null) continue;

				item.Parse(e.Arguments);
			}

			_modified = true;

			return true;
		}

		private bool HandleEvent(IBlock block)
		{
			if (!(block is EventBlock evBlock)) return false;
			var objId = -1;
			if (evBlock.ObjectId.HasValue)
				objId = evBlock.ObjectId.Value;

			bool handled = false;

			switch (objId)
			{
				case Globals.ID_EV_S88:
				// <EVENT 26>
				// 26 msg[LIST_CHANGED]
				// 105 appended
				// 26 size[6]
				// <END 0(OK)>

				case Globals.ID_EV_LOCOMOTIVES:
				// <EVENT 10>
				// 10 msg[LIST_CHANGED]
				// 1016 appended
				// 10 size[17]
				// <END 0(OK)>

				// <EVENT 10>
				// 10 msg[LIST_CHANGED]
				// 1016 removed
				// 10 size[16]
				// <END 0(OK)>

				case Globals.ID_EV_ACCESSORIES:
					{
						var targetId = evBlock.ListEntries[1].ObjectId;
						var mod = (evBlock.ListEntries[0].Arguments[0] as ICommandArgument).Parameter[0];
						var cmd = (evBlock.ListEntries[0].Arguments[0] as ICommandArgument).Name;

						if (mod.Equals("LIST_CHANGED", StringComparison.OrdinalIgnoreCase))
						{
							if (cmd.Equals("removed", StringComparison.OrdinalIgnoreCase))
							{
								var item = GetObjectBy(targetId);
								if (item != null)
								{
									lock (_objects)
									{
										_objects.Remove(item);
									}
								}
							}
							else if (cmd.Equals("appended", StringComparison.OrdinalIgnoreCase))
							{
								ICommand q = null;
								if (objId == Globals.ID_EV_S88)
									q = CommandFactory.Create($"queryObjects(26, ports)");
								else if (objId == Globals.ID_EV_LOCOMOTIVES)
									q = CommandFactory.Create($"queryObjects(10, addr, name, protocol)");
								else if (objId == Globals.ID_EV_ACCESSORIES)
									q = CommandFactory.Create($"queryObjects(11, addr, protocol, type, addrext, mode, symbol, name1, name2, name3, switching)");

								if (q != null)
									Commands?.Invoke(this, new List<ICommand> { q });
							}

							handled = true;
						}

					}
					break;
			}

			if (!handled)
			{
				var targetEvItem = GetObjectBy(objId);

				if (targetEvItem != null)
				{
					foreach (var e in evBlock.ListEntries)
					{
						if (e == null)
							continue;

						targetEvItem.Parse(e.Arguments);
					}

					handled = true;
				}
			}

			_modified = handled;

			return handled;
		}

		private bool HandleRequest(IBlock block)
		{
			if (block is EventBlock)
				return false;

			if (block.Command != null && block.Command.Type != CommandT.Request)
				return false;

			if (block.EndLine.Equals("<END 0 (OK)>", StringComparison.OrdinalIgnoreCase))
				return true;

			return false;
		}

		private bool HandleQuery(IBlock block)
		{
			if (block is EventBlock)
				return false;

			if (block.Command != null && block.Command.Type != CommandT.QueryObjects)
				return false;

			var objId = block.Command?.ObjectId;

			foreach (var e in block.ListEntries)
			{
				if (e == null)
					continue;

				var exist = GetObjectBy(e.ObjectId) != null;
				if (!exist)
				{
					IItem newItem = null;

					switch (objId)
					{
						case Globals.ID_EV_LOCOMOTIVES:
							newItem = new Locomotive();
							break;
						case Globals.ID_EV_ACCESSORIES:
							newItem = new Accessory();
							break;
						case Globals.ID_EV_S88:
							newItem = new S88();
							break;
					}

					if (newItem == null)
						continue;

					if (newItem.Parse(e.Arguments))
					{
						newItem.ObjectId = e.ObjectId;

						var viewItem = (IItemView)newItem;
						if (!viewItem.HasView)
							viewItem.EnableView();

						lock (_objects)
						{
							_objects.Add(newItem);
						}

						newItem.QueryState();
					}
				}
			}

			_modified = true;

			return true;
		}

		public bool Parse(JToken tkn)
		{
			if(_objects != null)
				_objects.Clear();

			if (tkn == null) return false;

			try
			{
				var o = tkn as JObject;
				if (o == null) return false;

				if (o["ecosbase"] != null)
				{
					var ar = o["ecosbase"] as JArray;
					if (ar != null)
					{
						for (int i = 0; i < ar.Count; ++i)
						{
							var it = ar[i] as JObject;
							if (it == null) continue;
							var iit = new Ecos2();
							iit.ParseJson(it);
							_objects?.Add(iit);
						}
					}
				}

				if (o["locomotives"] != null)
				{
					var ar = o["locomotives"] as JArray;
					if (ar != null)
					{
						for (int i = 0; i < ar.Count; ++i)
						{
							var it = ar[i] as JObject;
							if (it == null) continue;
							var iit = new Locomotive();
							iit.ParseJson(it);
							_objects?.Add(iit);
						}
					}
				}

				if (o["accessories"] != null)
				{
					var ar = o["accessories"] as JArray;
					if (ar != null)
					{
						for (int i = 0; i < ar.Count; ++i)
						{
							var it = ar[i] as JObject;
							if (it == null) continue;
							var iit = new Accessory();
							iit.ParseJson(it);
							_objects?.Add(iit);
						}
					}
				}

				if (o["feedbacks"] != null)
				{
					var ar = o["feedbacks"] as JArray;
					if (ar != null)
					{
						for (int i = 0; i < ar.Count; ++i)
						{
							var it = ar[i] as JObject;
							if (it == null) continue;
							var iit = new S88();
							iit.ParseJson(it);
							_objects?.Add(iit);
						}
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				ex.Show();
			}
			return false;
		}

		public JObject ToJson()
		{
			var ecosbase = new JArray();
			var locos = new JArray();
			var accessories = new JArray();
			var s88Busses = new JArray();

			lock (_objects)
			{
				var n = _objects.Count;

				for (int i = 0; i < n; ++i)
				{
					var it = _objects[i];
					var sit = _objects[i] as IItemSerializer;
					if (it == null || sit == null) continue;
					switch (it.TypeId())
					{
						case Ecos2.Typeid:
							{
								ecosbase.Add(sit.ToJson());
							}
							break;

						case Locomotive.Typeid:
							{
								locos.Add(sit.ToJson());
							}
							break;

						case Accessory.Typeid:
							{
								accessories.Add(sit.ToJson());
							}
							break;

						case S88.Typeid:
							{
								s88Busses.Add(sit.ToJson());
							}
							break;
					}
				}
			}

			var o = new JObject
			{
				["ecosbase"] = ecosbase,
				["locomotives"] = locos,
				["accessories"] = accessories,
				["feedbacks"] = s88Busses
			};

			return o;
		}
	}
}
