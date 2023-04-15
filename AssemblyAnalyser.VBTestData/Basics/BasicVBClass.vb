
Namespace AssemblyAnalyser.VBTestData.Basics

    Public Class BasicVBClass 
        Implements IBasicVBInterface
    
        Default Public Overloads Property PropertyWithParameters(ByVal index As Integer) As String
            Get
                Return Nothing
            End Get
            Set(ByVal Value As String)

            End Set
        End Property

        Default Public Overloads ReadOnly Property PropertyWithParameters(ByVal name As String) As String 
            Get
                Return Nothing
            End Get
        End Property

        Public Property AlternateNamedProperty As String Implements IBasicVBInterface.BasicProperty
            Get
                Return ""
            End Get
            Set(value As String)
                
            End Set
        End Property

        Public Function BasicFunction(str As String) As String
            Return 0
        End Function

        Public Function AlternateNamedFunction() As Integer Implements IBasicVBInterface.BasicFunction
            Return 0
        End Function
    End Class

End Namespace