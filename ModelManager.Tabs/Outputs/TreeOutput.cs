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
using System.Diagnostics.Metrics;

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
            
            _clipboardReady = ContentAsString();
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
                if (content is IEnumerable enumerable)
                {
                    var elementType = enumerable.GetType().GetGenericArguments()[0];
                    if (elementType != null)
                    {

                    }
                    if (enumerable.GetType().GetGenericTypeDefinition() != null
                        && enumerable.GetType().GetGenericTypeDefinition().GetInterfaces()
                        .Contains(typeof(IEnumerable<>).GetGenericTypeDefinition()))
                    {
                        return null;
                    }
                    else
                    {
                        return CreateTreeOutputItemsForEnumerable(enumerable);
                    }
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
                        yield return new TreeOutputItem(member, content);
                    }
                }
            }

            private static IEnumerable<TreeOutputItem> CreateTreeOutputItemsForEnumerable(IEnumerable enumerable)
            {
                int index = 0;
                foreach (var element in enumerable)
                {
                    yield return new TreeOutputItem(index++, element.GetType(), enumerable);
                }
            }

            private static IEnumerable<TreeOutputItem> CreateTreeOutputItemsForGenericEnumerable<TElement>(IEnumerable<TElement> enumerable)
            {
                int index = 0;
                foreach (var element in enumerable)
                {
                    yield return new TreeOutputItem(index++, typeof(TElement), enumerable);
                }
            }

            private TreeOutputItem[] _treeOutputItems;


            bool _loaded;
            public TreeOutputItem(MemberInfo member, object content)
            {
                Member = member;
                GetMemberValue = () =>
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
                };
                Label = CreateItemName(member, content);
                SourceContent = content;
                TreeViewItem = CreateTreeViewItem(member);
            }

            public TreeOutputItem(int index, Type elementType, IEnumerable enumerable)
            {
                Label = CreateItemName(index, elementType, enumerable);
                GetMemberValue = () =>
                {
                    return enumerable.Cast<object>().ElementAt(index);
                };
                SourceContent = enumerable;
                TreeViewItem = CreateTreeViewItem(index, elementType);
            }

            public TreeOutputItem(int index, Type elementType, IEnumerable<object> enumerable)
            {
                Label = CreateItemName(index, elementType, enumerable);
                SourceContent = enumerable;
                TreeViewItem = CreateTreeViewItem(index, elementType);
            }

            private TreeViewItem CreateTreeViewItem(MemberInfo member)
            {
                var treeViewItem = new TreeViewItem();
                treeViewItem.Header = Label;
                treeViewItem.IsExpanded = false;
                treeViewItem.Expanded += TreeViewItem_Expanded;
                if (IsExpandable(member))
                {
                    treeViewItem.Items.Add("Loading...");
                }
                return treeViewItem;
            }

            private TreeViewItem CreateTreeViewItem(int index, Type elementType)
            {
                var treeViewItem = new TreeViewItem();
                treeViewItem.Header = Label;
                treeViewItem.IsExpanded = false;
                treeViewItem.Expanded += TreeViewItem_Expanded;
                if (IsExpandable(elementType))
                {
                    treeViewItem.Items.Add("Loading...");
                }
                return treeViewItem;
            }

            private string CreateItemName(MemberInfo member, object content)
            {
                if (!IsExpandable(member))
                {
                    return $"{member.Name}: {GetMemberValue()}";
                }
                return $"{member.Name}";
            }

            private string CreateItemName(int index, Type elementType, IEnumerable content)
            {
                if (!IsExpandable(elementType))
                {
                    return $"{index}: {content}";
                }
                return $"{index}";
            }

            private Func<object> GetMemberValue { get; }

            private bool IsExpandable(MemberInfo member)
            {
                var resultType = member switch
                {
                    PropertyInfo propertyInfo => propertyInfo.PropertyType,
                    FieldInfo fieldInfo => fieldInfo.FieldType,
                    _ => throw new NotSupportedException()
                };
                return IsExpandable(resultType);
            }

            private bool IsExpandable(Type type)
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
                        
                        var propertyValue = GetMemberValue();
                        if (propertyValue != null)
                        {
                            _treeOutputItems = CreateTreeOutputItems(propertyValue).ToArray();
                            foreach (var outputItem in _treeOutputItems)
                            {
                                item.Items.Add(outputItem.TreeViewItem);
                            }
                        }
                        _loaded = true;
                    }
                    catch { }
                }
            }

            public string Label { get; set; }

            public TreeViewItem TreeViewItem { get; }

            public object SourceContent { get; }

            public MemberInfo Member { get; }
        }
    }
}
