using ModelManager.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ModelManager.Tabs.Outputs
{
    public class TableOutput : AbstractOutput<IDictionary<string, IEnumerable<string>>>
    {
        public TableOutput() : base(new Dictionary<string, IEnumerable<string>>())
        {

        }

        public TableOutput(IDictionary<string, IEnumerable<string>> content) : base(content)
        {

        }

        public override OutputType OutputType => OutputType.Table;

        public void AddColumn(string columnTitle, IEnumerable<string> entries)
        {
            Content[columnTitle] = entries;
        }

        public override Control GetOutput(double controlWidth, double tabHeight, out bool success)
        {
            success = false;
            var listView = new ListView();
            GridView gridView = new GridView();
            listView.View = gridView;

            List<dynamic> myItems = new List<dynamic>();
            dynamic myItem;
            IDictionary<string, object> myItemValues;

            var columns = Content;
            var rowCount = columns.First().Value.Count();
            var columnNames = columns.Keys;

            // Populate the objects with dynamic columns
            for (var i = 0; i < rowCount; i++)
            {
                myItem = new System.Dynamic.ExpandoObject();

                foreach (string columnName in columnNames)
                {
                    myItemValues = (IDictionary<string, object>)myItem;
                    myItemValues[columnName] = columns[columnName].ElementAt(i);
                }

                myItems.Add(myItem);
            }
            if (!myItems.Any())
                return new SingleOutput("0 Rows Returned").GetOutput(controlWidth, tabHeight, out success);
            // Assuming that all objects have same columns - using first item to determine the columns
            List<Column> gridColumns = new List<Column>();

            myItemValues = (IDictionary<string, object>)myItems[0];

            // Key is the column, value is the value
            foreach (var pair in myItemValues)
            {
                Column column = new Column();

                column.Title = pair.Key;
                column.SourceField = pair.Key;

                gridColumns.Add(column);
            }

            // Add the column definitions to the list view
            gridView.Columns.Clear();

            foreach (var column in gridColumns)
            {
                var binding = new Binding(column.SourceField);

                gridView.Columns.Add(new GridViewColumn { Header = column.Title, DisplayMemberBinding = binding });

                column.Dispose();
            }

            // Add all items to the list
            foreach (dynamic item in myItems)
            {
                listView.Items.Add(item);
            }
            _clipboardReady = clipboardReadyTable();
            success = true;
            Control = listView;
            SetControlLayout(controlWidth, tabHeight);
            return listView;
        }

        private string clipboardReadyTable()
        {
            var table = new StringBuilder();
            var tableContent = Content;
            var tableLength = tableContent.Values.First().Count();
            table.Append(tableContent.Keys.ElementAt(0));
            for (int i = 1; i < tableContent.Count; i++)
            {
                table.Append("\t" + tableContent.Keys.ElementAt(i));
            }
            for (int i = 0; i < tableLength; i++)
            {
                table.Append("\n");
                table.Append(tableContent.ElementAt(0).Value.ElementAt(i));
                for (int j = 1; j < tableContent.Count; j++)
                {
                    table.Append("\t" + tableContent.ElementAt(j).Value.ElementAt(i));
                }
            }
            return table.ToString();
        }

        private class Column : IDisposable
        {
            public string Title { get; set; }
            public string SourceField { get; set; }

            public void Dispose()
            {

            }
        }
    }
}
