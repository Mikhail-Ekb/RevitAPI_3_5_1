using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPI_3_5_1
{
    public class MainViewViewModel
    {
        private ExternalCommandData _commandData;
        public DelegateCommand SelectCommand_1 { get; }
        public DelegateCommand SelectCommand_2 { get; }
        public DelegateCommand SaveCommand { get; }
        public List<Element> PickObjects { get; } = new List<Element>();
        public List<WallType> WallTypes { get; } = new List<WallType>();
        
        public WallType SelectedWallType { get; set; }

        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;

            SelectCommand_1 = new DelegateCommand(OnSelectCommand_1);
            SelectCommand_2 = new DelegateCommand(OnSelectCommand_2);
            
            SaveCommand = new DelegateCommand(OnSaveCommand);           
            PickedObjects = SelectionUtils.PickObjects(commandData);
            WallTypes = WallUtils.GetWallTypes(commandData);
        }        

        public event EventHandler HideRequest;
        private void RaiseHideRequest()
        {
            HideRequest?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler ShowRequest;
        private void RaiseShowRequest()
        {
            ShowRequest?.Invoke(this, EventArgs.Empty);
        }        
        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }


        private void OnSelectCommand_1()
        {
            RaiseHideRequest();

            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            List<Pipe> fInstances = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_PipeCurves)
            .WhereElementIsNotElementType()
            .Cast<Pipe>()
            .ToList();

            TaskDialog.Show("Количество труб", fInstances.Count.ToString());

            RaiseShowRequest();
        }

        private void OnSelectCommand_2()
        {
            RaiseHideRequest();

            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            List<Wall> walls = new FilteredElementCollector(doc)
             .OfCategory(category: BuiltInCategory.OST_Walls)
             .WhereElementIsNotElementType()
             .Cast<Wall>()
             .ToList();

            List<double> volumeWallList = new List<double>();
            foreach (Wall oWall in walls)
            {
                Parameter volumeParametr = oWall.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED);
                double volumeWall;

                if (volumeParametr.StorageType == StorageType.Double)
                {
                    volumeWall = volumeParametr.AsDouble();
                    volumeWall = UnitUtils.ConvertFromInternalUnits(volumeWall, DisplayUnitType.DUT_CUBIC_METERS);
                    volumeWallList.Add(volumeWall);
                }
            }
            double sumVolume = volumeWallList.ToArray().Sum();
            TaskDialog.Show("Суммарный объем", $"{sumVolume}");

            RaiseShowRequest();
        }
       
        public static List<WallType> GetWallTypes(ExternalCommandData commandData)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            List<WallType> wallTipes = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .ToList();
            return wallTipes;
        }

        public static List<Element> PickObjects(ExternalCommandData commandData, string massage = "Выберите элементы")
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            IList<Reference> selectedElementRefList = null;            
            try
            {
                selectedElementRefList = uidoc.Selection.PickObjects(ObjectType.Face, "Выберете элемент");
                var wallList = new List<Wall>();

                foreach (var selectedElement in selectedElementRefList)
                {
                    Element element = doc.GetElement(selectedElement);
                    if (element is Wall)
                    {
                        Wall oWall = (Wall)element;
                        wallList.Add(oWall);                        
                    }                    
                }                                
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            { }           
            if (selectedElementRefList == null)
            {
                return Result.Cancelled;
            }
        }

        private void onSaveCommand()
        {
            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (PickedObjects.Count == 0 || WallTypes == null)
            {
                return;
            }
            using (var ts = new Transaction(doc, "Set class"))
            {
                ts.Start();

                foreach (pickedObject in PickedObjects)
                {
                    if (pickedObject is Wall)
                    {
                        var oWall = pickedObject as Wall;
                        oWall.SetWallType(SelectedWallType.Id);
                    }
                }
                ts.Commit();
            }
        }
    }
}



