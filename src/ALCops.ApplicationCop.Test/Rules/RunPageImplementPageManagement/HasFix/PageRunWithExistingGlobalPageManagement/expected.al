codeunit 50100 MyCodeunit
{
    var
        PageMgt: Codeunit "Page Management";

    procedure MyProcedure()
    var
        SalesHeader: Record "Sales Header";
    begin
        PageMgt.PageRun(SalesHeader);
    end;
}

page 50100 MyPage { }
table 36 "Sales Header"
{
    fields { field(1; MyField; Integer) { } }
}