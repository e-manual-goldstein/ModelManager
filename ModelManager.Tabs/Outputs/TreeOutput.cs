using ModelManager.Utils;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;
using System.Reflection.Metadata;
using System.IO;
using System.Collections;

namespace ModelManager.Tabs.Outputs
{
    public class TreeOutput : AbstractOutput<object>
    {
        private readonly TreeOutputItem[] _treeOutputItems;

        public TreeOutput(object content) : base(content)
        {
            _treeOutputItems = TreeOutputItem.CreateTreeOutputItems(content).ToArray();
        }

        public override OutputType OutputType => OutputType.Tree;

        public override Control GetOutput(double controlWidth, double tabHeight, out bool success)
        {
            var treeView = new TreeView();
            
            PopulateTreeView(treeView);
            
            //_clipboardReady = ContentAsString();
            success = true;
            Control = treeView;
            SetControlLayout(controlWidth, tabHeight);
            return treeView;            
        }

        private void PopulateTreeView(TreeView treeview)
        {
            foreach (var treeOutputItem in _treeOutputItems)
            {
                treeview.Items.Add(treeOutputItem.TreeViewItem);
            };
        }

        protected override object GetActionableContent()
        {
            throw new NotImplementedException();
        }

        private string ContentAsString()
        {
            return JsonConvert.SerializeObject(Content);
        }

        class TreeOutputItem
        {
            public static IEnumerable<TreeOutputItem> CreateTreeOutputItems(object content)
            {
                if (content is IDictionary dictionary) 
                {
                    return CreateTreeOutputItemsForDictionary(dictionary);
                }
                if (content is IEnumerable enumerable)
                {
                    return CreateTreeOutputItemsForEnumerable(enumerable);                    
                }                                
                else
                {
                    return CreateTreeOutputItemsForObject(content);
                }
            }

            private static IEnumerable<TreeOutputItem> CreateTreeOutputItemsForObject(object content)
            {
                foreach (var member in content.GetType().GetMembers())
                {
                    if (member is PropertyInfo || member is FieldInfo)
                    {
                        yield return new TreeOutputItem(CreateItemName(member, content), GetScalarValue(member, content), IsExpandable(member));
                    }
                }
            }

            private static IEnumerable<TreeOutputItem> CreateTreeOutputItemsForEnumerable(IEnumerable enumerable)
            {
                foreach (var element in enumerable)
                {
                    yield return new TreeOutputItem($"{element}", element, IsExpandable(element.GetType()));
                }
            }

            private static IEnumerable<TreeOutputItem> CreateTreeOutputItemsForDictionary(IDictionary dictionary)
            {
                foreach (DictionaryEntry kvp in dictionary)
                {
                    yield return new TreeOutputItem(kvp.Key.ToString(), kvp.Value, IsExpandable(kvp.Value.GetType()));
                }
            }

            private TreeOutputItem[] _treeOutputItems;
            bool _loaded;

            TreeOutputItem(string label, object value, bool expandable)
            {
                Label = label;
                Value = value;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TreeViewItem = CreateTreeViewItem(expandable);
                });
            }

            private TreeViewItem CreateTreeViewItem(bool isExpandable)
            {
                var treeViewItem = new TreeViewItem();
                treeViewItem.Header = $"{Label}";
                treeViewItem.IsExpanded = false;
                treeViewItem.Expanded += TreeViewItem_Expanded;
                if (isExpandable)
                {
                    treeViewItem.Items.Add("Loading...");
                }
                return treeViewItem;
            }

            private static string CreateItemName(MemberInfo member, object content)
            {
                if (!IsExpandable(member))
                {
                    return $"{member.Name}: {GetScalarValue(member, content)}";
                }                
                return $"{member.Name}";
            }

            private static bool IsAlreadyCounted(MemberInfo member, object content)
            {
                throw new NotImplementedException();
            }

            private static object GetScalarValue(MemberInfo member, object content)
            {
                if (member is PropertyInfo propertyInfo)
                {
                    return propertyInfo.GetValue(content);
                }
                else if (member is FieldInfo fieldInfo)
                {
                    return fieldInfo.GetValue(content);
                }
                throw new NotSupportedException();
            }

            private object Value { get; }

            private static bool IsExpandable(MemberInfo member)
            {
                var resultType = member switch
                {
                    PropertyInfo propertyInfo => propertyInfo.PropertyType,
                    FieldInfo fieldInfo => fieldInfo.FieldType,
                    _ => throw new NotSupportedException()
                };
                return IsExpandable(resultType);
            }

            private static bool IsExpandable(Type type)
            {
                if (type.IsPrimitive || typeof(string).Equals(type)) //for now
                {
                    return false;
                }
                return true;
            }

            private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
            {
                TreeViewItem item = e.Source as TreeViewItem;
                if (!_loaded)
                {
                    item.Items.Clear();
                    try
                    {
                        var propertyValue = Value;
                        if (propertyValue != null)
                        {
                            _treeOutputItems = CreateTreeOutputItems(propertyValue).ToArray();
                            int index = 0;
                            foreach (var outputItem in _treeOutputItems.OrderBy(r => r.Label))
                            {
                                outputItem.TreeViewItem.Header = $"{index++}. {outputItem.Label}";
                                item.Items.Add(outputItem.TreeViewItem);
                            }
                        }
                        _loaded = true;
                    }
                    catch { }
                }
            }

            public string Label { get; set; }
            
            public TreeViewItem TreeViewItem { get; set; }
                        
        }
    }
}
