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
using System.IO;
using System.Text;
using ECoSCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ECoSToolsLibrary.Base
{
	public enum EventType { Enter, In, OneShot, None }

	public class Event
	{
		public RailwayEssentialContext Ctx { get; private set; }

		public EventType Type { get; set; }
		public S88Feedback Feedback { get; set; }
		public int DelayMs { get; set; }

		public Event(RailwayEssentialContext ctx)
		{
			Ctx = ctx;
			Type = EventType.None;
			Feedback = null;
			DelayMs = 0;
		}

		public bool IsValid()
		{
			if (Feedback == null) return false;
			if (!Feedback.IsValid()) return false;
			return true;
		}

		public bool Parse(JToken tkn)
		{
			var o = tkn as JObject;
			if (o == null) return false;

			if (o["type"] != null)
			{
				var v = o["type"].ToString();
				if (v.Equals("Enter", StringComparison.OrdinalIgnoreCase))
					Type = EventType.Enter;
				else if (v.Equals("In", StringComparison.OrdinalIgnoreCase))
					Type = EventType.In;
				else if (v.Equals("OneShot", StringComparison.OrdinalIgnoreCase))
					Type = EventType.OneShot;
				else
					Type = EventType.None;
			}

			if (o["delayMs"] != null)
				DelayMs = o["delayMs"].ToString().ToInt(0);

			if (o["feedback"] != null)
			{
				var feedbackName = o["feedback"].ToString();
				if (!string.IsNullOrEmpty(feedbackName))
					Feedback = Ctx.AvailableFeedbacks.GetBy(feedbackName);
			}

			return true;
		}

		public JObject ToJson()
		{
			if (!IsValid()) return null;
			var o = new JObject
			{
				["type"] = Type.ToString(),
				["feedback"] = Feedback.Name,
				["delayMs"] = DelayMs
			};
			return o;
		}
	}

	public class BlockEvents : List<Event>
	{
		public RailwayEssentialContext Ctx { get; private set; }

		public BlockEvents(RailwayEssentialContext ctx)
		{
			Ctx = ctx;
		}

		public Event GetBy(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;
			foreach (var e in this)
			{
				if (e == null) continue;
				if(!e.IsValid()) continue;
				if (e.Feedback.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
					return e;
			}

			return null;
		}

		public JArray ToJson()
		{
			var ar = new JArray();
			foreach (var e in this)
			{
				if(e == null) continue;
				if (!e.IsValid()) continue;
				ar.Add(e.ToJson());
			}
			return ar;
		}

		public bool Parse(JToken tkn)
		{
			if (tkn == null) return false;
			var ar = tkn as JArray;
			if (ar == null) return false;

			if (ar != null)
			{
				foreach (var e in ar)
				{
					if (e == null) continue;
					var evItem = new Event(Ctx);
					if (evItem.Parse(e))
					{
						if(evItem.IsValid())
							this.Add(evItem);
					}
				}
			}

			return true;
		}
	}

	public class Block
	{
		public RailwayEssentialContext Ctx { get; private set; }

		public string Name { get; set; }
		public string Description { get; set; }
		public BlockWaitMode WaitMode { get; set; }
		public List<BlockLockRef> Exclude { get; set; }
		public bool Locked { get; set; }
		public BlockLockRef Occupation { get; set; }
		public bool Commuting { get; set; }

		public BlockEvents PlusEvents { get; private set; }
		public BlockEvents MinusEvents { get; private set; }

		private readonly List<string> _sidePlusNames = new List<string>();
		private readonly List<string> _sideMinusNames = new List<string>();

		private readonly Blocks _sidePlus;
		private readonly Blocks _sideMinus;

		public IReadOnlyList<Block> SidePlus => _sidePlus;
		public IReadOnlyList<Block> SideMinus => _sideMinus;

		/// <returns>always this</returns>
		public Block AllowPlus(params Block[] blks)
		{
			if (blks == null) return this;

			foreach (var blk in blks)
			{
				if (!blk.IsValid()) return this;
				var b = _sidePlus.GetBy(blk.Name);
				if (b != null) return this;
				_sidePlus.Add(blk);
			}

			return this;
		}

		/// <returns>always this</returns>
		public Block AllowMinus(params Block [] blks)
		{
			if (blks == null) return this;

			foreach (var blk in blks)
			{
				if (!blk.IsValid()) return this;
				var b = _sideMinus.GetBy(blk.Name);
				if (b != null) return this;
				_sideMinus.Add(blk);
			}

			return this;
		}

		/// <returns>the added event instance, null on any error</returns>
		public Event AddPlusEvent(EventType type, S88Feedback feedback, int delayMs = 0)
		{
			if (feedback == null) return null;
			var item = PlusEvents.GetBy(feedback.Name);
			if (item != null) return null;
			var ev = new Event(Ctx)
			{
				Type = type,
				Feedback = feedback,
				DelayMs = delayMs
			};
			PlusEvents.Add(ev);
			return ev;
		}

		/// <returns>the added event instance, null on any error</returns>
		public Event AddMinusEvent(EventType type, S88Feedback feedback, int delayMs = 0)
		{
			if (feedback == null) return null;
			var item = MinusEvents.GetBy(feedback.Name);
			if (item != null) return null;
			var ev = new Event(Ctx)
			{
				Type = type,
				Feedback = feedback,
				DelayMs = delayMs
			};
			MinusEvents.Add(ev);
			return ev;
		}

		public Block(RailwayEssentialContext ctx)
		{
			Ctx = ctx;
			_sidePlus = new Blocks(ctx);
			_sideMinus = new Blocks(ctx);
			WaitMode = new BlockWaitMode();
			Exclude = new List<BlockLockRef>();
			Locked = false;
			Occupation = null;
			Commuting = false;
			PlusEvents = new BlockEvents(ctx);
			MinusEvents = new BlockEvents(ctx);
		}

		public bool IsValid()
		{
			if (string.IsNullOrEmpty(Name)) return false;
			return true;
		}

		public bool Parse(JToken tkn)
		{
			var o = tkn as JObject;
			if (o == null) return false;

			try
			{
				if (o["name"] != null)
					Name = o["name"].ToString();
				if (o["description"] != null)
					Description = o["description"].ToString();
				if (o["waitMode"] != null)
				{
					var m = new BlockWaitMode();
					if (m.Parse(o["waitMode"]))
						WaitMode = m;
					else
						WaitMode = new BlockWaitMode
						{
							WaitMin = 1,
							WaitMax = 60,
							Mode = "random"
						};
				}
			 
				if (o["exclude"] != null)
				{
					if(Exclude == null)
						Exclude = new List<BlockLockRef>();

					var ar = o["exclude"] as JArray;
					if (ar != null)
					{
						for (int i = 0; i < ar.Count; ++i)
						{
							var item = ar[i];
							var itemObj = new BlockLockRef();
							if (itemObj.Parse(item))
								Exclude.Add(itemObj);
						}
					}
				}

				if (o["locked"] != null)
				{
					var lockTkn = o["locked"];
					if (lockTkn != null)
					{
						if (lockTkn.Type == JTokenType.String)
						{
							var lockV = lockTkn.ToString();
							if (!string.IsNullOrEmpty(lockV))
								Locked = lockV.Equals("true", StringComparison.OrdinalIgnoreCase);
							else
								Locked = false;
						}
						else if (lockTkn.Type == JTokenType.Boolean)
						{
							Locked = (bool) lockTkn;
						}
						else if (lockTkn.Type == JTokenType.Integer)
						{
							Locked = (int)lockTkn == 1;
						}
					}
				}

				if (o["occupation"] != null)
				{
					var locRef = new BlockLockRef();
					if (locRef.Parse(o["occupation"]))
						Occupation = locRef;
				}

				if (o["sidePlus"] != null)
				{
					var arPlus = o["sidePlus"] as JArray;
					if (arPlus != null)
					{
						foreach (var e in arPlus)
						{
							if (e == null) continue;
							if (_sidePlusNames.Contains(e.ToString())) continue;
							_sidePlusNames.Add(e.ToString());
						}
					}
				}

				if (o["sideMinus"] != null)
				{
					var arMinus = o["sideMinus"] as JArray;
					if (arMinus != null)
					{
						foreach (var e in arMinus)
						{
							if (e == null) continue;
							if (_sideMinusNames.Contains(e.ToString())) continue;
							_sideMinusNames.Add(e.ToString());
						}
					}
				}

				if (o["plusEvents"] != null)
				{
					var evList = new BlockEvents(Ctx);
					if (evList.Parse(o["plusEvents"]))
						PlusEvents = evList;
					else
						PlusEvents = null;
				}

				if (o["minusEvents"] != null)
				{
					var evList = new BlockEvents(Ctx);
					if (evList.Parse(o["minusEvents"]))
						MinusEvents = evList;
					else
						MinusEvents = null;
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Instead of using names too access neighbour blocks
		/// we allow the direct use of references.
		/// </summary>
		/// <param name="knownBlocks"></param>
		internal void FillWithNeighbours(Blocks knownBlocks)
		{
			if (knownBlocks == null) return;

			foreach (var plusName in _sidePlusNames)
			{
				if (string.IsNullOrEmpty(plusName)) continue;
				var blk = knownBlocks.GetBy(plusName);
				if(blk == null) continue;
				var item = _sidePlus.GetBy(plusName);
				if (item != null)
				{
					Trace.WriteLine($"{Name}: {plusName} already added to '+'");
					continue;
				}

				_sidePlus.Add(blk);
			}

			foreach (var minusName in _sideMinusNames)
			{
				if (string.IsNullOrEmpty(minusName)) continue;
				var blk = knownBlocks.GetBy(minusName);
				if (blk == null) continue;
				var item = _sideMinus.GetBy(minusName);
				if (item != null)
				{
					Trace.WriteLine($"{Name}: {minusName} already added to '-'");
					continue;
				}

				_sideMinus.Add(blk);
			}
		}

		public JObject GetJson()
		{
			var o = new JObject
			{
				["name"] = Name,
				["description"] = Description
			};

			if (WaitMode != null)
				o["waitMode"] = WaitMode.GetJson();
			else
				o["waitMode"] = null;

			if (Exclude != null)
			{
				var ar = new JArray();
				for (int i = 0; i < Exclude.Count; ++i)
				{
					var exItem = Exclude[i];
					ar.Add(exItem.GetJson());
				}

				o["exclude"] = ar;
			}
			else
			{
				o["exclude"] = null;
			}

			if (Occupation != null)
				o["occupation"] = Occupation.GetJson();
			else
				o["occupation"] = null;

			o["commuting"] = Commuting;

			var plusNames = new JArray();
			foreach (var blk in _sidePlus)
			{
				if (blk == null) continue;
				if (!blk.IsValid()) continue;
				plusNames.Add(blk.Name);
			}
			o["sidePlus"] = plusNames;

			var minusNames = new JArray();
			foreach (var blk in _sideMinus)
			{
				if (blk == null) continue;
				if (!blk.IsValid()) continue;
				minusNames.Add(blk.Name);
			}
			o["sideMinus"] = minusNames;

			o["plusEvents"] = PlusEvents.ToJson();
			o["minusEvents"] = MinusEvents.ToJson();

			return o;
		}
	}

	public class BlockWaitMode
	{
		/// <summary> 
		/// random :=
		/// fix := 
		/// </summary>
		public string Mode { get; set; }
		public int WaitMin { get; set; }
		public int WaitMax { get; set; }

		public BlockWaitMode()
		{
			Mode = "random";
			WaitMin = 1;
			WaitMax = 60;
		}

		public bool Parse(JToken tkn)
		{
			try
			{
				if (tkn == null) return false;
				var o = tkn as JObject;
				if (o == null) return false;
				if (o["mode"] != null)
					Mode = o["mode"].ToString();

				if (o["waitRange"] != null)
				{
					var oo = o["waitRange"] as JObject;
					if (oo != null)
					{
						WaitMin = oo["min"].ToString().ToInt(1);
						WaitMax = oo["max"].ToString().ToInt(60);
					}
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

		public JObject GetJson()
		{
			var o = new JObject();
			o["mode"] = Mode;
			var waitRange = new JObject();
			waitRange["min"] = WaitMin;
			waitRange["max"] = WaitMax;
			o["waitRange"] = waitRange;
			return o;
		}
	}

	public class BlockLockRef
	{
		public string Name { get; set; }
		public int ObjectId { get; set; }

		public bool Parse(JToken tkn)
		{
			try
			{
				if (tkn.Type == JTokenType.String)
					Name = tkn.ToString();
				else if (tkn.Type == JTokenType.Integer)
					ObjectId = tkn.ToString().ToInt(-1);

				return true;
			}
			catch
			{
				return false;
			}
		}

		public JToken GetJson()
		{
			if (string.IsNullOrEmpty(Name) && ObjectId != -1)
				return ObjectId;
			return Name;
		}
	}

	public class Blocks : List<Block>
	{
		public RailwayEssentialContext Ctx { get; private set; }

		public Blocks(RailwayEssentialContext ctx)
		{
			Ctx = ctx;
		}

		public Block GetBy(string name)
		{
			foreach (var b in this)
			{
				if (b == null) continue;
				if (string.IsNullOrEmpty(b.Name)) continue;
				if (b.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
					return b;
			}

			return null;
		}

		public Block GetBy(BlockLockRef locref)
		{
			if (locref == null) return null;
			foreach (var b in this)
			{
				if (b == null) continue;
				if (string.IsNullOrEmpty(b.Name)) continue;
				if (b.Occupation == null) continue;
				if (b.Occupation.ObjectId <= 0) continue;

				if( b.Occupation.ObjectId == locref.ObjectId 
				 || b.Occupation.Name.Equals(locref.Name, StringComparison.OrdinalIgnoreCase))
					return b;
			}

			return null;
		}

		public bool Parse(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			if (!File.Exists(path)) return false;

			try
			{
				var cnt = File.ReadAllText(path, Encoding.UTF8);
				if (string.IsNullOrEmpty(cnt)) return false;
				var ar = JArray.Parse(cnt);
				if (ar == null) return false;

				for (int i = 0; i < ar.Count; ++i)
				{
					var item = ar[i];
					if (item == null) continue;
					var b = new Block(Ctx);
					if (b.Parse(item))
					{
						if(b.IsValid())
							base.Add(b);
					}
				}

				var n = this.Count;
				for (int i = 0; i < n; ++i)
				{
					var blk = this[i];
					if (blk == null) continue;
					blk.FillWithNeighbours(this);
				}
			}
			catch
			{
				// ignore
			}

			return true;
		}

		public bool Save(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			try
			{
				var ar = new JArray();
				foreach (var item in this)
				{
					if (item == null) continue;
					ar.Add(item.GetJson());
				}
				File.WriteAllText(path, ar.ToString(Formatting.Indented), Encoding.UTF8);
				return true;
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				return false;
			}
		}
	}
}
