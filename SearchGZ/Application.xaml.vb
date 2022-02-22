Imports System.IO
Class Application
    Private Sub Application_Exit(sender As Object, e As ExitEventArgs) Handles Me.[Exit]
        Dim tmpFolder As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp")

        tmpFolder = Path.Combine(tmpFolder, Date.Now.ToString("yyyyMMdd"))
        If Directory.Exists(tmpFolder) = True Then
            Directory.Delete(tmpFolder, True)

        End If
    End Sub

    ' 应用程序级事件(例如 Startup、Exit 和 DispatcherUnhandledException)
    ' 可以在此文件中进行处理。

End Class
