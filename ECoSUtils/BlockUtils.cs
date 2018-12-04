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

using ECoSUtils.Replies;

namespace ECoSUtils
{
	using System;
	using System.Collections.Generic;

	public static class BlockUtils
	{
		public const char CR = '\r';
		public const char LF = '\n';
		public const string CRLF = "\r\n";

		public static bool HasAnyBlock(string msg)
		{
			var cmp = StringComparison.Ordinal;
			if (string.IsNullOrEmpty(msg)) return false;
			if (msg.IndexOf("<END ", cmp) == -1) return false;
			if (msg.IndexOf("<REPLY ", cmp) != -1) return true;
			if (msg.IndexOf("<EVENT ", cmp) != -1) return true;
			return false;
		}

		public static bool HasAnyBlock(IList<string> lines)
		{
			if (lines == null) return false;
			if (lines.Count < 2) return false;
			string msg = string.Join(CRLF, lines);
			return HasAnyBlock(msg);
		}

		public static IReadOnlyList<IBlock> GetBlocks(string msg)
		{
			var lines = msg.Split(new []{ LF }, StringSplitOptions.RemoveEmptyEntries);
			return GetBlocks(lines);
		}

		public static IReadOnlyList<IBlock> GetBlocks(IList<string> lines)
		{
			var blocks = new List<IBlock>();
			var blockLines = new List<string>();
			foreach (var currentLine in lines)
			{
				if (string.IsNullOrEmpty(currentLine)) continue;
				var line = currentLine.TrimStart(CR, LF);
				if (!line.ToUpper().StartsWith("<END "))
				{
					blockLines.Add(line + CRLF);
					continue;
				}

				blockLines.Add(line + CRLF);

				var firstLine = blockLines[0].ToUpper();
				IBlock instance;
				if (firstLine.StartsWith("<EVENT "))
					instance = new EventBlock();
				else if (firstLine.StartsWith("<REPLY "))
					instance = new ReplyBlock();
				else continue;

				if (!instance.Parse(blockLines)) return null;
				blocks.Add(instance);
				blockLines.Clear();
			}

			return blocks;
		}
	}
}
