Imports System.ComponentModel
Imports System.IO
Imports System.Text.RegularExpressions

Public Class FileItem : Implements INotifyPropertyChanged
    Public Property FileName As String
    Public Property LastModified As Date

    Private selected As Boolean

    Public Property isChecked As Boolean
        Set(value As Boolean)
            selected = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("item"))
        End Set
        Get
            Return selected
        End Get
    End Property

    Public ReadOnly Property DisplayName As String
        Get
            Return Path.GetFileName(FileName)
        End Get
    End Property

    Public ReadOnly Property Extension As String
        Get
            Return Path.GetExtension(FileName).Replace(".", "")
        End Get
    End Property

    Public ReadOnly Property DocType As String
        Get
            Dim strfilename As String = FileName.Replace("-", "_")
            Dim match = Regex.Match(strfilename, "_\d{3}_")
            If match.Success Then
                Return match.Groups(0).Value.Replace("_", "")
            Else
                Return ""
            End If
        End Get
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub New(fileName As String, lastModified As Date)
        Me.FileName = fileName
        Me.LastModified = lastModified
    End Sub
End Class
