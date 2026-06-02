Option Strict On
Option Infer Off

Imports Microsoft.VisualBasic

Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Xml

Public NotInheritable Class PostalCodeExporter

    Private ReadOnly _logger As ILogger
    Private ReadOnly _workingDir As String

    Public Sub New()
        Me.New(Nothing, Nothing)
    End Sub

    Public Sub New(ByVal workingDirectory As String)
        Me.New(workingDirectory, Nothing)
    End Sub

    Public Sub New(ByVal workingDirectory As String, ByVal logger As ILogger)
        _workingDir = If(String.IsNullOrEmpty(workingDirectory), AppContext.WorkingDirectory, workingDirectory)
        _logger = If(logger, AppContext.Logger)
    End Sub

    Public Function BuildExportPath(ByVal extension As String) As String
        Dim exportDir As String = Path.Combine(_workingDir, "exports")
        Directory.CreateDirectory(exportDir)
        Dim fileName As String = "mexico_postal_codes_" & DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) & "." & extension
        Return Path.Combine(exportDir, fileName)
    End Function

    Public Sub ExportToJson(ByVal postalCodes As IEnumerable(Of c_PostalCode), ByVal filePath As String)
        If String.IsNullOrEmpty(filePath) Then
            Throw New ArgumentException("File path cannot be null or empty.", NameOf(filePath))
        End If
        Dim sb As New StringBuilder()
        sb.Append("[").Append(Environment.NewLine)
        Dim first As Boolean = True
        For Each p As c_PostalCode In postalCodes
            If Not first Then sb.Append(",").Append(Environment.NewLine)
            first = False
            sb.Append("  {").Append(Environment.NewLine)
            AppendJsonProperty(sb, "CodigoPostal", p.CodigoPostal, lastProperty:=False)
            AppendJsonProperty(sb, "Asentamiento", p.Asentamiento, lastProperty:=False)
            AppendJsonProperty(sb, "TipoAsentamiento", p.TipoAsentamiento, lastProperty:=False)
            AppendJsonProperty(sb, "Municipio", p.Municipio, lastProperty:=False)
            AppendJsonProperty(sb, "Estado", p.Estado, lastProperty:=False)
            AppendJsonProperty(sb, "Ciudad", p.Ciudad, lastProperty:=False)
            AppendJsonProperty(sb, "D_CP", p.D_CP, lastProperty:=False)
            AppendJsonProperty(sb, "c_Estado", p.c_Estado, lastProperty:=False)
            AppendJsonProperty(sb, "c_Oficina", p.c_Oficina, lastProperty:=False)
            AppendJsonProperty(sb, "c_CP", p.c_CP, lastProperty:=False)
            AppendJsonProperty(sb, "c_TipoAsentamiento", p.c_TipoAsentamiento, lastProperty:=False)
            AppendJsonProperty(sb, "c_Municipio", p.c_Municipio, lastProperty:=False)
            AppendJsonProperty(sb, "id_Asentamiento_cpcons", p.id_Asentamiento_cpcons, lastProperty:=False)
            AppendJsonProperty(sb, "d_zona", p.d_zona, lastProperty:=False)
            AppendJsonProperty(sb, "c_cve_ciudad", p.c_cve_ciudad, lastProperty:=True)
            sb.Append("  }")
        Next
        sb.Append(Environment.NewLine).Append("]")
        File.WriteAllText(filePath, sb.ToString(), New UTF8Encoding(False))
    End Sub

    Public Sub ExportToCsv(ByVal postalCodes As IEnumerable(Of c_PostalCode), ByVal filePath As String)
        If String.IsNullOrEmpty(filePath) Then
            Throw New ArgumentException("File path cannot be null or empty.", NameOf(filePath))
        End If
        Const header As String = "CodigoPostal,Asentamiento,TipoAsentamiento,Municipio,Estado,Ciudad,D_CP,c_Estado,c_Oficina,c_CP,c_TipoAsentamiento,c_Municipio,id_Asentamiento_cpcons,d_zona,c_cve_ciudad"
        Using writer As New StreamWriter(filePath, False, New UTF8Encoding(False))
            writer.WriteLine(header)
            For Each p As c_PostalCode In postalCodes
                Dim line As String =
                    $"{EscapeCsv(p.CodigoPostal)}," &
                    $"{EscapeCsv(p.Asentamiento)}," &
                    $"{EscapeCsv(p.TipoAsentamiento)}," &
                    $"{EscapeCsv(p.Municipio)}," &
                    $"{EscapeCsv(p.Estado)}," &
                    $"{EscapeCsv(p.Ciudad)}," &
                    $"{EscapeCsv(p.D_CP)}," &
                    $"{EscapeCsv(p.c_Estado)}," &
                    $"{EscapeCsv(p.c_Oficina)}," &
                    $"{EscapeCsv(p.c_CP)}," &
                    $"{EscapeCsv(p.c_TipoAsentamiento)}," &
                    $"{EscapeCsv(p.c_Municipio)}," &
                    $"{EscapeCsv(p.id_Asentamiento_cpcons)}," &
                    $"{EscapeCsv(p.d_zona)}," &
                    $"{EscapeCsv(p.c_cve_ciudad)}"
                writer.WriteLine(line)
            Next
        End Using
    End Sub

    Public Sub ExportToXml(ByVal postalCodes As IEnumerable(Of c_PostalCode), ByVal filePath As String)
        If String.IsNullOrEmpty(filePath) Then
            Throw New ArgumentException("File path cannot be null or empty.", NameOf(filePath))
        End If
        Dim settings As New XmlWriterSettings() With {
            .Indent = True,
            .Encoding = New UTF8Encoding(False),
            .NewLineHandling = NewLineHandling.Replace
        }
        Using writer As XmlWriter = XmlWriter.Create(filePath, settings)
            writer.WriteStartDocument()
            writer.WriteStartElement("PostalCodes")
            For Each p As c_PostalCode In postalCodes
                writer.WriteStartElement("PostalCode")
                writer.WriteAttributeString("Code", p.CodigoPostal)
                writer.WriteAttributeString("Settlement", p.Asentamiento)
                writer.WriteAttributeString("Type", p.TipoAsentamiento)
                writer.WriteAttributeString("Municipio", p.Municipio)
                writer.WriteAttributeString("State", p.Estado)
                writer.WriteAttributeString("City", p.Ciudad)
                writer.WriteAttributeString("D_CP", p.D_CP)
                writer.WriteAttributeString("c_Estado", p.c_Estado)
                writer.WriteAttributeString("c_Oficina", p.c_Oficina)
                writer.WriteAttributeString("c_CP", p.c_CP)
                writer.WriteAttributeString("c_TipoAsentamiento", p.c_TipoAsentamiento)
                writer.WriteAttributeString("c_Municipio", p.c_Municipio)
                writer.WriteAttributeString("id_Asentamiento_cpcons", p.id_Asentamiento_cpcons)
                writer.WriteAttributeString("d_zona", p.d_zona)
                writer.WriteAttributeString("c_cve_ciudad", p.c_cve_ciudad)
                writer.WriteEndElement()
            Next
            writer.WriteEndElement()
            writer.WriteEndDocument()
        End Using
    End Sub

    Private Shared Sub AppendJsonProperty(ByVal sb As StringBuilder,
                                          ByVal name As String,
                                          ByVal value As String,
                                          ByVal lastProperty As Boolean)
        sb.Append("    """).Append(name).Append(""": """)
        If value IsNot Nothing Then
            sb.Append(EscapeJson(value))
        End If
        sb.Append("""")
        If Not lastProperty Then sb.Append(",")
        sb.Append(Environment.NewLine)
    End Sub

    Private Shared Function EscapeJson(ByVal value As String) As String
        Dim sb As New StringBuilder(value.Length)
        For Each c As Char In value
            Select Case c
                Case """"c : sb.Append("\""")
                Case "\"c : sb.Append("\\")
                Case CChar(vbBack) : sb.Append("\b")
                Case CChar(vbFormFeed) : sb.Append("\f")
                Case CChar(vbLf) : sb.Append("\n")
                Case CChar(vbCr) : sb.Append("\r")
                Case CChar(vbTab) : sb.Append("\t")
                Case Else
                    If AscW(c) < &H20 Then
                        sb.AppendFormat(CultureInfo.InvariantCulture, "\u{0:X4}", AscW(c))
                    Else
                        sb.Append(c)
                    End If
            End Select
        Next
        Return sb.ToString()
    End Function

    Private Shared Function EscapeCsv(ByVal value As String) As String
        If String.IsNullOrEmpty(value) Then Return String.Empty
        If value.Contains(","c) OrElse value.Contains(""""c) OrElse value.Contains(Environment.NewLine) Then
            Return $"""{value.Replace("""", """""")}"""
        End If
        Return value
    End Function

End Class
