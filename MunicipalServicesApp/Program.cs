using MunicipalServicesApp.Forms;
using MunicipalServicesApp.Services;
using System;
using System.Windows.Forms;

namespace MunicipalServicesApp
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Seed the demo service requests once, before any form is shown,
            // so Service Request Status has data to display.
            DemoDataSeeder.Seed();

            Application.Run(new MainMenuForm());
        }
    }
}