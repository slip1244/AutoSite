﻿Imports System.IO
Imports System.Text.RegularExpressions
Imports FastColoredTextBoxNS
Imports System.Text

Public Class Editor

    Public template_cache As New ArrayList

    Public openFile As String
    Public siteRoot As String
    Public Snapshot As String

    'https://stackoverflow.com/a/3448307
    Public Function ReadAllText(ByVal path As String)
        Dim inStream = New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        Dim streamReader = New StreamReader(inStream)
        Dim text As String = streamReader.ReadToEnd()
        streamReader.Dispose()
        inStream.Dispose()
        Return text
    End Function

    Sub WriteAllText(ByVal path As String)
        Try
            Code.SaveToFile(path, Main.encodingType)
        Catch
            Try
                Dim unlocker = New FileStream(path, FileMode.Open)
                unlocker.Unlock(1, unlocker.Length)
                unlocker.Close()
                Code.SaveToFile(path, Main.encodingType)
            Catch ex As Exception
                MsgBox("The file could not be saved.", MsgBoxStyle.Critical, "AutoSite")
            End Try
        End Try
    End Sub

    Public Sub doUndo() Handles UndoBtn.Click, Undo.Click
        Code.Undo()
    End Sub

    Public Sub doRedo() Handles RedoBtn.Click, Redo.Click
        Code.Redo()
    End Sub

    Private Sub Code_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Code.Load
        Code.WordWrap = My.Settings.WordWrap
        Code.VirtualSpace = My.Settings.VirtualSpace
        Code.WideCaret = My.Settings.WideCaret
        Code.Font = My.Settings.editorFont
        If Application.VisualStyleState = VisualStyles.VisualStyleState.NoneEnabled Then
            Me.BackColor = SystemColors.Control
            Strip.BackColor = SystemColors.Control
        End If
    End Sub

    Private Sub Code_MouseClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Code.MouseClick
        If e.Button = Windows.Forms.MouseButtons.Right Then
            Cut.Enabled = Code.SelectionLength > 0
            Copy.Enabled = Code.SelectionLength > 0
            Paste.Enabled = My.Computer.Clipboard.ContainsText
            Context.Show(Code, e.Location)
        End If
    End Sub

    Private Sub Editor_TextChanged(ByVal sender As System.Object, ByVal e As FastColoredTextBoxNS.TextChangedEventArgs) Handles Code.TextChanged
        If (Not Code.Text = Snapshot) And (Not openFile Is Nothing) Then
            Me.Parent.Text = openFile.Replace(siteRoot & "\", "") & "*"
            SaveBtn.Enabled = True
        End If
        'If My.Settings.SyntaxHighlight Then
        '    Dim GreenStyle As New TextStyle(Brushes.Green, Nothing, FontStyle.Regular)
        '    Dim TurqStyle As New TextStyle(Brushes.Turquoise, Nothing, FontStyle.Regular)
        '    'clear style of changed range
        '    'Code.Range.ClearStyle(TurqStyle)
        '    'Code.Range.ClearStyle(GreenStyle)
        '    'comment highlighting
        '    Code.Range.SetStyle(TurqStyle, "\[(.*?)=(.*?)\](.*?)\[\/\1(.{1,2})\]", RegexOptions.Singleline)
        '    Code.Range.SetStyle(GreenStyle, "\[#.*?#\]", RegexOptions.Singleline)
        '    'for atteql, value, text in re.findall(r'\[(.*)=(.*?)\](.*)\[\/\1.*\]', template):
        'End If
        Try
            If My.Settings.LivePreview Then
                If Me.Parent.Text.Replace("*", "").EndsWith(".md") Then
                    Main.Preview.DocumentText = CommonMark.CommonMarkConverter.Convert(Code.Text)
                Else
                    Main.Preview.DocumentText = Code.Text
                End If
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub Save() Handles SaveBtn.ButtonClick, SaveToolStripMenuItem.Click
        If Not openFile Is Nothing Then
            Dim refreshTree = False
            If My.Computer.Info.OSPlatform = "Win32Windows" Then   ' Detect non-NT Windows (98)
                If Not My.Computer.FileSystem.FileExists(openFile) Then
                    refreshTree = True
                End If
            End If
            WriteAllText(openFile)
            If refreshTree Then
                Main.refreshTree(Main.SiteTree.Nodes(0))
            End If
            Snapshot = Code.Text
            Me.Parent.Text = openFile.Replace(siteRoot & "\", "")
            SaveBtn.Enabled = False
        End If
    End Sub

    Public Sub Close() Handles CloseBtn.Click
        If Not Code.Text = Snapshot Then
            Dim d As DialogResult = MsgBox("Save changes to " & openFile.Replace(siteRoot & "\", "") & "?", MsgBoxStyle.Exclamation + MsgBoxStyle.YesNoCancel, "AutoSite")
            If d = DialogResult.Yes Then
                Save()
            End If
            If d = DialogResult.Cancel Then
                Exit Sub
            End If
        End If
        Main.openFiles.Remove(openFile)
        Me.Parent.Dispose()
    End Sub

    Public Sub doCut() Handles Cut.Click, CutBtn.Click
        Code.Cut()
    End Sub

    Public Sub doCopy() Handles Copy.Click, CopyBtn.Click
        Code.Copy()
    End Sub

    Public Sub doPaste() Handles Paste.Click, PasteBtn.Click
        Code.Paste()
    End Sub

    Public Sub doSelectAll() Handles SelectAll.Click
        Code.SelectAll()
    End Sub

    Private Sub SaveAllToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SaveAllToolStripMenuItem.Click
        Main.DoSaveAll()
    End Sub

    Public Sub doFind() Handles Find.Click
        Code.ShowFindDialog()
    End Sub

    Public Sub doGoto() Handles GTo.Click
        Code.ShowGoToDialog()
    End Sub

    Public Sub doReplace() Handles Replace.Click
        Code.ShowReplaceDialog()
    End Sub

    Public Sub doPreview() Handles Preview.ButtonClick
        Dim rel = openFile.Replace(siteRoot & "\pages\", "").Replace(siteRoot & "\includes\", "").Replace(siteRoot & "\templates\", "")

        If Not Main.PreviewPanel.Checked Then
            Main.PreviewPanel.Checked = True
            Main.panelUpdate()
        End If

        If Not Me.Parent.Text.StartsWith("pages\") Then
            Main.Preview.Navigate(Path.Combine(Main.SiteTree.Nodes(0).Text, "out\"))
            Main.Preview.DocumentText = Code.Text
        Else
            template_cache.Clear()
            Main.Preview.Navigate(siteRoot & "out\" & rel)
            Main.Preview.DocumentText = Apricot.Compile(Code.Text, rel, siteRoot, True, Now, Nothing).HTML
        End If
    End Sub

    Private Sub Preview_DropDownOpening(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Preview.DropDownOpening
        LivePreview.Checked = My.Settings.LivePreview
    End Sub

    Private Sub LivePreview_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LivePreview.CheckedChanged
        My.Settings.LivePreview = LivePreview.Checked
        Main.LivePreview.Checked = My.Settings.LivePreview
        Main.panelUpdate()
    End Sub

    ' https://www.codeproject.com/articles/8995/introduction-to-treeview-drag-and-drop-vb-net
    Public Sub Code_DragEnter(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DragEventArgs) Handles Code.DragEnter
        e.Effect = DragDropEffects.Link
    End Sub

    Private Sub Code_DragOver(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DragEventArgs) Handles Code.DragOver
        e.Effect = DragDropEffects.Link
    End Sub

    Public Sub doInsertConditional() Handles InsertConditional.Click
        Dim conditionals = New AddConditional
        If conditionals.ShowDialog() = DialogResult.OK Then
            Code.InsertText(conditionals.output)
        End If
    End Sub

    Public Sub doViewOutput() Handles ViewOutput.Click
        Dim rel = openFile.Replace(siteRoot & "\pages\", "").Replace(siteRoot & "\includes\", "").Replace(siteRoot & "\templates\", "")

        If Not Main.PreviewPanel.Checked Then
            Main.PreviewPanel.Checked = True
            Main.panelUpdate()
        End If

        If rel.EndsWith(".md") Then
            rel = Apricot.ReplaceLast(rel, ".md", ".html")
        End If

        If My.Computer.FileSystem.FileExists(siteRoot & "\out\" & rel) Then
            Main.Preview.Navigate(siteRoot & "\out\" & rel)
        End If
    End Sub

    Private Sub Build_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Build.Click
        Main.doBuild()
    End Sub

    Private Sub ViewinDefaultBrowser_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ViewinDefaultBrowser.Click
        Dim rel = openFile.Replace(siteRoot & "\pages\", "").Replace(siteRoot & "\includes\", "").Replace(siteRoot & "\templates\", "")
        If rel.EndsWith(".md") Then
            rel = Apricot.ReplaceLast(rel, ".md", ".html")
        End If
        Process.Start(siteRoot & "\out\" & rel)
    End Sub

    Private Sub Autocomplete_Selecting(ByVal sender As System.Object, ByVal e As AutocompleteMenuNS.SelectingEventArgs) Handles Autocomplete.Selecting
        If Autocomplete.Items.Length = 1 Then  ' AutoCompleteMenu automatically selects the one option instead of displaying the menu
            e.Cancel = True                    ' Fine in other cases but doesn't work so great here, unfortunately
            Exit Sub
        End If
        If e.Item.ImageIndex = 3 Then      ' Build option
            e.Cancel = True
            Autocomplete.Close()
            Main.doBuild()
        ElseIf e.Item.ImageIndex = 1 Or e.Item.ImageIndex = 5 Then  ' Define attribute
            e.Cancel = True

            If Code.GetLineText(Code.LineNumberStartValue - 1) = "" Then
                Code.InsertText(e.Item.Text)
            Else
                Code.Selection.Start = New Place(0, 0)
                Code.Selection.End = New Place(0, 0)
                Code.ProcessKey(Keys.End)
                Code.InsertText(Environment.NewLine & e.Item.Text)
                If e.Item.Text.Contains("...") Then
                    Code.Selection.Start = New Place(e.Item.Text.IndexOf("..."), Code.Selection.FromLine)
                    Code.Selection.End = New Place(e.Item.Text.IndexOf("...") + 3, Code.Selection.FromLine)
                End If
            End If
            Autocomplete.Close()
        ElseIf e.Item.ImageIndex = 4 Then  ' Insert conditional
            e.Cancel = True
            Autocomplete.Close()
            doInsertConditional()
        End If
    End Sub

    Private Sub prepMenu()
        Dim items As New List(Of AutocompleteMenuNS.AutocompleteItem)

        Dim internal As New List(Of String)
        internal.Add("root")
        internal.Add("path")
        internal.Add("modified")
        internal.Add("template")

        If (Not Me.Parent.Text.StartsWith("includes\")) And Not Code.GetLineText(Code.Selection.FromLine).StartsWith("<!-- attrib") Then
            ' Internal
            If Me.Parent.Text.StartsWith("templates\") And Not Code.Text.Contains("[#content#]") Then
                items.Add(New AutocompleteMenuNS.AutocompleteItem("[#content#]", 2, "content", "Content", "Outputs the page's content." & Environment.NewLine & Environment.NewLine & "Use once in templates."))
            End If
            items.Add(New AutocompleteMenuNS.AutocompleteItem("[#root#]", 2, "root", "Relative path to root", "Outputs the relative path from the page to the site root." & Environment.NewLine & "Use this to begin paths to stylesheets, images, and other" & Environment.NewLine & "pages." & Environment.NewLine & Environment.NewLine & rootCalc()))
            'items.Add(New AutocompleteMenuNS.AutocompleteItem("[#template#]", 2, "[#template#]", "Reference template", "Outputs the page's used template." & Environment.NewLine & Environment.NewLine & "Example: default"))
            items.Add(New AutocompleteMenuNS.AutocompleteItem("[#modified#]", 2, "modified", "Last modified date", "Outputs the date the page was last modified on." & Environment.NewLine & Environment.NewLine & "Example: " & Date.Now.ToString.Split(" ")(0)))
            items.Add(New AutocompleteMenuNS.AutocompleteItem("[#path#]", 2, "path", "Path", "Outputs the page's relative path from root." & Environment.NewLine & Environment.NewLine & pathCalc()))
            For Each Attribute As TreeNode In Main.AttributeTree.Nodes
                If Not internal.Contains(Attribute.Text) Then
                    If Code.Text.Contains("<!-- attrib " & Attribute.Text & ":") Or Me.Parent.Text.StartsWith("templates\") Then
                        items.Add(New AutocompleteMenuNS.AutocompleteItem("[#" & Attribute.Text & "#]", 0, Attribute.Text, Attribute.Text, "Outputs the page's value for the " & Attribute.Text & " attribute."))
                    End If
                End If
                'items.Add(New AutocompleteMenuNS.AutocompleteItem(Attribute.Text))
            Next
        End If

        ' Internal define option
        If Me.Parent.Text.StartsWith("pages\") Then
            If Not Code.Text.Contains("<!-- attrib template:") Then
                items.Insert(0, New AutocompleteMenuNS.AutocompleteItem("<!-- attrib template: default -->", 1, "Define template", "Define template", "Defines the template used by the current page." & Environment.NewLine & Environment.NewLine & "Default is default, which tells AutoSite to use default.html in the templates folder."))
            End If

            For Each Attribute As TreeNode In Main.AttributeTree.Nodes
                If Not internal.Contains(Attribute.Text) Then
                    If Not Code.Text.Contains("<!-- attrib " & Attribute.Text & ":") Then
                        items.Add(New AutocompleteMenuNS.AutocompleteItem("<!-- attrib " & Attribute.Text & ": ... -->", 1, "Define " & Attribute.Text, "Define " & Attribute.Text, "Defines the " & Attribute.Text & " attribute for this page." & Environment.NewLine & Environment.NewLine & "Example: <!-- attrib " & Attribute.Text & ": ... -->"))
                    End If
                End If
                'items.Add(New AutocompleteMenuNS.AutocompleteItem(Attribute.Text))
            Next

            items.Add(New AutocompleteMenuNS.AutocompleteItem("<!-- attrib ...: ... -->", 5, "Define a new attribute", "Define a new attribute", "Adds an attribute definition." & Environment.NewLine & Environment.NewLine & "Example: <!-- attrib ...: ... -->"))
        End If

        If Not Me.Parent.Text.StartsWith("includes\") Then
            If Not Code.GetLineText(Code.Selection.FromLine).StartsWith("<!-- attrib") Then
                items.Add(New AutocompleteMenuNS.AutocompleteItem("Insert conditional...", 4, "Insert conditional", "Insert conditional", "Open the Insert Conditional dialog." & Environment.NewLine & "Conditionals allow you to output text if an attribute has a certain value."))
            End If
            items.Add(New AutocompleteMenuNS.AutocompleteItem("Build", 3, "Build for more options", "Build", "AutoSite can give you more suggestions when you build" & Environment.NewLine & "your site and populate the Attribute Map."))
        End If

        Autocomplete.SetAutocompleteItems(items)
    End Sub

    Private Sub Code_KeyDown(ByVal sender As System.Object, ByVal e As KeyEventArgs) Handles Code.KeyDown
        If e.Control Then
            prepMenu()
        End If
    End Sub

    Private Function rootCalc() As String
        ' Estimates [#root#] attribute output
        Dim toreturn = "Example: ../"

        If Not Me.Parent.Text.StartsWith("templates\") Then
            Dim rel As String = openFile.Replace(siteRoot & "\pages\", "").Replace(siteRoot & "\includes\", "").Replace(siteRoot & "\templates\", "")

            toreturn = ""

            For Each e In rel.Split("\")
                toreturn &= "../"
            Next

            toreturn = toreturn.Substring(3) ' clip off first ../

            toreturn = "Output: " & toreturn
        End If

        Return toreturn
    End Function

    Private Function pathCalc() As String
        ' Estimates [#path#] attribute output
        Dim toreturn = "Example: about/index.md"

        If Not Me.Parent.Text.StartsWith("templates\") Then
            Dim rel As String = openFile.Replace(siteRoot & "\pages\", "").Replace(siteRoot & "\includes\", "").Replace(siteRoot & "\templates\", "")

            toreturn = "Output: " & rel

            Return toreturn
        End If
        Return toreturn
    End Function

    Public Sub doQuickInsert() Handles QuickInsert.Click
        Code.Focus()
        prepMenu()
        Autocomplete.Show(Code, True)
    End Sub
End Class