/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
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
using ECoSCore;
using ECoSUtils;
using Newtonsoft.Json.Linq;

namespace ECoSEntities
{
    public sealed class Locomotive : Item
    {
        public override string Caption => $"{ObjectId}: {Name}, {Addr}, {Speed}, " + (IsBackward ? "BACK" : "FORWARD");

        public DateTime StartTime { get; private set; }
        public DateTime StopTime { get; private set; }

        public const int Typeid = 1;

        public override int TypeId() { return Typeid; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
                return $"Locomotive {Addr},{Protocol}";
            return Name;
        }

        public bool Locked { get; set; }
        public string Name { get; set; }
        public int MaxSpeedFahrstufe { get; set; } = 0;
        public int BlockSpeedFahrstufe { get; set; } = 0;

        public int GetNumberOfSpeedsteps()
        {
            if (string.IsNullOrEmpty(Protocol))
                return 128;
            if (Protocol.Equals("MM14", StringComparison.OrdinalIgnoreCase))
                return 14;
            if (Protocol.Equals("MM27", StringComparison.OrdinalIgnoreCase))
                return 27;
            if (Protocol.Equals("MM128", StringComparison.OrdinalIgnoreCase))
                return 128;
            if (Protocol.Equals("DCC14", StringComparison.OrdinalIgnoreCase))
                return 14;
            if (Protocol.Equals("DCC28", StringComparison.OrdinalIgnoreCase))
                return 28;
            if (Protocol.Equals("DCC128", StringComparison.OrdinalIgnoreCase))
                return 128;
            return 128;
        }

        /// MM14, MM27, MM128, DCC14, DCC28, DCC128, SX32, MMFKT
        public string Protocol { get; set; }

        /// <summary>
        /// describes the address to control any object
        /// </summary>
        public int Addr { get; set; }

        public int Speed { get; set; }

        public int Speedstep { get; set; }

        public int Direction { get; set; }

        public int NrOfFunctions { get; set; }

        public List<bool> Funcset { get; set; }

        public Dictionary<int, int> Funcdesc { get; set; } = new Dictionary<int, int>();

        public Locomotive()
        {
            Name = "";
            Protocol = "";

            //StartTime = DateTime.MaxValue;
            //StopTime = DateTime.MinValue;

            StartTime = new DateTime(1970, 1, 1, 0, 0, 0);
            StopTime = new DateTime(2050, 1, 1, 0, 0, 0);

            Funcset = new List<bool>(32);

            if (Funcset.Count == 0)
            {
                for (int i = 0; i < 32; ++i)
                    Funcset.Add(false);
            }
        }

        public bool IsBackward => Direction == 1;
        public bool IsForward => Direction == 0;

        public void ChangeName(string name)
        {
            AddCmd(CommandFactory.Create($"set({ObjectId}, name[\"{name}\"])", true));
            AddCmd(CommandFactory.Create($"get({ObjectId}, name)"));
        }

        public void Stop()
        {
            AddCmd(CommandFactory.Create($"set({ObjectId}, stop)"));
            StopTime = DateTime.Now;
        }

	    public void ToggleFunctions(Dictionary<uint, bool> states)
	    {
		    AddCmd(CommandFactory.Create($"request({ObjectId}, control, force)"));

		    foreach (var it in states)
		    {
			    var nr = it.Key;
			    var state = it.Value;
			    int v = state ? 1 : 0;
			    AddCmd(CommandFactory.Create($"set({ObjectId}, func[{nr}, {v}])"));
			    Funcset[(int)nr] = state;
		    }

			AddCmd(CommandFactory.Create($"release({ObjectId}, control)"));
		}
		
        public void ChangeDirection(bool backward)
        {
            int v = backward ? 1 : 0;
            Direction = v;
            AddCmd(CommandFactory.Create($"request({ObjectId}, control, force)"));
            AddCmd(CommandFactory.Create($"set({ObjectId}, dir[{v}])"));
            AddCmd(CommandFactory.Create($"release({ObjectId}, control)"));
        }

        public void ChangeSpeed(int fahrstufe, bool isFirst = true, bool isLast = false)
        {
            if (Speed == 0 && fahrstufe > 0)
            {
                StartTime = DateTime.Now;
                StopTime = DateTime.MinValue;
            }
            else
            {
                if (fahrstufe <= 0)
                {
                    StartTime = DateTime.MaxValue;
                    StopTime = DateTime.Now;
                }
            }

			if(isFirst) AddCmd(CommandFactory.Create($"request({ObjectId}, control, force)"));
            AddCmd(CommandFactory.Create($"set({ObjectId}, speed[{fahrstufe}])"));
	        if (isLast)
	        {
		        AddCmd(CommandFactory.Create($"release({ObjectId}, control)"));
				AddCmd(CommandFactory.Create($"get({ObjectId}, speed, speedstep)"));
	        }

	        Speed = fahrstufe;
        }

        public void ChangeSpeedstep(int step)
        {
            if (Speed == 0 && step > 0)
            {
                StartTime = DateTime.Now;
                StopTime = DateTime.MinValue;
            }
            else
            {
                if (step <= 0)
                {
                    StartTime = DateTime.MaxValue;
                    StopTime = DateTime.Now;
                }
            }

            AddCmd(CommandFactory.Create($"request({ObjectId}, control, force)"));
            AddCmd(CommandFactory.Create($"set({ObjectId}, speedstep[{step}])"));
            AddCmd(CommandFactory.Create($"release({ObjectId}, control)"));
            //AddCmd(CommandFactory.Create($"get({ObjectId}, speed, speedstep)"));

            Speedstep = step;
        }

        public override void QueryState()
        {
            AddCmd(CommandFactory.Create(
                $"get({ObjectId}, speed, speedstep, profile, protocol, name, addr, dir, funcset, funcdesc)"));
        }

        public override bool Parse(List<object> arguments)
        {
			foreach (var a in arguments)
			{
				var arg = a as ICommandArgument;
				if (arg == null) continue;

				if (arg.Name.Equals("name", StringComparison.OrdinalIgnoreCase))
                    Name = arg.Parameter[0];
                else if (arg.Name.Equals("protocol", StringComparison.OrdinalIgnoreCase))
                    Protocol = arg.Parameter[0];
                else if (arg.Name.Equals("addr", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(arg.Parameter[0], out int v))
                        Addr = v;
                    else
                        Addr = -1;
                }
                else if (arg.Name.Equals("speed", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(arg.Parameter[0], out int v))
                        Speed = v;
                    else
                        Speed = -1;
                }
                else if (arg.Name.Equals("speedstep", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(arg.Parameter[0], out int v))
                        Speedstep = v;
                    else
                        Speedstep = -1;
                }
                else if (arg.Name.Equals("dir", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(arg.Parameter[0], out int v))
                        Direction = v;
                    else
                        Direction = -1;
                }
                else if (arg.Name.Equals("funcdesc", StringComparison.OrdinalIgnoreCase))
                {
                    var sindex = arg.Parameter[0].Trim();
                    var stype = arg.Parameter[1].Trim();

                    if (!int.TryParse(sindex, out int index))
                        index = -1;
                    if (!int.TryParse(stype, out int type))
                        type = -1;

                    if (Funcdesc.ContainsKey(index))
                        Funcdesc[index] = type;
                    else
                        Funcdesc.Add(index, type);
                }
                else if (arg.Name.Equals("funcset", StringComparison.OrdinalIgnoreCase))
                {
                    NrOfFunctions = arg.Parameter[0].Length;

                    for (int i = 0; i < NrOfFunctions; ++i)
                        Funcset[i] = arg.Parameter[0][i].Equals('1');
                }
                else if (arg.Name.Equals("func", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var idx = Convert.ToInt32(arg.Parameter[0]);
                        Funcset[idx] = arg.Parameter[1].Equals("1", StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        // ignore
                    }
                }
                else
                {
                    Trace.WriteLine("Unknown argument: " + arg.Name + " -> " + string.Join(", ", arg.Parameter));
                }
            }

            return true;
        }

        public override JObject ToJson()
        {
            string m = string.Empty;
            foreach (var f in Funcset)
            {
                if (f) m += "1";
                else m += "0";
            }

			JArray arFncDesc = new JArray();
	        foreach (var desc in Funcdesc)
	        {
		        var odesc = new JObject();
		        odesc["idx"] = desc.Key;
		        odesc["type"] = desc.Value;
				arFncDesc.Add(odesc);
	        }

            JObject o = new JObject
            {
                ["objectId"] = ObjectId,
                ["name"] = Name,
                ["protocol"] = Protocol,
                ["addr"] = Addr,
                ["speed"] = Speed,
                ["speedstep"] = Speedstep,
                ["direction"] = Direction,
                ["funcset"] = m,
				["funcdesc"] = arFncDesc,
                ["nrOfFunctions"] = NrOfFunctions,
                ["maxSpeedFahrstufe"] = MaxSpeedFahrstufe,
                ["blockSpeedFahrstufe"] = BlockSpeedFahrstufe,
                ["locked"] = Locked
            };

            return o;
        }

        public override void ParseJson(JObject obj)
        {
            if (obj == null)
                return;

            if (obj["objectId"] != null)
                ObjectId = (int)obj["objectId"];
            if (obj["name"] != null)
                Name = obj["name"].ToString();
            if (obj["protocol"] != null)
                Protocol = obj["protocol"].ToString();
            if (obj["addr"] != null)
                Addr = (int)obj["addr"];
            if (obj["speed"] != null)
                Speed = (int)obj["speed"];
            if (obj["speedstep"] != null)
                Speedstep = (int)obj["speedstep"];
            if (obj["direction"] != null)
                Direction = (int)obj["direction"];
            if (obj["funcset"] != null)
            {
                string m = obj["funcset"].ToString();
                for (int i = 0; i < m.Length; ++i)
                    Funcset[i] = m[i] == '1';
            }

	        if (obj["funcdesc"] != null)
	        {
		        var ar = obj["funcdesc"] as JArray;
				for(int i = 0; i < ar.Count; ++i)
				{
					var oar = ar[i] as JObject;
			        if (oar == null) continue;
			        if (oar["idx"] != null && oar["type"] != null)
			        {
				        var idx = oar["idx"].ToString().ToInt(i);
				        var type = oar["type"].ToString().ToInt(0);
				        if (Funcdesc.ContainsKey(idx))
					        Funcdesc[idx] = type;
				        else
					        Funcdesc.Add(idx, type);
			        }
		        }
	        }
            if (obj["nrOfFunctions"] != null)
                NrOfFunctions = (int)obj["nrOfFunctions"];
            if (obj["maxSpeedFahrstufe"] != null)
				MaxSpeedFahrstufe = (int)obj["maxSpeedFahrstufe"];
            if (obj["blockSpeedPercentage"] != null)
				BlockSpeedFahrstufe = (int)obj["blockSpeedFahrstufe"];
            if (obj["locked"] != null)
                Locked = (bool)obj["locked"];
        }
    }
}
