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
using ECoSUtils;
using Newtonsoft.Json.Linq;

namespace ECoSEntities
{
	public class Accessory : Item
    {
        public const int Typeid = 5;

        public override int TypeId() { return Typeid; }

        public override string Caption => $"{ObjectId}: {Name1}, {Addr}, " + string.Join("|", Addrext) + $", {State}, {Type}";

        private readonly string[] _names = new string[3];

        public string Name1
        {
            get => _names[0];
            set => _names[0] = value;
        }

        public string Name2
        {
            get => _names[1];
            set => _names[1] = value;
        }

        public string Name3
        {
            get => _names[2];
            set => _names[2] = value;
        }

        public bool InvertCommand { get; set; }

        public List<string> Addrext { get; set; } = new List<string>();

        public int Addr { get; set; }

        public string Protocol { get; set; }

        public string Type { get; set; }

        public string Mode { get; set; }

        public int State { get; set; }

        public int Switching { get; set; }

        public override string ToString()
        {
            var ext = string.Join(", ", Addrext);
            return $"{Name1} ({ext}, {ObjectId})";
        }

        public override bool Parse(List<object> arguments)
        {
			foreach (var a in arguments)
			{
				var arg = a as ICommandArgument;
				if (arg == null) continue;

				if (arg.Name.Equals("name1", StringComparison.OrdinalIgnoreCase))
                    Name1 = arg.Parameter[0];
                else if (arg.Name.Equals("name2", StringComparison.OrdinalIgnoreCase))
                    Name2 = arg.Parameter[0];
                else if (arg.Name.Equals("name3", StringComparison.OrdinalIgnoreCase))
                    Name3 = arg.Parameter[0];
                else if (arg.Name.Equals("addrext", StringComparison.OrdinalIgnoreCase))
                    Addrext = arg.Parameter;
                else if (arg.Name.Equals("addr", StringComparison.OrdinalIgnoreCase))
                {
                    int v;
                    if (int.TryParse(arg.Parameter[0], out v))
                        Addr = v;
                    else
                        Addr = -1;
                }
                else if (arg.Name.Equals("protocol", StringComparison.OrdinalIgnoreCase))
                    Protocol = arg.Parameter[0];
                else if (arg.Name.Equals("type", StringComparison.OrdinalIgnoreCase))
                    Type = arg.Parameter[0];
                else if (arg.Name.Equals("mode", StringComparison.OrdinalIgnoreCase))
                    Mode = arg.Parameter[0];
                else if (arg.Name.Equals("state", StringComparison.OrdinalIgnoreCase))
                {
                    int v;
                    if (int.TryParse(arg.Parameter[0], out v))
                        State = v;
                    else
                        State = -1;
                }
                else if (arg.Name.Equals("switching", StringComparison.OrdinalIgnoreCase))
                {
                    int v;
                    if (int.TryParse(arg.Parameter[0], out v))
                        Switching = v;
                    else
                        Switching = -1;
                }
                else if (arg.Name.Equals("symbol", StringComparison.OrdinalIgnoreCase))
                {
                    //Trace.WriteLine($"Handled, but purpose is unknown for: {arg.Name} -> {arg.Parameter[0]}");
                }
                else
                {
                    Trace.WriteLine("Unknown argument: " + arg.Name + " -> " + string.Join(", ", arg.Parameter));
                }
            }

            return true;
        }

		public override void QueryState()
		{
			AddCmd(CommandFactory.Create($"get({ObjectId}, state)"));
		}

		public override JObject ToJson()
        {
            JObject o = new JObject
            {
                ["name1"] = Name1,
                ["name2"] = Name2,
                ["name3"] = Name3
            };
            JArray a0 = new JArray();
            foreach (var e in Addrext)
                a0.Add(e);
            o["objectId"] = ObjectId;
            o["addrext"] = a0;
            o["addr"] = Addr;
            o["protocol"] = Protocol;
            o["type"] = Type;
            o["mode"] = Mode;
            o["state"] = State;
            o["switching"] = Switching;
            return o;
        }

        public override void ParseJson(JObject o)
        {
            if (o == null)
                return;

            if (o["name1"] != null)
                Name1 = o["name1"].ToString();
            if (o["name2"] != null)
                Name2 = o["name2"].ToString();
            if (o["name3"] != null)
                Name3 = o["name3"].ToString();
            if (o["addrext"] != null)
            {
                JArray a = o["addrext"] as JArray;
                if (a != null)
                {
                    foreach (var e in a)
                        Addrext.Add(e.ToString());
                }
            }
            if (o["objectId"] != null)
                ObjectId = (int)o["objectId"];
            if (o["addr"] != null)
                Addr = (int)o["addr"];
            if (o["protocol"] != null)
                Protocol = o["protocol"].ToString();
            if (o["type"] != null)
                Type = o["type"].ToString();
            if (o["mode"] != null)
                Mode = o["mode"].ToString();
            if (o["state"] != null)
                State = (int)o["state"];
            if (o["switching"] != null)
                Switching = (int)o["switching"];
        }

	    public void Switch(int index)
	    {
		    Switching = 1;
		    State = index;
		    string s = Addrext[index];
			//Console.WriteLine("Switch( {0}  {1}  {2}  )", Switching, State, s);
			AddCmd(CommandFactory.Create($"request(11, control, force)"));
		    AddCmd(CommandFactory.Create($"set(11, switch[{Protocol}{s}])"));
		    AddCmd(CommandFactory.Create($"release(11, control)"));
	    }
	}
}
