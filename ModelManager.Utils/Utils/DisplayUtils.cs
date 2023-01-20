using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ModelManager.Utils
{
	public static class DisplayUtils
	{
		public static void AddAsync(Canvas canvas, UIElement element)
		{
			Application.Current.Dispatcher.Invoke(() => canvas.Children.Add(element));
		}

		public static void UpdateAsync(UIElement element, Action<UIElement> action)
		{
			Application.Current.Dispatcher.Invoke(() => action(element));
		}
	}
}
