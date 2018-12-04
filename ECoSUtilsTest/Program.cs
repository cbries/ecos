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

namespace ECoSUtilsTest
{
	using System.Linq;
	using ECoSUtils;
	using C = System.Console;

	class Program
	{
		static string replyGet1000 =
			"<REPLY get(1000, protocol, name, dir, speed, addr, profile)>\r\n"
			+ "1000 protocol[MFX]\r\n"
			+ "1000 name[\"18 527 DRG\"]\r\n"
			+ "1000 dir[0]\r\n"
			+ "1000 speed[0]\r\n"
			+ "1000 addr[0]\r\n"
			+ "1000 profile[\"none\"]\r\n"
			+ "<END 0 (OK)>\r\n";
		
		static void TraverseBlock()
		{
			IBlock loc1000 = BlockUtils.GetBlocks(replyGet1000).First();
			string loc1000TypeName = loc1000.GetType().FullName;

			C.WriteLine($"Typ: {loc1000TypeName}\n");

			var replyObjectId = loc1000.Command.ObjectId;
			C.WriteLine($"Original: {loc1000.Command.NativeCommand}");
			C.WriteLine($"ObjectId: {replyObjectId}\n");

			var nameObjectId = loc1000.ListEntries[1].ObjectId;
			C.WriteLine($"Original: {loc1000.ListEntries[1].OriginalLine}");
			C.WriteLine($"ObjectId: {nameObjectId}\n");

			var arg0 = loc1000.ListEntries[0].Arguments[0] as ICommandArgument;
			var protocol = arg0.Parameter[0];
			C.WriteLine($"Original: {arg0}");
			C.WriteLine($"Protocol: {protocol}\n");

			var arg1 = loc1000.ListEntries[1].Arguments[0] as ICommandArgument;
			var name = arg1.Parameter[0];
			C.WriteLine($"Original: {arg1}");
			C.WriteLine($"Name:     {name}");
		}

		static string eventList =
			"<EVENT 1000>\r\n"        // Block 1
			+ "1000 control[other]\r\n"
			+ "<END 0 (OK)>\r\n"
			+ "<EVENT 1000>\r\n"        // Block 2
			+ "1000 active[1]\r\n"
			+ "<END 0 (OK)>\r\n"
			+ "<EVENT 1000>\r\n"        // Block 3
			+ "1000 speed[41]\r\n"
			+ "1000 speedstep[41]\r\n"
			+ "<END 0 (OK)>\r\n"
			+ "<EVENT 1000>\r\n"        // Block 4
			+ "1000 func[0, 1]\r\n"
			+ "1000 funcset[1000000000000000000000000000000]\r\n"
			+ "<END 0 (OK)>\r\n";

		static void CheckBlocks()
		{
			var r0 = BlockUtils.HasAnyBlock(replyGet1000);
			System.Console.WriteLine($"HasAnyBlock: {r0}");

			var r1 = BlockUtils.HasAnyBlock(replyGet1000.Split(BlockUtils.LF));
			System.Console.WriteLine($"HasAnyBlock: {r1}");
			var blocks1 = BlockUtils.GetBlocks(replyGet1000);
			System.Console.WriteLine($"Blocks: {blocks1.Count}");

			var r2 = BlockUtils.HasAnyBlock(eventList);
			System.Console.WriteLine($"HasAnyBlock: {r2}");
			var blocks2 = BlockUtils.GetBlocks(eventList);
			System.Console.WriteLine($"Blocks: {blocks2.Count}");
		}

		static void ParseBlocks()
		{
			var oneBlock = BlockUtils.GetBlocks(replyGet1000);
			var b0 = oneBlock[0];
			System.Console.WriteLine($"Start: {b0.StartLine}");
			foreach(var entry in b0.ListEntries)
				System.Console.WriteLine($"  Entry: {entry.OriginalLine}");
			System.Console.WriteLine($"End: {b0.EndLine}");

			var fourBlocks = BlockUtils.GetBlocks(eventList);
			foreach(var evBlock in fourBlocks)
			{
				if(evBlock == null) continue;
				System.Console.WriteLine($"Object ID: {evBlock.ObjectId}");
			}
		}

		static void Main()
		{
			//CheckBlocks();
			//ParseBlocks();
			TraverseBlock();
			System.Console.ReadLine();
		}
	}
}
