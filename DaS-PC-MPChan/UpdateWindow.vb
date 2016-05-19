Imports System.Net
Imports System.IO
Imports System.IO.Compression
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices
Imports System.Threading

Public Class UpdateWindow
    Private WithEvents client As New WebClient()
    Private url As String
    Private ctSource As New CancellationTokenSource()

    Public WasSuccessful As Boolean = False
    Public NewAssembly As String = Nothing
    Public OldAssembly As String = Nothing
    Sub New(url)
        InitializeComponent()
        Me.url = url
    End Sub
    Private Async Sub OnShow(sender As Object, e As EventArgs) Handles MyBase.Shown
        Await doUpdate(ctSource.Token)
        If WasSuccessful Then
            Me.Close()
        End If
    End Sub
    Private Sub CancelButton_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.Close()
    End Sub
    Private Sub OnClose(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ctSource.Cancel()
    End Sub
    Private Sub setStatus(message As String)
        statusLabel.Text = message
    End Sub
    Private Sub setError(message As String, details As String)
        statusLabel.Text = message
        errorDetails.Text = " " & details
        errorDetails.Visible = True
        errorDetails.Size = progress.Size
        errorDetails.Location = progress.Location
        errorDetails.Visible = True
        progress.Visible = False
    End Sub
    Private Sub DownloadProgress(sender As WebClient, e As DownloadProgressChangedEventArgs) Handles client.DownloadProgressChanged
        progress.Value = e.ProgressPercentage
    End Sub
    Private Async Function doUpdate(ct As CancellationToken) As Task
        ct.ThrowIfCancellationRequested()
        setStatus("Downloading new version …")
        Dim compressedContents() As Byte
        Try
            ct.Register(AddressOf client.CancelAsync)
            compressedContents = Await client.DownloadDataTaskAsync(url)
        Catch ex As Exception
            setError("Download failed:", ex.Message)
            Return
        End Try

        ct.ThrowIfCancellationRequested()
        progress.Style = ProgressBarStyle.Marquee
        setStatus("Extracting …")
        Dim extractedContents() As Byte
        Try
            extractedContents = Await Task.Run(Function() extractFile(compressedContents))
        Catch ex As Exception
            setError("Extraction failed:", ex.Message)
            Return
        End Try

        ct.ThrowIfCancellationRequested()
        setStatus("Replacing executable with new version …")
        Try
            Dim assemblyPath = Application.ExecutablePath
            Dim tmpPath = Path.GetTempFileName()
            Using tmpFile = File.Open(tmpPath, FileMode.Create)
                tmpFile.Write(extractedContents, 0, extractedContents.Length)
            End Using
            'Generate path in same directory to make sure we have write access there
            Dim tmpAssemblyPath = assemblyPath & ".old"
            If File.Exists(tmpAssemblyPath) Then File.Delete(tmpAssemblyPath)
            File.Move(assemblyPath, tmpAssemblyPath)
            File.Move(tmpPath, assemblyPath)
            NewAssembly = assemblyPath
            OldAssembly = tmpAssemblyPath
        Catch ex As Exception
            setError("Replacing old version failed:", ex.Message)
            Return
        End Try

        WasSuccessful = True
    End Function
    Private Function extractFile(compressedContents() As Byte) As Byte()
        Dim ignoreExtensions() As String = {".txt", ".doc", ".docx", ".md"}
        Dim extractedContents() As Byte = Nothing
        Using compressedStream As New MemoryStream(compressedContents), _
                archive As New ZipArchive(compressedStream, ZipArchiveMode.Read)
            For Each entry As ZipArchiveEntry In archive.Entries
                Dim entryExtension = Path.GetExtension(entry.Name).ToLower()
                If ignoreExtensions.Contains(entryExtension) Then
                    Continue For
                End If
                'Apart from the ignored file types, the archive should contain
                'exactly one executable
                If extractedContents IsNot Nothing Or Not entryExtension = ".exe" Then
                    Throw New ApplicationException("Unknown structure, please update manually")
                End If
                extractedContents = New Byte(entry.Length) {}
                Using extractedStream = entry.Open()
                    extractedStream.Read(extractedContents, 0, entry.Length)
                End Using
            Next
        End Using
        If extractedContents Is Nothing Then
            Throw New ApplicationException("Unknown structure, please update manually")
        End If
        Return extractedContents
    End Function
End Class