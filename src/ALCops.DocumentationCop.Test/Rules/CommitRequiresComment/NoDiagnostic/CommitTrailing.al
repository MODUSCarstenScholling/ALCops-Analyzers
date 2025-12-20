codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    begin
        [|Commit();|] // MyComment
    end;
}