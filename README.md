# ModelManager

This application was created to support small to medium exploratory, prototyping, and analysis projects/exercises. The application primarily provides the developer with a means to quickly execute experimental code and display the output in one of three formats; a single string, a list of strings, or a table (made up of multiple lists of strings).

In order to execute some experimental code, the developer simply needs to create a new class which inherits from AbstractServiceTab. Any and all public methods added to this class will be surfaced on the UI as buttons.

The format of the output is determined by the return type of the method:
  --
void: no output
  --
string: output is a simple string
  --
List<string>: output is a simple string comprising a concatenation of the strings in the list, delimited by line breaks (\n)
  --
Dictionary<string, IEnumerable<string>>: output is a table of 'n' columns, where n is the number of entries in the Dictionary. The row count in the table is determined by the number of entries in the IEnumerable. The column names are determined by the string keys in the Dictionary.
