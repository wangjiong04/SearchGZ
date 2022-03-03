
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports ICSharpCode.SharpZipLib.GZip
Imports ICSharpCode.SharpZipLib.Tar

Class MainWindow
    Private Property allItems As New ArrayList
    Private Property gzFiles As New ArrayList
    Private Property tarFiles As New ArrayList
    Private destPath As String
    Private _lastHeaderClicked As GridViewColumnHeader = Nothing
    Private _lastDirection As ListSortDirection = ListSortDirection.Ascending
    Private needUnzip As Boolean = True
    Private Sub btnSelect_Click(sender As Object, e As RoutedEventArgs) Handles btnSelect.Click
        Dim dlg As FolderBrowserDialog = New FolderBrowserDialog
        If dlg.ShowDialog() = Forms.DialogResult.OK Then
            txtFile.Text = dlg.SelectedPath
            Unzip(txtFile.Text)
        End If

    End Sub

    Private Sub Unzip(sourceFolder As String)
        Mouse.OverrideCursor = Input.Cursors.Wait
        destPath = initFolder()
        GetAllFile(sourceFolder, destPath)
        Mouse.OverrideCursor = Nothing
        needUnzip = False
    End Sub

    Private Function initFolder() As String
        Dim tmpFolder As String = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "tmp")
        allItems = New ArrayList()
        gzFiles = New ArrayList()
        tarFiles = New ArrayList()
        If Directory.Exists(tmpFolder) = False Then
            Directory.CreateDirectory(tmpFolder)
        End If
        tmpFolder = Path.Combine(tmpFolder, Date.Now.ToString("yyyyMMdd"))
        If Directory.Exists(tmpFolder) = True Then
            Directory.Delete(tmpFolder, True)
        End If
        Directory.CreateDirectory(tmpFolder)
        Return tmpFolder
    End Function
    Private Sub extractGZFile(gzFile As String)
        'Mouse.OverrideCursor = Input.Cursors.Wait
        If gzFiles.Contains(Path.GetFileName(gzFile)) = True Then
            Exit Sub
        End If
        gzFiles.Add(Path.GetFileName(gzFile))
        Dim inStream As Stream = File.OpenRead(gzFile)
        Dim gzipStream As Stream = New GZipInputStream(inStream)
        Dim tarArchive As TarArchive = TarArchive.CreateInputTarArchive(gzipStream)
        tarArchive.ExtractContents(destPath)
        tarArchive.Close()
        gzipStream.Close()
        inStream.Close()

        Dim files As String() = Directory.GetFiles(destPath, "*.tar", SearchOption.AllDirectories)
        If files.Count > 0 Then
            ExtractTar(files(0), destPath)
            File.Delete(files(0))
        End If
        'Mouse.OverrideCursor = Nothing
    End Sub

    Private Sub GetAllFile(ByVal folder As String, destFolder As String)
        Dim strDir As String() = System.IO.Directory.GetDirectories(folder)
        Dim strFile As String() = System.IO.Directory.GetFiles(folder)
        Dim i As Integer
        If strDir.Length > 0 Then
            For i = 0 To strDir.Length - 1
                Debug.Print(strDir(i))
            Next
        End If

        If strFile.Length > 0 Then
            For i = 0 To strFile.Length - 1
                If Path.GetExtension(strFile(i)) = ".gz" Then
                    extractGZFile(strFile(i))
                ElseIf Path.GetExtension(strFile(i)) = ".tar" Then
                    ExtractTar(strFile(i), destFolder)
                Else
                    File.Copy(strFile(i), Path.Combine(destFolder, Path.GetFileName(strFile(i))), True)

                    Dim fileItem As FileItem = New FileItem(Path.Combine(destFolder, Path.GetFileName(strFile(i))), File.GetLastWriteTime(Path.Combine(destFolder, Path.GetFileName(strFile(i)))))
                    fileItem.isChecked = False
                    allItems.Add(fileItem)
                End If
            Next
        End If
        If strDir.Length > 0 Then
            For i = 0 To strDir.Length - 1
                GetAllFile(strDir(i), destFolder)
            Next
        End If
    End Sub

    Private Sub btnSearch_Click(sender As Object, e As RoutedEventArgs) Handles btnSearch.Click
        If needUnzip And Directory.Exists(txtFile.Text) Then
            Unzip(txtFile.Text)
        End If
        checkBox.IsChecked = False
        SearchItems()
    End Sub

    Public Sub ExtractTar(tarFileName As String, destFolder As String)
        If tarFiles.Contains(Path.GetFileName(tarFileName)) = True Then
            Exit Sub
        End If

        Dim dest As String = Path.Combine(destFolder, Path.GetFileNameWithoutExtension(tarFileName))
        Directory.CreateDirectory(dest)
        Using fsIn As New FileStream(tarFileName, FileMode.Open, FileAccess.Read)

            ' The TarInputStream reads a UNIX tar archive as an InputStream.
            '
            Dim tarIn As New TarInputStream(fsIn)

            Dim tarEntry As TarEntry

            While (InlineAssignHelper(tarEntry, tarIn.GetNextEntry())) IsNot Nothing

                If tarEntry.IsDirectory Then
                    Continue While
                End If
                ' Converts the unix forward slashes in the filenames to windows backslashes
                '
                Dim name As String = tarEntry.Name.Replace("/"c, Path.DirectorySeparatorChar).Replace(":", "")

                ' Apply further name transformations here as necessary
                Dim outName As String = Path.Combine(dest, name)

                Dim directoryName As String = Path.GetDirectoryName(outName)
                Directory.CreateDirectory(directoryName)

                Dim outStr As New FileStream(outName, FileMode.Create)
                CopyWithAsciiTranslate(tarIn, outStr)
                outStr.Close()
                ' Set the modification date/time. This approach seems to solve timezone issues.
                Dim myDt As DateTime = DateTime.SpecifyKind(tarEntry.ModTime, DateTimeKind.Utc)
                File.SetLastWriteTime(outName, myDt)

                Dim fileItem As FileItem = New FileItem(outName, myDt)
                fileItem.isChecked = False
                allItems.Add(fileItem)
            End While
            tarIn.Close()
        End Using

        'Dim inStream As Stream = File.OpenRead(tarFileName)

        'Dim tarArchive As TarArchive = TarArchive.CreateInputTarArchive(inStream)
        'tarArchive.ExtractContents(dest)

        'inStream.Seek(0, SeekOrigin.Begin)
        'Dim tarInputStream As TarInputStream = New TarInputStream(inStream)
        'Dim entry As TarEntry = tarInputStream.GetNextEntry()
        'While Not entry Is Nothing
        '    If Not entry.IsDirectory Then
        '        Dim filename As String = entry.Name
        '        Dim modDate As Date = entry.ModTime
        '        Dim fileItem As FileItem = New FileItem(filename, modDate)
        '        fileItem.isChecked = False
        '        allItems.Add(fileItem)
        '    End If
        '    entry = tarInputStream.GetNextEntry()
        'End While
        'tarArchive.Close()
        'tarInputStream.Close()
        'inStream.Close()
        tarFiles.Add(Path.GetFileName(tarFileName))
    End Sub

    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function

    Private Sub CopyWithAsciiTranslate(tarIn As TarInputStream, outStream As Stream)
        Dim buffer As Byte() = New Byte(4095) {}
        Dim isAscii As Boolean = True
        Dim cr As Boolean = False

        Dim numRead As Integer = tarIn.Read(buffer, 0, buffer.Length)
        Dim maxCheck As Integer = Math.Min(200, numRead)
        For i As Integer = 0 To maxCheck - 1
            Dim b As Byte = buffer(i)
            If b < 8 OrElse (b > 13 AndAlso b < 32) OrElse b = 255 Then
                isAscii = False
                Exit For
            End If
        Next
        While numRead > 0
            If isAscii Then
                ' Convert LF without CR to CRLF. Handle CRLF split over buffers.
                For i As Integer = 0 To numRead - 1
                    Dim b As Byte = buffer(i)   ' assuming plain Ascii and not UTF-16
                    If b = 10 AndAlso Not cr Then   ' LF without CR
                        outStream.WriteByte(13)
                    End If
                    cr = (b = 13)

                    outStream.WriteByte(b)
                Next
            Else
                outStream.Write(buffer, 0, numRead)
            End If
            numRead = tarIn.Read(buffer, 0, buffer.Length)
        End While
    End Sub

    Private Sub txtDay_PreviewTextInput(sender As Object, e As TextCompositionEventArgs) Handles txtDay.PreviewTextInput
        Dim re As Regex = New Regex("[^0-9.-]+")
        e.Handled = re.IsMatch(e.Text)
    End Sub

    Private Sub checkBox_Unchecked(sender As Object, e As RoutedEventArgs) Handles checkBox.Unchecked
        Dim items As ObservableCollection(Of FileItem) = TryCast(lstFiles.ItemsSource, ObservableCollection(Of FileItem))
        For Each item As FileItem In items
            item.isChecked = False
        Next
        Me.lstFiles.Items.Refresh()
    End Sub

    Private Sub checkBox_Checked(sender As Object, e As RoutedEventArgs) Handles checkBox.Checked
        Dim items As ObservableCollection(Of FileItem) = TryCast(lstFiles.ItemsSource, ObservableCollection(Of FileItem))
        For Each item As FileItem In items
            item.isChecked = True
        Next
        Me.lstFiles.Items.Refresh()
    End Sub

    Private Sub SearchItems()
        Dim list As New ObservableCollection(Of FileItem)

        For Each item As FileItem In allItems
            Dim fileExtention As String = item.Extension.ToLower
            Dim isAllMatch As Boolean = True
            Dim strExtention As String = ""
            If chkEDI.IsChecked Then
                strExtention = strExtention + ".edi"
            End If
            If chkJson.IsChecked Then
                strExtention = strExtention + ".json"
            End If
            If chkXML.IsChecked Then
                strExtention = strExtention + ".xml"
            End If
            If chkDat.IsChecked Then
                strExtention = strExtention + ".dat"
            End If
            If chkAny.IsChecked And txtAny.Text.Trim <> "" Then
                strExtention = strExtention + txtAny.Text
            End If
            If chkempty.IsChecked = True Then
                strExtention = strExtention + ".empty"
                If item.Extension = "" Then
                    fileExtention = ".empty"
                End If
            End If
            If strExtention <> "" Then
                If strExtention.IndexOf(fileExtention) < 0 Then
                    isAllMatch = False
                End If
            End If
            If rdoRange.IsChecked Then
                If item.LastModified.CompareTo(dtFrom.SelectedDate) < 0 Or item.LastModified.CompareTo(dtTo.SelectedDate) > 0 Then
                    isAllMatch = False
                End If
            End If
            If rdoWithin.IsChecked And txtDay.Text.Trim <> "" Then
                If item.LastModified.CompareTo(Date.Now.Subtract(New TimeSpan(txtDay.Text, 0, 0, 0))) < 0 Then
                    isAllMatch = False
                End If
            End If
            If txtDocType.Text.Trim <> "" Then
                Dim strDocTypes As String() = txtDocType.Text.Replace(" ", ",").Replace(";", ",").Split(",")
                If item.DocType <> "" Then
                    If strDocTypes.Contains(item.DocType) = False Then
                        isAllMatch = False
                    End If
                Else
                    isAllMatch = False
                End If
            End If
            If txtTPName.Text.Trim <> "" Then
                Dim strNames As String() = txtTPName.Text.Replace(" ", ",").Replace(";", ",").ToLower.Split(",")
                Dim strfilename As String = Path.GetFileName(item.FileName).Replace("-", "_").ToLower
                Dim strName As String = strfilename.Split("_")(0)
                If strNames.Contains(strName) = False Then
                    isAllMatch = False
                End If
            End If
            If txtSubstring.Text.Trim <> "" Then
                Dim strfilename As String = Path.GetFileName(item.FileName).Replace("-", "_").ToLower
                If strfilename.IndexOf(txtSubstring.Text.Trim.ToLower) < 0 Then
                    isAllMatch = False
                End If
            End If
                If isAllMatch Then
                list.Add(item)
            End If
        Next
        If txtContent.Text.Trim <> "" Then
            For Each item In list.ToList
                If findInFile(item.FileName, txtContent.Text) = False Then
                    list.Remove(item)
                End If
            Next
        End If
        lblCount.Content = "File count: " + list.Count.ToString
        Me.lstFiles.ItemsSource = list
    End Sub

    Private Sub MainWindow_Initialized(sender As Object, e As EventArgs) Handles Me.Initialized
        dtFrom.DisplayDate = Date.Now.Subtract(New TimeSpan(7, 0, 0, 0))
        dtFrom.SelectedDate = Date.Now.Subtract(New TimeSpan(7, 0, 0, 0))
        dtTo.DisplayDate = Date.Now
        dtTo.SelectedDate = Date.Now
    End Sub

    Private Sub btnCopyTo_Click(sender As Object, e As RoutedEventArgs) Handles btnCopyTo.Click
        Dim dlg As FolderBrowserDialog = New FolderBrowserDialog
        If dlg.ShowDialog() = Forms.DialogResult.OK Then
            copyFile(dlg.SelectedPath)
        End If
    End Sub

    Private Sub GridViewColumnHeaderClickedHandler(ByVal sender As Object, ByVal e As RoutedEventArgs)
        Dim headerClicked = TryCast(e.OriginalSource, GridViewColumnHeader)
        Dim direction As ListSortDirection

        If headerClicked IsNot Nothing Then
            If headerClicked.Role <> GridViewColumnHeaderRole.Padding Then
                If headerClicked IsNot _lastHeaderClicked Then
                    direction = ListSortDirection.Ascending
                Else
                    If _lastDirection = ListSortDirection.Ascending Then
                        direction = ListSortDirection.Descending
                    Else
                        direction = ListSortDirection.Ascending
                    End If
                End If

                Dim columnBinding = TryCast(headerClicked.Column.DisplayMemberBinding, System.Windows.Data.Binding)
                Dim sortBy = If(columnBinding?.Path.Path, TryCast(headerClicked.Column.Header, String))
                If sortBy Is Nothing Then Exit Sub
                Sort(sortBy, direction)

                If direction = ListSortDirection.Ascending Then
                    headerClicked.Column.HeaderTemplate = TryCast(Resources("HeaderTemplateArrowUp"), DataTemplate)
                Else
                    headerClicked.Column.HeaderTemplate = TryCast(Resources("HeaderTemplateArrowDown"), DataTemplate)
                End If

                ' Remove arrow from previously sorted header
                If _lastHeaderClicked IsNot Nothing AndAlso _lastHeaderClicked IsNot headerClicked Then
                    _lastHeaderClicked.Column.HeaderTemplate = Nothing
                End If

                _lastHeaderClicked = headerClicked
                _lastDirection = direction
            End If
        End If
    End Sub

    Private Sub Sort(ByVal sortBy As String, ByVal direction As ListSortDirection)
        Dim dataView As ICollectionView = CollectionViewSource.GetDefaultView(lstFiles.ItemsSource)
        If dataView Is Nothing Then
            Exit Sub
        End If
        dataView.SortDescriptions.Clear()
        Dim sd As New SortDescription(sortBy, direction)
        dataView.SortDescriptions.Add(sd)
        dataView.Refresh()
    End Sub

    Private Sub btnCopyToTest_Click(sender As Object, e As RoutedEventArgs) Handles btnCopyToTest.Click
        Dim testFolder As String = Path.Combine(txtFile.Text, "test")
        copyFile(testFolder)
    End Sub

    Private Sub copyFile(filePath As String)
        If Directory.Exists(filePath) = False Then
            Directory.CreateDirectory(filePath)
        End If
        Dim items As ObservableCollection(Of FileItem) = TryCast(lstFiles.ItemsSource, ObservableCollection(Of FileItem))
        For Each item As FileItem In items
            If item.isChecked Then
                Dim sourcefile As String = Path.Combine(destPath, item.FileName)
                File.Copy(sourcefile, Path.Combine(filePath, Path.GetFileName(item.FileName)), True)
            End If
        Next
    End Sub

    Private Sub txtFile_TextChanged(sender As Object, e As TextChangedEventArgs) Handles txtFile.TextChanged
        needUnzip = True
    End Sub

    Public Function findInFile(ByVal filename As String, ByVal strContent As String) As Boolean
        Dim strInput = IO.File.ReadAllText(filename)
        Dim iResult As Int32
        iResult = InStr(1, strInput, strContent)
        If iResult = 0 Then
            Return False
        Else
            Return True
        End If
    End Function
    Private Sub HandleDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles lstFiles.MouseDoubleClick
        If TypeOf (sender) IsNot Controls.ListViewItem Then
            Exit Sub
        End If
        Dim listItem As Controls.ListViewItem = CType(sender, Controls.ListViewItem)
        Dim item As FileItem = CType(listItem.Content, FileItem)
        Process.Start("notepad.exe", item.FileName)
    End Sub
End Class
