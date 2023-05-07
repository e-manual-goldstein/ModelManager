
Namespace AssemblyAnalyser.VBTestData.Basics
    Public Class BasicVBSubClass
        Inherits BasicVBClass

        
        Public Overrides Property AlternateNamedProperty As String
            Get
                Return ""
            End Get
            Set(value As String)
                
            End Set
        End Property

        Public Overrides Property OverridableProperty As String
            Get
                Return ""
            End Get
            Set(value As String)
                
            End Set
        End Property

    End Class

End Namespace