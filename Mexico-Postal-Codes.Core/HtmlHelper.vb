Option Strict On
Option Infer Off

Imports System.Net

Friend NotInheritable Class HtmlHelper
    Public Shared Function GetString(ByVal source As String,
                                     ByVal startIn As String, ByVal endTo As String,
                                     Optional ByVal excessAmount As Integer = 0) As String

        If String.IsNullOrEmpty(source) OrElse
            String.IsNullOrEmpty(startIn) OrElse
            String.IsNullOrEmpty(endTo) Then
            Return String.Empty
        End If

        Dim startIdx As Integer = source.IndexOf(startIn, StringComparison.OrdinalIgnoreCase)
        If startIdx = -1 Then Return String.Empty

        Dim searchStartForEnd As Integer = startIdx + startIn.Length
        Dim endIdx As Integer = source.IndexOf(endTo, searchStartForEnd, StringComparison.OrdinalIgnoreCase)
        If endIdx = -1 Then Return String.Empty

        Dim resultLength As Integer = (endIdx - startIdx) + endTo.Length - Math.Max(0, excessAmount)
        If resultLength <= 0 Then Return String.Empty

        If startIdx + resultLength > source.Length Then
            resultLength = source.Length - startIdx
        End If

        Return source.Substring(startIdx, resultLength)
    End Function

    Public Shared Function GetInputValue(ByVal fullString As String, ByVal id As String) As String
        If String.IsNullOrEmpty(fullString) OrElse String.IsNullOrEmpty(id) Then Return String.Empty

        Dim start As String = $"{id}"" value="""
        Dim [end] As String = """ />"
        Dim raw As String = GetString(fullString, start, [end], excessAmount:=[end].Length)
        If String.IsNullOrEmpty(raw) Then Return String.Empty

        raw = raw.Replace(start, String.Empty)
        Return WebUtility.UrlEncode(raw)
    End Function

    Public Shared Function BuildSepomexPostData(ByVal htmlContent As String,
                                                ByVal buttonCoordinates As Coordinates) As String

        Return $"__EVENTTARGET={GetInputValue(htmlContent, "__EVENTTARGET")}&" &
            $"__EVENTARGUMENT={GetInputValue(htmlContent, "__EVENTARGUMENT")}&" &
            $"__LASTFOCUS={GetInputValue(htmlContent, "__LASTFOCUS")}&" &
            $"__VIEWSTATE={GetInputValue(htmlContent, "__VIEWSTATE")}&" &
            $"__VIEWSTATEGENERATOR={GetInputValue(htmlContent, "__VIEWSTATEGENERATOR")}&" &
            $"__EVENTVALIDATION={GetInputValue(htmlContent, "__EVENTVALIDATION")}&" &
            $"cboEdo=00&" &
            $"rblTipo=txt&" &
            $"btnDescarga.x={buttonCoordinates.X}&" &
            $"btnDescarga.y={buttonCoordinates.Y}"
    End Function

    Public Shared Function GenerateButtonCoordinates(ByVal random As Random) As Coordinates
        If random Is Nothing Then Throw New ArgumentNullException(NameOf(random))
        Return New Coordinates(random)
    End Function
    Public Structure Coordinates
        '     _ _ _ _ _ _ _ _ _ 
        '   |          X        | 
        '   | Y    DESCARGAR    |
        '   | _ _ _ _ _ _ _ _ _ |
        Public Property X As UShort
        Public Property Y As UShort
        Public Sub New(random As Random)
            Me.X = Convert.ToUInt16(random.Next(2, 72))
            Me.Y = Convert.ToUInt16(random.Next(2, 22))
        End Sub
    End Structure
End Class
