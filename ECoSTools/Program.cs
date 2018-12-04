namespace ECoSTools
{
	using System;

	using CtrlAccessories = ECoSToolsLibrary.CtrlAccessories;
	using CtrlLocomotive = ECoSToolsLibrary.CtrlLocomotive;
	using S88Listener = ECoSToolsLibrary.S88Listener;

	class Program
	{
		static void Main()
		{
			//CtrlAccessories.Execute(new []{ "--action=oids" });
			//CtrlAccessories.Execute(new[] { "--action=info", "--oids=20010,20011,20012,20013" });
			//CtrlAccessories.Execute(new[] { "--action=switch", "--oids=20010,20011" });
			//CtrlAccessories.Execute(new[] { "--action=init" });
			//CtrlAccessories.Execute(new[] { "--action=listing" });

			//CtrlLocomotive.Execute(new[] { "--action=oids" });
			//CtrlLocomotive.Execute(new[] { "--action=stop" });
			//CtrlLocomotive.Execute(new[] { "--action=info", "--oid=1000" });
			//CtrlLocomotive.Execute(new[] { "--action=velocity", "--oid=1000", "--speed=10" });
			//CtrlLocomotive.Execute(new[] { "--action=velocity", "--oid=1000", "--speed=10", "--timeDelta=150" });
			//CtrlLocomotive.Execute(new[] { "--action=fncs", "--f=0on,1000" });
			//CtrlLocomotive.Execute(new[] { "--action=fncs", "--f=0off" });
			//CtrlLocomotive.Execute(new[] { "--action=turn" });
			
			//S88Listener.Execute(new []{ "--action=show" });
			S88Listener.Execute(new[] { "--action=show", "--human" });
			//S88Listener.Execute(new[] { "--action=listen" });
			//S88Listener.Execute(new[] { "--action=listen", "--human" });
			
			//ECoSToolsLibrary.Teaching.Execute(new []{ "--route=Route_Block01_to_Block13", "--oids=20012,20054,20100" });

			Console.ReadKey();
		}
	}
}
