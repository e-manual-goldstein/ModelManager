﻿using ModelManager.Utils;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ModelManager.Tabs.Outputs
{
    public interface IOutput
    {
        OutputType OutputType { get; }
        List<Button> ActionButtons { get; }

        Control GetOutput(double controlWidth, double tabHeight, out bool success);

        event ButtonClickedEventHandler ActionClicked;
        event ButtonClickedAsyncEventHandler ActionClickedAsync;

        void copyOutput(object sender, RoutedEventArgs e);
    }
}