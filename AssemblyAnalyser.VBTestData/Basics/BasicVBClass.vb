Namespace AssemblyAnalyser.VBTestData.Basics

    Public Class BasicVBClass 
    
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
    
    End Class
End Namespace