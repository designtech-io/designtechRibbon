﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssentialTools
{
    public static class Utils
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
        (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
    [Transaction(TransactionMode.Manual)]
    public class CommandRemoveTemplates : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Document doc = uidoc.Document;
            
            RemoveTemplates removeTempaltes = new RemoveTemplates(commandData);
            // Invoke the removeViewTemplates method
            removeTempaltes.removeViewTemplates();
            

            return Result.Succeeded;
        }
    }

    internal class RemoveTemplates
    {
        private ExternalCommandData _commandData;
        /// <summary>
        /// Constructor. Takes the external common data (of the Revit app)
        /// </summary>
        /// <param name="data"></param>
        public RemoveTemplates(ExternalCommandData data)
        {
            _commandData = data;
        }

        public void removeViewTemplates()
        {
            Document doc = this._commandData.Application.ActiveUIDocument.Document;

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<View> views = collector.OfClass(typeof(View)).Cast<View>().Where(x => !x.IsTemplate).ToList();
            List<ElementId> usedTemplateIds = collector.OfClass(typeof(View)).Cast<View>().Where(x => !x.IsTemplate).Select(x => x.ViewTemplateId).ToList();
            List<ElementId> allTemplateIds = collector.OfClass(typeof(View)).Cast<View>().Where(x => x.IsTemplate).Select(x => x.Id).ToList();
            List<ElementId> unusedTemplateIds = allTemplateIds.Except(usedTemplateIds).ToList();

            Dictionary<string, ElementId> store = unusedTemplateIds.DistinctBy(x => (doc.GetElement(x) as View).Name).ToDictionary(x => (doc.GetElement(x) as View).Name, x => x);
            Dictionary<string, ElementId> storeAll = allTemplateIds.DistinctBy(x => (doc.GetElement(x) as View).Name).ToDictionary(x => (doc.GetElement(x) as View).Name, x => x);
            //Dictionary<string, ElementId> store = unusedTemplateIds.ToDictionary(x => (doc.GetElement(x) as View).Name, x => x);
            //Dictionary<string, ElementId> storeAll = allTemplateIds.ToDictionary(x => (doc.GetElement(x) as View).Name, x => x);

            using (TemplatesForm form = new TemplatesForm(store, storeAll))
            {
                System.Windows.Forms.DialogResult result =  form.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    store = form.resultStore; 
                    using (Transaction t = new Transaction(doc, "Delete filters"))
                    {
                        t.Start();
                        doc.Delete(store.Values);
                        t.Commit();
                    }
                    TaskDialog.Show("Unused View Templates.", "Unused View Templates:" + Environment.NewLine + store.Count.ToString() + " View Templates were removed.");
                }
            }
        }
    }
}
