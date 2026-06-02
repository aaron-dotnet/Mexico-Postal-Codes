Option Strict On
Option Infer Off

Imports System.Collections.Generic
Imports System.Linq

Public NotInheritable Class PostalCodeQuery

    Public Shared Function Search(ByVal postalCodes As IEnumerable(Of c_PostalCode), ByVal query As String) As List(Of c_PostalCode)
        If postalCodes Is Nothing Then Return New List(Of c_PostalCode)()

        Dim source As IEnumerable(Of c_PostalCode) = postalCodes

        If String.IsNullOrWhiteSpace(query) Then
            Return source.ToList()
        End If

        Dim trimmed As String = query.Trim()
        Return source.Where(Function(p) MatchesQuery(p, trimmed)).ToList()
    End Function

    Private Shared Function MatchesQuery(ByVal p As c_PostalCode, ByVal q As String) As Boolean
        Return ContainsIgnoreCase(p.CodigoPostal, q) OrElse
               ContainsIgnoreCase(p.Asentamiento, q) OrElse
               ContainsIgnoreCase(p.Municipio, q) OrElse
               ContainsIgnoreCase(p.Estado, q)
    End Function

    Private Shared Function ContainsIgnoreCase(ByVal source As String, ByVal value As String) As Boolean
        If String.IsNullOrEmpty(source) Then Return False
        Return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0
    End Function

End Class
