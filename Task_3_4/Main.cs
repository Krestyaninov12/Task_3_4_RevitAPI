using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_3_4
{//
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;


            var pipes = new FilteredElementCollector(doc)
            .OfClass(typeof(Pipe))
                .Cast<Pipe>()
                .ToList();

            foreach (var pipe in pipes)
            {
                //Добавление параметра "Типоразмер трубы"
                var categorySet = new CategorySet();
                categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));

                using (Transaction ts = new Transaction(doc, "Add parameter"))
                {
                    ts.Start();
                    CreateSharedParameter(uiapp.Application, doc, "Типоразмер трубы", categorySet, BuiltInParameterGroup.PG_GEOMETRY, true);
                    ts.Commit();
                }

                //Получение значения параметров "наружный диаметр" и "внутренний диаметр"
                Parameter outsideDiameterParameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER);
                Parameter insideDiameterParameter = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_INNER_DIAM_PARAM);

                var OD = outsideDiameterParameter.AsValueString();
                var ID = insideDiameterParameter.AsValueString();

                //Установка значения в параметр "Длина с запасом"
                using (Transaction ts = new Transaction(doc, "Set parameters"))
                {
                    ts.Start();
                    Parameter dimensionTypeParameter = pipe.LookupParameter("Типоразмер трубы");
                    dimensionTypeParameter.Set($"Труба {OD} / {ID}");
                    ts.Commit();
                }
            }
            return Result.Succeeded;
        }
        private void CreateSharedParameter(Application application,
            Document doc, string parameterName, CategorySet categorySet,
            BuiltInParameterGroup builtInParameterGroup, bool isInstance)
        {
            DefinitionFile definitionFile = application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                TaskDialog.Show("Ошибка", "Не найден файл общих параметров");
                return;
            }

            Definition definition = definitionFile.Groups
                .SelectMany(group => group.Definitions)
                .FirstOrDefault(def => def.Name.Equals(parameterName));
            if (definition == null)
            {
                TaskDialog.Show("Ошибка", "Не найден указанный параметр");
                return;
            }

            Binding binding = application.Create.NewTypeBinding(categorySet);
            if (isInstance)
                binding = application.Create.NewInstanceBinding(categorySet);

            BindingMap map = doc.ParameterBindings;
            map.Insert(definition, binding, builtInParameterGroup);
        }
    }
}
