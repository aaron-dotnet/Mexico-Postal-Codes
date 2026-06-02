Option Strict On
Option Infer Off

Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Public NotInheritable Class PostalCodeParser

    Private Const FieldSeparator As Char = "|"c
    Public Shared Function Parse(ByVal filePath As String,
                                 Optional ByVal logger As ILogger = Nothing) As List(Of c_PostalCode)

        Dim result As New List(Of c_PostalCode)()
        Dim effectiveLogger As ILogger = If(logger, AppContext.Logger)

        If String.IsNullOrEmpty(filePath) Then
            effectiveLogger.Log("PostalCodeParser.Parse called with null/empty filePath.", LogLevel.Warning)
            Return result
        End If
        If Not File.Exists(filePath) Then
            effectiveLogger.Log($"Postal code file not found: {filePath}", LogLevel.Warning)
            Return result
        End If

        Try
            Using reader As New StreamReader(filePath, Encoding.GetEncoding("ISO-8859-1"))
                ' Skip first two lines (metadata and headers)
                Dim header1 As String = reader.ReadLine()
                Dim header2 As String = reader.ReadLine()
                If header1 Is Nothing OrElse header2 Is Nothing Then
                    Return result
                End If

                While Not reader.EndOfStream
                    Dim line As String = reader.ReadLine()
                    If String.IsNullOrWhiteSpace(line) Then Continue While

                    Dim fields As String() = line.Split(FieldSeparator)
                    If fields.Length < 6 Then Continue While

                    Dim postalCode As New c_PostalCode()
                    postalCode.CodigoPostal = fields(0)
                    postalCode.Asentamiento = fields(1)
                    postalCode.TipoAsentamiento = fields(2)
                    postalCode.Municipio = fields(3)
                    postalCode.Estado = fields(4)
                    postalCode.Ciudad = fields(5)

                    If fields.Length >= 15 Then
                        postalCode.D_CP = fields(6)
                        postalCode.c_Estado = fields(7)
                        postalCode.c_Oficina = fields(8)
                        postalCode.c_CP = fields(9)
                        postalCode.c_TipoAsentamiento = fields(10)
                        postalCode.c_Municipio = fields(11)
                        postalCode.id_Asentamiento_cpcons = fields(12)
                        postalCode.d_zona = fields(13)
                        postalCode.c_cve_ciudad = fields(14)
                    End If

                    result.Add(postalCode)
                End While
            End Using
        Catch ex As Exception
            effectiveLogger.Log($"Error parsing postal codes text file: {ex.Message}", LogLevel.[Error])
            Throw
        End Try

        Return result
    End Function

End Class
