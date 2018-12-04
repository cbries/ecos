namespace ECoS
{
	using System;
	using System.Threading;
	using Newtonsoft.Json;

	using ECoSListener;
	using ECoSEntities;

	class Program
	{
		static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);
		private static ECoSListener _listener = null;

		public static string IpAddress = "192.168.178.61";
		public static ushort Port = 15471;

		static void Main()
		{
			Console.WriteLine($"Try to connect to: {IpAddress}:{Port}");
			Console.CancelKeyPress += (sender, eArgs) =>
			{
				QuitEvent.Set();
				eArgs.Cancel = true;
			};

			_listener = new ECoSListener(IpAddress, Port);
			_listener.Changed += ListenerOnChanged;
			_listener.Start();

			QuitEvent.WaitOne();

			_listener.Stop();
		}

		private static void ListenerOnChanged(object sender, IDataProvider dataprovider)
		{
			var dp = dataprovider as DataProvider;
			if (dp == null) return;
			Console.WriteLine(dp.ToJson().ToString(Formatting.Indented));
		}
	}
}
