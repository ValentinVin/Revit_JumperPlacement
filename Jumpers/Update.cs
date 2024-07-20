using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Jumpers
{
    [Transaction(TransactionMode.Manual)]
    public class Update : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;

            Document doc = uiDoc.Document;

            int[] correct = check(doc);
            if ((correct[0] != 0) | (correct[1] != 0))
            {
                MessageBox.Show("Корректировка размещенных перемычек:\n\n" + "Количество удаленных перемычек: " + correct[0] +
                    "\nКоличество измененных перемычек: " + correct[1]);
            }

            return Result.Succeeded;
        }

        private int[] check(Document doc)
        {
            int countDelete = 0;
            int countEdit = 0;
            int tempid = 0;
            double wt = 0;

            Family[] families = new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>().ToArray();
            Family familyJumper = null;
            List<FamilyInstance> jumpers = new List<FamilyInstance>();

            foreach (Family fs in families)
            {
                if (fs.Name == "Перемычка")
                {
                    familyJumper = fs; break;
                }
            }

            ISet<ElementId> idsSet = familyJumper.GetFamilySymbolIds();
            List<ElementId> ids = idsSet.ToList();
            IList<Element> temp = new List<Element>();

            foreach (ElementId id in ids)
            {
                FamilyInstanceFilter filter = new FamilyInstanceFilter(doc, id);
                temp = new FilteredElementCollector(doc).WherePasses(filter).ToElements();
                foreach (Element t in temp)
                {
                    jumpers.Add(t as FamilyInstance);
                }
            }

            Element opening = null;
            FamilyInstance inst = null;
            bool flag = false;

            using (Transaction transaction = new Transaction(doc, "Correct"))
            {
                transaction.Start();

                foreach (FamilyInstance j in jumpers)
                {
                    tempid = j.LookupParameter("id проема").AsInteger();
                    flag = false;
                    if (tempid > 0)
                    {
                        opening = doc.GetElement(new ElementId(tempid));
                        if (opening == null) { doc.Delete(j.Id); countDelete++; }
                        else
                        {
                            LocationPoint openingLocation = opening.Location as LocationPoint;
                            XYZ openingCenter = openingLocation.Point;
                            LocationPoint jumperLocation = j.Location as LocationPoint;
                            XYZ jumperCenter = jumperLocation.Point;
                            if (Math.Round(openingCenter.X, 5) != Math.Round(jumperCenter.X, 5))
                            {
                                jumperLocation.Move(new XYZ((openingCenter.X - jumperCenter.X), 0, 0));
                                flag = true;
                            }
                            if (Math.Round(openingCenter.Y, 5) != Math.Round(jumperCenter.Y, 5))
                            {
                                jumperLocation.Move(new XYZ(0, (openingCenter.Y - jumperCenter.Y), 0));
                                flag = true;
                            }
                            inst = opening as FamilyInstance;
                            wt = doc.GetElement(inst.Host.GetTypeId()).LookupParameter("Толщина").AsDouble();
                            if (wt != j.LookupParameter("Ширина").AsDouble())
                            {
                                j.LookupParameter("Ширина").Set(wt);
                                flag = true;
                            }
                            if (flag) countEdit++;
                        }
                    }
                }
                transaction.Commit();
            }
            int[] result = { countDelete, countEdit };
            return result;
        }
    }
}
