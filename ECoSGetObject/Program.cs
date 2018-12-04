namespace ECoSGetObject
{
	using System;
	using ECoSEntities;
	using Newtonsoft.Json.Linq;
	using WebSocket4Net;

	class Program
	{
		static void Main()
		{
			int objectId = 1000;

			var dp = new DataProvider(DataProvider.DataModeT.Any);
			var ws = new WebSocket("ws://127.0.0.1:10050");
			ws.MessageReceived += (sender, args) =>
			{
				dp.Parse(JToken.Parse(args.Message));
				foreach (var o in dp.Objects)
				{
					var locObj = o as Locomotive;
					if (locObj == null) continue;
					if (locObj.ObjectId == objectId)
					{
						Console.WriteLine($"Name: {locObj.Name}");
						Console.WriteLine($"Protokoll: {locObj.Protocol}");
						Console.WriteLine($"Funktionen: {locObj.NrOfFunctions}");
						Console.WriteLine($"Geschwindigkeit: {locObj.Speed}");
					}
				}
			};
			ws.Open();

			System.Threading.Thread.Sleep(30 * 1000);
		}
	}
}
