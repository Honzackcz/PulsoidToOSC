﻿using System.Windows;

namespace PulsoidToOSC
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			MainProgram.StartUp();
		}
	}
}