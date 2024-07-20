using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Jumpers
{
    /// <summary>
    /// Логика взаимодействия для UserWindowJumpers.xaml
    /// </summary>
    public partial class UserWindowJumpers : Window
    {
        UIDocument _uiDoc;
        Document _doc;
        View view;
        FamilySymbol jumperSymbol;
        List<FamilyInstance> selectedTypeOpenings; //список экземпляров выбранного семейства проема
        List<Family> allFamilyOpenings; //список всех семейств проемов (двери и окна)
        List<FamilyInstance> selectedOpenings;
        List<Element> wallTypes;
        IList<Reference> refs;
        bool filterSelect = true; //переключатель выбора элементов (да - видимые на виде, нет - все в проекте)

        public UserWindowJumpers(Document doc, UIDocument uiDoc)
        {
            InitializeComponent();
            _uiDoc = uiDoc;
            _doc = doc;
            view = _doc.ActiveView;

            wallTypes = new FilteredElementCollector(_doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsElementType().ToList();
            foreach (Element e in wallTypes)
            {
                Combobox_WallTypes.Items.Add(e.Name);
            }

            Combobox_JumperFamily.Items.Add("Перемычка");
            Family[] families = new FilteredElementCollector(_doc).OfClass(typeof(Family)).Cast<Family>().ToArray();

            allFamilyOpenings = new List<Family>();
            selectedTypeOpenings = new List<FamilyInstance>();
            selectedOpenings = new List<FamilyInstance>();

            foreach (Family fs in families)
            {
                if (fs.FamilyCategory.Name == "Двери")
                {
                    allFamilyOpenings.Add(fs);
                }
            }

            foreach (Family fs in families)
            {
                if (fs.FamilyCategory.Name == "Окна")
                {
                    allFamilyOpenings.Add(fs);
                }
            }

            Family fj = null;
            foreach (Family fs in families)
            {
                if (fs.Name == "Перемычка")
                {
                    fj = fs; break;
                }
            }

            ISet<ElementId> idsSet = fj.GetFamilySymbolIds();
            List<ElementId> ids = idsSet.ToList();
            foreach (ElementId i in ids)
            {
                Combobox_JumperTypes.Items.Add(_doc.GetElement(i).Name);
            }
        }

        public class OpeningsSF : ISelectionFilter
        {
            public bool AllowElement(Element el)
            {
                if ((el.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Doors) || 
                    (el.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Windows)) return true;
                else return false;
            }
            public bool AllowReference(Reference refer, XYZ point)
            {
                return true;
            }
        }

        //функция - при нажатии кнопки "Разместить перемычки"
        private void placeJumpers(object sender, RoutedEventArgs e)
        {
            using (Transaction transaction = new Transaction(_doc, "Insert doors"))
            {
                if (Combobox_JumperFamily.SelectedItem == null) MessageBox.Show("Выберите семейство перемычки");
                else if (Combobox_JumperTypes.SelectedItem == null) MessageBox.Show("Выберите типоразмер перемычки");
                else if ((Combobox_WallTypes.SelectedItem == null) & (Rbtn3.IsChecked == false)) MessageBox.Show("Выберите типоразмер стены");
                else
                {
                    if (Rbtn3.IsChecked == true)
                    {
                        Close();
                        try
                        {
                            ISelectionFilter openingSF = new OpeningsSF();
                            refs = _uiDoc.Selection.PickObjects(ObjectType.Element, openingSF);
                            foreach (Reference r in refs)
                            {
                                selectedOpenings.Add(_doc.GetElement(r.ElementId) as FamilyInstance);
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        List<string> s = new List<string>();
                        foreach(CheckBox c in openingsBox.Children)
                        {
                            if(c.IsChecked == true)
                            {
                                s.Add(c.Content.ToString());
                            }
                        }

                        foreach(Family f in allFamilyOpenings)
                        {
                            if(s.Contains(f.Name))
                            {
                                ISet<ElementId> idsSet = f.GetFamilySymbolIds();
                                List<ElementId> ids = idsSet.ToList();
                                IList<Element> temp = new List<Element>();

                                foreach (ElementId id in ids)
                                {
                                    FamilyInstanceFilter filter = new FamilyInstanceFilter(_doc, id);
                                    if (filterSelect) temp = new FilteredElementCollector(_doc, view.Id).WherePasses(filter).ToElements();
                                    else temp = new FilteredElementCollector(_doc).WherePasses(filter).ToElements();
                                    foreach (Element t in temp)
                                    {
                                        selectedTypeOpenings.Add(t as FamilyInstance);
                                    }
                                }
                            }
                        }
                        foreach (FamilyInstance fi in selectedTypeOpenings)
                        {
                            if (fi.Host.Name == Combobox_WallTypes.SelectedItem.ToString()) selectedOpenings.Add(fi);
                        }
                    }

                    //получение типоразмера перемычки для размещения
                    jumperSymbol = new FilteredElementCollector(_doc).OfClass(typeof(FamilySymbol))
                                                                           .OfCategory(BuiltInCategory.OST_GenericModel)
                                                                           .Cast<FamilySymbol>()
                                                                           .First(it => it.FamilyName == Combobox_JumperFamily.SelectedItem.ToString() && it.Name == Combobox_JumperTypes.SelectedItem.ToString());
                    transaction.Start();

                    if (!jumperSymbol.IsActive)
                    {
                        jumperSymbol.Activate();
                    }

                    double wallThikness = 0;

                    foreach (var opening in selectedOpenings)
                    {
                        LocationPoint openingLocation = opening.Location as LocationPoint;
                        XYZ openingCenter = openingLocation.Point;
                        double angle = openingLocation.Rotation;
                        double h = opening.LookupParameter(openingHeight.Text).AsDouble();

                        //Создание перемычки
                        Element jumper = _doc.Create.NewFamilyInstance(new XYZ(openingCenter.X, openingCenter.Y, openingCenter.Z + h), jumperSymbol, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        //Поворот перемычки
                        LocationPoint jumperLocation = jumper.Location as LocationPoint;
                        Line axis = Line.CreateBound(openingCenter, new XYZ(openingCenter.X, openingCenter.Y, openingCenter.Z + 1));
                        jumperLocation.Rotate(axis, angle);
                        //Изменение ширины перемычки
                        wallThikness = _doc.GetElement(opening.Host.GetTypeId()).LookupParameter("Толщина").AsDouble();
                        jumper.LookupParameter("Ширина").Set(wallThikness);
                        //Изменение длины перемычки
                        double length = opening.LookupParameter(openingWidth.Text).AsDouble();
                        jumper.LookupParameter("Длина").Set(length);
                        if (length < UnitUtils.ConvertToInternalUnits(1500, UnitTypeId.Millimeters))
                        {
                            jumper.LookupParameter("Длина опирания 1").Set(UnitUtils.ConvertToInternalUnits(200, UnitTypeId.Millimeters));
                            jumper.LookupParameter("Длина опирания 2").Set(UnitUtils.ConvertToInternalUnits(200, UnitTypeId.Millimeters));
                        }
                        else if (length <= UnitUtils.ConvertToInternalUnits(3000, UnitTypeId.Millimeters))
                        {
                            jumper.LookupParameter("Длина опирания 1").Set(UnitUtils.ConvertToInternalUnits(250, UnitTypeId.Millimeters));
                            jumper.LookupParameter("Длина опирания 2").Set(UnitUtils.ConvertToInternalUnits(250, UnitTypeId.Millimeters));
                        }
                        else
                        {
                            jumper.LookupParameter("Длина опирания 1").Set(UnitUtils.ConvertToInternalUnits(300, UnitTypeId.Millimeters));
                            jumper.LookupParameter("Длина опирания 2").Set(UnitUtils.ConvertToInternalUnits(300, UnitTypeId.Millimeters));
                        }
                        //Запись информации о проеме в перемычку
                        jumper.LookupParameter("id проема").Set(opening.Id.IntegerValue);
                    }
                    transaction.Commit();
                    MessageBox.Show("Количество размещенных перемычек: " + selectedOpenings.Count.ToString());
                    Close();
                }
            }
        }

        //функция - при выборе пункта "Видимые на виде"
        private void filterView(object sender, RoutedEventArgs e)
        {
            filterSelect = true;
            Combobox_WallTypes.IsEnabled = true;
        }

        //функция - при выборе пункта "Во всем проекте"
        private void filterAll(object sender, RoutedEventArgs e)
        {
            filterSelect = false;
            Combobox_WallTypes.IsEnabled = true;
        }

        private void filterUserSelect(object sender, RoutedEventArgs e)
        {
            Combobox_WallTypes.IsEnabled = false;
        }

        //функция - при выборе типоразмера стен. Выводит его толщину
        private void selectWallTypes(object sender, SelectionChangedEventArgs e)
        {
            double width = 0;
            if (Combobox_WallTypes.SelectedItem == null) Thickness.Text = "";
            else
            {
                foreach (Element wt in wallTypes)
                {
                    if (wt.Name == Combobox_WallTypes.SelectedItem.ToString()) 
                    {
                        width = wt.LookupParameter("Толщина").AsDouble();
                        break;
                    }
                }
                Thickness.Text = Math.Round(UnitUtils.ConvertFromInternalUnits(width, UnitTypeId.Millimeters)).ToString();
            }

            openingsBox.Children.Clear();

            foreach (Family f in allFamilyOpenings)
            {
                List<FamilyInstance> openings = new List<FamilyInstance>();
                ISet<ElementId> idsSet = f.GetFamilySymbolIds();
                List<ElementId> ids = idsSet.ToList();
                IList<Element> temp = new List<Element>();

                foreach (ElementId id in ids)
                {
                    FamilyInstanceFilter filter = new FamilyInstanceFilter(_doc, id);
                    if (filterSelect) temp = new FilteredElementCollector(_doc, view.Id).WherePasses(filter).ToElements();
                    else temp = new FilteredElementCollector(_doc).WherePasses(filter).ToElements();
                    foreach (Element t in temp)
                    {
                        openings.Add(t as FamilyInstance);
                    }
                }
                foreach (FamilyInstance fi in openings)
                {
                    if (fi.Host != null)
                    {
                        if (fi.Host.Name == Combobox_WallTypes.SelectedItem.ToString())
                        {
                            CheckBox checkbox = new CheckBox();
                            checkbox.Content = f.Name;
                            checkbox.Height = 25;
                            openingsBox.Children.Add(checkbox);
                            break;
                        }
                    }
                }
                if (openingsBox.Children.Count > 4)
                {
                    this.Height = 450 + openingsBox.Children.Count * 25;
                    ButtonPlace.Margin = new Thickness(0, 360 + openingsBox.Children.Count * 25, 0, 0);
                }
                else
                {
                    this.Height = 550;
                    ButtonPlace.Margin = new Thickness(0, 460, 0, 0);
                }
            }
        }
    }
}
