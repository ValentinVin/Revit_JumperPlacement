using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Jumpers
{
    internal class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location,
                    iconsDirectoryPath = Path.GetDirectoryName(assemblyLocation) + @"\icons\",
                    tabName = "Перемычки";

            application.CreateRibbonTab(tabName);

            {
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Размещение перемычек");


                panel.AddItem(new PushButtonData(nameof(NewFamilyInstance_Jumpers), "Разместить\nперемычки", assemblyLocation, typeof(NewFamilyInstance_Jumpers).FullName)
                {
                    LargeImage = new BitmapImage(new Uri(iconsDirectoryPath + "Перемычки.png"))
                });

                panel.AddItem(new PushButtonData(nameof(Update), "Обновить\nперемычки", assemblyLocation, typeof(Update).FullName)
                {
                    LargeImage = new BitmapImage(new Uri(iconsDirectoryPath + "Обновить.png"))
                });

                return Result.Succeeded;
            }
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
