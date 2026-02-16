Option Strict On
Option Infer Off

Imports System.Net
Imports System.Net.Http
Imports System.IO
Imports System.Text
Imports System.Net.Http.Headers


Public Class c_Scraper
    Implements IDisposable

    Private Property _httpClient As HttpClient
    Private Property _cookieContainer As New CookieContainer
    Public Property Host As String = String.Empty
    Public Property Referer As String = String.Empty

    Sub New()
        Dim handler As New HttpClientHandler() With {
            .AutomaticDecompression = DecompressionMethods.All,
            .CookieContainer = _cookieContainer,
            .UseCookies = True
        }
        _httpClient = New HttpClient(handler)
    End Sub

    Private Sub Headers()
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/146.0")
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5")
        _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd")
        _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive")
        If Not String.IsNullOrEmpty(Host) Then
            _httpClient.DefaultRequestHeaders.Host = Host
        End If
        If Not String.IsNullOrEmpty(Referer) Then
            _httpClient.DefaultRequestHeaders.Referrer = New Uri(Referer)
        End If
    End Sub

    Public Async Function [GET](url As String) As Task(Of String)

        Headers()
        Using response As HttpResponseMessage = Await _httpClient.GetAsync(url)

            If response.IsSuccessStatusCode Then
                Return Await response.Content.ReadAsStringAsync()
            End If
        End Using

        Return String.Empty
    End Function
    Public Async Function POST(url As String, content As String) As Task(Of String)
        ' Convenience overload that wraps a string into StringContent
        Dim httpContent As New StringContent(content, Encoding.UTF8, "application/x-www-form-urlencoded")
        Return Await InternalPost(url, httpContent)
    End Function

    Private Async Function InternalPost(url As String, content As HttpContent) As Task(Of String)
        Headers()
        Using response As HttpResponseMessage = Await _httpClient.PostAsync(url, content)

            If response.IsSuccessStatusCode Then
                Dim respContent As HttpContent = response.Content

                ' Detect Content-Disposition filename (attachment)
                Dim cd As ContentDispositionHeaderValue = respContent.Headers.ContentDisposition
                Dim contentType As String = If(respContent.Headers.ContentType?.MediaType, String.Empty)
                Dim isFile As Boolean = False
                Dim fileName As String = Nothing

                If cd IsNot Nothing AndAlso Not String.IsNullOrEmpty(cd.FileName) Then
                    isFile = True
                    fileName = cd.FileName.Trim(""""c)
                ElseIf Not String.IsNullOrEmpty(contentType) AndAlso
                        Not contentType.StartsWith("text", StringComparison.OrdinalIgnoreCase) AndAlso
                        Not contentType.Contains("json") AndAlso Not contentType.Contains("xml") AndAlso
                        Not contentType.Contains("html") Then
                    isFile = True
                End If

                If isFile Then
                    Dim bytes As Byte() = Await respContent.ReadAsByteArrayAsync()
                    If String.IsNullOrEmpty(fileName) Then
                        fileName = "download_" & DateTime.Now.ToString("yyyyMMddHHmmss")
                        Select Case contentType
                            Case "application/pdf" : fileName &= ".pdf"
                            Case "application/zip" : fileName &= ".zip"
                            Case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel"
                                fileName &= ".xlsx"
                            Case "image/png" : fileName &= ".png"
                            Case "image/jpeg" : fileName &= ".jpg"
                            Case Else
                                ' try to infer extension from subtype
                                If Not String.IsNullOrEmpty(contentType) AndAlso contentType.Contains("/"c) Then
                                    Dim parts As String() = contentType.Split("/"c)
                                    If parts.Length = 2 Then
                                        fileName &= "." & parts(1)
                                    End If
                                End If
                        End Select
                    End If

                    Dim tmp As String = Path.Combine(Path.GetTempPath(), fileName)
                    File.WriteAllBytes(tmp, bytes)
                    Return "FILE:" & tmp
                Else
                    Return Await respContent.ReadAsStringAsync()
                End If
            End If
        End Using

        Return String.Empty
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        _httpClient?.Dispose()
        GC.SuppressFinalize(Me)
    End Sub
End Class
