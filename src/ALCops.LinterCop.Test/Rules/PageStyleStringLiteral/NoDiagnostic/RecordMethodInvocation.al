codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.SetRange(MyField, [|'None'|]);
        MyTable.SetFilter(MyField, [|'Standard'|]);
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Text[100]) { }
    }
}
