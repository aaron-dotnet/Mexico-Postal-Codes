Option Strict On
Option Infer Off

Imports System.Collections.Generic
Imports System.Linq

Public Class PostalCodeStatistics

    Public ReadOnly Property TotalRecords As Integer
    Public ReadOnly Property UniqueStates As Integer
    Public ReadOnly Property UniqueMunicipalities As Integer
    Public ReadOnly Property UniqueSettlements As Integer
    Public ReadOnly Property TopStates As IReadOnlyList(Of KeyValuePair(Of String, Integer))
    Public ReadOnly Property TopSettlementTypes As IReadOnlyList(Of KeyValuePair(Of String, Integer))

    Public Shared Function Compute(ByVal postalCodes As IEnumerable(Of c_PostalCode),
                                   Optional ByVal topCount As Integer = 10) As PostalCodeStatistics

        If postalCodes Is Nothing Then
            Return New PostalCodeStatistics(0, 0, 0, 0,
                New List(Of KeyValuePair(Of String, Integer)),
                New List(Of KeyValuePair(Of String, Integer)))
        End If

        Dim materialised As List(Of c_PostalCode) = postalCodes.ToList()
        Dim statesSet As New HashSet(Of String)()
        Dim municipalitiesSet As New HashSet(Of String)()
        Dim settlementsSet As New HashSet(Of String)()

        For Each p As c_PostalCode In materialised
            If Not String.IsNullOrEmpty(p.Estado) Then statesSet.Add(p.Estado)
            If Not String.IsNullOrEmpty(p.Municipio) Then municipalitiesSet.Add(p.Municipio)
            If Not String.IsNullOrEmpty(p.Asentamiento) Then settlementsSet.Add(p.Asentamiento)
        Next

        Dim topStates As IReadOnlyList(Of KeyValuePair(Of String, Integer)) =
            materialised _
                .Where(Function(p) Not String.IsNullOrEmpty(p.Estado)) _
                .GroupBy(Function(p) p.Estado) _
                .Select(Function(g) New KeyValuePair(Of String, Integer)(g.Key, g.Count())) _
                .OrderByDescending(Function(x) x.Value) _
                .Take(topCount) _
                .ToList()

        Dim topTypes As IReadOnlyList(Of KeyValuePair(Of String, Integer)) =
            materialised _
                .Where(Function(p) Not String.IsNullOrEmpty(p.TipoAsentamiento)) _
                .GroupBy(Function(p) p.TipoAsentamiento) _
                .Select(Function(g) New KeyValuePair(Of String, Integer)(g.Key, g.Count())) _
                .OrderByDescending(Function(x) x.Value) _
                .Take(topCount) _
                .ToList()

        Return New PostalCodeStatistics(
            materialised.Count,
            statesSet.Count,
            municipalitiesSet.Count,
            settlementsSet.Count,
            topStates,
            topTypes)
    End Function

    Private Sub New(ByVal totalRecords As Integer,
                    ByVal uniqueStates As Integer,
                    ByVal uniqueMunicipalities As Integer,
                    ByVal uniqueSettlements As Integer,
                    ByVal topStates As IReadOnlyList(Of KeyValuePair(Of String, Integer)),
                    ByVal topSettlementTypes As IReadOnlyList(Of KeyValuePair(Of String, Integer)))
        Me.TotalRecords = totalRecords
        Me.UniqueStates = uniqueStates
        Me.UniqueMunicipalities = uniqueMunicipalities
        Me.UniqueSettlements = uniqueSettlements
        Me.TopStates = topStates
        Me.TopSettlementTypes = topSettlementTypes
    End Sub

End Class
