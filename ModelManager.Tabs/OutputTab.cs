using ModelManager.Core;
using ModelManager.Tabs.Outputs;
using ModelManager.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ModelManager.Tabs
{
	public class OutputTab : IDisposable
    {
		#region Private Fields

		private TabManager _tabManager;
		private TabControl _tabControl;
		private TabItem _tabItemControl;
		private Canvas _tabCanvas;

		private Control _outputControl;

		private const double CANVAS_MARGIN = 15;
		private const double TAB_ITEM_WIDTH = 100;
		private const double FIELD_HEIGHT = 30;
		private const double FIELD_LABEL_WIDTH = 150;
		private const double FIELD_WIDTH = 150;
        private const double BUTTON_HEIGHT = 30;
        private const double BUTTON_WIDTH = 250;
        
		public OutputTab PreviousTab;

		private IOutputSource _executingTab;
		private MethodInfo _executedAction;
		private Dictionary<string, Control> _inputControls;
		private List<UIElement> _disposableElements;
		private Dictionary<string, Control> _mandatoryControls;
		private TextBlock _errorMessage;
		private ProgressBar _progressBar;

        #endregion

        #region Constructors

        public OutputTab(TabManager tabManager, TabControl tabControl, int tabId, string tabTitle)
		{
			_tabManager = tabManager;
			_tabControl = tabControl;
			_tabItemControl = new TabItem();
			_tabCanvas = new Canvas();
			_disposableElements = new List<UIElement>();
			_tabItemControl.Content = _tabCanvas;
			_tabItemControl.Name = "Output" + tabId;
			_tabItemControl.Header = tabTitle;
			_tabItemControl.Width = TAB_ITEM_WIDTH;
			_tabItemControl.Margin = _tabManager.FocusedOutputTabThickness;
			//_tabItemControl.MouseLeftButtonUp += selectOutputTab;
			_tabControl.Items.Add(_tabItemControl);
			addCloseButton();
			addProgressBar();
			TabId = tabId;
		}

		#endregion

		#region Tab Management

		public int TabId { get; private set; }

		public TabItem TabItemControl
		{
			get
			{
				return _tabItemControl;
			}
		}

		public string TabTitle
		{
			get
			{
				return _tabItemControl.Header.ToString();
			}
			set
			{
				_tabItemControl.Header = value;
			}
		}

		public TextBlock OutputHeader { get; set; }

		public MethodInfo ExecutedAction
		{
			get
			{
				return _executedAction;
			}
		}

		public void DisplayOutput(IOutputSource callingService, string callingAction, object output, params string[] extraInfo)
		{
			var typedOutput = output as IOutput;
			if (typedOutput == null)
			{
                var outputType = TypeUtils.DetermineOutputType(output);
                switch (outputType)
                {
                    case OutputType.Single:
                        typedOutput = new SingleOutput((string)output);
                        break;
                    case OutputType.List:
                        typedOutput = new ListOutput((List<string>)output);
                        break;
                    case OutputType.Table:
                        typedOutput = new TableOutput((IDictionary<string, IEnumerable<string>>)output);
                        break;
                    default:
                        break;
                }
            }
            addOutputHeader(callingAction, extraInfo);
			var tabItemControlWidth = double.IsNaN(_tabItemControl.Width) ? TAB_ITEM_WIDTH : _tabItemControl.Width;
			var controlWidth = _tabControl.Width - tabItemControlWidth - (CANVAS_MARGIN * 2) - 10;
            _outputControl = typedOutput.GetOutput(controlWidth, _tabControl.Height - 100, out bool success);
			typedOutput.ActionClicked += OutputActionClicked;
			typedOutput.ActionClickedAsync += OutputActionClickedAsync;
            addCopyOutputButton(typedOutput);
			foreach (Button button in typedOutput.ActionButtons)
			{
				_tabCanvas.Children.Add(button);
			}
			//SetControlLayout(_outputControl, typedOutput);
            Canvas.SetTop(_outputControl, 50);
            Canvas.SetLeft(_outputControl, CANVAS_MARGIN);
            if (success)
            {
                _tabCanvas.Children.Add(_outputControl);
                _disposableElements.Add(_outputControl);
            }
            _executingTab = callingService;
        }

		private void OutputActionClicked(Func<object> func, string actionName)
		{
            var tab = _tabManager.InitialiseOutputTab(actionName);
			var actionContext = new ActionContext(actionName, tab, func);
			tab.DisplayExecutingMessage();
            try
            {
				Task.Run(async () => await actionContext.ExecuteAction());
            }
            catch (Exception ex)
            {
                _tabManager.DisplayError(ex, null, tab);
            }
		}

        private async Task OutputActionClickedAsync(Func<object> func, string actionName)
        {
            var tab = _tabManager.InitialiseOutputTab(actionName);
            var actionContext = new ActionContext(actionName, tab, func);
            try
            {
                await actionContext.ExecuteAction();
            }
            catch (Exception ex)
            {
                _tabManager.DisplayError(ex, null, tab);
            }
        }

        //public void DisplayOutput<T>(AbstractServiceTab callingService, string callingAction, T output, params string[] extraInfo)
        //	where T : IOutput
        //      {
        //          addOutputHeader(callingAction, extraInfo);
        //          addCopyOutputButton();
        //          bool success = false;
        //          _outputControl = output switch
        //	{
        //		SingleOutput single => outputAsSingle(single, out success),
        //		ListOutput list => outputAsList(list, out success),
        //		TableOutput table => outputAsTable(table, out success),
        //		_ => new Control()
        //	};            
        //          Canvas.SetTop(_outputControl, 50);
        //          Canvas.SetLeft(_outputControl, CANVAS_MARGIN);
        //          if (success)
        //          {
        //              _tabCanvas.Children.Add(_outputControl);
        //              _disposableElements.Add(_outputControl);
        //          }
        //          _executingTab = callingService;
        //      }

        private void selectOutputTab(object sender, RoutedEventArgs e)
		{
			_tabManager.SelectOutputTab(this);
		}

		private void addOutputHeader(string headerContent, string[] extraInfo)
		{
			OutputHeader = new TextBlock();
			foreach (var item in extraInfo)
			{
				headerContent += " - " + item;
			}
			OutputHeader.Text = AppUtils.CreateDisplayString(headerContent);
			Canvas.SetTop(OutputHeader, CANVAS_MARGIN);
			Canvas.SetLeft(OutputHeader, CANVAS_MARGIN);
			_tabCanvas.Children.Add(OutputHeader);
			_disposableElements.Add(OutputHeader);
		}

		public void Focus()
		{
			_tabItemControl.Margin = _tabManager.FocusedOutputTabThickness;
		}

		public void Blur()
		{
			_tabItemControl.Margin = new Thickness(0);
		}

		#region Close Tab

		private void addCloseButton()
		{
			var closeButton = new Button();
			closeButton.Height = 20;
			closeButton.Width = 20;
			closeButton.Content = "X";
			closeButton.Click += closeTab;
			Canvas.SetTop(closeButton, 2);
			Canvas.SetRight(closeButton, 2);
			_tabCanvas.Children.Add(closeButton);
        }

        private void closeTab(object sender, RoutedEventArgs e)
		{
			_tabManager.CloseTab(this);
        }

        private void addProgressBar()
        {
            _progressBar = new ProgressBar();
            _progressBar.Height = 40;
            _progressBar.Width = 280;
            Canvas.SetTop(_progressBar, 2);
            Canvas.SetRight(_progressBar, 300);
            _tabCanvas.Children.Add(_progressBar);
        }

        public void UpdateProgress(double current, double max)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
				var percentProgress = 100.0 * current / max;
                _progressBar.Value = percentProgress;
            });
        }

        #endregion

        public override string ToString()
		{
			return _tabItemControl != null ? _tabItemControl.Header.ToString() : "Output Tab";
		}

		#endregion

		#region Copy Output

		private void addCopyOutputButton(IOutput output)
		{
			var copyButton = new Button();
			copyButton.Height = 20;
			copyButton.Width = 60;
			copyButton.Content = "Copy";
			copyButton.Click += output.copyOutput;
			Canvas.SetTop(copyButton, 2);
			Canvas.SetRight(copyButton, 200);
			copyButton.HorizontalAlignment = HorizontalAlignment.Center;
			_tabCanvas.Children.Add(copyButton);
		}

		

		//private string clipboardReadyList(ListOutput outputContent)
		//{
		//	var list = new StringBuilder();
		//	foreach (var item in outputContent.Content)
		//	{
		//		list.Append(item + "\n");
		//	}
		//	return list.ToString().TrimEnd();
		//}

		//private string clipboardReadyTable(TableOutput outputContent)
		//{
		//	var table = new StringBuilder();
		//	var tableContent = outputContent.Content;
		//	var tableLength = tableContent.Values.First().Count();
		//	table.Append(tableContent.Keys.ElementAt(0));
		//	for (int i = 1; i < tableContent.Count; i++)
		//	{
		//		table.Append("\t" + tableContent.Keys.ElementAt(i));
		//	}
		//	for (int i = 0; i < tableLength; i++)
		//	{
		//		table.Append("\n");
		//		table.Append(tableContent.ElementAt(0).Value.ElementAt(i));
		//		for (int j = 1; j < tableContent.Count; j++)
		//		{
		//			table.Append("\t" + tableContent.ElementAt(j).Value.ElementAt(i));
		//		}
		//	}
		//	return table.ToString();
		//}

		#endregion

		#region Input Mode

		public void DisplayInputFields(AbstractServiceTab callingService, MethodInfo callingAction)
		{
			_mandatoryControls = new Dictionary<string, Control>();
			_inputControls = new Dictionary<string, Control>();
			int i = 1;
			_executedAction = callingAction;
			_executingTab = callingService;
			foreach (var parameter in callingAction.GetParameters())
			{
				var fieldLabel = createFieldLabel(parameter, i);
				_disposableElements.Add(fieldLabel);
				if (parameter.ParameterType == typeof(bool))
					_inputControls[parameter.Name] = createBoolField(parameter, i);
				else
					_inputControls[parameter.Name] = createInputField(parameter, i);
				if (parameter.IsMandatory())
					_mandatoryControls[parameter.Name] = _inputControls[parameter.Name];
				_disposableElements.Add(_inputControls[parameter.Name]);
				i++;
			}
			addExecuteButton();
		}

		private Button addExecuteButton()
		{
			var executeButton = new Button();
			executeButton.Height = 20;
			executeButton.Width = 100;
			executeButton.Content = "Execute";
			executeButton.Click += executeAction;
			Canvas.SetTop(executeButton, 2);
			Canvas.SetRight(executeButton, 100);
			executeButton.HorizontalAlignment = HorizontalAlignment.Center;
			_tabCanvas.Children.Add(executeButton);
			return executeButton;
		}

		private async void executeAction(object sender, RoutedEventArgs e)
		{
			clearErrorMessages();
			(sender as Button).Content = "Re-Execute";
			var validationErrorMessage = validateInputFields();
			if (!string.IsNullOrEmpty(validationErrorMessage))
			{
				displayValidationErrorMessage(validationErrorMessage);
				return;
			}
			try
			{
				var actionContext = new ActionContext(_executedAction, this, null);
				DisplayExecutingMessage();
				object[] parameters = getInputParameters();
                if (parameters.Any())
                {
                    //actionContext.ExecuteAction(parameters));
					var task = Task.Run(() => _executingTab.InvokeAction(_executedAction, parameters));
                    await task;
                    DisplayOutput(task.Result, _executingTab, _executedAction);
                }
            }
			catch (Exception ex)
			{
				_tabManager.DisplayError(ex, _executingTab, this);
			}
		}

		private void clearErrorMessages()
		{
			if (_errorMessage != null)
				_tabCanvas.Children.Remove(_errorMessage);
		}

		private void displayValidationErrorMessage(string validationErrorMessage)
		{
			_errorMessage = new TextBlock();
			_errorMessage.Text = validationErrorMessage;
			_errorMessage.Foreground = _tabManager.ErrorTextBrush;
			Canvas.SetLeft(_errorMessage, CANVAS_MARGIN);
			Canvas.SetTop(_errorMessage, CANVAS_MARGIN);
			_tabCanvas.Children.Add(_errorMessage);
		}

		public void DisplayExecutingMessage(string executingMessage = "Executing...")
		{
			var _messageText = new TextBlock();
			_messageText.Text = executingMessage;
			Canvas.SetLeft(_messageText, CANVAS_MARGIN);
			Canvas.SetTop(_messageText, CANVAS_MARGIN * 2);
			_tabCanvas.Children.Add(_messageText);
			_disposableElements.Add(_messageText);
		}

		private string validateInputFields()
		{
			var invalidFieldMessage = new StringBuilder();
			foreach (var control in _inputControls)
			{
				var controlName = control.Key;
				var isMandatory = _mandatoryControls.Any(c => c.Key == controlName);
				var inputField = control.Value as TextBox;

				var enteredValue = (inputField != null) ? inputField.Text : "";
				if (!validateField(controlName, enteredValue, isMandatory))
				{
					//TODO: Maybe add text above each invalid field
					invalidFieldMessage.Append("Invalid entry in field: " + AppUtils.CreateDisplayString(controlName) + Environment.NewLine);
				}

			}
			return invalidFieldMessage.ToString();
		}

		private bool validateField(string fieldName, string enteredValue, bool isMandatory)
		{
			if (enteredValue == "" && isMandatory)
				return false;
			if (string.IsNullOrEmpty(enteredValue) && !isMandatory)
				return true;
			var fieldType = _executedAction.GetParameters().First(p => p.Name == fieldName).ParameterType;
			if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
				fieldType = fieldType.GetGenericArguments()[0];
			if (fieldType == typeof(int))
				return Validate.IsInt(enteredValue);
			if (fieldType == typeof(decimal))
				return Validate.IsDecimal(enteredValue);
			if (fieldType == typeof(double))
				return Validate.IsDouble(enteredValue);
			//TODO: Add more options here
			return true;
		}

		private object[] getInputParameters()
		{
			List<object> parameters = new List<object>();
			foreach (var parameter in _executedAction.GetParameters())
			{
				var inputField = _inputControls[parameter.Name];
				var fieldValue = getFieldValue(parameter, inputField);
				parameters.Add(fieldValue);
			}
			return parameters.ToArray();
		}

		private object getFieldValue(ParameterInfo parameter, Control inputControl)
		{
			var inputField = inputControl as TextBox;
			if (inputField != null)
			{
				var fieldType = parameter.ParameterType.IsGenericType ?
					parameter.ParameterType.GetGenericArguments()[0] :
					parameter.ParameterType;
				if (string.IsNullOrWhiteSpace(inputField.Text))
					return null;
				return Convert.ChangeType(inputField.Text, fieldType);
			}
			var checkBox = inputControl as CheckBox;
			if (checkBox != null)
				return checkBox.IsChecked;
			return null;
		}

		private TextBlock createFieldLabel(ParameterInfo parameter, int fieldId)
		{
			var label = new TextBlock();
			var labelText = AppUtils.CreateDisplayString(parameter.Name, 25);
			if (parameter.IsMandatory())
				label.FontWeight = FontWeights.Bold;
			label.Text = labelText + ":";
			label.Width = FIELD_LABEL_WIDTH;
			Canvas.SetLeft(label, CANVAS_MARGIN);
			var top = CANVAS_MARGIN + (fieldId * FIELD_HEIGHT) + 2;
			Canvas.SetTop(label, top);
			_tabCanvas.Children.Add(label);
			return label;
		}

		private TextBox createInputField(ParameterInfo parameter, int fieldId)
		{
			var inputBox = new TextBox();
			inputBox.AcceptsReturn = false;
			inputBox.AcceptsTab = false;
			inputBox.Width = FIELD_WIDTH;
			inputBox.Name = parameter.Name;
			Canvas.SetLeft(inputBox, CANVAS_MARGIN + FIELD_LABEL_WIDTH);
			var top = CANVAS_MARGIN + (fieldId * FIELD_HEIGHT);
			Canvas.SetTop(inputBox, top);
			_tabCanvas.Children.Add(inputBox);
			return inputBox;
		}

		private CheckBox createBoolField(ParameterInfo parameter, int fieldId)
		{
			var checkBox = new CheckBox();
			checkBox.Name = parameter.Name;
			Canvas.SetLeft(checkBox, CANVAS_MARGIN + FIELD_LABEL_WIDTH);
			var top = CANVAS_MARGIN + (fieldId * FIELD_HEIGHT);
			Canvas.SetTop(checkBox, top);
			_tabCanvas.Children.Add(checkBox);
			return checkBox;
		}

		private void addStringHelperControls(int fieldId)
		{
			//TODO: Have a think about how this will work
			//addRegexToggleButton(fieldId);
			//addCaseSensitiveToggleButton(fieldId);
		}

		public void ClearInputElements()
		{
			foreach (var element in _disposableElements)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					_tabCanvas.Children.Remove(element);
				});
			}
		}

        #endregion Input Mode

        #region Output Processing

        public void DisplayOutput(object objectToDisplay, IOutputSource source, MethodInfo actionMethod)
        {
            var callingAction = ExecutedAction ?? actionMethod;
            ClearInputElements();
            DisplayOutput(source, callingAction.Name, objectToDisplay ?? "No Output To Display");
            Focus();
        }

        public void DisplayOutput(object objectToDisplay, IOutputSource source, string actionName)
        {
            ClearInputElements();
			Application.Current.Dispatcher?.Invoke(() =>
			{
				DisplayOutput(source, actionName, objectToDisplay ?? "No Output To Display");
				Focus();
			});
        }

        public void DisplayError(Exception exception, IOutputSource source)
        {
            TabTitle = "Error";
            var outputString = new StringBuilder();
            outputString.AppendLine("Error calling action. Unwinding Stacktrace:");
            outputString.AppendLine("-------------------------------------------");
            outputString.AppendLine(exception.Message);
            var indent = string.Empty;
            while (exception.InnerException != null)
            {
                indent += " ";
                outputString.AppendLine(indent + ">" + exception.InnerException.Message);
                exception = exception.InnerException;
            }
            DisplayOutput(source, "Error", outputString.ToString());
            Focus();
        }

        #endregion

        public void Dispose()
		{
		}

		public void SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (_outputControl != null)
				_outputControl.MaxHeight = e.NewSize.Height - 100;
		}

		
	}
}
