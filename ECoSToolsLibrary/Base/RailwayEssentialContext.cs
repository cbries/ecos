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

using System.IO;

namespace ECoSToolsLibrary.Base
{
	public class RailwayEssentialContext
	{
		private static string FallbackDirectory = @"C:\RailwayEssential\";
		private readonly string _baseDirectory = null;

		public Blocks AvailableBlocks { get; }
		public S88FeedbackList AvailableFeedbacks { get; }
		public Routes AvailableRoutes { get; }

		public RailwayEssentialContext(string baseDirectory=null)
		{
			if (string.IsNullOrEmpty(baseDirectory))
				_baseDirectory = FallbackDirectory;
			else
				_baseDirectory = baseDirectory;

			var fileBlocksPath = Path.Combine(_baseDirectory, "blocks.json");
			var fileFeedbacksPath = Path.Combine(_baseDirectory, "feedbacks.json");
			var fileRoutesPath = Path.Combine(_baseDirectory, "routes.json");

			AvailableFeedbacks = new S88FeedbackList(this);
			AvailableFeedbacks.Parse(fileFeedbacksPath);

			AvailableBlocks = new Blocks(this);
			AvailableBlocks.Parse(fileBlocksPath);

			AvailableRoutes = new Routes(this);
			AvailableRoutes.Parse(fileRoutesPath);
		}
	}
}
