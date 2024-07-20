using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Jumpers
{
    [Transaction(TransactionMode.Manual)]
    public class NewFamilyInstance_Jumpers : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;

            Document doc = uiDoc.Document;

            UserWindowJumpers window = new UserWindowJumpers(doc, uiDoc);
            window.ShowDialog();

            return Result.Succeeded;
        }
    }
}
