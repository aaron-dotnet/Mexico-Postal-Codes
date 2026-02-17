Option Strict On
Option Infer Off

Imports System.IO.Compression
Imports System.IO

Module Program
    Property DesktopPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
    Public Structure s_Coordinates
        Public ReadOnly X_POS As Integer
        Public ReadOnly Y_POS As Integer
        Sub New(x As Integer, y As Integer)
            X_POS = x
            Y_POS = y
        End Sub
    End Structure
    Public Class c_PostalCode
        'pendiente
    End Class

    Sub Main()
        'GetPostalCodes().GetAwaiter().GetResult()
        'Dim txtFile As String = ExtractZip($"{DesktopPath}\CPdescargatxt.zip")
        ParseTxtFile($"{DesktopPath}\postal_codes\CPdescarga.txt")
    End Sub
    Public Function ParseTxtFile(filePath As String) As Boolean
        ' response headers | Content-Type:Text/ html; charset=iso-8859-1 (Latin-1)
        Dim lines As String() = File.ReadAllLines(filePath, Text.Encoding.Latin1)

        ' quitar las dos primeras líneas que no contienen datos relevantes
        For Each line As String In lines.Skip(2).ToArray()
            Dim fields As String() = line.Split("|"c)
            ' Aquí puedes procesar cada campo según tus necesidades
            Dim d_codigo As String = fields(0)
            Dim d_asenta As String = fields(1)
            Dim d_tipo_asenta As String = fields(2)
            Dim D_mnpio As String = fields(3)
            Dim d_estado As String = fields(4)
            Dim d_ciudad As String = fields(5)
            Dim d_CP As String = fields(6)
            Dim c_estado As String = fields(7)
            Dim c_oficina As String = fields(8)
            Dim c_CP As String = fields(9)
            Dim c_tipo_asenta As String = fields(10)
            Dim c_mnpio As String = fields(11)
            Dim id_asenta_cpcons As String = fields(12)
            Dim d_zona As String = fields(13)
            Dim c_cve_ciudad As String = fields(14)
            ' Procesa los campos como necesites, por ejemplo, guardarlos en una base de datos o mostrarlos en consola
            Console.WriteLine($"Código Postal: {d_codigo}, Asentamiento: {d_asenta}, Tipo de Asentamiento: {d_tipo_asenta}, Municipio: {D_mnpio}, Estado: {d_estado}, Ciudad: {d_ciudad}")
        Next

        Return True
    End Function
    Public Function ExtractZip(zipPath As String) As String
        Dim extractPath As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) & "\postal_codes\"
        If Not Directory.Exists(extractPath) Then
            Directory.CreateDirectory(extractPath)
        End If

        ZipFile.ExtractToDirectory(zipPath, extractPath)

        ' regresar la ruta del archivo extraído
        Dim extractedFile As String = Directory.GetFiles(extractPath)(0)
        Return extractedFile
    End Function
    Public Async Function GetPostalCodes() As Task
        Dim mainUrl As String = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"
        Dim host As String = "www.correosdemexico.gob.mx"

        Using scraper As New c_Scraper()
            scraper.Host = host
            scraper.Referer = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/Descarga.aspx"

            ' primera interacción
            Dim response As String = Await scraper.Get(mainUrl)
            Dim necessaryData As String = GetString(response,
                                                    startStr:="<input type=""hidden"" name=""__EVENTTARGET""",
                                                    endStr:="<nav class=""navbar",
                                                    firstCoincidence:=True)

            Dim postData As String = BuildPostData(necessaryData)

            scraper.Origin = "https://www.correosdemexico.gob.mx"
            scraper.Referer = "https://www.correosdemexico.gob.mx/SSLServicios/ConsultaCP/CodigoPostal_Exportar.aspx"

            Dim response2 As String = Await scraper.Post(mainUrl, postData)
            Stop
        End Using

        'GetString("", "", 1)
    End Function

    Public Function BuildPostData(htmlContent As String) As String
        Dim coor As s_Coordinates = GenerateCoordinates()
        Dim fileType As String = "txt"

        ' En este caso, la página solo responde con valores estos inputs:
        ' VIEWSTATE, VIEWSTATEGENERATOR y EVENTVALIDATION,
        ' pero se agregarón los demás campos ya que existen en el HTML y se mandan tal cual en el Post.

        Dim post As String =
            $"__EVENTTARGET={GetInputValue(htmlContent, "__EVENTTARGET")}&" &
            $"__EVENTARGUMENT={GetInputValue(htmlContent, "__EVENTARGUMENT")}&" &
            $"__LASTFOCUS={GetInputValue(htmlContent, "__LASTFOCUS")}&" &
            $"__VIEWSTATE={GetInputValue(htmlContent, "__VIEWSTATE")}&" &
            $"__VIEWSTATEGENERATOR={GetInputValue(htmlContent, "__VIEWSTATEGENERATOR")}&" &
            $"__EVENTVALIDATION={GetInputValue(htmlContent, "__EVENTVALIDATION")}&" &
            $"cboEdo=00&" &
            $"rblTipo={fileType}&" &
            $"btnDescarga.x={coor.X_POS}&" &
            $"btnDescarga.y={coor.Y_POS}"

        Return post
    End Function

    Public Function GetInputValue(fullString As String, id As String) As String
        Dim start As String = $"{id}"" value="""
        Dim [end] As String = """ />"
        Dim result As String = GetString(fullString, start, [end],
                                         excessAmount:=[end].Length,
                                         firstCoincidence:=True).
                                         Replace(start, String.Empty)
        If String.IsNullOrEmpty(result) Then
            Return String.Empty
        End If

        Return System.Net.WebUtility.UrlEncode(result)
    End Function


    Public Function GenerateCoordinates() As s_Coordinates
        ' # Cordenadas asociadas a un boton de descarga de la pagina
        '   que registra donde se hizo clic (dentro del boton) para mandarlo en el Post
        Dim generator As New System.Random()
        Dim Y_POS As Integer = generator.Next(2, 22)
        Dim X_POS As Integer = generator.Next(2, 72)

        Return New s_Coordinates(X_POS, Y_POS)
    End Function

    Private Function GetString(fullString As String, startStr As String, endStr As String,
                               Optional excessAmount As Integer = 0,
                               Optional firstCoincidence As Boolean = False) As String
        ' Función para filtrar contenido de una cadena.

        Dim startWord As Integer = fullString.IndexOf(startStr, StringComparison.OrdinalIgnoreCase)
        If startWord = -1 Then Return String.Empty

        Dim endWord As Integer
        If firstCoincidence Then
            endWord = fullString.IndexOf(endStr, fullString.IndexOf(startStr), StringComparison.OrdinalIgnoreCase)
        Else
            endWord = fullString.LastIndexOf(endStr, StringComparison.OrdinalIgnoreCase)
        End If

        Return fullString.AsSpan(startWord, endWord - startWord + endStr.Length - excessAmount).ToString()
    End Function
End Module