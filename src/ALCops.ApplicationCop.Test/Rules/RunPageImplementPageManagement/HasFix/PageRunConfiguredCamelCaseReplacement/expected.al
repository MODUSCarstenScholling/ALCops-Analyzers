codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        SalesHeader: Record "Sales Header";
        pageMgt: Codeunit "Page Mgt";
    begin
        pageMgt.RunPage(SalesHeader);
    end;
}

page 50100 MyPage { }
table 36 "Sales Header"
{
    fields { field(1; MyField; Integer) { } }
}