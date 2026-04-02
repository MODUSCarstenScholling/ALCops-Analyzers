page 50100 MyPage
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
    begin
        MyTable.MyField := [|'Standard'|];
    end;
}

table 50100 MyTable
{
    fields
    {
        field(1; MyField; Text[100]) { }
    }
}
